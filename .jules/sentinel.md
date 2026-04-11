## 2025-04-11 - [Middleware Logging PII Exposure]
**Vulnerability:** Found `Authorization` headers and plaintext request bodies being logged to the database by `ErrorLoggingMiddleware.cs` on failed HTTP requests.
**Learning:** Middleware acting indiscriminately to trace errors can inadvertently leak sensitive secrets and passwords if filtering logic is not applied explicitly for sensitive headers and paths.
**Prevention:** Implement redaction mechanisms for `Authorization` headers and sensitive body paths (`login`, `register`, `auth`, `password`) systematically in all API logging middleware.
