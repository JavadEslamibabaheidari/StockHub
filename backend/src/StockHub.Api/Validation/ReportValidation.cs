using StockHub.Api.Contracts;

namespace StockHub.Api.Validation;

public static class ReportValidation
{
    public static string? Alerts(AlertSettingsInput input)
    {
        string[] keys = ["lowStock","outOfStock","syncFailure","oversellingRisk"];
        string[] frequencies = ["instant","hourly","daily"];
        if (input.Alerts is null || input.Alerts.Count != keys.Length || input.Alerts.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count() != keys.Length || !input.Alerts.Select(x => x.Key).Order().SequenceEqual(keys.Order())) return "Provide each supported alert key exactly once.";
        foreach (var alert in input.Alerts)
        {
            if (alert.Channels is null || !frequencies.Contains(alert.Frequency,StringComparer.Ordinal)) return "Each alert needs delivery channel preferences and a supported frequency.";
            if (alert.Key == "outOfStock")
            {
                if (alert.Threshold != 0) return "Out-of-stock alerts use a fixed threshold of zero.";
            }
            else if (alert.Key is "lowStock" or "syncFailure" or "oversellingRisk")
            {
                var minimum = alert.Key == "lowStock" ? 0 : 1;
                if (alert.Threshold is null || alert.Threshold < minimum || alert.Threshold > 100000)
                    return alert.Key == "syncFailure"
                        ? "Sync failure duration must be between 1 and 100000 minutes."
                        : "Alert thresholds must be between 0 and 100000.";
            }
        }
        return null;
    }
}
