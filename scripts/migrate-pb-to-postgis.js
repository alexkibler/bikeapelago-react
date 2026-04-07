const sqlite3 = require('sqlite3').verbose();
const { Client } = require('pg');

const PB_DB_PATH = '/Volumes/1TB/Repos/avarts/pb_data/data.db';
const PG_CONN_STRING = process.env.PG_CONN_STRING || 'postgresql://osm:osm_secret@localhost:5432/bikeapelago';

async function runMigration() {
    console.log(`Starting migration from PocketBase (${PB_DB_PATH}) to PostGIS (${PG_CONN_STRING})`);
    
    const pgClient = new Client({ connectionString: PG_CONN_STRING });
    await pgClient.connect();

    // Enable PostGIS extension if not present
    await pgClient.query('CREATE EXTENSION IF NOT EXISTS postgis;');

    // Open SQLite DB
    const sqliteDb = new sqlite3.Database(PB_DB_PATH, sqlite3.OPEN_READONLY, (err) => {
        if (err) {
            console.error('Error opening SQLite DB:', err.message);
            process.exit(1);
        }
    });

    const getSqliteRows = (query) => {
        return new Promise((resolve, reject) => {
            sqliteDb.all(query, [], (err, rows) => {
                if (err) reject(err);
                else resolve(rows);
            });
        });
    };

    // 1. Create Stage 1 Tables in Postgres
    await pgClient.query(`
        CREATE TABLE IF NOT EXISTS "Users" (
            "Id" VARCHAR(36) PRIMARY KEY,
            "Username" TEXT NOT NULL,
            "Name" TEXT,
            "Weight" DOUBLE PRECISION,
            "Avatar" TEXT,
            "Email" TEXT,
            "Password" TEXT
        );

        CREATE TABLE IF NOT EXISTS "GameSessions" (
            "Id" VARCHAR(36) PRIMARY KEY,
            "UserId" VARCHAR(36) REFERENCES "Users"("Id"),
            "ApSeedName" TEXT,
            "ApServerUrl" TEXT,
            "ApSlotName" TEXT,
            "Location" GEOMETRY(Point, 4326),
            "Radius" INTEGER,
            "Status" TEXT,
            "CreatedAt" TEXT,
            "UpdatedAt" TEXT
        );

        CREATE TABLE IF NOT EXISTS "MapNodes" (
            "Id" VARCHAR(36) PRIMARY KEY,
            "SessionId" VARCHAR(36) REFERENCES "GameSessions"("Id"),
            "Name" TEXT,
            "ApLocationId" INTEGER,
            "OsmNodeId" TEXT,
            "Location" GEOMETRY(Point, 4326),
            "State" TEXT
        );

        CREATE TABLE IF NOT EXISTS "Routes" (
            "Id" VARCHAR(36) PRIMARY KEY,
            "UserId" VARCHAR(36) REFERENCES "Users"("Id"),
            "Title" TEXT,
            "Sport" TEXT,
            "Distance" DOUBLE PRECISION,
            "Elevation" DOUBLE PRECISION,
            "Time" DOUBLE PRECISION,
            "Path" GEOMETRY(LineString, 4326)
        );

        CREATE TABLE IF NOT EXISTS "Activities" (
            "Id" VARCHAR(36) PRIMARY KEY,
            "UserId" VARCHAR(36) REFERENCES "Users"("Id"),
            "Name" TEXT,
            "Description" TEXT,
            "Sport" TEXT,
            "StartTime" TEXT,
            "TotDistance" DOUBLE PRECISION,
            "TotElevation" DOUBLE PRECISION,
            "Path" GEOMETRY(LineString, 4326)
        );
    `);
    console.log('Target tables created.');

    // 2. Migrate Users (PocketBase table is 'users')
    console.log('Migrating users...');
    const users = await getSqliteRows('SELECT * FROM users');
    for (const u of users) {
        await pgClient.query(
            'INSERT INTO "Users" ("Id", "Username", "Name", "Weight", "Avatar", "Email", "Password") VALUES ($1, $2, $3, $4, $5, $6, $7) ON CONFLICT ("Id") DO UPDATE SET "Password" = EXCLUDED."Password"',
            [u.id, u.username || u.name, u.name, u.weight || 75.0, u.avatar, u.email, u.password]
        );
    }
    console.log(`Migrated ${users.length} users.`);

    // 3. Migrate GameSessions
    console.log('Migrating game sessions...');
    const sessions = await getSqliteRows('SELECT * FROM game_sessions');
    for (const s of sessions) {
        // Point is Lon Lat
        const wkt = (s.center_lon != null && s.center_lat != null) ? `SRID=4326;POINT(${s.center_lon} ${s.center_lat})` : null;
        
        await pgClient.query(
            'INSERT INTO "GameSessions" ("Id", "UserId", "ApSeedName", "ApServerUrl", "ApSlotName", "Location", "Radius", "Status", "CreatedAt", "UpdatedAt") VALUES ($1, $2, $3, $4, $5, ST_GeomFromEWKT($6), $7, $8, $9, $10) ON CONFLICT ("Id") DO NOTHING',
            [s.id, s.user, s.ap_seed_name, s.ap_server_url, s.ap_slot_name, wkt, s.radius, s.status || 'SetupInProgress', s.created, s.updated]
        );
    }
    console.log(`Migrated ${sessions.length} game sessions.`);

    // 4. Migrate MapNodes
    console.log('Migrating map nodes...');
    const nodes = await getSqliteRows('SELECT * FROM map_nodes');
    for (const n of nodes) {
        const wkt = (n.lon != null && n.lat != null) ? `SRID=4326;POINT(${n.lon} ${n.lat})` : null;
        
        await pgClient.query(
            'INSERT INTO "MapNodes" ("Id", "SessionId", "Name", "ApLocationId", "OsmNodeId", "Location", "State") VALUES ($1, $2, $3, $4, $5, ST_GeomFromEWKT($6), $7) ON CONFLICT ("Id") DO NOTHING',
            [n.id, n.session, n.name, n.ap_location_id, n.osm_node_id, wkt, n.state || 'Hidden']
        );
    }
    console.log(`Migrated ${nodes.length} map nodes.`);

    // 5. Migrate Routes
    console.log('Migrating routes (inferring LineString from builder json if available)...');
    try {
        const routes = await getSqliteRows('SELECT * FROM routes');
        for (const r of routes) {
            let lineStringWkt = null;
            if (r.builder) {
                try {
                    const parsed = JSON.parse(r.builder);
                    // Assuming builder is an array of [lon, lat] or {lon, lat} objects
                    if (Array.isArray(parsed) && parsed.length > 1) {
                        const coords = parsed.map(pt => {
                            if (Array.isArray(pt)) return `${pt[0]} ${pt[1]}`;
                            if (pt.lon && pt.lat) return `${pt.lon} ${pt.lat}`;
                            return null;
                        }).filter(c => c !== null);

                        if (coords.length > 1) {
                            lineStringWkt = `SRID=4326;LINESTRING(${coords.join(', ')})`;
                        }
                    }
                } catch (e) {
                    // JSON parse error or unexpected format, skip geometry
                }
            }

            await pgClient.query(
                'INSERT INTO "Routes" ("Id", "UserId", "Title", "Sport", "Distance", "Elevation", "Time", "Path") VALUES ($1, $2, $3, $4, $5, $6, $7, ST_GeomFromEWKT($8)) ON CONFLICT ("Id") DO NOTHING',
                [r.id, r.user, r.title, r.sport, r.distance, r.elevation, r.time, lineStringWkt]
            );
        }
        console.log(`Migrated ${routes.length} routes.`);
    } catch (e) {
        console.log('No routes table found or failed to migrate routes:', e.message);
    }

    // 6. Migrate Activities
    console.log('Migrating activities...');
    try {
        const activities = await getSqliteRows('SELECT * FROM activities');
        for (const a of activities) {
            // Activities don't have explicit paths mapped in the schema except GPX/FIT files.
            // Leaving Path as null for now unless structured data exists in SQLite `location` column.
            let lineStringWkt = null;

            await pgClient.query(
                'INSERT INTO "Activities" ("Id", "UserId", "Name", "Description", "Sport", "StartTime", "TotDistance", "TotElevation", "Path") VALUES ($1, $2, $3, $4, $5, $6, $7, $8, ST_GeomFromEWKT($9)) ON CONFLICT ("Id") DO NOTHING',
                [a.id, a.user, a.name, a.description, a.sport, a.start_time, a.tot_distance, a.tot_elevation, lineStringWkt]
            );
        }
        console.log(`Migrated ${activities.length} activities.`);
    } catch (e) {
         console.log('No activities table found or failed to migrate activities:', e.message);
    }

    sqliteDb.close();
    await pgClient.end();
    console.log('Migration completed.');
}

runMigration().catch(console.error);
