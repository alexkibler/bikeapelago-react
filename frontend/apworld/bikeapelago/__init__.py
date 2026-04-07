from BaseClasses import Region
from worlds.AutoWorld import WebWorld, World
from worlds.generic.Rules import set_rule

from .items import BikeapelagoItem, item_table, MAX_CHECKS, LOCATION_SWAP_RATIO
from .locations import BikeapelagoLocation, location_table
from .options import BikeapelagoOptions


class BikeapelagoWeb(WebWorld):
    theme = "ocean"
    setup_en = """
# Bikeapelago Archipelago Setup

1. Place `bikeapelago.apworld` in your Archipelago `worlds/` directory.
2. Create a YAML for your slot:
   ```yaml
   game: Bikeapelago
   name: YourSlotName
   Bikeapelago:
     check_count: 50
     goal_type: all_intersections
     starting_nodes: 3
   ```
3. Generate a multiworld seed on the Archipelago website.
4. Deploy the Bikeapelago web client, create a game session, and connect.
"""


class BikeapelagoWorld(World):
    """
    Bikeapelago is a web client that uses real-world cycling intersections as Archipelago
    Locations. Players receive Node Unlock items that reveal new intersections on the map.
    Complete intersections by riding to them IRL and uploading a FIT file.
    """

    game = "Bikeapelago"
    web = BikeapelagoWeb()

    # Static ID mappings — exclude event locations (code=None) and Victory from location map
    item_name_to_id = {
        name: data.code
        for name, data in item_table.items()
        if data.code is not None and name != "Victory"
    }
    location_name_to_id = {
        name: data.code
        for name, data in location_table.items()
        if data.code is not None
    }

    # Allows set_rules to count any "Node Unlock X" item with has_group()
    item_name_groups = {
        "Node Unlock": {f"Node Unlock {i}" for i in range(1, MAX_CHECKS + 1)},
        "Location Swap": {"Location Swap"},
    }

    options_dataclass = BikeapelagoOptions

    def create_items(self) -> None:
        check_count = self.options.check_count.value
        starting_nodes = self.options.starting_nodes.value
        num_swaps = int(check_count * LOCATION_SWAP_RATIO)
        for i in range(1, starting_nodes + 1):
            self.multiworld.push_precollected(self.create_item(f"Node Unlock {i}"))
        # Pool must cover all check_count + num_swaps locations; starting_nodes slots are filled with
        # Wheel Patch Kits so the counts balance.
        for i in range(starting_nodes + 1, check_count + 1):
            self.multiworld.itempool.append(self.create_item(f"Node Unlock {i}"))
        for _ in range(num_swaps):
            self.multiworld.itempool.append(self.create_item("Location Swap"))
        for _ in range(starting_nodes):
            self.multiworld.itempool.append(self.create_item("Wheel Patch Kit"))

    def create_regions(self) -> None:
        check_count = self.options.check_count.value
        num_swaps = int(check_count * LOCATION_SWAP_RATIO)
        total_locations = check_count + num_swaps

        menu_region = Region("Menu", self.player, self.multiworld)
        self.multiworld.regions.append(menu_region)

        map_region = Region("Map", self.player, self.multiworld)
        menu_region.connect(map_region)
        self.multiworld.regions.append(map_region)

        for i in range(1, total_locations + 1):
            loc_name = f"Intersection {i}"
            loc = BikeapelagoLocation(
                self.player, loc_name, self.location_name_to_id[loc_name], map_region
            )
            map_region.locations.append(loc)

        # Goal is an event location (code=None); Victory item will be locked here
        goal_loc = BikeapelagoLocation(self.player, "Goal", None, map_region)
        map_region.locations.append(goal_loc)

    def set_rules(self) -> None:
        check_count = self.options.check_count.value
        goal_type = self.options.goal_type.value

        if goal_type == 0:  # all_intersections
            required = check_count
        else:  # percentage — require 70 % of checks
            required = max(1, int(check_count * 0.7))

        goal_loc = self.multiworld.get_location("Goal", self.player)
        set_rule(
            goal_loc,
            lambda state: state.has_group("Node Unlock", self.player, required),
        )

        self.multiworld.completion_condition[self.player] = (
            lambda state: state.has("Victory", self.player)
        )

    def generate_basic(self) -> None:
        # Place the locked Victory item at the Goal event location
        goal_loc = self.multiworld.get_location("Goal", self.player)
        goal_loc.place_locked_item(self.create_item("Victory"))

    def get_filler_item_name(self) -> str:
        return "Wheel Patch Kit"

    def fill_slot_data(self) -> dict:
        return {
            "check_count": self.options.check_count.value,
            "starting_nodes": self.options.starting_nodes.value,
            "goal_type": self.options.goal_type.value,
        }

    def create_item(self, name: str) -> BikeapelagoItem:
        item_data = item_table[name]
        return BikeapelagoItem(
            name, item_data.classification, item_data.code, self.player
        )
