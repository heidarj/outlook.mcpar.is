namespace OutlookMcp.Server.Models;

public record MailboxProfileDto(
    string Id,
    string DisplayName,
    string? Mail,
    string? UserPrincipalName,
    string? JobTitle,
    string? OfficeLocation
);

public record MailFolderDto(
    string Id,
    string DisplayName,
    int? TotalItemCount,
    int? UnreadItemCount,
    bool? IsHidden
);

public record PagedResult<T>(
    IReadOnlyList<T> Value,
    string? NextLink
);

public record MessageDto(
    string Id,
    string? Subject,
    string? BodyPreview,
    string? BodyContent,
    string? BodyContentType,
    DateTimeOffset? ReceivedDateTime,
    DateTimeOffset? SentDateTime,
    bool? IsRead,
    bool? HasAttachments,
    EmailAddressDto? From,
    IReadOnlyList<EmailAddressDto>? ToRecipients,
    IReadOnlyList<EmailAddressDto>? CcRecipients,
    string? Importance,
    string? InternetMessageId,
    string? WebLink
);

public record EmailAddressDto(string? Name, string? Address);

public record EventDto(
    string Id,
    string? Subject,
    string? BodyPreview,
    DateTimeDto? Start,
    DateTimeDto? End,
    string? Location,
    bool? IsAllDay,
    bool? IsCancelled,
    bool? IsOnlineMeeting,
    string? OnlineMeetingUrl,
    string? Organizer,
    IReadOnlyList<AttendeeDto>? Attendees,
    string? WebLink
);

public record DateTimeDto(string? DateTime, string? TimeZone);

public record AttendeeDto(string? Name, string? Address, string? Status);

public record ContactDto(
    string Id,
    string? DisplayName,
    string? GivenName,
    string? Surname,
    string? JobTitle,
    string? CompanyName,
    IReadOnlyList<string>? EmailAddresses,
    IReadOnlyList<string>? BusinessPhones,
    IReadOnlyList<string>? MobilePhone
);

public record MailboxSettingsDto(
    string? TimeZone,
    string? Language,
    string? DateFormat,
    string? TimeFormat,
    AutomaticRepliesSettingDto? AutomaticRepliesSetting,
    string? WorkingHours
);

public record AutomaticRepliesSettingDto(
    string? Status,
    string? ExternalAudience,
    string? InternalReplyMessage,
    string? ExternalReplyMessage
);
