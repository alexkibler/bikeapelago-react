using System;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Services;

public interface IArchipelagoService
{
    Task CheckLocationsAsync(Guid sessionId, long[] locationIds);
}
