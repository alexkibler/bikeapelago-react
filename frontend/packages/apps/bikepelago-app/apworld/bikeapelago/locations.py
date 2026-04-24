from typing import Dict, NamedTuple
from BaseClasses import Location

class LocationData(NamedTuple):
    code: int

class BikeapelagoLocation(Location):
    game: str = "Bikeapelago"

MAX_CHECKS = 1000 # 1000 nodes * 2 checks = 2000 locations
START_ID = 800000

location_table: Dict[str, LocationData] = {}

for i in range(1, MAX_CHECKS + 1):
    location_table[f"Node {i} Arrival"] = LocationData(START_ID + (2 * i - 1))
    location_table[f"Node {i} Precision"] = LocationData(START_ID + (2 * i))

location_table["Goal"] = LocationData(None)
