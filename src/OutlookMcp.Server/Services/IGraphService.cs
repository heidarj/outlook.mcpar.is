using OutlookMcp.Server.Models;

namespace OutlookMcp.Server.Services;

public interface IGraphService
{
    Task<MailboxProfileDto> GetMailboxProfileAsync(CancellationToken ct = default);
    Task<PagedResult<MailFolderDto>> ListMailFoldersAsync(int? top = null, string? nextLink = null, CancellationToken ct = default);
    Task<PagedResult<MessageDto>> ListMessagesAsync(string? folderId = null, int? top = null, string? nextLink = null, string? filter = null, CancellationToken ct = default);
    Task<MessageDto> GetMessageAsync(string messageId, CancellationToken ct = default);
    Task<PagedResult<EventDto>> ListCalendarViewAsync(DateTimeOffset startDateTime, DateTimeOffset endDateTime, int? top = null, string? nextLink = null, CancellationToken ct = default);
    Task<PagedResult<ContactDto>> ListContactsAsync(int? top = null, string? nextLink = null, CancellationToken ct = default);
    Task<MailboxSettingsDto> GetMailboxSettingsAsync(CancellationToken ct = default);
}
