using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

public static class ItemPoolFactory
{
    public static List<long> GenerateItemPool(List<MapNode> nodes, string? progressionMode, GameSession session)
    {
        var pool = new List<long>();
        var totalCapacity = nodes.Count * 2;

        // 1. Macguffin Items (Triforce Hunt win condition)
        int totalMacguffins = (int)Math.Ceiling(totalCapacity * 0.15);
        int macguffinsRequired = (int)Math.Ceiling(totalMacguffins * 0.80);
        session.MacguffinsRequired = macguffinsRequired;
        for (int i = 0; i < totalMacguffins; i++)
        {
            pool.Add(ItemDefinitions.Macguffin);
        }

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
            pool.Add(usefulItems[Random.Shared.Next(usefulItems.Length)]);
        }

        // 3. Filler Items (fill exact remaining capacity)
        var fillers = ItemDefinitions.GetFillerItems().ToArray();
        while (pool.Count < totalCapacity)
        {
            pool.Add(fillers[Random.Shared.Next(fillers.Length)]);
        }

        // Shuffle the pool before returning it
        Random.Shared.Shuffle(CollectionsMarshal.AsSpan(pool));
        return pool;
    }
}
