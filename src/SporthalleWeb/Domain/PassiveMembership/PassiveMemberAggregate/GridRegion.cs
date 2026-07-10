namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public sealed record GridRegion(double X0, double Y0, double X1, double Y1)
{
    public static GridRegion Default { get; } = new(0.14515, 0.05714, 0.91906, 0.93968);

    public static GridRegion Create(double x0, double y0, double x1, double y1)
    {
        static double Clamp(double v) => Math.Clamp(v, 0.0, 1.0);
        x0 = Clamp(x0);
        y0 = Clamp(y0);
        x1 = Clamp(x1);
        y1 = Clamp(y1);
        return x1 <= x0 || y1 <= y0 ? Default : new GridRegion(x0, y0, x1, y1);
    }
}
