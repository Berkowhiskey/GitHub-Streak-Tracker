using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreakTracker.API.Models.Auth;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Controllers;

[Route("api/v1/onboarding")]
[Authorize]
public class OnboardingController : BaseApiController
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    /// <summary>
    /// Kullanicinin onayini alir, gizli bildirim reposu ile Issue'yu kurar
    /// ve ilk streak hesaplamasini yapar.
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<OnboardingResultDto>> Complete(
        [FromBody] OnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _onboardingService.CompleteOnboardingAsync(CurrentUserId, request, cancellationToken);

        return Ok(result);
    }
}
