using Microsoft.Graph;
using Microsoft.Graph.Models;
using OutlookMcp.Server.Models;

namespace OutlookMcp.Server.Services;

public sealed class GraphService : IGraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphService> _logger;

    public GraphService(GraphServiceClient graphClient, ILogger<GraphService> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }

    public async Task<MailboxProfileDto> GetMailboxProfileAsync(CancellationToken ct = default)
    {
        var user = await _graphClient.Me.GetAsync(config =>
        {
            config.QueryParameters.Select =
            [
                "id", "displayName", "mail", "userPrincipalName", "jobTitle", "officeLocation"
            ];
        }, ct);

        if (user is null) throw new InvalidOperationException("Could not retrieve user profile.");

        return new MailboxProfileDto(
            user.Id ?? string.Empty,
            user.DisplayName ?? string.Empty,
            user.Mail,
            user.UserPrincipalName,
            user.JobTitle,
            user.OfficeLocation);
    }

    public async Task<PagedResult<MailFolderDto>> ListMailFoldersAsync(int? top = null, string? nextLink = null, CancellationToken ct = default)
    {
        Microsoft.Graph.Models.MailFolderCollectionResponse? response;

        if (nextLink is not null)
        {
            response = await _graphClient.Me.MailFolders.WithUrl(nextLink).GetAsync(cancellationToken: ct);
        }
        else
        {
            response = await _graphClient.Me.MailFolders.GetAsync(config =>
            {
                config.QueryParameters.Select =
                [
                    "id", "displayName", "totalItemCount", "unreadItemCount", "isHidden"
                ];
                if (top.HasValue) config.QueryParameters.Top = top;
            }, ct);
        }

        var folders = response?.Value?.Select(f => new MailFolderDto(
            f.Id ?? string.Empty,
            f.DisplayName ?? string.Empty,
            f.TotalItemCount,
            f.UnreadItemCount,
            f.IsHidden)).ToList() ?? [];

        return new PagedResult<MailFolderDto>(folders, response?.OdataNextLink);
    }

    public async Task<PagedResult<MessageDto>> ListMessagesAsync(string? folderId = null, int? top = null, string? nextLink = null, string? filter = null, CancellationToken ct = default)
    {
        Microsoft.Graph.Models.MessageCollectionResponse? response;

        if (nextLink is not null)
        {
            // Use the full OData next link URL for pagination
            if (folderId is not null)
            {
                response = await _graphClient.Me.MailFolders[folderId].Messages.WithUrl(nextLink).GetAsync(cancellationToken: ct);
            }
            else
            {
                response = await _graphClient.Me.Messages.WithUrl(nextLink).GetAsync(cancellationToken: ct);
            }
        }
        else if (folderId is not null)
        {
            response = await _graphClient.Me.MailFolders[folderId].Messages.GetAsync(config =>
            {
                config.QueryParameters.Select =
                [
                    "id", "subject", "bodyPreview", "receivedDateTime", "sentDateTime",
                    "isRead", "hasAttachments", "from", "toRecipients", "ccRecipients",
                    "importance", "internetMessageId", "webLink"
                ];
                if (top.HasValue) config.QueryParameters.Top = top;
                if (filter is not null) config.QueryParameters.Filter = filter;
            }, ct);
        }
        else
        {
            response = await _graphClient.Me.Messages.GetAsync(config =>
            {
                config.QueryParameters.Select =
                [
                    "id", "subject", "bodyPreview", "receivedDateTime", "sentDateTime",
                    "isRead", "hasAttachments", "from", "toRecipients", "ccRecipients",
                    "importance", "internetMessageId", "webLink"
                ];
                if (top.HasValue) config.QueryParameters.Top = top;
                if (filter is not null) config.QueryParameters.Filter = filter;
            }, ct);
        }

        var messages = response?.Value?.Select(MapMessage).ToList() ?? [];
        return new PagedResult<MessageDto>(messages, response?.OdataNextLink);
    }

    public async Task<MessageDto> GetMessageAsync(string messageId, CancellationToken ct = default)
    {
        var message = await _graphClient.Me.Messages[messageId].GetAsync(config =>
        {
            config.QueryParameters.Select =
            [
                "id", "subject", "body", "bodyPreview", "receivedDateTime", "sentDateTime",
                "isRead", "hasAttachments", "from", "toRecipients", "ccRecipients",
                "importance", "internetMessageId", "webLink"
            ];
            config.Headers.Add("Prefer", "outlook.body-content-type=\"text\"");
        }, ct);

        if (message is null) throw new InvalidOperationException($"Message {messageId} not found.");
        return MapMessage(message);
    }

    public async Task<PagedResult<EventDto>> ListCalendarViewAsync(DateTimeOffset startDateTime, DateTimeOffset endDateTime, int? top = null, string? nextLink = null, CancellationToken ct = default)
    {
        Microsoft.Graph.Models.EventCollectionResponse? response;

        if (nextLink is not null)
        {
            response = await _graphClient.Me.CalendarView.WithUrl(nextLink).GetAsync(cancellationToken: ct);
        }
        else
        {
            response = await _graphClient.Me.CalendarView.GetAsync(config =>
            {
                config.QueryParameters.StartDateTime = startDateTime.ToString("o");
                config.QueryParameters.EndDateTime = endDateTime.ToString("o");
                config.QueryParameters.Select =
                [
                    "id", "subject", "bodyPreview", "start", "end", "location",
                    "isAllDay", "isCancelled", "isOnlineMeeting", "onlineMeetingUrl",
                    "organizer", "attendees", "webLink"
                ];
                if (top.HasValue) config.QueryParameters.Top = top;
            }, ct);
        }

        var events = response?.Value?.Select(MapEvent).ToList() ?? [];
        return new PagedResult<EventDto>(events, response?.OdataNextLink);
    }

    public async Task<PagedResult<ContactDto>> ListContactsAsync(int? top = null, string? nextLink = null, CancellationToken ct = default)
    {
        Microsoft.Graph.Models.ContactCollectionResponse? response;

        if (nextLink is not null)
        {
            response = await _graphClient.Me.Contacts.WithUrl(nextLink).GetAsync(cancellationToken: ct);
        }
        else
        {
            response = await _graphClient.Me.Contacts.GetAsync(config =>
            {
                config.QueryParameters.Select =
                [
                    "id", "displayName", "givenName", "surname", "jobTitle",
                    "companyName", "emailAddresses", "businessPhones", "mobilePhone"
                ];
                if (top.HasValue) config.QueryParameters.Top = top;
            }, ct);
        }

        var contacts = response?.Value?.Select(MapContact).ToList() ?? [];
        return new PagedResult<ContactDto>(contacts, response?.OdataNextLink);
    }

    public async Task<MailboxSettingsDto> GetMailboxSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _graphClient.Me.MailboxSettings.GetAsync(cancellationToken: ct);

        if (settings is null) throw new InvalidOperationException("Could not retrieve mailbox settings.");

        AutomaticRepliesSettingDto? autoReplies = null;
        if (settings.AutomaticRepliesSetting is not null)
        {
            autoReplies = new AutomaticRepliesSettingDto(
                settings.AutomaticRepliesSetting.Status?.ToString(),
                settings.AutomaticRepliesSetting.ExternalAudience?.ToString(),
                settings.AutomaticRepliesSetting.InternalReplyMessage,
                settings.AutomaticRepliesSetting.ExternalReplyMessage);
        }

        return new MailboxSettingsDto(
            settings.TimeZone,
            settings.Language?.Locale,
            settings.DateFormat,
            settings.TimeFormat,
            autoReplies,
            settings.WorkingHours is not null
                ? System.Text.Json.JsonSerializer.Serialize(settings.WorkingHours)
                : null);
    }

    private static MessageDto MapMessage(Microsoft.Graph.Models.Message m) =>
        new(
            m.Id ?? string.Empty,
            m.Subject,
            m.BodyPreview,
            m.Body?.Content,
            m.Body?.ContentType?.ToString(),
            m.ReceivedDateTime,
            m.SentDateTime,
            m.IsRead,
            m.HasAttachments,
            MapEmailAddress(m.From?.EmailAddress),
            m.ToRecipients?.Select(r => MapEmailAddress(r.EmailAddress))
                .OfType<EmailAddressDto>().ToList(),
            m.CcRecipients?.Select(r => MapEmailAddress(r.EmailAddress))
                .OfType<EmailAddressDto>().ToList(),
            m.Importance?.ToString(),
            m.InternetMessageId,
            m.WebLink);

    private static EmailAddressDto? MapEmailAddress(Microsoft.Graph.Models.EmailAddress? ea) =>
        ea is null ? null : new EmailAddressDto(ea.Name, ea.Address);

    private static EventDto MapEvent(Microsoft.Graph.Models.Event e) =>
        new(
            e.Id ?? string.Empty,
            e.Subject,
            e.BodyPreview,
            e.Start is null ? null : new DateTimeDto(e.Start.DateTime, e.Start.TimeZone),
            e.End is null ? null : new DateTimeDto(e.End.DateTime, e.End.TimeZone),
            e.Location?.DisplayName,
            e.IsAllDay,
            e.IsCancelled,
            e.IsOnlineMeeting,
            e.OnlineMeetingUrl,
            e.Organizer?.EmailAddress?.Address,
            e.Attendees?.Select(a => new AttendeeDto(
                a.EmailAddress?.Name,
                a.EmailAddress?.Address,
                a.Status?.Response?.ToString())).ToList(),
            e.WebLink);

    private static ContactDto MapContact(Microsoft.Graph.Models.Contact c) =>
        new(
            c.Id ?? string.Empty,
            c.DisplayName,
            c.GivenName,
            c.Surname,
            c.JobTitle,
            c.CompanyName,
            c.EmailAddresses?.Select(e => e.Address ?? string.Empty)
                .Where(a => !string.IsNullOrEmpty(a)).ToList(),
            c.BusinessPhones?.ToList(),
            c.MobilePhone is null ? null : [c.MobilePhone]);
}
