const { Client } = require('pg');

const PG_CONN_STRING = process.env.PG_CONN_STRING || 'postgresql://osm:osm_secret@localhost:5432/bikeapelago';

async function checkLogs() {
    console.log(`Connecting to PostGIS at ${PG_CONN_STRING}`);
    
    const pgClient = new Client({ connectionString: PG_CONN_STRING });
    
    try {
        await pgClient.connect();
        
        // Check if table exists
        const tableCheck = await pgClient.query(`
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_name = 'ApiLogs'
            );
        `);
        
        if (!tableCheck.rows[0].exists) {
            console.log("Table 'ApiLogs' does not exist yet.");
            return;
        }

        const res = await pgClient.query('SELECT * FROM "ApiLogs" ORDER BY "Timestamp" DESC LIMIT 20;');
        
        if (res.rows.length === 0) {
            console.log("No logs found in ApiLogs table.");
        } else {
            console.log(`Found ${res.rows.length} logs:`);
            res.rows.forEach(log => {
                console.log('---');
                console.log(`ID: ${log.Id}`);
                console.log(`Timestamp: ${log.Timestamp}`);
                console.log(`Method: ${log.Method}`);
                console.log(`Path: ${log.Path}`);
                console.log(`Status: ${log.StatusCode}`);
                console.log(`IP: ${log.IpAddress}`);
                console.log(`Body: ${log.RequestBody}`);
                if (log.ExceptionType) {
                    console.log(`Exception: ${log.ExceptionType}`);
                    console.log(`Stack: ${log.StackTrace?.substring(0, 500)}...`);
                }
            });
        }
    } catch (err) {
        console.error('Error querying logs:', err.message);
    } finally {
        await pgClient.end();
    }
}

checkLogs().catch(console.error);
