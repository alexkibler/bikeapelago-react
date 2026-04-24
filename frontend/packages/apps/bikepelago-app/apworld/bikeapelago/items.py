from typing import Dict, NamedTuple
from BaseClasses import Item, ItemClassification

class ItemData(NamedTuple):
    code: int
    classification: ItemClassification

class BikeapelagoItem(Item):
    game: str = "Bikeapelago"

MAX_CHECKS = 2000
START_ID = 800000

# Base item table
item_table: Dict[str, ItemData] = {
    # Progression Items
    "North Quadrant Pass": ItemData(START_ID + 2001, ItemClassification.progression),
    "East Quadrant Pass": ItemData(START_ID + 2002, ItemClassification.progression),
    "South Quadrant Pass": ItemData(START_ID + 2003, ItemClassification.progression),
    "West Quadrant Pass": ItemData(START_ID + 2004, ItemClassification.progression),
    "Progressive Radius Increase": ItemData(START_ID + 2005, ItemClassification.progression),
    
    # Useful Items
    "Detour": ItemData(START_ID + 2010, ItemClassification.useful),
    "Drone": ItemData(START_ID + 2011, ItemClassification.useful),
    "Signal Amplifier": ItemData(START_ID + 2012, ItemClassification.useful),
    
    # Filler Items
    "Fresh Air": ItemData(START_ID + 2100, ItemClassification.filler),
    "Leg Cramp": ItemData(START_ID + 2101, ItemClassification.filler),
    "Empty CO2 Cartridge": ItemData(START_ID + 2102, ItemClassification.filler),
    "Kudos": ItemData(START_ID + 2103, ItemClassification.filler),
    "Sense of Accomplishment": ItemData(START_ID + 2104, ItemClassification.filler),
    "Sunburn": ItemData(START_ID + 2105, ItemClassification.filler),
    
    "Victory": ItemData(START_ID + 3000, ItemClassification.progression)
}

# Add dynamic Node Reveals
for i in range(1, MAX_CHECKS + 1):
    item_table[f"Node {i} Reveal"] = ItemData(START_ID + 4000 + i, ItemClassification.progression)

# For backward compatibility or internal reference
item_table["Wheel Patch Kit"] = ItemData(START_ID + 2106, ItemClassification.filler)
item_table["Location Swap"] = item_table["Detour"]
