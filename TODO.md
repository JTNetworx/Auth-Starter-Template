# Auth Starter Template — Roadmap

Legend: ✅ Done · 🔄 In Progress · ⬜ Pending

---

## ✅ Completed

- JWT authentication (access + rotating refresh tokens)
- User registration with email confirmation
- Login (password-based) with email confirmed guard
- Forgot password / reset password flows
- Change password
- Refresh token rotation with server-side revocation on logout
- Rate limiting on auth endpoints
- Account lockout after failed attempts
- User profile (edit personal info, address, date of birth, phone)
- Profile photo upload — swappable Database / S3 storage
- Country selector on profile — wired to `/api/countries`
- Passkey (WebAuthn) — register, login, list, remove
- Email confirmed guard on login page (targeted warning + resend hint)
- MudBlazor UI — custom theme, dark/light mode persisted to localStorage
- Responsive drawer layout with AppBar, NotificationBell, AuthHeader avatar menu
- Clean Architecture — Domain / Application / Infrastructure / SharedKernel / Api / WebClient

---

## Phase 1 — Session Management ⬜

> Users can see all active sessions and revoke any of them individually.

- [ ] **Backend** — Add `DeviceInfo`, `IpAddress`, `UserAgent`, `LastUsedUtc` columns to `RefreshToken`
- [ ] **Backend** — EF migration for new columns
- [ ] **Backend** — Capture metadata at login, passkey login, and token refresh (from `HttpContext`)
- [ ] **Backend** — `GetActiveTokensForUserAsync(userId)` + `GetTokenByIdAsync(id, userId)` in `ITokenRepository`
- [ ] **Backend** — `GET /api/users/me/sessions` — list active sessions (exclude current? mark current?) - Mark as current - Allow revoke current session
- [ ] **Backend** — `DELETE /api/users/me/sessions/{id}` — revoke one session
- [ ] **Backend** — `DELETE /api/users/me/sessions` — revoke all sessions except current
- [ ] **Frontend** — `SessionDto` + service methods in `IUserApiService`
- [ ] **Frontend** — Sessions tab on Profile page — list, revoke individual, "Sign out all other devices"

---

## Phase 2 — Roles (Admin + User) ⬜

> Two built-in roles seeded at startup. Developer extends from there.

- [ ] **Backend** — Seed `Admin` and `User` roles at startup via `RoleManager<IdentityRole>`
- [ ] **Backend** — Assign `User` role to every new registrant in `AuthService.RegisterAsync`
- [ ] **Backend** — `[Authorize(Roles = "Admin")]` guard on all admin endpoints
- [ ] **Backend** — `GET /api/users/me` returns current user's roles in JWT claims
- [ ] **Frontend** — Role claim parsed from JWT in `JwtAuthenticationStateProvider`
- [ ] **Frontend** — `[Authorize(Roles = "Admin")]` on admin Razor pages
- [ ] **Frontend** — Admin nav section in `NavMenu.razor` (visible only to Admin role)

---

## Phase 3 — Admin Panel ⬜

> Admin users can manage users, sessions, roles, and view logs.

- [ ] **Backend** — `GET /api/admin/users` — paginated user list (id, name, email, roles, status, lastLogin)
- [ ] **Backend** — `GET /api/admin/users/{id}` — full user details including active sessions
- [ ] **Backend** — `DELETE /api/admin/users/{id}/sessions` — revoke all sessions for a user
- [ ] **Backend** — `DELETE /api/admin/users/{id}/sessions/{sessionId}` — revoke one session for a user
- [ ] **Backend** — `POST /api/admin/users/{id}/roles` — assign role
- [ ] **Backend** — `DELETE /api/admin/users/{id}/roles/{role}` — remove role
- [ ] **Backend** — `GET /api/admin/logs` — paginated audit log (filterable by user/action/date)
- [ ] **Frontend** — `/admin/users` — users list page with search + pagination
- [ ] **Frontend** — `/admin/users/{id}` — user detail page (profile, sessions, roles, audit history)
- [ ] **Frontend** — Role assignment UI (chip selector)
- [ ] **Frontend** — Audit log viewer with filters

---

## Phase 4 — Auditability ⬜

> Every significant action is recorded with who, what, when, and from where.

