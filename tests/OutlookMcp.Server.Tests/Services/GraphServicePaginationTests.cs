using OutlookMcp.Server.Models;
using Xunit;

namespace OutlookMcp.Server.Tests.Services;

public class GraphServicePaginationTests
{
    [Fact]
    public void PagedResult_NextLink_Passthrough()
    {
        var nextLink = "https://graph.microsoft.com/v1.0/me/messages?$skip=10";
        var result = new PagedResult<MessageDto>(new List<MessageDto>(), nextLink);
        Assert.Equal(nextLink, result.NextLink);
    }

    [Fact]
    public void PagedResult_NoNextLink_IsNull()
    {
        var result = new PagedResult<MessageDto>(new List<MessageDto>(), null);
        Assert.Null(result.NextLink);
    }

    [Fact]
    public void PagedResult_PreservesAllItems()
    {
        var messages = new List<MessageDto>
        {
            new("id1", "Subject 1", null, null, null, null, null, false, false, null, null, null, null, null, null),
            new("id2", "Subject 2", null, null, null, null, null, true, false, null, null, null, null, null, null),
        };
        var result = new PagedResult<MessageDto>(messages, null);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("id1", result.Value[0].Id);
        Assert.Equal("id2", result.Value[1].Id);
    }

    [Fact]
    public void PagedResult_Value_IsReadOnly()
    {
        var result = new PagedResult<MessageDto>(new List<MessageDto>(), null);
        Assert.IsAssignableFrom<IReadOnlyList<MessageDto>>(result.Value);
    }
}
