# VR Drawing 3D

VR Drawing 3D is a comprehensive virtual reality application developed as a diploma thesis to facilitate advanced psychological research and assessment. Designed in collaboration with psychology researchers at ELTE, the platform translates traditional bilateral and free-form drawing therapies into a fully tracked, immersive 3D space. By capturing real-time spatial data and behavioral metrics, the system provides clinicians with unprecedented insights into user interactions. Developed for Meta Quest headsets, the application bridges isolated therapeutic sandbox environments with remote, peer-to-peer multiplayer collaboration.

## System Architecture

The project utilizes a decoupled client-server architecture, ensuring high-performance local VR rendering while safely persisting complex spatial telemetry to a centralized database.

### The VR Client (Unity)
*   **Engine & Hardware:** Built in Unity for Meta Quest devices (Quest 2, 3, Pro), utilizing the XR Interaction Toolkit (XRI 3.1.2) for robust input handling and global UI management.
*   **Networking:** Powered by FishNet and Steamworks.NET (FishySteamworks). 
*   **Always-Host Bootstrap Model:** Upon launch, the application instantly initializes a local sandbox server. When a user creates or joins a shared session via a 6-character shortcode, the network gracefully transitions to a Peer-to-Peer (P2P) remote client, utilizing Steam Datagram Relay for seamless NAT punch-through.

### The Backend (Python FastAPI)
*While initially conceptualized alongside Flask, the backend is actively built on high-performance FastAPI to handle concurrent 3D coordinate streaming.*
*   **SQLite (Relational Metadata):** Manages user authentication, session parameters, and active room routing logic.
*   **MongoDB (Spatial Data):** Stores the heavy volumetric data, including chronological `LineRenderers` points, color properties, and continuous spatial tracking of the user's head and hands.

## Data Analysis & Evaluation (Future Scope)

To fully realize the clinical goals of the project, the ecosystem will be expanded to include a dedicated data analysis suite:
*   **External Web Application:** A standalone portal allowing psychologists to filter, visualize, and export session telemetry, bolstered by automated semantic evaluation of the 3D drawings.
*   **In-Game Dashboard:** Integration of an in-game WebView interface, enabling the supervising clinician to monitor real-time behavioral metrics and access session reports directly within the VR environment.