- [ ] **Domain** — `AuditLog` entity: `Id`, `UserId`, `Action`, `EntityType`, `EntityId`, `IpAddress`, `UserAgent`, `Timestamp`, `Details` (JSON)
- [ ] **Infrastructure** — `AuditLogConfiguration` (EF config, no soft-delete cascade)
- [ ] **Infrastructure** — `IAuditLogRepository` + implementation
- [ ] **Application** — `IAuditLogService.RecordAsync(userId, action, ...)`
- [ ] **Capture** — Login success/failure, logout, password change, password reset, profile update, role assigned/removed, token revoked, passkey added/removed, account locked
- [ ] **Admin view** — See Phase 3 audit log viewer

---

## Phase 5 — Background Tasks (Quartz.NET) ⬜

> Scheduled jobs keep the database clean and handle deferred work.

- [ ] **Infrastructure** — Add `Quartz` + `Quartz.Extensions.Hosting` NuGet packages
- [ ] **Job** — `ExpiredTokenCleanupJob` — delete refresh tokens where `ExpiresUtc < now - RetentionDays` or (`RevokedAtUtc IS NOT NULL` AND `RevokedAtUtc < now - RetentionDays`)
- [ ] **Config** — `Quartz:TokenCleanup:CronSchedule` and `Quartz:TokenCleanup:RetentionDays` in appsettings
- [ ] **Job** — `ExpiredAuditLogArchiveJob` (Phase 4 dependent) — archive/purge old audit records per retention policy
- [ ] **Registration** — Wire jobs into `DependencyInjection.cs`

---

## Phase 6 — Two-Factor Authentication (TOTP) ⬜

> Time-based OTP via authenticator app (Google Authenticator, Authy, etc.). Identity already has the primitives.

- [ ] **Backend** — `GET /api/auth/2fa/setup` — generate TOTP secret + QR code URI
- [ ] **Backend** — `POST /api/auth/2fa/enable` — verify first TOTP code + enable 2FA
- [ ] **Backend** — `POST /api/auth/2fa/disable`
- [ ] **Backend** — Login flow: if 2FA enabled, return `requiresTwoFactor: true` instead of tokens; second step `POST /api/auth/2fa/verify`
- [ ] **Frontend** — 2FA setup flow on Profile Security tab (QR code display + verification input)
- [ ] **Frontend** — 2FA challenge step on Login page

---

## Phase 7 — GDPR / Account Self-Service ⬜

> Users can delete their account and export their data.

- [ ] **Backend** — `DELETE /api/users/me` — anonymize or hard-delete user + cascade
- [ ] **Backend** — `GET /api/users/me/export` — JSON export of all user data
- [ ] **Frontend** — "Delete my account" confirmation dialog on Profile
- [ ] **Frontend** — "Export my data" button on Profile

---

## Phase 8 — Developer Experience ⬜

> Makes the template easier to clone, configure, and extend.

- [ ] Unit tests — `AuthService`, `TokenService`, `UserProfileService` (xUnit + Moq)
- [ ] Integration tests — Auth flow end-to-end (WebApplicationFactory)
- [ ] `docker-compose.yml` — API + SQL Server for zero-config local dev
- [ ] `Dockerfile` for the API project
- [ ] GitHub Actions CI — build + test on push to `main` and `development`
- [ ] Swagger / OpenAPI — annotated with `[ProducesResponseType]` on all controllers
- [ ] Health check endpoint — `GET /health` (DB connectivity, disk)

---

## Nice-to-Have / Future Consideration ⬜

- [ ] Social login — OAuth2 (Google, Microsoft, GitHub) via `AddAuthentication().AddGoogle()`
- [ ] Email OTP — alternative to TOTP, send a 6-digit code via email
- [ ] "Remember me" extended refresh token lifetime option
- [ ] New device / new IP login email notification
- [ ] Account lockout email notification (you've been locked out, here's an unlock link)
- [ ] Localization / i18n skeleton
- [ ] SignalR hub — real-time notification delivery (replace in-memory `NotificationService`)
- [ ] API versioning (`/api/v1/...`)
- [ ] Configurable password policy in appsettings (min length, complexity requirements)
- [ ] Request/response logging middleware with correlation IDs
- [ ] Read-only "impersonate user" mode for admins (view as user, no writes)

---

## Notes

- Roles are **hardcoded** (`Admin`, `User`) — seeded at startup, not from appsettings. Developers extend by adding to the seed list in code.
- Quartz jobs run **in-process** (same host as API). For high-scale apps, extract to a separate worker service.
- Audit log `Details` column stores JSON — keeps the schema flat while allowing rich capture per action type.
- Session metadata (IP, UserAgent) is captured from `IHttpContextAccessor` — already registered in `Api/Program.cs`.
