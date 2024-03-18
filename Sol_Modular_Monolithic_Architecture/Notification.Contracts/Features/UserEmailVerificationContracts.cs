namespace Notification.Contracts.Features;

public class UserEmailVerificationRequestDTO
{
    public UserEmailVerificationRequestDTO(string email, Guid? emailToken)
    {
        this.Email = email;
        this.EmailToken = emailToken;
    }

    public string Email { get; }

    public Guid? EmailToken { get; }
}

public class UserEmailVerificationIntegrationEvent : UserEmailVerificationRequestDTO, INotification
{
    public UserEmailVerificationIntegrationEvent(string email, Guid? emailToken) : base(email, emailToken)
    {
    }
}