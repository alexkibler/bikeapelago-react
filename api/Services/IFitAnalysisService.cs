using System.Collections.Generic;
using System.IO;

namespace Bikeapelago.Api.Services;

public interface IFitAnalysisService
{
    Models.FitAnalysisResult AnalyzeFitFile(Stream fitStream, IEnumerable<Models.MapNode>? availableNodes = null);
}
