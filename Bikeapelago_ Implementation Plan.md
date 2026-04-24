# **Bikeapelago: Item, Difficulty, and Progression Spheres Implementation Plan**

## **The Node Structure (Location Checks)**

To ensure the item pool has enough volume without excessive bloat, a single physical node contains two Archipelago location checks. 

* **Check 1: The Arrival (Standard):** The .fit parser confirms the GPS track passed within a wide MAXIMUM_RADIUS_METERS-meter radius of the node.  
* **Check 2: The Precision (Extra):** The parser confirms the track hit the MINIMUM_RADIUS_METERS bullseye. (Must be flagged as LocationProgressType.EXCLUDED in the APWorld so progression items are never locked behind strict routing constraints).

## **Items**

### **Progression Items:**

* **The Node (1:1 Reveals):** Used strictly in Free Mode. A 1:1 mapping where receiving the specific item unlocks the specific node on the Mapbox UI.  
* **Quadrant Pass:** Used in Quadrant Mode. Allows access to nodes in a specific cardinal wedge. Consists of North, East, West, and South Quadrant Passes.  
* **Progressive Radius Increase:** Used in Radius Mode. The player starts able to access nodes within 25% of the maximum radius. These items increase the total distance from the start point that nodes will be accessible (3 total items: 50%, 75%, and 100% of total radius).

### **Useful Items:**

*Note: The total amount of Useful Items generated per seed scales dynamically based on the total node count (e.g., ~20% of the total Location checks) to ensure consistent pacing regardless of seed length.*

* **The Detour:** Lets the player pick a node they don't want to do and swap it with a random node. *Architecture Note:* Backup nodes are NOT pre-generated. The .NET backend dynamically generates a new coordinate via PostGIS that matches the original node's logic constraints (e.g., must be in the North Quadrant). The database overwrites the physical coordinates but retains the original Archipelago Location ID to prevent softlocks.  
* **The Drone:** Lets the player instantly complete both the Arrival and Precision checks for a specific node from the UI without physically riding to it. Weighted to be rare.  
* **The Signal Amplifier:** Temporarily multiplies the radius of checks by 2 for a single uploaded .fit file. Stacks.

### **Filler Item:**

* **Fresh Air**  
* **Leg Cramp**  
* **Empty CO2 Cartridge**  
* **Kudos**  
* **Sense of Accomplishment**  
* **Sunburn**

## **Progression Modes**

* **Quadrant Mode:** Player starts out with 25% of the total radius unlocked (The Hub), and nodes will appear within this radius. The player will unlock Pass items for each cardinal direction, which will unlock the full 100% max radius in a wedge for that specific direction.  
* **Radius Mode:** Player starts with 25% of total radius unlocked (The Hub). Player will unlock Progressive Radius Increase items which will allow them to access nodes at 50%, 75%, and 100% of the total radius in all directions simultaneously.  
* **Free Mode:** Player can access nodes at 100% of the total radius from the start, but nodes are entirely hidden and must be unlocked one-by-one via their specific Node Reveal items.

## **Technical Addendum: APWorld Logic & Item Math**

Because Archipelago strictly enforces that Total Item Count == Total Location Count, the Python APWorld dynamically builds the pool based on the YAML inputs.  
Assuming a standard 50-node seed (100 Location Checks):

* **Quadrant Mode (100 Items):** 4 Passes, ~20 Useful Items, ~76 Filler.  
* **Radius Mode (100 Items):** 3 Radius Increases, ~20 Useful Items, ~77 Filler.  
* **Free Mode (100 Items):** 50 Node Reveals, ~20 Useful Items, ~30 Filler.

Here are the core Python and C# architectural templates to bring the implementation plan to life. These snippets cover the dynamic Archipelago generation, the geographic logic gating, the PostGIS spatial distribution, and the safe hot-swapping for the Detour item.

### **1. Archipelago Python (APWorld) Updates**

This handles the dynamic item scaling and the physical region logic for Quadrant Mode.

**create_items.py (Dynamic Item Scaling)**

```python
def create_items(self):
    # Total locations = user's requested nodes * 2 checks per node
    total_locations = self.options.node_count.value * 2 
    items_created = 0

    # 1. Progression Items (Branched by Mode)
    mode = self.options.progression_mode.value

    if mode == "quadrant":
        passes = ["North Quadrant Pass", "South Quadrant Pass", "East Quadrant Pass", "West Quadrant Pass"]
        for p in passes:
            self.multiworld.itempool.append(self.create_item(p))
            items_created += 1
    elif mode == "radius":
        for _ in range(3):
            self.multiworld.itempool.append(self.create_item("Progressive Radius Increase"))
            items_created += 1
    elif mode == "free":
        # Create a specific 1:1 reveal item for every single node
        for i in range(self.options.node_count.value):
            self.multiworld.itempool.append(self.create_item(f"Node {i+1} Reveal"))
            items_created += 1

    # 2. Useful Items (Scales dynamically to ~20% of total checks)
    target_useful_count = int(total_locations * 0.20)
    useful_pool = ["Detour", "Drone", "Signal Amplifier"]
    
    for _ in range(target_useful_count):
        item_name = self.random.choice(useful_pool)
        self.multiworld.itempool.append(self.create_item(item_name))
        items_created += 1

    # 3. Filler Items (Fills the exact remainder for AP math)
    remaining_capacity = total_locations - items_created
    for _ in range(remaining_capacity):
        self.multiworld.itempool.append(self.create_item("Fresh Air"))
```

