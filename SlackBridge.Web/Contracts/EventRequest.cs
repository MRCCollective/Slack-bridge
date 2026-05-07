using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SlackBridge.Web.Contracts;

public sealed class EventRequest
{
    [Required, MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public JsonElement Data { get; set; }
}
