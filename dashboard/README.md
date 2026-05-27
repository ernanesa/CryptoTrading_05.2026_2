# CryptoTrading Dashboard

React + TypeScript + Vite dashboard for the CryptoTrading API.

## Runtime Mode

The dashboard polls `GET /api/runtime/status` and shows a global `Runtime: <mode>` badge in the header. If the API is unavailable, the UI keeps a seeded `Simulation` status so local dashboard smoke tests and standalone development remain safe by default.

Supported API modes:

- `Simulation`
- `Paper`
- `TestnetDryRun`
- `TestnetReal`
- `Offline`

## Local Commands

```bash
npm install
npm run dev
npm run build
```
