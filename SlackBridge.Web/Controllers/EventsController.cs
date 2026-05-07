using Microsoft.AspNetCore.Mvc;
using SlackBridge.Web.Contracts;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(
    IApiKeyValidator apiKeyValidator,
    IEventIngestionService eventIngestionService,
    ILogger<EventsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<EventResponse>> Post(EventRequest request, CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("x-api-key", out var apiKeyValues))
        {
            return Unauthorized();
        }

        var apiKey = await apiKeyValidator.ValidateAsync(apiKeyValues.ToString(), cancellationToken);
        if (apiKey is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await eventIngestionService.HandleAsync(apiKey, request, cancellationToken));
        }
        catch (EventDefinitionNotFoundException exception)
        {
            return NotFound(new EventResponse(exception.LogId, "failed"));
        }
        catch (PlanLimitExceededException exception)
        {
            logger.LogInformation(exception, "Event rejected by plan limit.");
            return StatusCode(StatusCodes.Status429TooManyRequests, new { status = "limit_exceeded", message = exception.Message });
        }
        catch (EventDeliveryException exception)
        {
            logger.LogWarning(exception, "Event delivery failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new EventResponse(exception.LogId, "failed"));
        }
    }
}
