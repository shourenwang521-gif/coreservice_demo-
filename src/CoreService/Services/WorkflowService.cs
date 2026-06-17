using System.Collections.Concurrent;
using System.Text.Json;
using CoreService.Models;
using CoreService.Scheduling;
using Microsoft.Extensions.Logging;

namespace CoreService.Services;

/// <summary>
/// 工艺流程管理服务：接收上位机下发的工艺流程定义，管理执行与历史记录。
/// </summary>
public class WorkflowService
{
    private readonly IWorkflowEngine _engine;
    private readonly ILogger<WorkflowService> _logger;
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows = new();
    private readonly ConcurrentDictionary<string, WorkflowExecutionResult> _history = new();

    public WorkflowService(IWorkflowEngine engine, ILogger<WorkflowService> logger)
    {
        _engine = engine;
        _logger = logger;

        _engine.StepStarted += (_, r) =>
            _logger.LogDebug(">> Step started: [{StepId}] {Name}", r.StepId, r.StepName);
        _engine.StepCompleted += (_, r) =>
            _logger.LogDebug("<< Step completed: [{StepId}] {Name} success={Success}", r.StepId, r.StepName, r.Success);
        _engine.WorkflowCompleted += (_, r) =>
            _history[r.WorkflowId] = r;
    }

    /// <summary>从 JSON 加载工艺流程定义（上位机下发）。</summary>
    public WorkflowDefinition LoadWorkflowFromJson(string json)
    {
        var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Failed to parse workflow JSON.");
        RegisterWorkflow(workflow);
        return workflow;
    }

    /// <summary>注册工艺流程定义。</summary>
    public void RegisterWorkflow(WorkflowDefinition workflow)
    {
        _workflows[workflow.WorkflowId] = workflow;
        _logger.LogInformation("Workflow registered: [{Id}] {Name}", workflow.WorkflowId, workflow.Name);
    }

    /// <summary>执行已注册的工艺流程。</summary>
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        string workflowId, CancellationToken ct = default)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
            throw new KeyNotFoundException($"Workflow '{workflowId}' not found.");

        _logger.LogInformation("Starting workflow [{Id}] {Name}", workflowId, workflow.Name);
        var result = await _engine.ExecuteWorkflowAsync(workflow, ct);
        _history[workflowId] = result;
        return result;
    }

    /// <summary>直接执行传入的工艺流程定义。</summary>
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        WorkflowDefinition workflow, CancellationToken ct = default)
    {
        RegisterWorkflow(workflow);
        return await ExecuteWorkflowAsync(workflow.WorkflowId, ct);
    }

    /// <summary>取消当前正在执行的工艺流程。</summary>
    public Task CancelCurrentAsync() => _engine.CancelCurrentWorkflowAsync();

    /// <summary>获取工艺流程执行历史。</summary>
    public WorkflowExecutionResult? GetExecutionResult(string workflowId)
    {
        _history.TryGetValue(workflowId, out var result);
        return result;
    }

    /// <summary>获取所有已注册的工艺流程。</summary>
    public IReadOnlyCollection<WorkflowDefinition> GetAllWorkflows() =>
        _workflows.Values.ToList().AsReadOnly();
}
