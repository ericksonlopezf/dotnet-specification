// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.Specification.Diagnostics;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for <see cref="SpecificationDiagnostics"/> observability instrumentation.
/// </summary>
public sealed class SpecificationDiagnosticsTests
{
    [Fact]
    public void SpecificationDiagnostics_AreInitialized()
    {
        SpecificationDiagnostics.ActivitySource.Should().NotBeNull();
        SpecificationDiagnostics.Meter.Should().NotBeNull();
        SpecificationDiagnostics.SpecificationsCreated.Should().NotBeNull();
        SpecificationDiagnostics.SpecificationsComposed.Should().NotBeNull();
        SpecificationDiagnostics.ExpressionCacheHits.Should().NotBeNull();
        SpecificationDiagnostics.ExpressionCacheMisses.Should().NotBeNull();
        SpecificationDiagnostics.SqlTranslations.Should().NotBeNull();
        SpecificationDiagnostics.SqlTranslationDuration.Should().NotBeNull();
    }

    [Fact]
    public void MeterListener_RecordsSpecificationCreatedAndComposedCounters()
    {
        long createdCount = 0;
        long composedCount = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == SpecificationDiagnostics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "specification.created")
                Interlocked.Add(ref createdCount, measurement);
            else if (instrument.Name == "specification.composed")
                Interlocked.Add(ref composedCount, measurement);
        });
        listener.Start();

        var spec1 = new ActiveCustomerSpecification();
        var spec2 = new NotDeletedCustomerSpecification();
        _ = spec1.And(spec2);

        listener.RecordObservableInstruments();

        createdCount.Should().BeGreaterThanOrEqualTo(2);
        composedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void MeterListener_RecordsExpressionCacheMissesAndHits()
    {
        long cacheHits = 0;
        long cacheMisses = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == SpecificationDiagnostics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "specification.expression.cache.hits")
                Interlocked.Add(ref cacheHits, measurement);
            else if (instrument.Name == "specification.expression.cache.misses")
                Interlocked.Add(ref cacheMisses, measurement);
        });
        listener.Start();

        Expression<Func<Customer, bool>> expr = c => c.Id == 99999;
        _ = ExpressionCompilationCache.GetOrCompile(expr);
        _ = ExpressionCompilationCache.GetOrCompile(expr);

        listener.RecordObservableInstruments();

        cacheMisses.Should().BeGreaterThanOrEqualTo(1);
        cacheHits.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void ActivityListener_RecordsActivityWhenSampled()
    {
        var activityStarted = false;

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SpecificationDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => activityStarted = true
        };
        ActivitySource.AddActivityListener(activityListener);

        using (var activity = SpecificationDiagnostics.ActivitySource.StartActivity("CustomTestActivity"))
        {
            activity.Should().NotBeNull();
        }

        activityStarted.Should().BeTrue();
    }
}


