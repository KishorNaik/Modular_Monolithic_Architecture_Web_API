namespace Users.Application.Modules.Shared.BaseController;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public abstract class UserBaseController : ControllerBase
{
    private readonly IMediator mediator; // In Memory Bus

    public UserBaseController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    protected IMediator Mediator => mediator;
}