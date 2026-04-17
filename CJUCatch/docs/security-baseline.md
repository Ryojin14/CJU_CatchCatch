# Security Baseline

## Collection Rules

- Collect the least amount of data needed for the feature
- Do not persist activity history by default
- Do not log chat bodies in production
- Do not store plaintext passwords

## Transport Rules

- Use HTTPS and WSS only
- Reject insecure endpoints in production configuration

## Identity Rules

- Use generated session IDs instead of machine identifiers
- Do not read OS account names for user identity

## Logging Rules

- Error logs must avoid personal content
- Instance join failures may be counted, but not stored with passwords
- Retain logs for as short a period as possible

## Server Rules

- Hash private instance passwords
- Add rate limits for create and join operations
- Validate payload sizes and text lengths
- Never trust client visibility or membership claims without server checks

## Client Rules

- Do not monitor key contents
- Surface a plain-language privacy notice in the UI
- Ship `NOTICE.txt`, `PRIVACY.txt`, and `WARNING.txt` with the build
