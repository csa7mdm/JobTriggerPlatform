using Microsoft.AspNetCore.Mvc.Controllers;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Reflection;

namespace JobTriggerPlatform.WebApi.OpenApi;

/// <summary>
/// Operation processor that adds summary descriptions from XML comments.
/// </summary>
public class OperationSummaryFilter : IOperationProcessor
{
    /// <summary>
    /// Processes the operation and adds summary descriptions.
    /// </summary>
    /// <param name="context">The operation processor context.</param>
    /// <returns>True if the operation was processed, false otherwise.</returns>
    public bool Process(OperationProcessorContext context)
    {
        if (context.MethodInfo == null)
            return true;

        // Set operation ID based on controller and action name
        if (context.ControllerType != null)
        {
            var controllerName = context.ControllerType.Name;
            if (controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                controllerName = controllerName[..^10]; // Remove "Controller" suffix
            }

            context.OperationDescription.Operation.OperationId = $"{controllerName}_{context.MethodInfo.Name}";
        }
        else
        {
            // For minimal API endpoints
            context.OperationDescription.Operation.OperationId = context.MethodInfo.Name;
        }
        
        return true;
    }
}