namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public static class VipField
{
    // col 1-3, row 5-9 (0-based, fieldNumber = row*20 + col + 1)
    private static readonly HashSet<int> GoalCreaseLeft  = ComputeRange(cols: (1, 3),   rows: (5, 9));
    private static readonly HashSet<int> GoalCreaseRight = ComputeRange(cols: (16, 18), rows: (5, 9));
    private static readonly HashSet<int> CenterCircle    = ComputeRange(cols: (8, 11),  rows: (5, 9));

    // Face-off spots: top/bottom of each side + center top/bottom
    private static readonly HashSet<int> FaceOffSpots =
    [
        45,
        // left bottom (col 2, row 12) = 12*20+2+1=243
        243,
        // right top (col 17, row 2) = 2*20+17+1=58
        58,
        // right bottom (col 17, row 12) = 12*20+17+1=258
        258
    ];

    public static string? GetLabel(int fieldNumber) =>
        GoalCreaseLeft.Contains(fieldNumber)  ? "Torraum" :
        GoalCreaseRight.Contains(fieldNumber) ? "Torraum" :
        CenterCircle.Contains(fieldNumber)    ? "Anspielkreis" :
        FaceOffSpots.Contains(fieldNumber)    ? "Anspielpunkt" :
        null;

    public static bool IsVip(int fieldNumber) => GetLabel(fieldNumber) != null;

    private static HashSet<int> ComputeRange((int from, int to) cols, (int from, int to) rows)
    {
        var result = new HashSet<int>();
        for (var r = rows.from; r <= rows.to; r++)
            for (var c = cols.from; c <= cols.to; c++)
                result.Add(r * 20 + c + 1);
        return result;
    }
}
