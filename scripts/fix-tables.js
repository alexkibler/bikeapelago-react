const { Client } = require('pg');

const PG_CONN_STRING = process.env.PG_CONN_STRING || 'postgresql://osm:osm_secret@localhost:5432/bikeapelago';

async function fixTableNames() {
    console.log(`Connecting to PostGIS at ${PG_CONN_STRING}`);
    
    const pgClient = new Client({ connectionString: PG_CONN_STRING });
    
    try {
        await pgClient.connect();
        
        const tablesToFix = [
            'users',
            'gamesessions',
            'mapnodes',
            'routes',
            'activities'
        ];

        const mapping = {
            'users': 'Users',
            'gamesessions': 'GameSessions',
            'mapnodes': 'MapNodes',
            'routes': 'Routes',
            'activities': 'Activities'
        };

        for (const table of tablesToFix) {
            const res = await pgClient.query(`
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_name = '${table}'
                );
            `);

            if (res.rows[0].exists) {
                const target = mapping[table];
                console.log(`Renaming ${table} to "${target}"`);
                await pgClient.query(`ALTER TABLE "${table}" RENAME TO "${target}";`);
            } else {
                console.log(`Table ${table} already renamed or doesn't exist.`);
            }
        }
        
    } catch (err) {
        console.error('Error fixing tables:', err.message);
    } finally {
        await pgClient.end();
    }
}

fixTableNames().catch(console.error);
