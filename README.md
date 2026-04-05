# 🛡️ Don't, Just Don't (Demo)

A high-tension **resource management + reaction game** built in Unity 6.

You play as the **commander of a regional defense grid**, responsible for protecting civilians under a constant missile threat.

One mistake can cost lives.

---

## 🎮 Game Overview

The demo operates on a **Sudden Death Loop**:

* You start with **5 lives**
* Every mistake costs a life
* Lose all lives → **Game Over**

The challenge is not complexity — it's **focus, speed, and decision-making under pressure**.

---

## 🔁 Core Gameplay Loop

### 1. 🚨 Detection

* A directional indicator appears (N / S / E / W)
* A specific sector on the map begins to **flicker**

### 2. ⚠️ Alert

* Drag the **Alert** icon
* Drop it onto the flickering sector **before impact**

### 3. 💥 Interception

* If alerted in time → missile is intercepted
* Sector enters **Smoke Screen state**

### 4. 🔓 Recovery

* Wait until smoke clears
* Drag **Release** icon to return civilians home

### 5. 🚑 Emergency (Failure case)

* If missile hits:

  * Sector becomes **Damaged**
  * Drag **Ambulance** immediately

---

## 📉 Failure Conditions (The “Don’t” List)

| Event                           | Penalty              |
| ------------------------------- | -------------------- |
| Missile Impact                  | -1 Life              |
| Early Release (during smoke)    | -1 Life              |
| Delayed Ambulance               | -1 Life every 10s    |
| Citizen Neglect (kept too long) | -1 Life every 10s    |
| False Alarms                    | Faster anger buildup |

---

## 🧠 Design Philosophy

* **Simple actions, high stakes**
* **Cognitive overload through timing + prioritization**
* Forces player to balance:

  * Speed vs accuracy
  * Safety vs efficiency
  * Attention across multiple sectors

---

## 🧱 Technical Architecture

### Sector System

Each sector is a standalone **GameObject**:

* `SectorHandler.cs`

  * Controls states: `Idle → Alerted → Smoked → Damaged`
* `MoodController.cs`

  * Handles citizen happiness / anger decay
* `Collider2D`

  * Detects drag-and-drop interactions

---

### 🖐️ Input System

* Raycast-based drag & drop
* UI elements (Alert / Release / Ambulance) are draggable
* On release → raycast checks which sector is targeted

---

## 📱 Mobile Setup

* Orientation: **Portrait**
* Designed for touch interactions
* UI-based interaction recommended for clarity and scalability

---

## 🧩 Game States (Recommended)

* `Idle`
* `Incoming Threat`
* `Alerted`
* `Intercepted (Smoke)`
* `Damaged`

---

## 🚀 Getting Started

1. Open project in **Unity 6.4.1**
2. Set platform to **Android / iOS**
3. Use:

   * Unity Remote OR
   * Device Simulator
4. Press Play

---

## 🔧 Future Improvements

* Difficulty scaling (faster missiles, more sectors)
* Sound design for urgency
* Combo / scoring system
* Visual feedback polish (screen shake, UI glow)
* Tutorial onboarding

---

## 🎯 Target Experience

The player should feel:

* Constant pressure
* Fear of making mistakes
* Satisfaction from saving sectors just in time

---

## 📌 Notes for Development

* Prioritize **responsiveness over visuals**
* Keep feedback immediate and clear
* Avoid overcomplicating the core loop

---

## 💡 Tagline

> "You don’t manage chaos. You survive it."

---

If you want, I can also:

* Turn this into a **GitHub-ready README with badges + visuals**
* Add **architecture diagrams**
* Or convert it into a **pitch deck for your game** 🚀
