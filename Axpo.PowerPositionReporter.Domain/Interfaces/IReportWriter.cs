using Axpo.PowerPositionReporter.Domain.Models;
using System;
using System.Collections.Generic;
namespace Axpo.PowerPositionReporter.Domain.Interfaces
    {
    /// <summary>
    /// Interface for writing reports based on the extracted power position data.
    /// </summary>
    public interface IReportWriter
        {

        /// <summary>
        /// Writes the report based on the given ExtractionResult (power position data).
        /// </summary>
        /// <param name="result"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<string> WriteAsync (
        PowerTrade result,
        CancellationToken cancellationToken = default );
        }
    }
