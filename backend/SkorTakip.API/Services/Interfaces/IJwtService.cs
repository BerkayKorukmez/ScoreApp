using SkorTakip.API.Models;

namespace SkorTakip.API.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IList<string>? roles = null);
}
