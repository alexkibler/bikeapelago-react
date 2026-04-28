
## 2024-05-20 - Hardcoded Authorization Bypass and IDOR Prevention
**Vulnerability:** Found a hardcoded backdoor checking for `userName != "testuser"` in `AnalyzeFitFile` within `SessionsController`, allowing `testuser` to bypass Insecure Direct Object Reference (IDOR) checks on any session.
**Learning:** Hardcoded developer backdoors left in production code are severe security risks, especially when they allow bypassing resource ownership checks on critical actions. Additionally, manual authorization checks are prone to errors and bypasses.
**Prevention:** Remove any hardcoded user checks. Always use centralized, verified helper methods like `GetAuthorizedSessionResultAsync` to enforce resource ownership and prevent IDOR vulnerabilities consistently across all endpoints.
