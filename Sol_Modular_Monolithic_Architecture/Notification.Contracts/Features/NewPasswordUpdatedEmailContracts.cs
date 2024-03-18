namespace Notification.Contracts.Features;

public class NewPasswordUpdatedEmailIntegrationEvent : INotification
{
    public NewPasswordUpdatedEmailIntegrationEvent(string? emailId)
    {
        this.EmailId = emailId;
    }

    public string? EmailId { get; }
}