using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Meetings;

/// <summary>
/// MeetingNote entity ve Meeting.AddNote domain davranışı birim testleri.
/// </summary>
public sealed class MeetingNoteTests
{
    private static Meeting MakeMeeting() =>
        Meeting.Schedule(
            Guid.NewGuid(), Guid.NewGuid(), null,
            new DateOnly(2026, 6, 11), new TimeOnly(10, 0),
            "Test Adres", MeetingMethod.Visit);

    // -----------------------------------------------------------------------
    // MeetingNote ctor — doğrulama
    // -----------------------------------------------------------------------

    [Fact]
    public void MeetingNote_EmptyContent_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new MeetingNote(Guid.NewGuid(), string.Empty, null, "Yazar", null));
    }

    [Fact]
    public void MeetingNote_WhitespaceContent_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new MeetingNote(Guid.NewGuid(), "   ", null, "Yazar", null));
    }

    [Fact]
    public void MeetingNote_ValidContent_AuthorNameAndTitleAreSnapshotted()
    {
        var meetingId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var note = new MeetingNote(meetingId, "İlk not içeriği", authorId, "Umur KUTLU", "Pazarlama Direktörü");

        note.MeetingId.ShouldBe(meetingId);
        note.Content.ShouldBe("İlk not içeriği");
        note.AuthorUserId.ShouldBe(authorId);
        note.AuthorName.ShouldBe("Umur KUTLU");
        note.AuthorTitle.ShouldBe("Pazarlama Direktörü");
    }

    [Fact]
    public void MeetingNote_NullAuthorTitle_IsAllowed()
    {
        var note = new MeetingNote(Guid.NewGuid(), "İçerik", null, "Sistem", null);

        note.AuthorTitle.ShouldBeNull();
        note.AuthorUserId.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // Meeting.AddNote — içerik doğrulama
    // -----------------------------------------------------------------------

    [Fact]
    public void AddNote_EmptyContent_ThrowsArgumentException()
    {
        var meeting = MakeMeeting();

        Should.Throw<ArgumentException>(() =>
            meeting.AddNote(string.Empty, null, "Yazar", null));
    }

    [Fact]
    public void AddNote_WhitespaceContent_ThrowsArgumentException()
    {
        var meeting = MakeMeeting();

        Should.Throw<ArgumentException>(() =>
            meeting.AddNote("  \t  ", null, "Yazar", null));
    }

    // -----------------------------------------------------------------------
    // Meeting.AddNote — yazar snapshot saklanır
    // -----------------------------------------------------------------------

    [Fact]
    public void AddNote_Valid_AuthorNameAndTitleAreStoredInNote()
    {
        var meeting = MakeMeeting();
        var authorId = Guid.NewGuid();

        meeting.AddNote("Toplantı çok verimli geçti.", authorId, "Avniye ÖNER", "Satış Müdürü");

        meeting.Notes.Count.ShouldBe(1);
        var note = meeting.Notes.Single();
        note.AuthorName.ShouldBe("Avniye ÖNER");
        note.AuthorTitle.ShouldBe("Satış Müdürü");
        note.AuthorUserId.ShouldBe(authorId);
        note.Content.ShouldBe("Toplantı çok verimli geçti.");
    }

    [Fact]
    public void AddNote_NullAuthorTitle_StoredAsNull()
    {
        var meeting = MakeMeeting();

        meeting.AddNote("Notum var.", null, "Sistem", null);

        meeting.Notes.Single().AuthorTitle.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // Meeting.AddNote — kronolojik sıra (CreatedAtUtc artan)
    // -----------------------------------------------------------------------

    [Fact]
    public void AddNote_MultipleNotes_AreReturnedInCreationOrder()
    {
        var meeting = MakeMeeting();

        meeting.AddNote("Birinci not", null, "A", null);
        meeting.AddNote("İkinci not", null, "B", null);
        meeting.AddNote("Üçüncü not", null, "C", null);

        var notes = meeting.Notes.OrderBy(n => n.CreatedAtUtc).ToList();

        notes[0].Content.ShouldBe("Birinci not");
        notes[1].Content.ShouldBe("İkinci not");
        notes[2].Content.ShouldBe("Üçüncü not");
    }

    [Fact]
    public void AddNote_NotesCollectionIsReadOnly_CannotBeModifiedExternally()
    {
        var meeting = MakeMeeting();
        meeting.AddNote("Not", null, "A", null);

        // Notes özelliği IReadOnlyCollection döndürür;
        // doğrudan cast olmadığını kontrol ediyoruz.
        var notes = meeting.Notes;
        notes.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<MeetingNote>>();
    }

    // -----------------------------------------------------------------------
    // MeetingConfiguration cascade — konfigürasyon doğrulaması
    // -----------------------------------------------------------------------

    [Fact]
    public void AddNote_MeetingIdIsSetCorrectly()
    {
        var meeting = MakeMeeting();

        meeting.AddNote("İçerik", null, "Yazar", null);

        meeting.Notes.Single().MeetingId.ShouldBe(meeting.Id);
    }
}
