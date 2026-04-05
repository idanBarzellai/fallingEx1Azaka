# Don't, Just Don't (Demo)

**Don't, Just Don't** is a high-tension resource management game built in Unity 6. As the commander of a regional defense grid, you must manage civilian safety and emergency response under a constant missile threat. 

The demo operates on a **Sudden Death** loop: one major oversight leads to a loss of life. Lose all 5 lives, and the demo ends.

---

## Core Gameplay Loop

1.  **Detection:** A directional indicator (N, S, E, W) appears, and a specific map sector begins to **flicker**. 🚨
2.  **Alert:** Drag and drop the **Alert** icon onto the flickering sector before the missile impacts.
3.  **Interception:** If the alert is active, the missile is intercepted. A **Smoke Screen** 💨 covers the sector.
4.  **Recovery:** Once the smoke clears, drag the **Release** 🔓 icon to return citizens to their homes.
5.  **Emergency:** If a missile hits, drag the **Ambulance** 🚑 icon to the sector immediately to prevent further life loss.

---

## Failure Conditions (The "Don't" List)

| Event | Penalty |
| :--- | :--- |
| **Missile Impact** | -1 Life immediately. |
| **Early Release** | -1 Life if citizens are released while smoke is active. |
| **Delayed Ambulance** | -1 Life for every 10 seconds a hit sector is ignored. |
| **Citizen Neglect** | -1 Life for every 10 seconds citizens stay in shelters past the "Angry" threshold. |
| **False Alarms** | Increases the rate at which citizens become angry in that sector. |

---

## Technical Implementation (Unity 6.4.1)

### Sector Architecture
Each sector is a standalone GameObject with:
* **SectorHandler.cs**: Manages states (Idle, Alerted, Smoked, Damaged).
* **MoodController.cs**: Handles the "Smiley Face" UI logic and decay rates.
* **Trigger Collider 2D**: For detecting resource drag-and-drop events.

### Input System
The game uses a **Raycast-based Drag-and-Drop** system. Resources (Alert, Release, Ambulance) are UI elements that cast a ray to identify the `SectorHandler` beneath them upon release.


