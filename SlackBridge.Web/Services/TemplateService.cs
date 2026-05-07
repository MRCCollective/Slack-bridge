using System.Text.Json;
using Scriban;
using Scriban.Runtime;

namespace SlackBridge.Web.Services;

public interface ITemplateService
{
    Task<string> RenderAsync(string template, JsonElement data, CancellationToken cancellationToken);
}

public sealed class TemplateService : ITemplateService
{
    public async Task<string> RenderAsync(string template, JsonElement data, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parsedTemplate = Template.Parse(template);
        if (parsedTemplate.HasErrors)
        {
            var errors = string.Join("; ", parsedTemplate.Messages.Select(message => message.Message));
            throw new InvalidOperationException($"Template parse failed: {errors}");
        }

        var scriptObject = new ScriptObject();
        if (JsonTemplateData.ToObject(data) is Dictionary<string, object?> values)
        {
            scriptObject.Import(values);
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        return await parsedTemplate.RenderAsync(context);
    }
}
