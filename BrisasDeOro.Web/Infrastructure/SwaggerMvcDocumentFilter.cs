using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BrisasDeOro.Web.Infrastructure;

/// <summary>
/// Document filter that exposes all conventional-route MVC controllers in Swagger.
/// By default Swashbuckle only discovers attribute-routed / [ApiController] actions;
/// this filter reads IActionDescriptorCollectionProvider directly.
/// </summary>
public class SwaggerMvcDocumentFilter : IDocumentFilter
{
    private readonly IActionDescriptorCollectionProvider _provider;

    public SwaggerMvcDocumentFilter(IActionDescriptorCollectionProvider provider)
        => _provider = provider;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var descriptors = _provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .OrderBy(d => d.ControllerName)
            .ThenBy(d => d.ActionName);

        foreach (var descriptor in descriptors)
        {
            var httpMethods = GetHttpMethods(descriptor);
            var (path, parameters) = BuildPath(descriptor);

            var operation = new OpenApiOperation
            {
                Tags       = new List<OpenApiTag> { new OpenApiTag { Name = descriptor.ControllerName } },
                Summary    = $"{descriptor.ControllerName} › {descriptor.ActionName}",
                Parameters = parameters,
                Responses  = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse { Description = "OK" },
                    ["302"] = new OpenApiResponse { Description = "Redirect" },
                    ["401"] = new OpenApiResponse { Description = "Unauthorized" },
                }
            };

            if (!swaggerDoc.Paths.TryGetValue(path, out var pathItem))
            {
                pathItem = new OpenApiPathItem();
                swaggerDoc.Paths[path] = pathItem;
            }

            foreach (var method in httpMethods)
            {
                // Use a unique OperationId: Controller_Action_HTTPMETHOD
                operation = CloneWithId(operation,
                    $"{descriptor.ControllerName}_{descriptor.ActionName}_{method}");

                if (!pathItem.Operations.ContainsKey(method))
                    pathItem.Operations[method] = operation;
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (string path, IList<OpenApiParameter> parameters) BuildPath(
        ControllerActionDescriptor d)
    {
        var parameters = new List<OpenApiParameter>();
        var path = $"/{d.ControllerName}/{d.ActionName}";
        var addedToPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in d.Parameters)
        {
            var bindingSource = param.BindingInfo?.BindingSource?.Id;
            var isSimple = IsSimpleType(param.ParameterType);

            // Route parameter: explicit Path binding OR named "id" with simple type
            if (bindingSource == "Path" ||
                (isSimple && param.Name.Equals("id", StringComparison.OrdinalIgnoreCase)
                          && bindingSource is null or "Path"))
            {
                if (addedToPath.Add(param.Name))
                {
                    path += $"/{{{param.Name}}}";
                    parameters.Add(new OpenApiParameter
                    {
                        Name     = param.Name,
                        In       = ParameterLocation.Path,
                        Required = true,
                        Schema   = new OpenApiSchema { Type = ToSchemaType(param.ParameterType) }
                    });
                }
            }
            // Query parameter: explicit Query binding OR simple type without other binding
            else if (isSimple && bindingSource is null or "Query")
            {
                parameters.Add(new OpenApiParameter
                {
                    Name   = param.Name,
                    In     = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = ToSchemaType(param.ParameterType) }
                });
            }
        }

        return (path, parameters);
    }

    private static IList<OperationType> GetHttpMethods(ControllerActionDescriptor d)
    {
        var constraints = d.ActionConstraints?
            .OfType<HttpMethodActionConstraint>()
            .SelectMany(c => c.HttpMethods)
            .Distinct()
            .ToList();

        if (constraints is null || constraints.Count == 0)
            return new List<OperationType> { OperationType.Get };

        return constraints
            .Select(m => m.ToUpperInvariant() switch
            {
                "POST"   => OperationType.Post,
                "PUT"    => OperationType.Put,
                "DELETE" => OperationType.Delete,
                "PATCH"  => OperationType.Patch,
                _        => OperationType.Get
            })
            .ToList();
    }

    private static bool IsSimpleType(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t.IsPrimitive || t == typeof(string)
            || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(Guid);
    }

    private static string ToSchemaType(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        if (t == typeof(int) || t == typeof(long) || t == typeof(short)) return "integer";
        if (t == typeof(bool))                                             return "boolean";
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return "number";
        return "string";
    }

    private static OpenApiOperation CloneWithId(OpenApiOperation op, string id)
        => new OpenApiOperation
        {
            Tags        = op.Tags,
            Summary     = op.Summary,
            OperationId = id,
            Parameters  = op.Parameters,
            Responses   = op.Responses,
        };
}
