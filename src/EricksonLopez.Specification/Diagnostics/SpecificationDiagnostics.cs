// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EricksonLopez.Specification.Diagnostics;

/// <summary>
/// Provides OpenTelemetry activity sources and meters for the specification library.
/// </summary>
public static class SpecificationDiagnostics
{
    /// <summary>Gets the OpenTelemetry activity source name for specification operations.</summary>
    public const string ActivitySourceName = "EricksonLopez.Specification";

    /// <summary>Gets the OpenTelemetry meter name for specifications.</summary>
    public const string MeterName = "EricksonLopez.Specification";

    /// <summary>Gets the OpenTelemetry meter instance.</summary>
    public static readonly Meter Meter = new(MeterName, SpecificationVersion.Current);

    /// <summary>Gets the counter tracking total specifications created.</summary>
    public static readonly Counter<long> SpecificationsCreated =
        Meter.CreateCounter<long>(
            "specification.created",
            description: "Number of specification instances created.");

    /// <summary>Gets the counter tracking total specifications evaluated.</summary>
    public static readonly Counter<long> SpecificationsEvaluated =
        Meter.CreateCounter<long>(
            "specification.evaluated",
            description: "Number of specification evaluations performed.");

    /// <summary>Gets the counter tracking total specifications composed.</summary>
    public static readonly Counter<long> SpecificationsComposed =
        Meter.CreateCounter<long>(
            "specification.composed",
            description: "Number of specification composition operations performed.");

    /// <summary>Gets the counter tracking total specifications compiled.</summary>
    public static readonly Counter<long> SpecificationsCompiled =
        Meter.CreateCounter<long>(
            "specification.compiled",
            description: "Number of specification expressions compiled to delegates.");

    /// <summary>Gets the diagnostic activity source for distributed tracing.</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, SpecificationVersion.Current);

    /// <summary>Gets the counter tracking the number of expression compilation cache hits.</summary>
    // Stryker disable once all : Telemetry constants and descriptions are trivial
    public static readonly Counter<long> ExpressionCacheHits =
        Meter.CreateCounter<long>(
            "specification.expression.cache.hits",
            description: "Number of expression compilation cache hits.");

    /// <summary>Gets the counter tracking the number of expression compilation cache misses.</summary>
    // Stryker disable once all : Telemetry constants and descriptions are trivial
    public static readonly Counter<long> ExpressionCacheMisses =
        Meter.CreateCounter<long>(
            "specification.expression.cache.misses",
            description: "Number of expression compilation cache misses.");

    /// <summary>Gets the counter tracking the number of successful SQL translation operations.</summary>
    // Stryker disable once all : Telemetry constants and descriptions are trivial
    public static readonly Counter<long> SqlTranslations =
        Meter.CreateCounter<long>(
            "specification.sql.translations",
            description: "Number of successful SQL translation operations.");

    /// <summary>Gets the histogram tracking the duration of SQL translation operations in milliseconds.</summary>
    // Stryker disable once all : Telemetry constants and descriptions are trivial
    public static readonly Histogram<double> SqlTranslationDuration =
        Meter.CreateHistogram<double>(
            "specification.sql.translation.duration",
            unit: "ms",
            description: "Duration of SQL translation operations in milliseconds.");
}


