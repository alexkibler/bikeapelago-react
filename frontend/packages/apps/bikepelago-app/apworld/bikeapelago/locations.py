from typing import Dict, NamedTuple

from BaseClasses import Location


class LocationData(NamedTuple):
    code: int


class BikeapelagoLocation(Location):
    game: str = "Bikeapelago"


# Similar to items, we need a static location_name_to_id mapping.
MAX_CHECKS = 2000
START_ID = 800000

location_table: Dict[str, LocationData] = {
    f"Intersection {i}": LocationData(START_ID + i)
    for i in range(1, MAX_CHECKS + 1)
}
# Event location — no code (not reported to the multiworld server as a check)
location_table["Goal"] = LocationData(None)
