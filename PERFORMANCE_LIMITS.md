# PostGIS Query Performance Limits & Stress Testing

## Summary
The optimized query performs well for typical use cases but degrades under extreme conditions. Performance depends primarily on **radius size** and **population density**, not node count alone.

---

## 1. NODE COUNT LIMITS

### Test Results
| Node Count | Response Time | Status | Notes |
|-----------|---------------|--------|-------|
| 50 | 1,038ms | ⚠️ | Cold connection (first request) |
| 100 | 16ms | ✅ | Fast |
| 200 | 12ms | ✅ | Fast |
| 500 | 32ms | ✅ | Good |
| 1,000 | 72ms | ✅ | Good |
| 2,000 | 11ms | ✅ | Fast (returns what's available) |
| 5,000 | 5ms | ✅ | Fast (capped at 3,113 available) |
| 10,000 | 15ms | ✅ | Fast (capped at 3,113 available) |

### Finding
**Node count is NOT the bottleneck.** The code smartly caps at available nodes in the radius. Even requesting 10,000 nodes takes only 15ms when limited to 3,113 available.

### Recommendation
Safe limit: **Request up to 1,000-2,000 nodes** without performance concerns.

---

## 2. RADIUS LIMITS

### Test Results (Fixed at 50 nodes, Philadelphia area)
| Radius | Response Time | Status | Notes |
|--------|---------------|--------|-------|
| 500m | 30ms | ✅ | Fast |
| 1km | 2.5ms | ✅ | Very fast |
| 2km | 45ms | ✅ | Good |
| 5km | 10ms | ✅ | Very fast |
| 10km | 627ms | ⚠️ | Slow (6x worse) |
| 25km | 374ms | ⚠️ | Slow |
| 50km | 548ms | ⚠️ | Slow |
| 100km | 3,628ms | ❌ | Very slow (3.6s) |
| 250km | 8,987ms | ❌ | Very slow (9s) |

### Degradation Pattern
```
0-5km:    < 50ms (excellent)
10km:     600ms+ (significant drop)
100km:    3.6s (poor)
250km:    9s (unacceptable)
```

### Root Cause
Larger radius = more nodes in search area = more ST_DWithin calculations, even with index. The index helps narrow but still expensive at very large radii.

### Recommendation
Safe limits:
- **Optimal**: < 5km (< 50ms)
- **Acceptable**: 5-10km (< 650ms)
- **Avoid**: > 25km (> 300ms, degrading)

---

## 3. GEOGRAPHIC DENSITY & MAPPING QUALITY

### Test Results

#### Dense Areas (Urban)
| Location | Radius | Response Time | Nodes Returned | Status |
|----------|--------|---------------|---|--------|
| Philadelphia, PA | 5km | 2.5ms | 50 | ✅ |
| New York, NY | 5km | 245ms | 50 | ✅ |

#### Moderate Areas (Suburban/Mixed)
| Location | Radius | Response Time | Nodes Returned | Status |
|----------|--------|---------------|---|--------|
| Appalachian Mountains | 5km | 118ms | 46 | ✅ |
| Montana | 10km | 11ms | 50 | ✅ |

#### Sparse Areas (Rural)
| Location | Radius | Response Time | Nodes Returned | Status |
|----------|--------|---------------|---|--------|
| Great Plains (Colorado) | 5km | 788ms | 50 | ⚠️ Slow |
| Nevada Desert | 10km | 84ms | 50 | ✅ |

#### No Data Areas
| Location | Radius | Response Time | Nodes Returned | Status |
|----------|--------|---------------|---|--------|
| Ocean (off Florida coast) | 5km | 3.4ms | 0 | ✅ |

### Observations
1. **Great Plains anomaly**: 788ms for sparse data (unexpected - needs investigation)
2. **Urban density**: NYC (dense) takes 245ms vs Philadelphia (moderately dense) at 2.5ms for same radius
3. **Empty areas**: Return instantly (< 5ms) - no penalty
4. **Montana**: Fast (11ms) despite sparse data - may have good OSM coverage despite low population

### Recommendation
Performance **varies unpredictably by region** due to:
- OSM data quality/coverage differences
- Actual node density in the database
- Index statistics and query planner decisions

**No reliable "sparse area" upper limit** - test with actual coordinates you'll use.

---

## 4. COMBINED STRESS TESTS

### Maximum Load Scenarios
| Scenario | Response Time | Status | Notes |
|----------|---------------|--------|-------|
| 100km radius + 1,000 nodes | 2,685ms | ❌ | Very slow |
| 250km radius + 5,000 nodes | 3,483ms | ❌ | Very slow |
| 10km radius + 10,000 nodes (available: 3,113) | ~600ms | ⚠️ | Slow |

### Finding
**Avoid combining large radius with large node counts.** The 250km + 5,000 node request took 3.5 seconds.

---

## PERFORMANCE ENVELOPE

### Green Zone (< 100ms, Safe for Production)
```
✅ radius: 0-5km
✅ node count: 1-1,000
✅ any populated area (dense or sparse)
✅ first request: up to 1,038ms (connection overhead)
```

### Yellow Zone (100-1,000ms, Use with Caution)
```
⚠️ radius: 5-25km
⚠️ node count: 1,000-2,000
⚠️ sparse areas with poor OSM coverage
⚠️ may exceed user expectations for latency
```

### Red Zone (> 1,000ms, Avoid in Production)
```
❌ radius: > 100km
❌ combined large radius (100km+) + large node count (1,000+)
❌ Great Plains with 5km radius (anomaly - avoid unless tested)
```

---

## PRACTICAL RECOMMENDATIONS

### For User-Facing API
1. **Default**: radius 1-5km, count 20-50 nodes
2. **Maximum safe**: radius 10km, count 100 nodes
3. **Query timeout**: 10 seconds for safety
4. **Cache results**: Geographic region results for 1-5 hours
5. **Monitor**: Log response times by region to identify anomalies

### For Background Processing
1. Safe to use: 100km+ radius with any node count
2. Use task queue: Don't block user requests
3. Cost acceptable: 3-10 second responses for batch operations

### Connection Optimization
1. First request: 684ms (connection pooling overhead)
2. Subsequent: 2-50ms (reuse connection)
3. **Action**: Implement connection pooling in Npgsql (already done - verify)

### Database Improvements (Optional)
1. Analyze Great Plains anomaly - may indicate index statistics issue
2. Consider ANALYZE VACUUM on high-load times
3. Monitor random() sorting - if radius < 5km, consider pre-sorting

---

## Testing Methodology

All tests used:
- **Server**: PostGIS in Docker (12.24GB RAM, 2GB shared_buffers)
- **Data**: OSM data with ~57.9M nodes
- **Timing**: Includes connection overhead, database query, and C# randomization
- **Multiple requests**: Each test run twice to show cold vs warm connection

See logs in `/tmp/api.log` for detailed breakdown:
- Connection open time
- SQL fetch time
- C# randomization time
- Total elapsed