**create_regions.py (Quadrant Logic Gates)**

```python
def create_regions(self):
    # 1. Create the base regions
    menu = Region("Menu", self.player, self.multiworld)
    hub = Region("Hub Zone", self.player, self.multiworld)
    north = Region("North Zone", self.player, self.multiworld)
    east = Region("East Zone", self.player, self.multiworld)
    south = Region("South Zone", self.player, self.multiworld)
    west = Region("West Zone", self.player, self.multiworld)

    self.multiworld.regions += [menu, hub, north, east, south, west]

    # 2. Connect Menu to Hub (Always accessible)
    menu.connect(hub)

    # 3. Create Entrances from Hub to the Quadrants
    hub_to_north = Entrance(self.player, "Hub to North", hub)
    hub_to_east = Entrance(self.player, "Hub to East", hub)
    hub_to_south = Entrance(self.player, "Hub to South", hub)
    hub_to_west = Entrance(self.player, "Hub to West", hub)

    hub_to_north.connect(north)
    hub_to_east.connect(east)
    hub_to_south.connect(south)
    hub_to_west.connect(west)

    hub.entrances += [hub_to_north, hub_to_east, hub_to_south, hub_to_west]

    # 4. Apply the Pass requirements to the Entrances
    self.multiworld.get_entrance("Hub to North", self.player).access_rule = \
        lambda state: state.has("North Quadrant Pass", self.player)

    self.multiworld.get_entrance("Hub to East", self.player).access_rule = \
        lambda state: state.has("East Quadrant Pass", self.player)
        
    self.multiworld.get_entrance("Hub to South", self.player).access_rule = \
        lambda state: state.has("South Quadrant Pass", self.player)
        
    self.multiworld.get_entrance("Hub to West", self.player).access_rule = \
        lambda state: state.has("West Quadrant Pass", self.player)
```
---

### **2. C# (.NET / PostGIS) Updates**

This handles the spatial map generation and the anti-softlock logic for The Detour.

**NodeGenerationService.cs (The Wedge Method)**

```csharp
public async Task GenerateSeedNodesAsync(int playerId, double originLat, double originLon, double maxRadiusMeters, int totalNodes)
{
    // 20% to the Hub, the remaining 80% split across 4 quadrants
    int hubNodeCount = (int)(totalNodes * 0.20);
    int nodesPerQuadrant = (totalNodes - hubNodeCount) / 4;
    double hubRadiusMeters = maxRadiusMeters * 0.25;

    // Use PostGIS to fetch random valid road geometries
    // Hub Query (Distance <= HubRadius)
    var hubNodes = await _dbContext.ValidRoads
        .Where(r => r.Geom.Distance(originGeom) <= hubRadiusMeters)
        .OrderBy(r => Guid.NewGuid()) // Random sample
        .Take(hubNodeCount)
        .ToListAsync();

    // North Query (Distance > HubRadius AND Azimuth between 315 and 45 degrees)
    // ST_Azimuth returns radians, so 315 deg = 5.49 rad, 45 deg = 0.78 rad
    var northNodes = await _dbContext.ValidRoads
        .Where(r => r.Geom.Distance(originGeom) > hubRadiusMeters 
                 && r.Geom.Distance(originGeom) <= maxRadiusMeters)
        .Where(r => r.Geom.Azimuth(originGeom) >= 5.49 || r.Geom.Azimuth(originGeom) <= 0.78)
        .OrderBy(r => Guid.NewGuid())
        .Take(nodesPerQuadrant)
        .ToListAsync();

    // Repeat queries for East, South, West using respective Radian boundaries.
    // Tag each node with its region string ("Hub", "North", etc.) and save to DB.
}
```

**ItemExecutionService.cs (The Detour Hot-Swap)**

```csharp
public async Task ExecuteDetourAsync(int playerId, int nodeToSkipId)
{
    // 1. Fetch the node the user wants to skip
    var nodeToSkip = await _dbContext.Nodes.FindAsync(nodeToSkipId);
    if (nodeToSkip == null || nodeToSkip.IsCompleted) return;

    // 2. Fetch a replacement geometry matching the exact same logic constraints
    // This ensures we don't swap a North node for a South node, which could softlock
    var replacementGeom = await _dbContext.ValidRoads
        .Where(r => r.RegionTag == nodeToSkip.RegionTag) // Match "North", "Hub", etc.
        .Where(r => r.Geom.Distance(originGeom) <= maxRadiusMeters)
        .Where(r => !_dbContext.Nodes.Any(n => n.Geom.Equals(r.Geom))) // Ensure it's not already in use
        .OrderBy(r => Guid.NewGuid())
        .Select(r => r.Geom)
        .FirstOrDefaultAsync();

    if (replacementGeom != null)
    {
        // 3. The Hot-Swap: Update physical coordinates, KEEP Archipelago IDs
        nodeToSkip.Geom = replacementGeom;
        // nodeToSkip.ArrivalLocationId remains unchanged
        // nodeToSkip.PrecisionLocationId remains unchanged
        
        // Update UI state so the frontend refreshes the Mapbox pin
        nodeToSkip.HasBeenRelocated = true; 
        
        await _dbContext.SaveChangesAsync();
    }
}
```
