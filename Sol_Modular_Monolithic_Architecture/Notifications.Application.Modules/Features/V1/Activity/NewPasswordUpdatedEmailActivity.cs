namespace Notifications.Application.Modules.Features.V1.Activity;

#region Event Service

public class NewPasswordUpdatedEmailIntegrationEventHandler : INotificationHandler<NewPasswordUpdatedEmailIntegrationEvent>
{
    private readonly ILogger<NewPasswordUpdatedEmailIntegrationEventHandler> logger = null;

    public NewPasswordUpdatedEmailIntegrationEventHandler(ILogger<NewPasswordUpdatedEmailIntegrationEventHandler> logger)
    {
        this.logger = logger;
    }

    Task INotificationHandler<NewPasswordUpdatedEmailIntegrationEvent>.Handle(NewPasswordUpdatedEmailIntegrationEvent notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(nameof(NewPasswordUpdatedEmailIntegrationEventHandler), "Sent New Password Update email done.");
        return Task.CompletedTask;
    }
}

#endregion Event Service