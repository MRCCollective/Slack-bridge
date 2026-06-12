using System.Text;
using Microsoft.AspNetCore.Mvc;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Controllers;

[ApiController]
[Route("api/slack/commands")]
public sealed class SlackCommandsController(
    ISlackCommandGatewayService slackCommandGatewayService,
    ILogger<SlackCommandsController> logger) : ControllerBase
{
    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        Request.EnableBuffering();
        string requestBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync(cancellationToken);
        }

        Request.Body.Position = 0;
        var form = await Request.ReadFormAsync(cancellationToken);

        var result = await slackCommandGatewayService.HandleAsync(
            requestBody,
            form,
            Request.Headers,
            cancellationToken);

        logger.LogInformation(
            "Slack command endpoint completed with status {StatusCode}.",
            result.StatusCode);

        if (result.Body is null)
        {
            return StatusCode(result.StatusCode);
        }

        return Content(result.Body, result.ContentType ?? "text/plain", Encoding.UTF8);
    }
}
