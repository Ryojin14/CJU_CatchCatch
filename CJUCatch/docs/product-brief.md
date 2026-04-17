# Product Brief

## Working Name

CJUCatch

## Goal

Create an original desktop app for trusted friends that shows lightweight shared presence inside the same instance.

## Core Terms

- `Instance`: a shared space that people join
- `Public instance`: visible and joinable without a password
- `Private instance`: requires a password

## Non-Negotiables

- Security is the highest priority
- No reuse of the original app's code or artwork
- No collection of typed text outside of messages the user explicitly sends
- No collection of window titles, process names, clipboard contents, screenshots, or local files
- No public distribution for now

## MVP Features

- Desktop client with a clearly branded original UI
- Server-owned instance creation and join flow
- Public and private instances
- Password verification for private instances
- Presence sharing inside the same instance
- Minimal profile: display name, color/theme placeholder, simple activity state, and coarse position
- Text notice files included in the build output

## Data Allowed

- Random client session ID
- Optional display name chosen by the user
- Current instance ID
- Visibility mode
- Password only during private instance join or create
- Coarse avatar position and simple state
- Explicitly sent chat or emoji data

## Data Forbidden

- Raw keystrokes
- Exact typed contents outside explicit chat messages
- Window titles
- Running app list
- Screen capture
- Microphone or camera
- Clipboard
- Browser history

## Initial Build Order

1. Shared contracts and naming
2. Server instance lifecycle
3. Password hashing and validation
4. Desktop shell and settings
5. Real-time state sync
6. Hardening, logs, and packaging
