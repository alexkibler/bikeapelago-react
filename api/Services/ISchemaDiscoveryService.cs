using System.Collections.Generic;

namespace Bikeapelago.Api.Services;

public interface ISchemaDiscoveryService
{
    List<TableGroup> GetSchema();
}
