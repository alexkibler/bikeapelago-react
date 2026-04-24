using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public class ReachabilityEvaluator
{
    private readonly double _centerLat;
    private readonly double _centerLon;
    private readonly double _maxRadius;
    private readonly double _hubRadius;
    private readonly string? _progressionMode;

    public ReachabilityEvaluator(double centerLat, double centerLon, double maxRadius, string? progressionMode)
    {
        _centerLat = centerLat;
        _centerLon = centerLon;
        _maxRadius = maxRadius;
        _hubRadius = maxRadius * 0.25;
        _progressionMode = progressionMode;
    }

    public List<MapNode> GetReachableNodes(IEnumerable<MapNode> allNodes, HashSet<long> inventory)
    {
        var reachable = new List<MapNode>();

        foreach (var node in allNodes)
        {
            if (node.Lat == null || node.Lon == null) continue;

            double dist = CalculateDistance(_centerLat, _centerLon, node.Lat.Value, node.Lon.Value);

            // 1. Hub is always reachable
            if (dist <= _hubRadius)
            {
                reachable.Add(node);
                continue;
            }

            // 2. Progression evaluation
            if (_progressionMode == "quadrant")
            {
                double az = CalculateAzimuth(_centerLat, _centerLon, node.Lat.Value, node.Lon.Value);
                string tag = GetRegionTag(az);

                if (tag == "North" && inventory.Contains(ItemDefinitions.NorthPass)) reachable.Add(node);
                else if (tag == "East" && inventory.Contains(ItemDefinitions.EastPass)) reachable.Add(node);
                else if (tag == "South" && inventory.Contains(ItemDefinitions.SouthPass)) reachable.Add(node);
                else if (tag == "West" && inventory.Contains(ItemDefinitions.WestPass)) reachable.Add(node);
            }
            else if (_progressionMode == "radius")
            {
                int increases = inventory.Count(i => i == ItemDefinitions.ProgressiveRadiusIncrease);
                double allowedRadius = _hubRadius * (increases + 1);
                
                if (dist <= allowedRadius)
                {
                    reachable.Add(node);
                }
            }
            else if (_progressionMode == "free")
            {
                long revealId = ItemDefinitions.StartId + (node.ApArrivalLocationId - ItemDefinitions.StartId);
                if (inventory.Contains(revealId))
                {
                    reachable.Add(node);
                }
            }
        }

        return reachable;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double r = 6371000;
        double phi1 = lat1 * Math.PI / 180;
        double phi2 = lat2 * Math.PI / 180;
        double dphi = (lat2 - lat1) * Math.PI / 180;
        double dlambda = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dphi / 2) * Math.Sin(dphi / 2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dlambda / 2) * Math.Sin(dlambda / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double CalculateAzimuth(double lat1, double lon1, double lat2, double lon2)
    {
        double lat1Rad = lat1 * Math.PI / 180.0;
        double lat2Rad = lat2 * Math.PI / 180.0;
        double dLonRad = (lon2 - lon1) * Math.PI / 180.0;

        double y = Math.Sin(dLonRad) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLonRad);
        double brng = Math.Atan2(y, x);
        return (brng * 180.0 / Math.PI + 360.0) % 360.0;
    }

    private static string GetRegionTag(double az)
    {
        if (az >= 315 || az < 45) return "North";
        if (az >= 45 && az < 135) return "East";
        if (az >= 135 && az < 225) return "South";
        return "West";
    }
}

public class SinglePlayerSeedGenerator(ILogger<SinglePlayerSeedGenerator> logger)
{
    private readonly ILogger<SinglePlayerSeedGenerator> _logger = logger;

    public void GenerateSeed(GameSession session, List<MapNode> nodes)
    {
        if (session.CenterLat == null || session.CenterLon == null || session.Radius == null)
            throw new Exception("Session missing required geographic metrics for generation.");

        var evaluator = new ReachabilityEvaluator(
            session.CenterLat.Value, 
            session.CenterLon.Value, 
            session.Radius.Value, 
            session.ProgressionMode);

        var itemDeck = ItemPoolFactory.GenerateItemPool(nodes, session.ProgressionMode, session);
        
        // Separate progression items from filler/useful
        var progressionItemsInDeck = new List<long>();
        var nonProgressionItems = new List<long>();

        foreach (var item in itemDeck)
        {
            if (ItemDefinitions.GetItemType(item) == ItemType.Progression || ItemDefinitions.GetItemType(item) == ItemType.NodeReveal)
            {
                progressionItemsInDeck.Add(item);
            }
            else
            {
                nonProgressionItems.Add(item);
            }
        }

        var rng = new Random();
        progressionItemsInDeck = progressionItemsInDeck.OrderBy(_ => rng.Next()).ToList();

        _logger.LogInformation("Assumed Fill: Placing {Count} progression items.", progressionItemsInDeck.Count);

        // 1. Assumed Fill
        foreach (var itemToPlace in progressionItemsInDeck)
        {
            // Simulated inventory: everything EXCEPT the item we are currently trying to place
            var simulatedInventory = new HashSet<long>(progressionItemsInDeck.Where(i => i != itemToPlace));

            // Find all nodes reachable with that simulated inventory
            var reachableNodes = evaluator.GetReachableNodes(nodes, simulatedInventory);

            // Find an empty slot in those reachable nodes
            var emptySlots = new List<(MapNode Node, bool IsArrival)>();
            foreach (var node in reachableNodes)
            {
                if (!node.ArrivalRewardItemId.HasValue) emptySlots.Add((node, true));
                if (!node.PrecisionRewardItemId.HasValue) emptySlots.Add((node, false));
            }

            if (emptySlots.Count == 0)
            {
                _logger.LogError("Assumed Fill Failed! No empty reachable slots for item {Item}", itemToPlace);
                throw new Exception("Seed generation failed due to logic softlock.");
            }

            // Pick a random slot
            var slot = emptySlots[rng.Next(emptySlots.Count)];

            if (slot.IsArrival)
            {
                slot.Node.ArrivalRewardItemId = itemToPlace;
                slot.Node.ArrivalRewardItemName = ItemDefinitions.GetItemName(itemToPlace);
            }
            else
            {
                slot.Node.PrecisionRewardItemId = itemToPlace;
                slot.Node.PrecisionRewardItemName = ItemDefinitions.GetItemName(itemToPlace);
            }

            // Once placed, we "remove" it from our list of items left to place,
            // which effectively adds it permanently to our base assumed inventory
            // for subsequent iterations. (We do this implicitly by continuing the loop).
        }

        // 2. Fast Fill
        _logger.LogInformation("Fast Fill: Placing {Count} remaining items.", nonProgressionItems.Count);
        int fillerIndex = 0;

        foreach (var node in nodes)
        {
            if (!node.ArrivalRewardItemId.HasValue && fillerIndex < nonProgressionItems.Count)
            {
                long item = nonProgressionItems[fillerIndex++];
                node.ArrivalRewardItemId = item;
                node.ArrivalRewardItemName = ItemDefinitions.GetItemName(item);
            }
            if (!node.PrecisionRewardItemId.HasValue && fillerIndex < nonProgressionItems.Count)
            {
                long item = nonProgressionItems[fillerIndex++];
                node.PrecisionRewardItemId = item;
                node.PrecisionRewardItemName = ItemDefinitions.GetItemName(item);
            }
        }
    }
}
