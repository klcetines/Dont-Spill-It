const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

wss.on('connection', (ws) => {
    console.log('Cliente conectado');
    ws.on('message', (message) => {
        // Reenvía todos los mensajes tal cual
        wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(message.toString());
            }
        });
    });
});

console.log("Servidor WebSocket iniciado en ws://localhost:8080");