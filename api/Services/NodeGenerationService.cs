using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;

namespace Bikeapelago.Api.Services
{
    public class NodeGenerationRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public double CenterLat { get; set; }
        public double CenterLon { get; set; }
        public double Radius { get; set; }
        public int NodeCount { get; set; } = 50;
        public string Mode { get; set; } = "archipelago"; // or "singleplayer"
    }

    public class NodeGenerationService
    {
        private readonly OverpassService _overpassService;
        private readonly IMapNodeRepository _nodeRepository;
        private readonly IGameSessionRepository _sessionRepository;

        public NodeGenerationService(OverpassService overpassService, IMapNodeRepository nodeRepository, IGameSessionRepository sessionRepository)
        {
            _overpassService = overpassService;
            _nodeRepository = nodeRepository;
            _sessionRepository = sessionRepository;
        }

        private static List<T> Shuffle<T>(List<T> list)
        {
            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
            Random.Shared.Shuffle(span);
            return list;
        }

        public async Task<int> GenerateNodesAsync(NodeGenerationRequest request)
        {
            // 1. Update session status to SetupInProgress
            var session = await _sessionRepository.GetByIdAsync(request.SessionId);
            if (session == null) throw new Exception("Session not found");
            
            // 2. Fetch intersections
            var allIntersections = await _overpassService.FetchCyclingIntersectionsAsync(request.CenterLat, request.CenterLon, request.Radius);

            if (allIntersections.Count < request.NodeCount)
            {
                throw new Exception($"Found only {allIntersections.Count} intersections, need {request.NodeCount}. Increase radius.");
            }

            // 3. Shuffle and select
            var selectedNodes = Shuffle(allIntersections).Take(request.NodeCount).ToList();

            // 4. Delete existing nodes for session if any
            await _nodeRepository.DeleteBySessionIdAsync(request.SessionId);

            // 5. Create MapNodes in PocketBase
            int createdCount = 0;
            for (var i = 0; i < selectedNodes.Count; i++)
            {
                var osmNode = selectedNodes[i];
                var mapNode = new MapNode
                {
                    SessionId = session.Id,
                    ApLocationId = 800000 + (i + 1), // Archipelago mimic
                    OsmNodeId = osmNode.Id.ToString(),
                    Name = $"OSM Node {osmNode.Id}", // Svelte does geocoding, skip for now or add proxy call
                    Lat = osmNode.Lat,
                    Lon = osmNode.Lon,
                    State = request.Mode == "singleplayer" && i < 3 ? "Available" : "Hidden"
                };

                await _nodeRepository.CreateAsync(mapNode);
                createdCount++;
            }

            // 6. Update session metadata and switch to Active
            session.CenterLat = request.CenterLat;
            session.CenterLon = request.CenterLon;
            session.Radius = (int)request.Radius;
            session.Status = SessionStatus.Active;
            await _sessionRepository.UpdateAsync(session);

            return createdCount;
        }
    }
}
