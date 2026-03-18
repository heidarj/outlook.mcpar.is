using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OutlookMcp.Server.Services;
using OutlookMcp.Server.Tools;
using Xunit;

namespace OutlookMcp.Server.Tests.Tools;

public class ToolValidationTests
{
    private readonly Mock<IGraphService> _mockGraph = new();
    private readonly OutlookMcpTools _tools;

    public ToolValidationTests()
    {
        _tools = new OutlookMcpTools(_mockGraph.Object, NullLogger<OutlookMcpTools>.Instance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task ListMailFolders_InvalidTop_Throws(int top)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.ListMailFoldersAsync(top));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ListMessages_InvalidTop_Throws(int top)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.ListMessagesAsync(top: top));
    }

    [Fact]
    public async Task GetMessage_EmptyId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _tools.GetMessageAsync(""));
    }

    [Fact]
    public async Task GetMessage_WhitespaceId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _tools.GetMessageAsync("   "));
    }

    [Fact]
    public async Task ListCalendarView_EmptyStartDateTime_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _tools.ListCalendarViewAsync("", "2024-01-31T00:00:00Z"));
    }

    [Fact]
    public async Task ListCalendarView_InvalidDateFormat_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _tools.ListCalendarViewAsync("not-a-date", "2024-01-31T00:00:00Z"));
    }

    [Fact]
    public async Task ListCalendarView_EndBeforeStart_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _tools.ListCalendarViewAsync("2024-01-31T00:00:00Z", "2024-01-01T00:00:00Z"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ListContacts_InvalidTop_Throws(int top)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.ListContactsAsync(top));
    }

    [Fact]
    public async Task GetMailboxProfile_CallsGraphService()
    {
        _mockGraph.Setup(g => g.GetMailboxProfileAsync(default))
            .ReturnsAsync(new OutlookMcp.Server.Models.MailboxProfileDto(
                "id1", "Test User", "test@example.com", "test@example.com", null, null));

        var result = await _tools.GetMailboxProfileAsync();

        Assert.Equal("id1", result.Id);
        Assert.Equal("Test User", result.DisplayName);
        _mockGraph.Verify(g => g.GetMailboxProfileAsync(default), Times.Once);
    }

    [Fact]
    public async Task ListMailFolders_ValidTop_CallsGraphService()
    {
        _mockGraph.Setup(g => g.ListMailFoldersAsync(10, null, default))
            .ReturnsAsync(new OutlookMcp.Server.Models.PagedResult<OutlookMcp.Server.Models.MailFolderDto>(
                [], null));

        var result = await _tools.ListMailFoldersAsync(10);

        Assert.NotNull(result);
        _mockGraph.Verify(g => g.ListMailFoldersAsync(10, null, default), Times.Once);
    }
}
