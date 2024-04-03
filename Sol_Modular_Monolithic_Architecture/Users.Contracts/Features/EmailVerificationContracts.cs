namespace Users.Contracts.Features;

public class EmailVerificationSignUpRequestDTO
{
    [JsonIgnore]
    public Guid? Token { get; set; }
}

public class EmailVerificationSignUpResponseDTO
{
    public DateTime GenerateDateTime { get; set; }
}