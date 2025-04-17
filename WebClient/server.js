const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

const rooms = new Map();

wss.on('connection', (ws, req) => {
    const QUICKGAMEID = "QUICKGAMEID";
    const YOUR_TURN = "YOUR_TURN";

    const path = req.url.split('/').filter(Boolean);
    
    // Conexión del Host (Unity)
    if (path[0] === 'host') {
        const roomCode = path[1];
        rooms.set(roomCode, { host: ws, clients: new Map() });
        console.log(`Sala ${roomCode} creada por host`);
    }
    // Conexión de Cliente (HTML)
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
        ws.send("JOIN_SUCCESS");
        
        // Notificar al host sobre nuevo jugador
        if (room.host && room.host.readyState === WebSocket.OPEN) {
            room.host.send(`NEW_PLAYER|${playerName}`);
        }

        const connectionType = path[0]; 

        ws.on('message', (message) => {
            const messageStr = message.toString();
            console.log(`[${connectionType}] Mensaje de ${playerName}: ${messageStr}`);
        
            if (messageStr === "READY") return;
        
            try {
                const splittedMsg = messageStr.split('|');

                if (splittedMsg[0] === YOUR_TURN) {
                    ws.send("ASDASDASD")
                }
                else if (splittedMsg[0] === 'DICE_RESULT') {
                    const result = splittedMsg[1];
                    broadcastToClients(room, `DICE_RESULT|${result}`);
                }
                else if (splittedMsg[0] === QUICKGAMEID) {
                    const gameID = splittedMsg[1];
                    broadcastToClients(room, `GAME_TYPE|${gameID}`);
                }

                if (splittedMsg[0] === 'THROW_DICE' && room.host?.readyState === WebSocket.OPEN) {
                    room.host.send(`THROW_DICE|${playerName}`);
                }
                else if (splittedMsg[0] === 'VOTE' && room.host?.readyState === WebSocket.OPEN) {
                    room.host.send(`VOTE|${playerName}|${splittedMsg[1]}`);
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
            console.log(`${playerName} abandonó la sala`);
        });
    }
});

console.log("Servidor WebSocket iniciado en ws://localhost:8080");