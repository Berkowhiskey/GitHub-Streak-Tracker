using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace StreakTracker.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// JWT'nin "sub" claim'inden mevcut kullanicinin kimligini okur.
    /// Yalnizca [Authorize] ile korunan action'larda cagrilmalidir.
    /// </summary>
    protected Guid CurrentUserId
    {
        get
        {
            var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(subject, out var userId))
            {
                throw new InvalidOperationException("Token icinde gecerli bir kullanici kimligi bulunamadi.");
            }

            return userId;
        }
    }
}
