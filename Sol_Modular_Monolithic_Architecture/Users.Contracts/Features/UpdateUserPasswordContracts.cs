namespace Users.Contracts.Features;

public class UpdateUserPasswordRequestDTO
{
    [JsonIgnore]
    public Guid? Identifier { get; set; }

    public string? Password { get; set; }
}

public class UpdateUserPasswordResponseDTO
{
    public DateTime? UpdatedTime { get; set; }
}

public class UpdateUserPasswordCommand : UpdateUserPasswordRequestDTO, IRequest<DataResponse<UpdateUserPasswordResponseDTO>>
{
}