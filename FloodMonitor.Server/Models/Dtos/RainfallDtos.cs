namespace FloodMonitor.Server.Models.Dtos;

public record RainfallCityDto(string City, string State, double Latitude, double Longitude, int Occurrences);

public record MonthlyRainfallDto(int Year, int Month, double? Millimeters, int DaysWithData);

public record RainfallStationDto(string Code, string Name, double Latitude, double Longitude, double DistanceKm, int DaysWithData);

public record RainfallSeriesDto(RainfallSource Source, RainfallStationDto? Station, IReadOnlyList<MonthlyRainfallDto> Months);

public record MonthTotalOccurrencesDto(int Year, int Month, int Total);

public record CityRainfallDto(
    string City,
    string State,
    double Latitude,
    double Longitude,
    RainfallSeriesDto? Measured,
    RainfallSeriesDto? Estimated,
    IReadOnlyList<RainfallStationDto> Stations,
    IReadOnlyList<MonthTotalOccurrencesDto> Occurrences,
    bool MeasurementsEnabled,
    bool SyncPending);
