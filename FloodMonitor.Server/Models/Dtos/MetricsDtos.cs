namespace FloodMonitor.Server.Models.Dtos;

public record MetricsSummaryDto(
    int Total,
    int Last7Days,
    int Last30Days,
    string? MostAffectedNeighborhood,
    IReadOnlyList<WaterLevelTotalDto> ByWaterLevel);

public record WaterLevelTotalDto(WaterLevel WaterLevel, int Total);

public record NeighborhoodTotalDto(string Neighborhood, int Total, DateTime LastOccurrence);

public record MonthTotalDto(int Year, int Month, int Total, IReadOnlyList<WaterLevelTotalDto> ByWaterLevel);

public record HotspotDto(
    double Latitude,
    double Longitude,
    int Occurrences,
    int DistinctDays,
    string? Neighborhood,
    DateTime LastOccurrence);
