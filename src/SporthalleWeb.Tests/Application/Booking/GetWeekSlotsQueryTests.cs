using Moq;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using Xunit;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Tests.Application.Booking;

public sealed class GetWeekSlotsQueryTests
{
    private readonly Mock<IBookingSlots> _repo = new();
    private readonly GetWeekSlots _sut;

    private static readonly DateTime BaseUtc = new(2025, 6, 2, 8, 0, 0, DateTimeKind.Utc);

    public GetWeekSlotsQueryTests()
    {
        _sut = new GetWeekSlots(_repo.Object);
    }

    private static BookingSlot MakeSlot(SlotType type, string title, bool showTitlePublic) =>
        BookingSlot.FromPersistence(
            id: 1, memberId: null, type: type.ToString(),
            startUtc: BaseUtc, endUtc: BaseUtc.AddHours(2),
            title: title, color: null, notes: null,
            createdAt: BaseUtc, updatedAt: BaseUtc, createdBy: "admin",
            recurringSlotId: null, showTitlePublic: showTitlePublic);

    // ── ShowTitlePublic title-masking ──────────────────────────────────────────

    [Fact]
    public async Task Execute_ShowTitlePublicTrue_ReturnsTitleInDto()
    {
        _repo.Setup(r => r.GetForWeekAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync([MakeSlot(SlotType.Booked, "Unihockey", showTitlePublic: true)]);

        var result = await _sut.ExecuteAsync(new DateOnly(2025, 6, 2));

        Assert.Equal("Unihockey", result[0].Title);
    }

    [Fact]
    public async Task Execute_ShowTitlePublicFalse_ReturnsEmptyTitleInDto()
    {
        // Before the fix the title was always returned. Now ShowTitlePublic gates it.
        _repo.Setup(r => r.GetForWeekAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync([MakeSlot(SlotType.Booked, "Unihockey", showTitlePublic: false)]);

        var result = await _sut.ExecuteAsync(new DateOnly(2025, 6, 2));

        Assert.Equal("", result[0].Title);
    }

    [Fact]
    public async Task Execute_ShowTitlePublicFalse_TitleIsNotLeaked()
    {
        // The internal title must not appear in the public DTO at all.
        _repo.Setup(r => r.GetForWeekAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync([MakeSlot(SlotType.Reserved, "Geheimes Training", showTitlePublic: false)]);

        var result = await _sut.ExecuteAsync(new DateOnly(2025, 6, 2));

        Assert.DoesNotContain("Geheimes Training", result[0].Title);
    }

    // ── Rejected slots are excluded ────────────────────────────────────────────

    [Fact]
    public async Task Execute_RejectedSlot_IsExcludedFromResult()
    {
        _repo.Setup(r => r.GetForWeekAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync([
                 MakeSlot(SlotType.Booked,    "Bestätigt",  showTitlePublic: true),
                 MakeSlot(SlotType.Rejected,  "Abgelehnt",  showTitlePublic: false),
             ]);

        var result = await _sut.ExecuteAsync(new DateOnly(2025, 6, 2));

        Assert.Single(result);
        Assert.Equal("Booked", result[0].Type);
    }

    [Fact]
    public async Task Execute_AllRejectedSlots_ReturnsEmptyList()
    {
        _repo.Setup(r => r.GetForWeekAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync([
                 MakeSlot(SlotType.Rejected, "A", showTitlePublic: false),
                 MakeSlot(SlotType.Rejected, "B", showTitlePublic: false),
             ]);

        var result = await _sut.ExecuteAsync(new DateOnly(2025, 6, 2));

        Assert.Empty(result);
    }

    // ── All non-rejected slot types are included ───────────────────────────────

    [Theory]
    [InlineData(SlotType.Booked)]
    [InlineData(SlotType.Reserved)]
    [InlineData(SlotType.Blocker)]
    [InlineData(SlotType.Recurring)]
    public async Task Execute_NonRejectedSlotType_IsIncluded(SlotType type)
    {
        _repo.Setup(r => r.GetForWeekAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync([MakeSlot(type, "Slot", showTitlePublic: false)]);

        var result = await _sut.ExecuteAsync(new DateOnly(2025, 6, 2));

        Assert.Single(result);
        Assert.Equal(type.ToString(), result[0].Type);
    }

    // ── DTO field mapping ──────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_MapsUtcTimesCorrectly()
    {
        _repo.Setup(r => r.GetForWeekAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync([MakeSlot(SlotType.Booked, "Slot", showTitlePublic: false)]);

        var result = await _sut.ExecuteAsync(new DateOnly(2025, 6, 2));

        Assert.Equal(BaseUtc, result[0].StartUtc);
        Assert.Equal(BaseUtc.AddHours(2), result[0].EndUtc);
    }

    [Fact]
    public async Task Execute_QueriesCorrectWeekWindow()
    {
        var monday = new DateOnly(2025, 6, 2);
        var expectedFrom = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc);
        var expectedTo   = expectedFrom.AddDays(7);

        _repo.Setup(r => r.GetForWeekAsync(expectedFrom, expectedTo))
             .ReturnsAsync([])
             .Verifiable();

        await _sut.ExecuteAsync(monday);

        _repo.Verify(r => r.GetForWeekAsync(expectedFrom, expectedTo), Times.Once);
    }
}
