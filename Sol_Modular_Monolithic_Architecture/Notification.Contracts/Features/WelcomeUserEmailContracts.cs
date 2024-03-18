namespace Notifications.Application.Modules.Features;

public class WelcomeUserEmailRequestDTO
{
    public WelcomeUserEmailRequestDTO(string emailId, string firstName, string lastName)
    {
        this.EmailId = emailId;
        this.FirstName = firstName;
        this.LastName = lastName;
    }

    public string EmailId { get; }

    public string FirstName { get; }

    public string LastName { get; }
}

public class WelcomeUserEmailIntegrationEvent : WelcomeUserEmailRequestDTO, INotification
{
    public WelcomeUserEmailIntegrationEvent(string emailId, string firstName, string lastName) : base(emailId, firstName, lastName)
    {
    }
}