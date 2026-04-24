using System.Collections.Generic;

namespace Bikeapelago.Api.Models
{
    public class PathPoint
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double? Alt { get; set; }
    }

    public class RideStats
    {
        public double DistanceMeters { get; set; }
        public double ElevationGainMeters { get; set; }
        public double DurationSeconds { get; set; }
        public double AvgSpeedKph { get; set; }
    }

    public class NewlyCheckedNode
    {
        public Guid Id { get; set; }
        public long ApArrivalLocationId { get; set; }
        public long ApPrecisionLocationId { get; set; }
        public bool ArrivalChecked { get; set; }
        public bool PrecisionChecked { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class FitAnalysisResult
    {
        public List<PathPoint> Path { get; set; } = new();
        public RideStats Stats { get; set; } = new();
        public List<NewlyCheckedNode> NewlyCheckedNodes { get; set; } = new();
    }
}
