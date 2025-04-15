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
        
        // Verificar nombre único
        /*if (room.clients.has(playerName)) {
            ws.send("ERROR|Nombre ya en uso");
            ws.close();
            return;
        }*/

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
                if (connectionType === 'host') {
                    const [command, ...args] = messageStr.split('|');
                    
                    if (command === 'YOUR_TURN') {
                        broadcastToClients(room, 'DICE_MOMENT');
                    }
                    else if (command === 'DICE_RESULT') {
                        broadcastToClients(room, `DICE_RESULT|${args[0]}`);
                    }
                }
                else if (connectionType === 'client') {
                    const [command, ...args] = messageStr.split('|');
                    
                    if (command === 'THROW_DICE' && room.host?.readyState === WebSocket.OPEN) {
                        room.host.send(`THROW_DICE|${playerName}`);
                    }
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