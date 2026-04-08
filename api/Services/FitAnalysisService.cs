using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Dynastream.Fit;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services
{
    public class FitAnalysisService
    {
        private const double SemicircleToDegree = 180.0 / 2147483648.0;

        public FitAnalysisResult AnalyzeFitFile(Stream fitStream, IEnumerable<MapNode> availableNodes)
        {
            var result = new FitAnalysisResult();
            var decode = new Decode();
            var mesgBroadcaster = new MesgBroadcaster();

            double firstTime = 0;
            double lastTime = 0;
            bool firstTimeSet = false;

            mesgBroadcaster.RecordMesgEvent += (sender, e) =>
            {
                var msg = new RecordMesg(e.mesg);
                var latSemi = msg.GetPositionLat();
                var lonSemi = msg.GetPositionLong();
                var time = msg.GetTimestamp();
                var alt = msg.GetAltitude(); // usually nullable or returns null if invalid

                if (latSemi != null && lonSemi != null)
                {
                    double lat = latSemi.Value * SemicircleToDegree;
                    double lon = lonSemi.Value * SemicircleToDegree;
                    
                    result.Path.Add(new PathPoint
                    {
                        Lat = lat,
                        Lon = lon,
                        Alt = alt != null ? (double?)(float)alt : null
                    });
                }

                if (time != null)
                {
                    var ts = time.GetTimeStamp();
                    if (!firstTimeSet)
                    {
                        firstTime = ts;
                        firstTimeSet = true;
                    }
                    lastTime = ts;
                }
            };

            mesgBroadcaster.SessionMesgEvent += (sender, e) =>
            {
                var msg = new SessionMesg(e.mesg);
                if (msg.GetTotalDistance() != null)
                    result.Stats.DistanceMeters = (double)msg.GetTotalDistance()!;
                
                if (msg.GetTotalAscent() != null)
                    result.Stats.ElevationGainMeters = (double)msg.GetTotalAscent()!;

                if (msg.GetAvgSpeed() != null)
                    result.Stats.AvgSpeedKph = (double)msg.GetAvgSpeed()! * 3.6; // m/s to kph
            };

            decode.MesgEvent += mesgBroadcaster.OnMesg;
            decode.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;

            var isValid = decode.CheckIntegrity(fitStream);
            fitStream.Position = 0;
            
            decode.Read(fitStream);

            if (firstTimeSet)
            {
                result.Stats.DurationSeconds = lastTime - firstTime;
            }

            // Calculate Reached Nodes
            foreach (var node in availableNodes)
            {
                if (node.Lat == null || node.Lon == null) continue;

                bool isReached = false;
                foreach (var point in result.Path)
                {
                    if (GetDistance(node.Lat.Value, node.Lon.Value, point.Lat, point.Lon) <= 30) // 30 meters
                    {
                        isReached = true;
                        break;
                    }
                }

                if (isReached)
                {
                    result.NewlyCheckedNodes.Add(new NewlyCheckedNode
                    {
                        Id = node.Id,
                        ApLocationId = node.ApLocationId,
                        Lat = node.Lat.Value,
                        Lon = node.Lon.Value
                    });
                }
            }

            return result;
        }

        private static double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var r = 6371e3;
            var p1 = lat1 * Math.PI / 180;
            var p2 = lat2 * Math.PI / 180;
            var dp = (lat2 - lat1) * Math.PI / 180;
            var dl = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(dp / 2) * Math.Sin(dp / 2) +
                    Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return r * c;
        }
    }
}
