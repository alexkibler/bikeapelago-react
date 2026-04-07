# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: unlock_flow.spec.ts >> Verify .fit upload unlocks new nodes on the map
- Location: tests/e2e/unlock_flow.spec.ts:6:1

# Error details

```
Error: Channel closed
```

```
Error: locator.click: Target page, context or browser has been closed
Call log:
  - waiting for locator('button:has-text("Upload")').filter({ visible: true })

```

```
Error: browserContext.close: Target page, context or browser has been closed
```