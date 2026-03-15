namespace Application;

/// <summary>
/// Well-known audit action strings. Keep values stable — they are persisted to the database.
/// </summary>
public static class AuditActions
{
    public const string Register               = "Register";
    public const string EmailConfirmed         = "EmailConfirmed";
    public const string LoginSuccess           = "LoginSuccess";
    public const string LoginFailed            = "LoginFailed";
    public const string Logout                 = "Logout";
    public const string PasswordChanged        = "PasswordChanged";
    public const string PasswordResetRequested = "PasswordResetRequested";
    public const string PasswordReset          = "PasswordReset";
    public const string ProfileUpdated         = "ProfileUpdated";
    public const string SessionRevoked         = "SessionRevoked";
    public const string AllSessionsRevoked     = "AllSessionsRevoked";
    public const string PasskeyAdded           = "PasskeyAdded";
    public const string PasskeyRemoved         = "PasskeyRemoved";
    public const string RoleAssigned           = "RoleAssigned";
    public const string RoleRemoved            = "RoleRemoved";
    public const string TwoFactorEnabled       = "TwoFactorEnabled";
    public const string TwoFactorDisabled      = "TwoFactorDisabled";

    // Account self-service (GDPR)
    public const string AccountDeleted         = "AccountDeleted";
    public const string AccountDataExported    = "AccountDataExported";

    // Background jobs
    public const string JobTriggeredManually   = "JobTriggeredManually";
    public const string JobCompleted           = "JobCompleted";
    public const string JobFailed              = "JobFailed";
}
