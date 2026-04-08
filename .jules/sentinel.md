## 2024-04-08 - [HIGH] Fix authorization bypass in UpdateUser
**Vulnerability:** IDOR in API on `/api/users/{id}` (PATCH) endpoint. It allowed any unauthenticated or authenticated user to change any other user's profile information by guessing their ID.
**Learning:** Found a pattern where Controller actions implicitly rely on token processing logic missing from standard `[Authorize]` attributes in this repo. Manual token extraction and user validation are required for proper authorization.
**Prevention:** Implement endpoint-specific user checks ensuring `currentUser.Id == id` before applying updates. Standardize `[Authorize]` usage across the ASP.NET Core project in future updates.
