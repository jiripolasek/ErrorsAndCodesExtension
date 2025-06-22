// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace JPSoftworks.ErrorsAndCodes.Services.WindowsErrors;

/// <summary>
/// Handles HRESULT decomposition and analysis
/// </summary>
public class HResultProcessor
{
    private readonly Dictionary<string, Dictionary<int, string>> _facilitiesByFile;
    private readonly Dictionary<int, string> _globalFacilities;

    public HResultProcessor(
        Dictionary<string, Dictionary<int, string>> facilitiesByFile,
        Dictionary<int, string> globalFacilities)
    {
        this._facilitiesByFile = facilitiesByFile ?? throw new ArgumentNullException(nameof(facilitiesByFile));
        this._globalFacilities = globalFacilities ?? throw new ArgumentNullException(nameof(globalFacilities));
    }

    /// <summary>
    /// Gets a human-readable description of the severity
    /// </summary>
    private static string GetSeverityDescription(int severity)
    {
        return severity == 1 ? "FAILURE" : "SUCCESS";
    }

    /// <summary>
    /// Gets the facility description, prioritizing file-local definitions
    /// </summary>
    internal string GetFacilityDescription(int facility, IEnumerable<ErrorCodeWithSource>? matchingErrors = null)
    {
        // First, try to find facility in the same files as matching errors
        if (matchingErrors != null)
        {
            // Sort by priority if possible
            var sortedErrors = matchingErrors.OrderByDescending(e =>
                (e as WindowsErrorLookup.PrioritizedErrorCodeWithSource)?.Priority ?? HeaderFilePriority.Normal);

            foreach (var error in sortedErrors)
            {
                if (this._facilitiesByFile.TryGetValue(error.SourceFile, out var fileFacilities) &&
                    fileFacilities.TryGetValue(facility, out var localFacilityName))
                {
                    return $"{localFacilityName} (from {error.SourceFile})";
                }
            }
        }

        // Fallback to global facilities
        if (this._globalFacilities.TryGetValue(facility, out var globalFacilityName))
        {
            return globalFacilityName;
        }

        return $"Unknown ({facility})";
    }

    /// <summary>
    /// Formats HRESULT components into a readable string
    /// </summary>
    public string FormatComponents(HResultComponents components)
    {
        var severityDesc = GetSeverityDescription(components.Severity);
        var facilityDesc = this.GetFacilityDescription(components.Facility);

        return $"Severity: {severityDesc} ({components.Severity}), " +
               $"Facility: {components.Facility} ({facilityDesc}), " +
               $"Code: 0x{components.Code:X} ({components.Code})";
    }

    /// <summary>
    /// Formats HRESULT components with known matching errors
    /// </summary>
    internal string FormatComponents(HResultComponents components, List<ErrorCodeWithSource> matchingErrors)
    {
        var severityDesc = GetSeverityDescription(components.Severity);
        var facilityDesc = this.GetFacilityDescription(components.Facility, matchingErrors);

        return $"Severity: {severityDesc} ({components.Severity}), " +
               $"Facility: {components.Facility} ({facilityDesc}), " +
               $"Code: 0x{components.Code:X} ({components.Code})";
    }

    /// <summary>
    /// Represents the components of an HRESULT
    /// </summary>
    public struct HResultComponents
    {
        // HRESULT bit masks
        private const uint SeverityMask = 0x80000000;
        private const int SeverityShift = 31;
        private const uint FacilityMask = 0x1FFF0000;
        private const int FacilityShift = 16;
        private const uint CodeMask = 0xFFFF;

        public int Severity { get; private init; }
        public int Facility { get; private init; }
        public int Code { get; private init; }
        public uint Original { get; private init; }

        /// <summary>
        /// Creates an HResultComponents instance by parsing an HRESULT value
        /// </summary>
        /// <param name="hResult">The HRESULT value to parse</param>
        /// <returns>A new HResultComponents instance with the parsed values</returns>
        public static HResultComponents FromHResult(uint hResult)
        {
            return new HResultComponents
            {
                Severity = (int)((hResult & SeverityMask) >> SeverityShift),
                Facility = (int)((hResult & FacilityMask) >> FacilityShift),
                Code = (int)(hResult & CodeMask),
                Original = hResult
            };
        }
    }
}