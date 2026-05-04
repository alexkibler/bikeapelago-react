## 2024-05-24 - [Fix plaintext password logging in ErrorLoggingMiddleware]
**Vulnerability:** Plaintext passwords submitted during login or registration were being logged in the database by `ErrorLoggingMiddleware` if an exception or HTTP error occurred.
**Learning:** Raw request bodies logged generically across all endpoints can unintentionally capture and persist sensitive credentials.
**Prevention:** Implement input scrubbing (e.g., using regular expressions) to redact sensitive fields like "password" before persisting raw request payloads to logs.
