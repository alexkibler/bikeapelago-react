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

    // Accepts IEnumerable<long> so callers may pass either a HashSet<long> (O(1) Contains)
    // for normal use, or a List<long> (supports duplicates) for sphere-mapping simulations
    // where duplicate item IDs (e.g. multiple ProgressiveRadiusIncrease) must be counted.
    public List<MapNode> GetReachableNodes(IEnumerable<MapNode> allNodes, IEnumerable<long> inventory)
    {
        // Materialise once so we can query it multiple ways efficiently.
        var inventoryList = inventory as IList<long> ?? inventory.ToList();
        var inventorySet  = inventoryList as ISet<long> ?? inventoryList.ToHashSet();

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

                if (tag == "North" && inventorySet.Contains(ItemDefinitions.NorthPass)) reachable.Add(node);
                else if (tag == "East" && inventorySet.Contains(ItemDefinitions.EastPass)) reachable.Add(node);
                else if (tag == "South" && inventorySet.Contains(ItemDefinitions.SouthPass)) reachable.Add(node);
                else if (tag == "West" && inventorySet.Contains(ItemDefinitions.WestPass)) reachable.Add(node);
            }
            else if (_progressionMode == "radius")
            {
                // Use inventoryList so duplicate IDs are counted correctly.
                int increases = inventoryList.Count(i => i == ItemDefinitions.ProgressiveRadiusIncrease);
                double allowedRadius = _hubRadius * (increases + 1);

                if (dist <= allowedRadius)
                {
                    reachable.Add(node);
                }
            }
            else if (_progressionMode == "free")
            {
                long revealId = ItemDefinitions.StartId + (node.ApArrivalLocationId - ItemDefinitions.StartId);
                if (inventorySet.Contains(revealId))
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

        // ── Bucket the item deck ──────────────────────────────────────────────
        // strictProgressionItems: passes, radius increases, node reveals — go through Assumed Fill.
        // macguffinItems:         placed last-in-first-out by sphere depth (Phase 3).
        // fillerItems:            randomly scattered into remaining slots (Phase 4).
        var strictProgressionItems = new List<long>();
        var macguffinItems         = new List<long>();
        var fillerItems            = new List<long>();

        foreach (var item in itemDeck)
        {
            if (item == ItemDefinitions.Macguffin)
                macguffinItems.Add(item);
            else if (ItemDefinitions.GetItemType(item) == ItemType.Progression ||
                     ItemDefinitions.GetItemType(item) == ItemType.NodeReveal)
                strictProgressionItems.Add(item);
            else
                fillerItems.Add(item);
        }

        var rng = new Random();
        strictProgressionItems = strictProgressionItems.OrderBy(_ => rng.Next()).ToList();
        fillerItems            = fillerItems.OrderBy(_ => rng.Next()).ToList();

        // ── Phase 1: Forward Fill (strict progression items only) ─────────────
        // Place each progression item only in locations reachable with items already
        // placed before it. This prevents pass/radius/reveal cycles that are valid
        // under assumed-fill simulation but impossible from an empty inventory.
        _logger.LogInformation("Phase 1 - Forward Fill: Placing {Count} progression item(s).", strictProgressionItems.Count);

        var placementInventory = new List<long>();
        foreach (var itemToPlace in strictProgressionItems)
        {
            var reachableNodes = evaluator.GetReachableNodes(nodes, placementInventory);

            var emptySlots = new List<(MapNode Node, bool IsArrival)>();
            foreach (var node in reachableNodes)
            {
                if (!node.ArrivalRewardItemId.HasValue)   emptySlots.Add((node, true));
                if (!node.PrecisionRewardItemId.HasValue) emptySlots.Add((node, false));
            }

            if (emptySlots.Count == 0)
            {
                _logger.LogError("Phase 1 Failed! No empty reachable slots for item {Item}", itemToPlace);
                throw new Exception("Seed generation failed due to logic softlock.");
            }

            var slot = emptySlots[rng.Next(emptySlots.Count)];
            if (slot.IsArrival)
            {
                slot.Node.ArrivalRewardItemId   = itemToPlace;
                slot.Node.ArrivalRewardItemName = ItemDefinitions.GetItemName(itemToPlace);
            }
            else
            {
                slot.Node.PrecisionRewardItemId   = itemToPlace;
                slot.Node.PrecisionRewardItemName = ItemDefinitions.GetItemName(itemToPlace);
            }

            placementInventory.Add(itemToPlace);
        }

        // ── Phase 2: Forward Playthrough (Sphere Mapping) ─────────────────────
        // Walk through the graph exactly as a player would, collecting progression
        // items as they become reachable, and record the sphere level of every node.
        // Uses a List<long> inventory so duplicate item IDs (e.g. radius increases)
        // are counted correctly during the simulation.
        _logger.LogInformation("Phase 2 - Sphere Mapping: Calculating logic spheres.");

        var nodeSphereLevel  = new Dictionary<Guid, int>();
        var sphereInventory  = new List<long>();   // allows duplicates for correct Count() queries
        var processedNodeIds = new HashSet<Guid>();
        int currentSphere    = 0;

        while (true)
        {
            var reachable = evaluator.GetReachableNodes(nodes, sphereInventory);
            var newNodes  = reachable.Where(n => !processedNodeIds.Contains(n.Id)).ToList();

            if (newNodes.Count == 0) break;

            foreach (var node in newNodes)
            {
                nodeSphereLevel[node.Id] = currentSphere;
                processedNodeIds.Add(node.Id);

                // Collect progression items found in this sphere.
                // Macguffins are intentionally excluded — they do not unlock new areas.
                if (node.ArrivalRewardItemId.HasValue && node.ArrivalRewardItemId.Value != ItemDefinitions.Macguffin)
                {
                    var t = ItemDefinitions.GetItemType(node.ArrivalRewardItemId.Value);
                    if (t == ItemType.Progression || t == ItemType.NodeReveal)
                        sphereInventory.Add(node.ArrivalRewardItemId.Value);
                }
                if (node.PrecisionRewardItemId.HasValue && node.PrecisionRewardItemId.Value != ItemDefinitions.Macguffin)
                {
                    var t = ItemDefinitions.GetItemType(node.PrecisionRewardItemId.Value);
                    if (t == ItemType.Progression || t == ItemType.NodeReveal)
                        sphereInventory.Add(node.PrecisionRewardItemId.Value);
                }
            }

            currentSphere++;
        }

        _logger.LogInformation(
            "Phase 2 Complete: Mapped {NodeCount} node(s) across {Spheres} sphere(s).",
            nodeSphereLevel.Count, currentSphere);

        // ── Phase 3: Late-Sphere Macguffin Placement ──────────────────────────
        // Gather every still-empty slot, annotate with its node's sphere level,
        // then sort deepest-first so Macguffins land in the hardest-to-reach locations.
        _logger.LogInformation("Phase 3 - Macguffin Placement: Placing {Count} Macguffin(s).", macguffinItems.Count);

        var emptySlotsBySphere = new List<(MapNode Node, bool IsArrival, int Sphere)>();
        foreach (var node in nodes)
        {
            int sphere = nodeSphereLevel.TryGetValue(node.Id, out var s) ? s : 0;
            if (!node.ArrivalRewardItemId.HasValue)   emptySlotsBySphere.Add((node, true,  sphere));
            if (!node.PrecisionRewardItemId.HasValue) emptySlotsBySphere.Add((node, false, sphere));
        }

        // Descending sphere level — shuffle within same level for variety.
        emptySlotsBySphere = emptySlotsBySphere
            .OrderByDescending(e => e.Sphere)
            .ThenBy(_ => rng.Next())
            .ToList();

        // At most one Macguffin per node — skip the second slot of any node that already
        // received one, so Macguffins spread across as many distinct nodes as possible.
        var macguffinNodeIds = new HashSet<Guid>();
        int macguffinPlaced  = 0;

        for (int i = 0; macguffinPlaced < macguffinItems.Count && i < emptySlotsBySphere.Count; i++)
        {
            var (node, isArrival, _) = emptySlotsBySphere[i];

            if (macguffinNodeIds.Contains(node.Id))
                continue;

            if (isArrival)
            {
                node.ArrivalRewardItemId   = macguffinItems[macguffinPlaced];
                node.ArrivalRewardItemName = ItemDefinitions.GetItemName(macguffinItems[macguffinPlaced]);
            }
            else
            {
                node.PrecisionRewardItemId   = macguffinItems[macguffinPlaced];
                node.PrecisionRewardItemName = ItemDefinitions.GetItemName(macguffinItems[macguffinPlaced]);
            }

            macguffinNodeIds.Add(node.Id);
            macguffinPlaced++;
        }

        // ── Phase 4: Fast Fill ─────────────────────────────────────────────────
        _logger.LogInformation("Phase 4 - Fast Fill: Placing {Count} filler item(s).", fillerItems.Count);
        int fillerIndex = 0;

        foreach (var node in nodes)
        {
            if (!node.ArrivalRewardItemId.HasValue && fillerIndex < fillerItems.Count)
            {
                node.ArrivalRewardItemId   = fillerItems[fillerIndex];
                node.ArrivalRewardItemName = ItemDefinitions.GetItemName(fillerItems[fillerIndex]);
                fillerIndex++;
            }
            if (!node.PrecisionRewardItemId.HasValue && fillerIndex < fillerItems.Count)
            {
                node.PrecisionRewardItemId   = fillerItems[fillerIndex];
                node.PrecisionRewardItemName = ItemDefinitions.GetItemName(fillerItems[fillerIndex]);
                fillerIndex++;
            }
        }
    }
}
