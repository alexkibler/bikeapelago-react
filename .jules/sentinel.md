## 2025-04-09 - [Hardcoded Secrets in API Configuration]
**Vulnerability:** Found hardcoded fallback for JWT IssuerSigningKey and Admin password in Program.cs that would be applied in all environments, potentially allowing compromise in production if configuration is missed.
**Learning:** Avoid defaulting to weak fallback secrets in non-development environments to prevent silent security degradation.
**Prevention:** Explicitly check environment (e.g. `builder.Environment.IsDevelopment()`) and throw an explicit exception or safely degrade (skip seeding) in production when configuration variables are missing.
