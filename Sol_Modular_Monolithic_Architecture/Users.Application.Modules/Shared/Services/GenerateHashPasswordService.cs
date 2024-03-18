namespace Users.Application.Modules.Shared.Services;

public interface IGenerateHashPasswordService
{
    Task<(string salt, string hash)> GenerateAsync(string password);
}

public class GenerateHashPasswordService : IGenerateHashPasswordService
{
    async Task<(string salt, string hash)> IGenerateHashPasswordService.GenerateAsync(string password)
    {
        var saltData = await Salt.CreateAsync(ByteRange.byte256);
        var hashData = await Hash.CreateAsync(password, saltData, ByteRange.byte256);

        return (salt: saltData, hash: hashData);
    }
}