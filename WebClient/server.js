const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

const rooms = new Map();
const pendingMessages = new Map();

function generateRoomCode() {
    let code;
    do {
        code = Math.floor(1000 + Math.random() * 9000).toString();
    } while (rooms.has(code));
    return code;
}

wss.on('connection', (ws, req) => {
    const path = req.url.split('/').filter(Boolean);

    // --- HOST UNITY ---
    if (path[0] === 'host') {
        const roomCode = generateRoomCode();
        rooms.set(roomCode, { host: ws, clients: new Map(), clientsUnity: new Map() });
        ws.send(`ROOM_CODE|${roomCode}`); // Envía el código a Unity
        console.log(`Sala ${roomCode} creada por host`);
    }

    // --- CLIENTE HTML ---
    else if (path[0] === 'client') {
        const roomCode = path[1];
        const playerName = path[2];

        if (!rooms.has(roomCode)) {
            ws.send("JOIN_FAIL");
            ws.close();
            return;
        }

        const room = rooms.get(roomCode);
        room.clients.set(playerName, ws);
        console.log(`Cliente HTML ${playerName} registrado en la sala ${roomCode}`);
        sendWithRetry(room, playerName, "JOIN_SUCCESS");

        // Notificar al host sobre nuevo jugador
        if (room.host && room.host.readyState === WebSocket.OPEN) {
            room.host.send(`NEW_PLAYER|${playerName}`);
        }

        ws.on('message', (message) => {
            const messageStr = message.toString();
            console.log(`[client] Mensaje de ${playerName}: ${messageStr}`);
            if (messageStr.startsWith('CONFIRM|')) {
                const messageId = messageStr.split('|')[1];
                pendingMessages.delete(messageId);
                sendToUnity(room, playerName, `CONFIRM|${messageId}`);
                return;
            }

            if (messageStr === "PONG") {
                console.log(`Recibido PONG de ${playerName}`);
                return;
            }

            if (messageStr.startsWith('CHARACTER_SELECT')) {
                const character = messageStr.split('|')[1];
                // Enviar mensaje formateado a Unity
                const unityMessage = `CHARACTER_SELECT|${playerName}|${character}`;
                sendToUnity(room, playerName, unityMessage);
            }

            try {
                if (messageStr === 'THROW_DICE') {
                    sendToUnity(room, playerName, messageStr);
                }
                else if (messageStr.startsWith('VOTE')) {
                    sendToUnity(room, playerName, messageStr);
                }
                else if (messageStr.startsWith('MINIGAME2')) {
                    sendToUnity(room, playerName, messageStr);
                } 
                else if (messageStr.endsWith('WELL')) {
                    sendToUnity(room, playerName, messageStr);
                }
            } 
            catch (e) {
                console.error('Error procesando mensaje:', e);
            }
        });

        ws.on('close', () => {
            room.clients.delete(playerName);
            if (room.host && room.host.readyState === WebSocket.OPEN) {
                room.host.send(`PLAYER_LEFT|${playerName}`);
            }
            console.log(`${playerName} (HTML) abandonó la sala`);
        });
    }

    // --- CLIENTE UNITY ---
    else if (path[0] === 'client-unity') {
        const roomCode = path[1];
        const playerName = path[2];

        if (!rooms.has(roomCode)) {
            ws.send("JOIN_FAIL");
            ws.close();
            return;
        }

        const room = rooms.get(roomCode);
        if (!room.clientsUnity) room.clientsUnity = new Map();
        room.clientsUnity.set(playerName, ws);

        console.log(`Cliente Unity ${playerName} registrado en la sala ${roomCode}`);
        sendWithRetry(room, playerName, "JOIN_SUCCESS");

        ws.on('message', (message) => {
            const messageStr = message.toString();
            console.log(`[client-unity] Mensaje de ${playerName}: ${messageStr}`);
            
            // Si el mensaje incluye ID, primero confirmamos la recepción
            if (messageStr.includes('|ID:')) {
                const [actualMessage, messageId] = messageStr.split('|ID:');
                // Confirmar recepción a Unity
                ws.send(`CONFIRM|${messageId}`);
                
                // Procesar el mensaje
                const splittedMsg = actualMessage.split('|');
                try {
                    const targetPlayer = splittedMsg[0];
                    const clientMessage = splittedMsg.slice(1).join('|');
                    // Enviar al cliente HTML con nuevo ID para tracking
                    sendWithRetry(room, targetPlayer, clientMessage);
                } catch (e) {
                    console.error('Error procesando mensaje:', e);
                }
            }
            // Si es una confirmación
            else if (messageStr.startsWith('CONFIRM|')) {
                const messageId = messageStr.split('|')[1];
                pendingMessages.delete(messageId);
                return;
            }
        });

        ws.on('close', () => {
            room.clientsUnity.delete(playerName);
            console.log(`Cliente Unity ${playerName} abandonó la sala`);
        });
    }
});

// --- ENVÍO DE MENSAJES A CLIENTE HTML ---
function sendToClient(room, playerName, message, messageId = null) {
    const client = room.clients.get(playerName);
    if (client && client.readyState === WebSocket.OPEN) {
        const msgToSend = messageId ? `MSGID|${messageId}|${message}` : message;
        client.send(msgToSend);
        return true;
    } 
    else {
        console.log(`No se pudo enviar el mensaje a ${playerName}`);
        return false;
    }
}

function sendToUnity(room, playerName, message, messageId = null) {
    const unityClient = room.clientsUnity.get(playerName);
    if (unityClient && unityClient.readyState === WebSocket.OPEN) {
        const msgToSend = messageId ? `MSGID|${messageId}|${message}` : message;
        unityClient.send(msgToSend);
        return true;
    } else {
        console.log(`No se pudo enviar el mensaje a ${playerName}`);
        return false;
    }
}

//Funciones para confirmar los mensajes
function sendWithRetry(room, playerName, message, retries = 3) {
    const messageId = Math.random().toString(36).substr(2, 9);
    if (sendToClient(room, playerName, message, messageId)) {
        pendingMessages.set(messageId, { room, playerName, message, retries });
        setTimeout(() => checkConfirmation(messageId), 2000); // 2 segundos de espera
    }
}

function checkConfirmation(messageId) {
    const pending = pendingMessages.get(messageId);
    if (pending) {
        if (pending.retries > 0) {
            console.log(`Reintentando mensaje a ${pending.playerName}, intentos restantes: ${pending.retries}`);
            sendToClient(pending.room, pending.playerName, pending.message, messageId);
            pending.retries--;
            setTimeout(() => checkConfirmation(messageId), 2000);
        } else {
            console.log(`No se pudo entregar el mensaje a ${pending.playerName} tras varios intentos.`);
            pendingMessages.delete(messageId);
        }
    }
}

console.log("Servidor WebSocket iniciado en ws://192.168.0.19:8080");