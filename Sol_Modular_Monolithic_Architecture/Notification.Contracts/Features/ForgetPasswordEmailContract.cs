namespace Notification.Contracts.Features;

public class ForgetPasswordEmailVerificationIntegrationEvent : INotification
{
    public ForgetPasswordEmailVerificationIntegrationEvent(string? emailId, Guid? passwordResetToken)
    {
        this.PasswordResetToken = passwordResetToken;
        this.EmailId = emailId;
    }

    public string? EmailId { get; }

    public Guid? PasswordResetToken { get; }
}