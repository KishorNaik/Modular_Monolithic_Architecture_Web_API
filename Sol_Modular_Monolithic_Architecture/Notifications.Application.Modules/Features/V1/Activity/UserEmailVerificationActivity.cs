namespace Notifications.Application.Modules.Features.V1.Activity;

#region Event Service

public class UserEmailVerificationIntegrationEventHandler : INotificationHandler<UserEmailVerificationIntegrationEvent>
{
    private readonly ILogger<UserEmailVerificationIntegrationEventHandler> logger;

    public UserEmailVerificationIntegrationEventHandler(ILogger<UserEmailVerificationIntegrationEventHandler> logger)
    {
        this.logger = logger;
    }

    Task INotificationHandler<UserEmailVerificationIntegrationEvent>.Handle(UserEmailVerificationIntegrationEvent notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(nameof(UserEmailVerificationIntegrationEventHandler), "Sent User verification email done.");
        return Task.CompletedTask;
    }
}

#endregion Event Service