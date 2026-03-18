using System.ComponentModel.DataAnnotations;

namespace OutlookMcp.Server.Configuration;

public class GraphOptions
{
    [Required]
    public string BaseUrl { get; set; } = "https://graph.microsoft.com/v1.0";

    public string[] Scopes { get; set; } =
    [
        "User.Read",
        "Mail.Read",
        "Calendars.Read",
        "Contacts.Read",
        "MailboxSettings.Read"
    ];
}
