-- Staging lua style for blue/green imports.
-- Writes to planet_osm_nodes_new, planet_osm_ways_new, and planet_osm_way_nodes_new.
-- The import script atomically swaps these tables after import completes.

local nodes = osm2pgsql.define_node_table('planet_osm_nodes_new', {
    { column = 'id', type = 'int8' },
    { column = 'geom', type = 'point', projection = 4326 },
})

local ways = osm2pgsql.define_way_table('planet_osm_ways_new', {
    { column = 'id', type = 'int8' },
    { column = 'geom', type = 'linestring', projection = 4326 },
    { column = 'cycling_safe', type = 'boolean' },
    { column = 'walking_safe', type = 'boolean' },
})

-- No explicit way_nodes table needed - will be created post-import via spatial queries

-- Check if a way is safe for cycling or walking
local function is_cycling_safe(tags)
    local highway = tags.highway
    if not highway then return false end

    -- Exclude motorways and explicit bicycle=no
    if highway == 'motorway' or highway == 'motorway_link' then
        return false
    end
    if tags.bicycle == 'no' then
        return false
    end

    -- Include most other highways
    local good_highways = {
        residential = true, secondary = true, tertiary = true, primary = true,
        primary_link = true, secondary_link = true, tertiary_link = true,
        unclassified = true, living_street = true, path = true,
        cycleway = true, footway = true, track = true, service = true
    }

    return good_highways[highway] or false
end

local function is_walking_safe(tags)
    local highway = tags.highway
    if not highway then return false end

    -- Exclude motorways and explicit foot=no
    if highway == 'motorway' or highway == 'motorway_link' then
        return false
    end
    if tags.foot == 'no' then
        return false
    end

    -- Include most other highways
    local good_highways = {
        residential = true, secondary = true, tertiary = true, primary = true,
        primary_link = true, secondary_link = true, tertiary_link = true,
        unclassified = true, living_street = true, path = true,
        cycleway = true, footway = true, track = true, service = true
    }

    return good_highways[highway] or false
end

function osm2pgsql.process_node(object)
    nodes:insert({
        id = object.id,
        geom = object:as_point()
    })
end

function osm2pgsql.process_way(object)
    local tags = object.tags
    local cycling = is_cycling_safe(tags)
    local walking = is_walking_safe(tags)

    -- Only store ways that are safe for cycling or walking
    if cycling or walking then
        ways:insert({
            id = object.id,
            geom = object:as_linestring(),
            cycling_safe = cycling,
            walking_safe = walking
        })
    end
end
