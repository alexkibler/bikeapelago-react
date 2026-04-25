# Bikeapelago Game App (`@bikeapelago/bikepelago-app`)

Main player-facing React application.

## Commands

From `frontend/`:

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run dev
pnpm --filter "@bikeapelago/bikepelago-app" run build
pnpm --filter "@bikeapelago/bikepelago-app" run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
```

## Environment

`.env` in this folder:

```env
VITE_PUBLIC_API_URL=https://bikeapelago.alexkibler.com
```

- **Browser dev:** leave unset — Vite's proxy forwards `/api` to `localhost:5054`.
- **Native (iOS/Android) builds:** must be set to the full API URL. Relative paths don't work in a native WebView — there is no proxy.

## iOS Native Build

Prerequisites: Xcode, CocoaPods (`gem install cocoapods`), `xcode-select` pointing at Xcode (not CLT):
```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
```

Build and deploy:
```bash
pnpm run build          # must be run with VITE_PUBLIC_API_URL set
npx cap sync ios        # copies dist/ into the Xcode project and runs pod install
```

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
