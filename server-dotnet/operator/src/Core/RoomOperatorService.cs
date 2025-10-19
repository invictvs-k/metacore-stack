using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using RoomOperator.Abstractions;

namespace RoomOperator.Core;

public sealed class RoomOperatorService
{
    private readonly ReconcilePhases _reconcilePhases;
    private readonly ILogger<RoomOperatorService> _logger;
    private readonly CancellationToken _serviceCancellationToken;
    private readonly SemaphoreSlim _reconcileLock = new(1, 1);
    private readonly ConcurrentQueue<ApplyRequest> _requestQueue = new();
    private readonly ConcurrentDictionary<string, RoomStatus> _roomStatuses = new();
    
    public RoomOperatorService(
        ReconcilePhases reconcilePhases,
        ILogger<RoomOperatorService> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _reconcilePhases = reconcilePhases;
        _logger = logger;
        _serviceCancellationToken = hostApplicationLifetime.ApplicationStopping;
    }
    
    public async Task<ReconcileResult> ApplySpecAsync(ApplyRequest request, CancellationToken ct)
    {
        // Queue the request if already reconciling
        if (!await _reconcileLock.WaitAsync(0, ct))
        {
            _logger.LogInformation("Reconciliation already in progress, queueing request for room {RoomId}", 
                request.Spec.Spec.RoomId);
            _requestQueue.Enqueue(request);
            
            return new ReconcileResult
            {
                Success = false,
                Warnings = new List<string> { "Request queued, reconciliation in progress" }
            };
        }
        
        try
        {
            var roomId = request.Spec.Spec.RoomId;
            
            // Update status
            _roomStatuses[roomId] = new RoomStatus
            {
                RoomId = roomId,
                IsReconciling = true,
                CurrentPhase = "PLANNING"
            };
            
            // Execute reconciliation
            var result = await _reconcilePhases.ExecuteAsync(
                request.Spec, 
                request.DryRun, 
                request.Confirm, 
                ct);
            
            // Update final status
            _roomStatuses[roomId] = new RoomStatus
            {
                RoomId = roomId,
                IsReconciling = false,
                LastReconcile = DateTime.UtcNow,
                PendingDiff = result.Diff,
                Blocked = result.Diff?.Blocked ?? new List<string>()
            };
            
            return result;
        }
        finally
        {
            _reconcileLock.Release();
            
            // Process next queued request
            if (_requestQueue.TryDequeue(out var nextRequest))
            {
                _ = Task.Run(
                    async () => await ApplySpecAsync(nextRequest, _serviceCancellationToken),
                    _serviceCancellationToken);
            }
        }
    }
    
    public OperatorStatus GetStatus()
    {
        return new OperatorStatus
        {
            Version = "1.0.0",
            Health = HealthStatus.Healthy,
            Rooms = _roomStatuses.Values.ToList(),
            QueuedRequests = _requestQueue.Count
        };
    }
    
    public RoomStatus? GetRoomStatus(string roomId)
    {
        return _roomStatuses.TryGetValue(roomId, out var status) ? status : null;
    }
}
