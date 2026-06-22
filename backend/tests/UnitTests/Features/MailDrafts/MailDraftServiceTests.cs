using System.Text;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.MailDrafts;
using Oypa.Crm.Domain.Entities;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.MailDrafts;

public sealed class MailDraftServiceTests
{
    private readonly IRepository<MailDraft> _mailDrafts = Substitute.For<IRepository<MailDraft>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<MailDraftService> _logger = Substitute.For<ILogger<MailDraftService>>();

    private MailDraftService CreateSut() => new(_mailDrafts, _unitOfWork, _logger);

    private static MailDraft NewDraft(string? cc = null) => new("to@x.com", "Konu", "Govde", cc: cc);

    [Fact]
    public async Task SendAsync_DraftNotFound_ThrowsNotFound()
    {
        _mailDrafts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MailDraft?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.SendAsync(Guid.NewGuid()));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_AlreadySent_ThrowsConflict()
    {
        var draft = NewDraft();
        draft.MarkSent();
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var sut = CreateSut();

        await Should.ThrowAsync<ConflictException>(() => sut.SendAsync(draft.Id));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_PendingDraft_MarksSentAndSaves()
    {
        var draft = NewDraft();
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var sut = CreateSut();

        await sut.SendAsync(draft.Id);

        draft.Sent.ShouldBeTrue();
        draft.SentAtUtc.ShouldNotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- BuildEmlAsync ----

    [Fact]
    public async Task BuildEmlAsync_DraftNotFound_ThrowsNotFound()
    {
        _mailDrafts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MailDraft?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.BuildEmlAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task BuildEmlAsync_DraftWithoutCc_ReturnsValidEml_WithRequiredHeaders_AndNoCcLine()
    {
        var draft = NewDraft(); // Cc = null
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var sut = CreateSut();

        var (fileName, content) = await sut.BuildEmlAsync(draft.Id);
        var eml = Encoding.UTF8.GetString(content);

        fileName.ShouldBe($"toplanti-{draft.Id}.eml");
        eml.ShouldContain("To: ");
        eml.ShouldContain("X-Unsent: 1");
        eml.ShouldContain("Content-Transfer-Encoding: base64");
        eml.ShouldContain("=?UTF-8?B?");
        eml.ShouldNotContain("Cc: ");
    }

    [Fact]
    public async Task BuildEmlAsync_DraftWithCc_ReturnsEmlWithCcLine()
    {
        var draft = NewDraft(cc: "contact@example.com");
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var sut = CreateSut();

        var (fileName, content) = await sut.BuildEmlAsync(draft.Id);
        var eml = Encoding.UTF8.GetString(content);

        fileName.ShouldBe($"toplanti-{draft.Id}.eml");
        eml.ShouldContain("Cc: contact@example.com");
    }

    // ---- GetByMeetingAsync ----

    [Fact]
    public async Task GetByMeetingAsync_MatchingDraft_ReturnsDtoWithCc()
    {
        var meetingId = Guid.NewGuid();
        var draft = new MailDraft("rep@oypa.com", "Konu", "Govde", meetingId, "contact@firm.com");
        _mailDrafts.ListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<MailDraft, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MailDraft> { draft });
        var sut = CreateSut();

        var result = await sut.GetByMeetingAsync(meetingId);

        result.MeetingId.ShouldBe(meetingId);
        result.To.ShouldBe("rep@oypa.com");
        result.Cc.ShouldBe("contact@firm.com");
    }

    [Fact]
    public async Task GetByMeetingAsync_NoMatchingDraft_ThrowsNotFound()
    {
        _mailDrafts.ListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<MailDraft, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MailDraft>());
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.GetByMeetingAsync(Guid.NewGuid()));
    }
}
