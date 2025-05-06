const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

const rooms = new Map();

wss.on('connection', (ws, req) => {
    const path = req.url.split('/').filter(Boolean);

    // --- HOST UNITY ---
    if (path[0] === 'host') {
        const roomCode = path[1];
        rooms.set(roomCode, { host: ws, clients: new Map(), clientsUnity: new Map() });
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
        ws.send("JOIN_SUCCESS");

        // Notificar al host sobre nuevo jugador
        if (room.host && room.host.readyState === WebSocket.OPEN) {
            room.host.send(`NEW_PLAYER|${playerName}`);
        }

        ws.on('message', (message) => {
            const messageStr = message.toString();
            console.log(`[client] Mensaje de ${playerName}: ${messageStr}`);

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
                else {
                    console.log('Mensaje no reconocido:', messageStr);
                }
            } catch (e) {
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
        ws.send("JOIN_SUCCESS");

        ws.on('message', (message) => {
            const messageStr = message.toString();
            console.log(`[client-unity] Mensaje de ${playerName}: ${messageStr}`);

            try {
                const splittedMsg = messageStr.split('|');
                if (splittedMsg[0] === 'TO_CLIENT') {
                    // Mensaje de Unity para HTML
                    const targetPlayer = splittedMsg[1];
                    const clientMessage = splittedMsg.slice(2).join('|');
                    sendToClient(room, targetPlayer, clientMessage);
                } else if (splittedMsg[0] === 'TO_HOST') {
                    // Mensaje de Unity para el host (poco común, pero posible)
                    if (room.host && room.host.readyState === WebSocket.OPEN) {
                        room.host.send(messageStr);
                    }
                } else {
                    console.log('Mensaje no reconocido:', messageStr);
                }
            } catch (e) {
                console.error('Error procesando mensaje:', e);
            }
        });

        ws.on('close', () => {
            room.clientsUnity.delete(playerName);
            console.log(`Cliente Unity ${playerName} abandonó la sala`);
        });
    }
});

// --- ENVÍO DE MENSAJES A CLIENTE HTML ---
function sendToClient(room, playerName, message) {
    const client = room.clients.get(playerName);
    if (client && client.readyState === WebSocket.OPEN) {
        client.send(message);
    } else {
        console.log(`No se pudo enviar el mensaje a ${playerName}`);
    }
}

function sendToUnity(room, playerName, message) {
    const unityClient = room.clientsUnity.get(playerName);
    if (unityClient && unityClient.readyState === WebSocket.OPEN) {
        unityClient.send(message);
    } else {
        console.log(`No se pudo enviar el mensaje a ${playerName}`);
    }
}

console.log("Servidor WebSocket iniciado en ws://192.168.0.19:8080");