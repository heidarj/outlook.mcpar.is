using System.ComponentModel;
using ModelContextProtocol.Server;
using OutlookMcp.Server.Models;
using OutlookMcp.Server.Services;

namespace OutlookMcp.Server.Tools;

[McpServerToolType]
public sealed class OutlookMcpTools
{
    private readonly IGraphService _graph;
    private readonly ILogger<OutlookMcpTools> _logger;

    public OutlookMcpTools(IGraphService graph, ILogger<OutlookMcpTools> logger)
    {
        _graph = graph;
        _logger = logger;
    }

    [McpServerTool(Name = "get_mailbox_profile", ReadOnly = true)]
    [Description("Get the signed-in user's mailbox profile (display name, email, job title).")]
    public async Task<MailboxProfileDto> GetMailboxProfileAsync()
    {
        _logger.LogInformation("Executing get_mailbox_profile");
        return await _graph.GetMailboxProfileAsync();
    }

    [McpServerTool(Name = "list_mail_folders", ReadOnly = true)]
    [Description("List top-level mail folders in the user's mailbox.")]
    public async Task<PagedResult<MailFolderDto>> ListMailFoldersAsync(
        [Description("Maximum number of folders to return (1-100).")] int? top = null,
        [Description("The nextLink value from a previous response for pagination.")] string? nextLink = null)
    {
        if (top is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(top), "top must be between 1 and 100.");
        _logger.LogInformation("Executing list_mail_folders top={Top}", top);
        return await _graph.ListMailFoldersAsync(top, nextLink);
    }

    [McpServerTool(Name = "list_messages", ReadOnly = true)]
    [Description("List email messages from the user's mailbox or a specific folder.")]
    public async Task<PagedResult<MessageDto>> ListMessagesAsync(
        [Description("Folder ID to list messages from. Omit for all messages.")] string? folderId = null,
        [Description("Maximum number of messages to return (1-100).")] int? top = null,
        [Description("The nextLink value from a previous response for pagination.")] string? nextLink = null,
        [Description("OData filter expression (e.g. \"isRead eq false\").")] string? filter = null)
    {
        if (top is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(top), "top must be between 1 and 100.");
        _logger.LogInformation("Executing list_messages folderId={FolderId} top={Top}", folderId, top);
        return await _graph.ListMessagesAsync(folderId, top, nextLink, filter);
    }

    [McpServerTool(Name = "get_message", ReadOnly = true)]
    [Description("Get a single email message by ID, with body text content.")]
    public async Task<MessageDto> GetMessageAsync(
        [Description("The message ID to retrieve. Required.")] string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentException("messageId is required.", nameof(messageId));
        _logger.LogInformation("Executing get_message messageId={MessageId}", messageId);
        return await _graph.GetMessageAsync(messageId);
    }

    [McpServerTool(Name = "list_calendar_view", ReadOnly = true)]
    [Description("List calendar events in a given time window.")]
    public async Task<PagedResult<EventDto>> ListCalendarViewAsync(
        [Description("Start of the time window in ISO 8601 format (e.g. 2024-01-01T00:00:00Z). Required.")] string startDateTime,
        [Description("End of the time window in ISO 8601 format (e.g. 2024-01-31T23:59:59Z). Required.")] string endDateTime,
        [Description("Maximum number of events to return (1-100).")] int? top = null,
        [Description("The nextLink value from a previous response for pagination.")] string? nextLink = null)
    {
        if (string.IsNullOrWhiteSpace(startDateTime)) throw new ArgumentException("startDateTime is required.", nameof(startDateTime));
        if (string.IsNullOrWhiteSpace(endDateTime)) throw new ArgumentException("endDateTime is required.", nameof(endDateTime));
        if (!DateTimeOffset.TryParse(startDateTime, out var start)) throw new ArgumentException("startDateTime must be a valid ISO 8601 date/time.", nameof(startDateTime));
        if (!DateTimeOffset.TryParse(endDateTime, out var end)) throw new ArgumentException("endDateTime must be a valid ISO 8601 date/time.", nameof(endDateTime));
        if (end <= start) throw new ArgumentException("endDateTime must be after startDateTime.");
        if (top is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(top), "top must be between 1 and 100.");

        _logger.LogInformation("Executing list_calendar_view start={Start} end={End}", start, end);
        return await _graph.ListCalendarViewAsync(start, end, top, nextLink);
    }

    [McpServerTool(Name = "list_contacts", ReadOnly = true)]
    [Description("List contacts from the user's default contacts folder.")]
    public async Task<PagedResult<ContactDto>> ListContactsAsync(
        [Description("Maximum number of contacts to return (1-100).")] int? top = null,
        [Description("The nextLink value from a previous response for pagination.")] string? nextLink = null)
    {
        if (top is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(top), "top must be between 1 and 100.");
        _logger.LogInformation("Executing list_contacts top={Top}", top);
        return await _graph.ListContactsAsync(top, nextLink);
    }

    [McpServerTool(Name = "get_mailbox_settings", ReadOnly = true)]
    [Description("Get the user's mailbox settings including timezone, language, and auto-reply settings.")]
    public async Task<MailboxSettingsDto> GetMailboxSettingsAsync()
    {
        _logger.LogInformation("Executing get_mailbox_settings");
        return await _graph.GetMailboxSettingsAsync();
    }
}
