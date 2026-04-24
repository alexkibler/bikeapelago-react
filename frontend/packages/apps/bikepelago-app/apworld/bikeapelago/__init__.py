from typing import List, Dict, Set
from BaseClasses import Region, Entrance, ItemClassification
from worlds.AutoWorld import WebWorld, World
from worlds.generic.Rules import set_rule

from .items import BikeapelagoItem, item_table, MAX_CHECKS
from .locations import BikeapelagoLocation, location_table
from .options import BikeapelagoOptions

class BikeapelagoWeb(WebWorld):
    theme = "ocean"

class BikeapelagoWorld(World):
    game = "Bikeapelago"
    web = BikeapelagoWeb()

    item_name_to_id = {name: data.code for name, data in item_table.items() if data.code is not None and name != "Victory"}
    location_name_to_id = {name: data.code for name, data in location_table.items() if data.code is not None}

    options_dataclass = BikeapelagoOptions

    def create_items(self) -> None:
        node_count = self.options.check_count.value
        total_locations = node_count * 2
        items_created = 0

        # 1. Progression Items
        mode = self.options.progression_mode.value
        if mode == 0: # quadrant
            for p in ["North Quadrant Pass", "East Quadrant Pass", "South Quadrant Pass", "West Quadrant Pass"]:
                self.multiworld.itempool.append(self.create_item(p))
                items_created += 1
        elif mode == 1: # radius
            for _ in range(3):
                self.multiworld.itempool.append(self.create_item("Progressive Radius Increase"))
                items_created += 1
        elif mode == 2: # free
            for i in range(1, node_count + 1):
                self.multiworld.itempool.append(self.create_item(f"Node {i} Reveal"))
                items_created += 1

        # 2. Useful Items (~20% of total checks)
        useful_target = int(total_locations * 0.20)
        useful_pool = ["Detour", "Drone", "Signal Amplifier"]
        for _ in range(useful_target):
            self.multiworld.itempool.append(self.create_item(self.random.choice(useful_pool)))
            items_created += 1

        # 3. Filler Items
        filler_pool = ["Fresh Air", "Leg Cramp", "Empty CO2 Cartridge", "Kudos", "Sense of Accomplishment", "Sunburn"]
        remaining = total_locations - items_created
        for _ in range(remaining):
            self.multiworld.itempool.append(self.create_item(self.random.choice(filler_pool)))

    def create_regions(self) -> None:
        node_count = self.options.check_count.value
        mode = self.options.progression_mode.value

        menu = Region("Menu", self.player, self.multiworld)
        hub = Region("Hub", self.player, self.multiworld)
        self.multiworld.regions += [menu, hub]
        menu.connect(hub)

        if mode == 0: # quadrant
            regions = {
                "North": Region("North Quadrant", self.player, self.multiworld),
                "East": Region("East Quadrant", self.player, self.multiworld),
                "South": Region("South Quadrant", self.player, self.multiworld),
                "West": Region("West Quadrant", self.player, self.multiworld)
            }
            self.multiworld.regions += list(regions.values())
            
            for name, reg in regions.items():
                ent = Entrance(self.player, f"Hub to {name}", hub)
                hub.exits.append(ent)
                ent.connect(reg)

            # Distribute nodes to regions (20% Hub, 20% each quadrant)
            hub_nodes = int(node_count * 0.20)
            nodes_per_quad = (node_count - hub_nodes) // 4
            
            current_node = 1
            # Hub nodes
            for _ in range(hub_nodes):
                self.add_node_to_region(current_node, hub)
                current_node += 1
            # Quadrant nodes
            for name, reg in regions.items():
                for _ in range(nodes_per_quad):
                    if current_node > node_count: break
                    self.add_node_to_region(current_node, reg)
                    current_node += 1
            # Any remainder to Hub
            while current_node <= node_count:
                self.add_node_to_region(current_node, hub)
                current_node += 1
        else:
            # Radius/Free mode: all in Hub region but logic-gated
            for i in range(1, node_count + 1):
                self.add_node_to_region(i, hub)

        # Goal
        goal_reg = Region("Goal Region", self.player, self.multiworld)
        self.multiworld.regions.append(goal_reg)
        hub.connect(goal_reg, "Reach Goal")
        goal_loc = BikeapelagoLocation(self.player, "Goal", None, goal_reg)
        goal_reg.locations.append(goal_loc)

    def add_node_to_region(self, node_num: int, region: Region):
        arrival = BikeapelagoLocation(self.player, f"Node {node_num} Arrival", self.location_name_to_id[f"Node {node_num} Arrival"], region)
        precision = BikeapelagoLocation(self.player, f"Node {node_num} Precision", self.location_name_to_id[f"Node {node_num} Precision"], region)
        region.locations += [arrival, precision]

    def set_rules(self) -> None:
        mode = self.options.progression_mode.value
        
        if mode == 0: # quadrant
            self.multiworld.get_entrance("Hub to North", self.player).access_rule = lambda state: state.has("North Quadrant Pass", self.player)
            self.multiworld.get_entrance("Hub to East", self.player).access_rule = lambda state: state.has("East Quadrant Pass", self.player)
            self.multiworld.get_entrance("Hub to South", self.player).access_rule = lambda state: state.has("South Quadrant Pass", self.player)
            self.multiworld.get_entrance("Hub to West", self.player).access_rule = lambda state: state.has("West Quadrant Pass", self.player)
        elif mode == 2: # free
            for i in range(1, self.options.check_count.value + 1):
                rule = lambda state, n=i: state.has(f"Node {n} Reveal", self.player)
                set_rule(self.multiworld.get_location(f"Node {i} Arrival", self.player), rule)
                set_rule(self.multiworld.get_location(f"Node {i} Precision", self.player), rule)

        # Goal logic
        goal_loc = self.multiworld.get_location("Goal", self.player)
        goal_loc.access_rule = lambda state: state.can_reach_location("Node 1 Arrival", "Location") # Placeholder, usually needs a percentage

        self.multiworld.completion_condition[self.player] = lambda state: state.has("Victory", self.player)

    def generate_basic(self) -> None:
        self.multiworld.get_location("Goal", self.player).place_locked_item(self.create_item("Victory"))

    def fill_slot_data(self) -> dict:
        return {
            "progression_mode": self.options.progression_mode.value,
            "node_count": self.options.check_count.value
        }

    def create_item(self, name: str) -> BikeapelagoItem:
        data = item_table[name]
        return BikeapelagoItem(name, data.classification, data.code, self.player)
