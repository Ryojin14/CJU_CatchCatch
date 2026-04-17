# CJUCatch

Security-first private desktop presence app for small groups.

## Principles

- New implementation only. The original `CatchCatch` folder is reference material and should not be copied into this app.
- Minimum data collection. No key contents, window titles, screenshots, clipboard, or process lists are sent to the server.
- Private by default mindset. Instances can be public or password-protected.
- Small-scale hosting. Designed for a low-cost self-owned server deployment for friends.

## Solution Layout

- `CJUCatch.Client.Desktop`: Windows desktop client
- `CJUCatch.Server`: ASP.NET Core server
- `CJUCatch.Shared`: shared DTOs and enums
- `docs`: planning and security notes
- `legal`: text files copied into the client build output
- `assets-temp`: temporary artwork and placeholders only

## First Milestone

1. Security baseline and scope freeze
2. Shared contracts
3. In-memory instance server
4. Desktop shell with clear privacy messaging
5. Public/private instance join flow
6. Live presence rendering
