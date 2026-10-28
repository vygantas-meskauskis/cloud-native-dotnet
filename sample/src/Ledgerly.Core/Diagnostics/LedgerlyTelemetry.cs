using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ledgerly.Core.Diagnostics;

/// <summary>
/// One place for Ledgerly's custom telemetry names. ServiceDefaults subscribes to these
/// (AddSource / AddMeter) so they flow to the Aspire dashboard locally and Azure Monitor in the cloud.
/// </summary>
public static class LedgerlyTelemetry
{
    public const string SourceName = "Ledgerly";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> TransactionsRecorded =
        Meter.CreateCounter<long>("ledgerly.transactions.recorded", unit: "{transaction}", description: "Trades recorded via API or import.");

    public static readonly Counter<long> ImportsProcessed =
        Meter.CreateCounter<long>("ledgerly.imports.processed", unit: "{import}", description: "Broker imports processed, tagged by outcome.");

    public static readonly Histogram<double> ImportDuration =
        Meter.CreateHistogram<double>("ledgerly.imports.duration", unit: "s", description: "Time to process one broker import.");

    public static readonly Counter<long> AlertsTriggered =
        Meter.CreateCounter<long>("ledgerly.alerts.triggered", unit: "{alert}", description: "Price alerts that fired.");
}
