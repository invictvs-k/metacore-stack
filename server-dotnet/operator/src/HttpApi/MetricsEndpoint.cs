using Prometheus;

namespace RoomOperator.HttpApi;

public static class MetricsEndpoint
{
  private static readonly Counter ReconcileAttempts = Metrics
      .CreateCounter("room_operator_reconcile_attempts_total", "Total reconciliation attempts");

  private static readonly Counter ReconcileSuccesses = Metrics
      .CreateCounter("room_operator_reconcile_successes_total", "Successful reconciliations");

  private static readonly Counter ReconcileFailures = Metrics
      .CreateCounter("room_operator_reconcile_failures_total", "Failed reconciliations");

  private static readonly Histogram ReconcileDuration = Metrics
      .CreateHistogram("room_operator_reconcile_duration_seconds", "Reconciliation duration in seconds");

  private static readonly Gauge QueuedRequests = Metrics
      .CreateGauge("room_operator_queued_requests", "Number of queued reconciliation requests");

  private static readonly Gauge ActiveReconciliations = Metrics
      .CreateGauge("room_operator_active_reconciliations", "Number of active reconciliations");

  public static void RecordAttempt() => ReconcileAttempts.Inc();
  public static void RecordSuccess() => ReconcileSuccesses.Inc();
  public static void RecordFailure() => ReconcileFailures.Inc();
  public static void RecordDuration(double seconds) => ReconcileDuration.Observe(seconds);
  public static void UpdateQueuedRequests(int count) => QueuedRequests.Set(count);
  public static void UpdateActiveReconciliations(int count) => ActiveReconciliations.Set(count);
}
