using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AudioStemPlayer.Core.Services;

public interface IDemixingService
{
    Task<IReadOnlyList<string>> DemixAsync(string inputFile, IProgress<string> progress, CancellationToken cancellationToken = default);
}