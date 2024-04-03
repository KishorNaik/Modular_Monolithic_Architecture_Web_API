namespace Users.Contracts.Features;

public class RemoveUserRequestDTO
{
    [JsonIgnore]
    public Guid Identifier { get; set; }
}

public class RemoveUserResponseDTO
{
    public DateTime? UpdatedTime { get; set; }
}