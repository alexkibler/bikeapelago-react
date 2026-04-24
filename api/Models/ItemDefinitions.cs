using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bikeapelago.Api.Models;

public enum ItemType
{
    Progression,
    Useful,
    Filler,
    NodeReveal
}

[AttributeUsage(AttributeTargets.Field)]
public class ItemTypeAttribute(ItemType type) : Attribute
{
    public ItemType Type { get; } = type;
}

public static class ItemDefinitions
{
    public const long StartId = 800000;

    [ItemType(ItemType.Progression)]
    public const long Macguffin = 802001;

    [ItemType(ItemType.Progression)]
    public const long NorthPass = 802002;

    [ItemType(ItemType.Progression)]
    public const long SouthPass = 802003;

    [ItemType(ItemType.Progression)]
    public const long EastPass = 802004;

    [ItemType(ItemType.Progression)]
    public const long WestPass = 802005;

    [ItemType(ItemType.Progression)]
    public const long ProgressiveRadiusIncrease = 802006;

    [ItemType(ItemType.Useful)]
    public const long Detour = 802010;

    [ItemType(ItemType.Useful)]
    public const long Drone = 802011;

    [ItemType(ItemType.Useful)]
    public const long SignalAmplifier = 802012;

    [ItemType(ItemType.Filler)]
    public const long FreshAir = 802020;

    [ItemType(ItemType.Filler)]
    public const long LegCramp = 802021;

    [ItemType(ItemType.Filler)]
    public const long EmptyCo2 = 802022;

    [ItemType(ItemType.Filler)]
    public const long Kudos = 802023;

    [ItemType(ItemType.Filler)]
    public const long WheelPatchKit = 802024;

    public static readonly Dictionary<long, string> ItemNames = new()
    {
        { Macguffin, "Macguffin" },
        { NorthPass, "North Quadrant Pass" },
        { SouthPass, "South Quadrant Pass" },
        { EastPass, "East Quadrant Pass" },
        { WestPass, "West Quadrant Pass" },
        { ProgressiveRadiusIncrease, "Progressive Radius Increase" },
        { Detour, "Detour" },
        { Drone, "Drone" },
        { SignalAmplifier, "Signal Amplifier" },
        { FreshAir, "Fresh Air" },
        { LegCramp, "Leg Cramp" },
        { EmptyCo2, "Empty CO2" },
        { Kudos, "Kudos" },
        { WheelPatchKit, "Wheel Patch Kit" }
    };

    private static readonly Dictionary<long, ItemType> _idToTypeCache;

    static ItemDefinitions()
    {
        _idToTypeCache = new Dictionary<long, ItemType>();
        var fields = typeof(ItemDefinitions).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly); // Get constants

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ItemTypeAttribute>();
            if (attr != null && field.GetValue(null) is long id)
            {
                _idToTypeCache[id] = attr.Type;
            }
        }
    }

    public static string GetItemName(long itemId)
    {
        if (ItemNames.TryGetValue(itemId, out var name))
        {
            return name;
        }

        if (itemId > StartId && itemId <= StartId + 2000)
        {
            return $"Node Reveal {itemId - StartId}";
        }

        return $"Item {itemId}";
    }

    public static ItemType GetItemType(long itemId)
    {
        if (_idToTypeCache.TryGetValue(itemId, out var type))
        {
            return type;
        }

        if (itemId > StartId && itemId <= StartId + 2000)
        {
            return ItemType.NodeReveal;
        }

        return ItemType.Filler;
    }

    public static IEnumerable<long> GetItemsByType(ItemType type)
    {
        return _idToTypeCache.Where(kvp => kvp.Value == type).Select(kvp => kvp.Key);
    }

    public static IEnumerable<long> GetProgressionItems() => GetItemsByType(ItemType.Progression);
    public static IEnumerable<long> GetUsefulItems() => GetItemsByType(ItemType.Useful);
    public static IEnumerable<long> GetFillerItems() => GetItemsByType(ItemType.Filler);
}
