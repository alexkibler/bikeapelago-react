const sqlite3 = require('sqlite3').verbose();
const { Client } = require('pg');

const PB_DB_PATH = '/Volumes/1TB/Repos/avarts/pb_data/data.db';
const PG_CONN_STRING = 'postgresql://osm:osm_secret@localhost:5432/bikeapelago';

async function fixPasswords() {
    console.log(`Fixing passwords from ${PB_DB_PATH} to ${PG_CONN_STRING}`);
    
    const pgClient = new Client({ connectionString: PG_CONN_STRING });
    await pgClient.connect();

    const sqliteDb = new sqlite3.Database(PB_DB_PATH, sqlite3.OPEN_READONLY);

    sqliteDb.all('SELECT username, password FROM users', [], async (err, rows) => {
        if (err) {
            console.error(err);
            return;
        }

        console.log(`Found ${rows.length} users in SQLite.`);
        for (const row of rows) {
            console.log(`Updating password for ${row.username}...`);
            await pgClient.query(
                'UPDATE users SET "Password" = $1 WHERE "Username" = $2',
                [row.password, row.username]
            );
        }

        console.log('Done.');
        sqliteDb.close();
        await pgClient.end();
    });
}

fixPasswords().catch(console.error);
