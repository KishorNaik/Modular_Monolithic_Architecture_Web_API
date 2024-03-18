namespace Notifications.Application.Modules.Features.V1.Activity;

#region Event Service

public class WelcomeUserEmailIntegrationEventHandler : INotificationHandler<WelcomeUserEmailIntegrationEvent>
{
    private readonly ILogger<WelcomeUserEmailIntegrationEventHandler> logger;

    public WelcomeUserEmailIntegrationEventHandler(ILogger<WelcomeUserEmailIntegrationEventHandler> logger)
    {
        this.logger = logger;
    }

    Task INotificationHandler<WelcomeUserEmailIntegrationEvent>.Handle(WelcomeUserEmailIntegrationEvent notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(nameof(WelcomeUserEmailIntegrationEventHandler), "SentWelcome User email done.");
        return Task.CompletedTask;
    }
}

#endregion Event Service