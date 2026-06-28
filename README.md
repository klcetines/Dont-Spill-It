# 🍻 Don't Spill It! — A Different Party Game

**Don't Spill It** is a social party game in the spirit of *Mario Party* and *Jackbox*, with a twist of risk, chaos and drinking mechanics. The game runs on a shared screen (Unity) while each player joins and plays from their phone's browser — no controllers, no app installs, just a room code.

![Unity](https://img.shields.io/badge/Unity-C%23-black?logo=unity&logoColor=white)
![Node.js](https://img.shields.io/badge/Node.js-339933?logo=node.js&logoColor=white)
![WebSocket](https://img.shields.io/badge/Realtime-WebSocket-blue)

<!-- TODO: add a screenshot or short GIF (shared screen + a phone joining) here — it sells the concept instantly.
     ![Gameplay](docs/gameplay.gif) -->

---

## 🎯 Project summary

This project was my final-year undergraduate thesis (TFG) at the University of Girona (2025). The goal: explore innovative social mechanics, remove entry barriers (no gamepads, no downloads), and create a chaotic multiplayer experience for a group of friends sharing one screen.

## 🧩 Architecture

The interesting part of this project isn't a single program — it's getting three different runtimes to behave as one realtime system: a Unity host on the big screen, several phone browsers, and a Node.js server brokering between them.

```
        Phones (web clients)                 Unity host (shared screen)
        ────────────────────                 ──────────────────────────
        index.html + JS                      C# WebSocketManager
               │                                       │
               │  ws  /client/{room}/{name}            │  ws  /host  →  ROOM_CODE
               ▼                                       ▼
        ┌──────────────────────────────────────────────────────────────┐
        │              Node.js WebSocket server (ws, port 8080)          │
        │    • rooms keyed by a 4-digit code (collision-checked)         │
        │    • routes by connection role: host / client / client-unity   │
        │    • relays player actions  ⇄  host prompts & state            │
        │    • app-level reliability: every message gets an ID and is     │
        │      re-sent (up to 3×, every 2s) until the peer CONFIRMs       │
        └──────────────────────────────────────────────────────────────┘
```

A few design decisions worth calling out:

- **Rooms and roles.** Each connection identifies its role through the URL path (`/host`, `/client/{room}/{name}`, `/client-unity/{room}/{name}`). The Unity host opens a room and receives a generated `ROOM_CODE`; phones join that code and are linked to the host.
- **Message relay.** Players' phones send game actions (`THROW_DICE`, `VOTE`, `CHARACTER_SELECT`, minigame inputs); the server forwards them to the Unity host, and Unity's per-player messages are routed back to the right phone.
- **Reliability over an unreliable channel.** WebSocket delivery can be lost on flaky phone Wi-Fi, so the server implements its own acknowledgement layer: messages carry an ID, are tracked in a pending map, and are retried until the receiver sends `CONFIRM|{id}`. This keeps the game state consistent even when a phone briefly drops.
- **Static delivery.** A small Node HTTP server (port 8000) serves the phone web client, so players just open a URL — nothing to install.

## ⚙️ How a session works

1. The Unity host starts and connects as `/host`; the server creates a room and returns a 4-digit code, shown on the shared screen.
2. Each player opens the web client on their phone, enters the code and a name, and connects as `/client`.
3. The server validates the room, links the player to the host, and notifies the host (`NEW_PLAYER`).
4. From there, phone inputs flow up to Unity and game prompts flow back down — all through the server, with the ACK/retry layer keeping messages in sync.

## 🛠️ Tech stack

- **Game / host:** Unity (C#) — `WebSocketManager`, `WebSocketClient`, `RoomManager`
- **Server:** Node.js + [`ws`](https://github.com/websockets/ws) (realtime relay, port 8080)
- **Web client:** HTML5 + JavaScript (WebSocket), served by a small Node HTTP server (port 8000)

## ▶️ Getting started

You'll run two Node processes (the realtime server + the static web server) and the Unity host.

**1. Start the servers**
```bash
cd WebClient
npm install
node server.js        # WebSocket relay  → ws://<your-LAN-IP>:8080
node http-server.js   # serves the phone web client → http://<your-LAN-IP>:8000
```

**2. Point the clients at your machine**

The connection URL is a placeholder in two files — replace it with your computer's LAN IP and the WebSocket port (8080):
- `WebClient/index.html` → the `new WebSocket("ws://...")` line
- `Assets/Scripts/WebSocketManager.cs` → the `_mainSocket = new WebSocket("ws://...")` line

**3. Launch**
- Open the project in Unity and press Play — the host connects and shows the room code.
- On each phone (same network), open `http://<your-LAN-IP>:8000`, enter the code and a name, and play.

## 🔑 Key features

- 🎮 Played entirely from phones — no keyboard, gamepad or app install.
- 🧠 Social mini-games like *"Would You Rather"* and *"Find the Real Answer"*.
- 🧪 A dynamic board with risk–reward mechanics.
- 🤝 Built for couch play, drinking games and laughter.
- 🎨 Rubber-hose cartoon aesthetic for a satirical, retro look.

## License
<!-- TODO: add a LICENSE file (MIT is a common, permissive choice) and state it here. -->
