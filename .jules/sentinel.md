## 2025-04-12 - Fix Hardcoded JWT Secret Fallback
**Vulnerability:** A hardcoded secret (`"your-secret-key-at-least-32-chars-long"`) was being used as a fallback for generating and validating JSON Web Tokens (JWT) if the environment variable was missing.
**Learning:** Even though `Program.cs` properly checked for the presence of `JWT_KEY` during application startup and threw an error, the `EfCoreUserRepository.cs` component duplicated the key retrieval logic and included an insecure fallback. This could lead to a scenario where, if validation was somehow bypassed or testing code loaded this repository directly without validation, the application would securely fall back to a known hardcoded key, exposing tokens to attackers.
**Prevention:** Centralize secret validation or ensure all components that retrieve sensitive configuration values throw an exception explicitly when the configuration is missing, rather than providing an insecure fallback.

## 2024-05-18 - Fix IDOR in SessionsController
**Vulnerability:** The `GetSession` and `UpdateSession` endpoints in `api/Controllers/SessionsController.cs` retrieved and updated game sessions based solely on the provided session ID without verifying that the session belonged to the authenticated user. This allowed any authenticated user to view or modify any other user's sessions (Insecure Direct Object Reference).
**Learning:** Even if an endpoint uses a randomized ID (like a UUID), access control checks must be explicitly enforced to ensure the resource owner matches the requester. The presence of authentication is not the same as authorization. Additionally, authentication context should always be extracted using standard framework middleware (like `[Authorize]` and `User.FindFirstValue`) rather than manually parsing HTTP headers within the controller.
**Prevention:** Always secure user-specific endpoints with the `[Authorize]` attribute, extract the authenticated user's ID via `User.FindFirstValue(ClaimTypes.NameIdentifier)`, and verify resource ownership (e.g., `if (session.UserId != userId) return Forbid();`) before allowing read or write operations.

## 2026-04-27 - Fix Exposed JWT in Error Logs
**Vulnerability:** The error logging middleware (`ErrorLoggingMiddleware.cs`) was recording the raw `Authorization` header directly into the database (`ApiLogs` table). This meant that whenever a client or server error occurred, the plaintext JWT was stored, exposing active user sessions to anyone with database access.
**Learning:** Logging entire HTTP headers without redaction often leads to the exposure of sensitive credentials, such as Bearer tokens, cookies, or API keys.
**Prevention:** Always sanitize or selectively redact sensitive HTTP headers (especially `Authorization` and `Cookie`) before logging them to persistent storage.

## 2024-05-18 - Fix Password Logging in ErrorLoggingMiddleware
**Vulnerability:** The error logging middleware (`ErrorLoggingMiddleware.cs`) was recording the raw request body directly into the database (`ApiLogs` table) and truncating it to 2000 characters. This meant that whenever a client or server error occurred during authentication or user update requests, sensitive fields like passwords could be logged in plaintext.
**Learning:** Logging entire HTTP request bodies without redaction often leads to the exposure of sensitive credentials, such as passwords, in the database. When redacting sensitive data, it must be done before the string is truncated, otherwise the truncation might sever regex boundaries (e.g., closing quotes), preventing the redaction from working properly.
**Prevention:** Always sanitize or selectively redact sensitive JSON fields (e.g., passwords) from request bodies before saving to persistent storage to prevent credential exposure.
