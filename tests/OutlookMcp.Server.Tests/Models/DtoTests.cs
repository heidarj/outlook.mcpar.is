using OutlookMcp.Server.Models;
using Xunit;

namespace OutlookMcp.Server.Tests.Models;

public class DtoTests
{
    [Fact]
    public void MailboxProfileDto_CreatesCorrectly()
    {
        var dto = new MailboxProfileDto("id", "Test User", "test@example.com", "test@example.com", "Engineer", "Building 1");
        Assert.Equal("id", dto.Id);
        Assert.Equal("Test User", dto.DisplayName);
        Assert.Equal("test@example.com", dto.Mail);
    }

    [Fact]
    public void PagedResult_WithNextLink_HasNextLink()
    {
        var items = new List<MailFolderDto>
        {
            new("id1", "Inbox", 100, 10, false)
        };
        var result = new PagedResult<MailFolderDto>(items, "https://graph.microsoft.com/v1.0/me/mailFolders?$skiptoken=abc");
        Assert.Single(result.Value);
        Assert.NotNull(result.NextLink);
    }

    [Fact]
    public void PagedResult_WithoutNextLink_NextLinkIsNull()
    {
        var items = new List<MailFolderDto>();
        var result = new PagedResult<MailFolderDto>(items, null);
        Assert.Null(result.NextLink);
    }

    [Fact]
    public void EmailAddressDto_Properties()
    {
        var dto = new EmailAddressDto("John Doe", "john@example.com");
        Assert.Equal("John Doe", dto.Name);
        Assert.Equal("john@example.com", dto.Address);
    }

    [Fact]
    public void MessageDto_CreatesCorrectly()
    {
        var from = new EmailAddressDto("Sender", "sender@example.com");
        var dto = new MessageDto(
            "msg-1", "Hello", "Hello...", "Hello World", "text",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            false, false, from, null, null, "normal", "msgid@example.com", "https://outlook.com/msg");

        Assert.Equal("msg-1", dto.Id);
        Assert.Equal("Hello", dto.Subject);
        Assert.Equal("Sender", dto.From?.Name);
    }

    [Fact]
    public void EventDto_CreatesCorrectly()
    {
        var dto = new EventDto(
            "evt-1", "Team Meeting", "Join us for...",
            new DateTimeDto("2024-01-15T09:00:00", "UTC"),
            new DateTimeDto("2024-01-15T10:00:00", "UTC"),
            "Conference Room A", false, false, false, null,
            "organizer@example.com", null, "https://outlook.com/evt");

        Assert.Equal("evt-1", dto.Id);
        Assert.Equal("Team Meeting", dto.Subject);
        Assert.Equal("UTC", dto.Start?.TimeZone);
    }

    [Fact]
    public void ContactDto_CreatesCorrectly()
    {
        var dto = new ContactDto(
            "cnt-1", "Jane Smith", "Jane", "Smith",
            "Developer", "Contoso",
            ["jane@contoso.com"],
            ["+1-555-0100"],
            ["+1-555-0199"]);

        Assert.Equal("cnt-1", dto.Id);
        Assert.Equal("Jane Smith", dto.DisplayName);
        Assert.Single(dto.EmailAddresses!);
    }
}
