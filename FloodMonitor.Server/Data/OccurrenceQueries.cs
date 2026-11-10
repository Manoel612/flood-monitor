using FloodMonitor.Server.Models;
using FloodMonitor.Server.Models.Dtos;

namespace FloodMonitor.Server.Data;

public static class OccurrenceQueries
{
    private const double MetersPerDegreeOfLatitude = 111_320;
    private const double EarthRadiusInMeters = 6_371_000;

    public static IQueryable<Occurrence> VisibleInPeriod(this IQueryable<Occurrence> query, DateTime? from, DateTime? to)
    {
        query = query.Where(o => !o.IsHidden);

        if (from is not null)
        {
            var start = from.Value.ToUniversalTime();
            query = query.Where(o => o.OccurredAt >= start);
        }

        if (to is not null)
        {
            var end = to.Value.ToUniversalTime();
            query = query.Where(o => o.OccurredAt <= end);
        }

        return query;
    }

    public static IQueryable<Occurrence> InLocation(this IQueryable<Occurrence> query, LocationFilter? location)
    {
        if (location is null)
            return query;

        if (!string.IsNullOrWhiteSpace(location.State))
        {
            var state = location.State.Trim();
            query = query.Where(o => o.State == state);
        }

        if (!string.IsNullOrWhiteSpace(location.City))
        {
            var city = location.City.Trim();
            query = query.Where(o => o.City == city);
        }

        if (!string.IsNullOrWhiteSpace(location.Neighborhood))
        {
            var neighborhood = location.Neighborhood.Trim();
            query = query.Where(o => o.Neighborhood == neighborhood);
        }

        if (location.ByRadius)
        {
            var lat = location.Lat!.Value;
            var lon = location.Lon!.Value;
            var deltaLat = location.RadiusInMeters / MetersPerDegreeOfLatitude;
            var deltaLon = location.RadiusInMeters / (MetersPerDegreeOfLatitude * Math.Cos(lat * Math.PI / 180));
            query = query.Where(o =>
                o.Latitude >= lat - deltaLat && o.Latitude <= lat + deltaLat &&
                o.Longitude >= lon - deltaLon && o.Longitude <= lon + deltaLon);
        }

        return query;
    }

    public static IQueryable<Occurrence> ByAuthor(this IQueryable<Occurrence> query, string authorHash) =>
        query.Where(o => o.AuthorHash == authorHash);

    public static bool IsWithinRadius(this LocationFilter? location, double latitude, double longitude) =>
        location?.ByRadius != true ||
        DistanceInMeters(location.Lat!.Value, location.Lon!.Value, latitude, longitude) <= location.RadiusInMeters;

    public static double DistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        static double ToRadians(double degrees) => degrees * Math.PI / 180;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadiusInMeters * Math.Asin(Math.Sqrt(a));
    }
}
