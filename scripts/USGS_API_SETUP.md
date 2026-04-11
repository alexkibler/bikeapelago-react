# Using USGS API Token to Download SRTM Data

**Secure approach**: Token stays in your local `.env`, script runs locally, I never see it.

## Step 1: Get USGS API Token

1. **Register at USGS**: https://ers.usgs.gov/registration
2. **Log in** to https://earthexplorer.usgs.gov/
3. **Get API token**:
   - Click your username (top right)
   - Select "Profile"
   - Scroll to "USGS M2M API"
   - Click "Generate New Token"
   - Copy the token

## Step 2: Add to .env

Add your token to `.env` in the project root:

```bash
# In /Volumes/1TB/Repos/avarts/bikeapelago-react/.env
USGS_API_TOKEN=your_token_here_from_usgs
```

**Important**:
- ✅ `.env` is already in `.gitignore`
- ✅ Token never leaves your machine
- ✅ Token never appears in git history

## Step 3: Install Dependencies

```bash
pip install python-dotenv requests
```

## Step 4: Download SRTM Data

```bash
cd /Volumes/1TB/Repos/avarts/bikeapelago-react/scripts

# Download all of USA
python3 download_srtm_usgs_api.py --region US

# Or download all of North America
python3 download_srtm_usgs_api.py --region ALL

# Custom output directory
python3 download_srtm_usgs_api.py --region US --output-dir /custom/path
```

## Step 5: Load into PostGIS

Once tiles are downloaded:

```bash
cd /path/to/downloaded/srtm

# Merge all tiles
gdalbuildvrt combined.vrt *.tif

# Load into PostGIS
python3 ../load_elevation_simple.py \
  --host localhost \
  --port 5433 \
  --database bikeapelago \
  --user osm \
  --password osm_secret \
  --geotiff combined.vrt
```

## Security Notes

- **Token in .env**: Local file, never committed to git
- **Script runs locally**: Download happens on your machine
- **No token in logs**: Token only used for API authentication
- **DELETE token when done**: If concerned, revoke it at USGS after downloading

## Troubleshooting

**"USGS_API_TOKEN not found in .env"**
- Make sure token is in `.env` file
- Restart terminal for changes to take effect

**"Authentication failed"**
- Verify token is correct in USGS account
- Check token hasn't expired
- Generate a new token if needed

**"No tiles found"**
- USGS API parameters might need adjustment
- Fall back to manual download from Earth Explorer
- Or use `load_elevation_simple.py` with manually downloaded tiles

## Alternative: Manual Download

If USGS API has issues, use Earth Explorer directly:

1. Go to https://earthexplorer.usgs.gov/
2. Log in with your USGS account
3. Search your area
4. Filter: "SRTM 1 Arc-Second Global DEM"
5. Download tiles
6. Run same merge + load steps above

## Cost

- ✅ Free SRTM data (public domain)
- ✅ Free USGS account
- ✅ No API costs
- ✅ Bandwidth: ~7-8GB for all North America

## After Loading

All your nodes with coordinates in SRTM coverage will have elevation automatically:

```sql
SELECT COUNT(*) FROM "MapNodes" WHERE "Elevation" IS NOT NULL;
-- Result: number of nodes with elevation
```

You're ready to use elevation in your app!
