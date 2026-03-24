using System.ComponentModel.DataAnnotations;

namespace OutlookMcp.Server.Configuration;

public class McpServerOptions
{
    public string? BaseUrl { get; set; }

    [Required]
    public string ScopeName { get; set; } = string.Empty;
}
