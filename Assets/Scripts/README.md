# Missile Crisis – Unity Game

## TL;DR
- Defend multiple sectors from incoming missiles
- **Drag** the rightside tools to the correct place on time
- **Tap** on the missiles to intercept them in time (rememeber to alert the sector first)
- Use **Alert → Tap → Release / Ambulance** in the correct order
- Each sector is a state machine with consequences for mistakes
- Missiles spawn faster over time → increasing difficulty
- You lose lives for bad decisions or slow reactions
- Survive as long as possible and beat your high score

## Overview

**Missile Crisis** is a fast-paced, reaction-based strategy game where the player must manage multiple regions under incoming missile threats. The goal is to prevent disasters by correctly using tools (Alert, Release, Ambulance) at the right time, under increasing pressure.

The game combines:

* Real-time threat management
* State-driven sector logic
* Increasing difficulty over time
* UI-based interactions and feedback systems

---

## Core Gameplay Loop

1. A missile is assigned to a random **idle sector**
2. The sector enters **Incoming** state
3. Player must:

   * **Alert** the sector before impact
   * **Tap the missile** to intercept it
4. Depending on player actions:

   * Success → Smoke → Release → Crisis avoided
   * Mistakes → Ambulance / penalties / life loss
5. Difficulty increases as the game progresses

---

## Sector System

Each sector operates as a **state machine**:

### States

* `Idle` – Safe
* `Incoming` – Missile approaching
* `AlertedIncoming` – Player alerted in time
* `Smoked` – Intercepted missile created smoke
* `WaitingForRelease` – Ready for release
* `NeedsAmbulanceCheck` – Missed alert, intercepted
* `NeedsAmbulance` – Hit after alert
* `AmbulanceWorking` – Ambulance in progress
* `Lost` – Failure (game over condition)

### Key Rules

* Alert must be applied **before intercept**
* Release must be applied **after smoke clears**
* Ambulance must be applied **when required and on time**

---

## Player Tools

### 🚨 Alert

* Valid only during `Incoming`
* Enables safe interception

### 🟦 Release

* Valid only during `WaitingForRelease`
* Ends the event successfully

### 🚑 Ambulance

* Required after certain failures
* Must be applied within a time window

---

## Missile System

Each missile:

* Spawns from outside the screen
* Travels toward a sector
* Can be tapped to intercept
* Triggers different outcomes based on sector state

Components:

* `MissileUI` – Handles movement and tap input
* `MissileDirectionIndicator` – Off-screen tracking

---

## Difficulty System

The game includes a **dynamic difficulty ramp**:

* Early game → slow missile frequency
* Late game → rapid missile spawning
* Based on:

  * Time survived
  * Player performance (crises avoided)

This is handled in `GameManager` using interpolated spawn delays.

---

## UI & Feedback Systems

### Visual Feedback

* Sector color changes by state
* Flickering for incoming threats
* Smoke & explosion VFX
* Timer icons (alert / release / ambulance)

### Animations

* Breathing UI
* Vibrating alerts
* Countdown / fill timers
* Clear animation (✓ shape)

### Audio

Managed by `AudioManager`:

* Missile warnings
* Intercepts & impacts
* UI feedback (valid/invalid actions)
* Background music
* TV chatter system

---

## Life System

* Player starts with a fixed number of lives
* Lives are lost when:

  * Missing alert before intercept
  * Releasing too early
  * Delaying ambulance
  * Ignoring release
* Game ends when lives reach 0

---

## Game Flow

### Start

* Intro TV animation
* UI fades in
* Game loop begins

### During Game

* Missiles spawn at intervals
* Player reacts in real-time
* Difficulty ramps up

### Game Over

* Triggered on:

  * Sector loss
  * Lives reaching zero
* UI transitions to end state
* Player can reset

---

## Reset System

Reset fully clears:

* Active missiles
* Sector states and VFX
* Timers and coroutines
* UI state

Game restarts from initial conditions.

---

## Code Structure

### Core Systems

* `GameManager` – Main game loop, difficulty, lives
* `SectorHandler` – State machine per sector
* `MissileUI` – Missile movement & interaction

### Interaction

* `DraggableTool` – Tool drag system
* `SectorDropZone` – Handles tool application

### UI Systems

* `UIFade` – Fade transitions
* `UIAlarmVibrate` – Alert vibration
* `UIBreathing` – Pulse animations

### Utilities

* `AutoDestroyAfterSeconds`
* `AudioManager`

---

## Design Philosophy

* **Clarity first**: Each state has clear visual and logical feedback
* **Skill-based difficulty**: Game becomes harder as player improves
* **Tight feedback loop**: Immediate response to player actions
* **Controlled chaos**: Increasing pressure without randomness overload

---

## Future Improvements (Ideas)

* Multiple simultaneous missiles
* Sector prioritization mechanics
* Power-ups / special tools
* Difficulty tiers or levels
* Score-based progression system
* Save/load high scores & sessions

---

## Controls

* Tap missiles to intercept
* Drag tools onto sectors:

  * Alert
  * Release
  * Ambulance

---

## Summary

Missile Crisis is a **real-time decision-making game** where:

* Timing is critical
* Mistakes are punished
* Pressure increases continuously

The player must balance awareness, speed, and correct decision-making to survive as long as possible.

---
