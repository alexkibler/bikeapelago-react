# Bikeapelago Game App (`@bikeapelago/bikepelago-app`)

Main player-facing React application.

## Commands

Use the root [README](../../../../README.md) for run, build, validation, and deployment commands.

## Environment

- **Browser dev:** leave `VITE_PUBLIC_API_URL` unset so same-origin `/api` calls go through the Vite proxy.
- **Native (iOS/Android) builds:** must be set to the full API URL. Relative paths don't work in a native WebView — there is no proxy.

## iOS Native Build

Prerequisites: Xcode, CocoaPods, and `xcode-select` pointing at Xcode rather than Command Line Tools.

Then open **`ios/App/App.xcworkspace`** in Xcode (not `.xcodeproj`), select the `App` scheme and your device, and hit ⌘R.

## Notes

- `build` and `dev` both run `build:apworld` before Vite.
- Unit/component tests use Vitest (`src/components/game/__tests__/`).

## Related Docs

- [Frontend README](../../README.md)
- [Root README](../../../../README.md)
- [Claude instructions](CLAUDE.md)
- [Gemini instructions](GEMINI.md)
- [Codex instructions](CODEX.md)
