using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

public static class ItemPoolFactory
{
    public static List<long> GenerateItemPool(List<MapNode> nodes, string? progressionMode)
    {
        var pool = new List<long>();
        var totalCapacity = nodes.Count * 2;
        var rng = new Random();

        // 1. Progression Items
        pool.Add(ItemDefinitions.Goal); // Always add the Goal item to the pool

        if (progressionMode == "quadrant")
        {
            pool.AddRange(new[] { ItemDefinitions.NorthPass, ItemDefinitions.SouthPass, ItemDefinitions.EastPass, ItemDefinitions.WestPass });
        }
        else if (progressionMode == "radius")
        {
            pool.AddRange(new[] { ItemDefinitions.ProgressiveRadiusIncrease, ItemDefinitions.ProgressiveRadiusIncrease, ItemDefinitions.ProgressiveRadiusIncrease });
        }
        else if (progressionMode == "free")
        {
            // Each node outside the hub requires a Reveal Item
            foreach (var node in nodes.Where(n => n.RegionTag != "Hub"))
            {
                long revealItemId = ItemDefinitions.StartId + (node.ApArrivalLocationId - ItemDefinitions.StartId);
                pool.Add(revealItemId);
            }
        }

        // 2. Useful Items (approx 20% of TOTAL CAPACITY, minus what we already have if we exceed it somehow)
        int usefulTarget = (int)(totalCapacity * 0.20);
        var usefulItems = ItemDefinitions.GetUsefulItems().ToArray();
        
        for (int i = 0; i < usefulTarget; i++)
        {
            if (pool.Count >= totalCapacity) break;
            pool.Add(usefulItems[rng.Next(usefulItems.Length)]);
        }

        // 3. Filler Items (fill exact remaining capacity)
        var fillers = ItemDefinitions.GetFillerItems().ToArray();
        while (pool.Count < totalCapacity)
        {
            pool.Add(fillers[rng.Next(fillers.Length)]);
        }

        // Shuffle the pool before returning it
        return pool.OrderBy(_ => rng.Next()).ToList();
    }
}
