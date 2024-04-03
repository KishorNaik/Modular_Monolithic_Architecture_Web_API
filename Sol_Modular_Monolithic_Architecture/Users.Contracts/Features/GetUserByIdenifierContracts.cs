namespace Users.Contracts.Features;

public class GetUserByIdentifierRequestDTO
{
    [JsonIgnore]
    public Guid? Identifier { get; set; }
}

public class GetUserByIdentifierResponseDTO
{
    public Guid Identifier { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailId { get; set; }

    public string MobileNo { get; set; }

    public int UserType { get; set; }
}