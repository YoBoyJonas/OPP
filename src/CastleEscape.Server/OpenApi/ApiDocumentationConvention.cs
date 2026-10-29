using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace CastleEscape.Server.OpenApi;

/// <summary>Marks an endpoint that acts as a player and needs the <c>X-Player-Token</c> header.</summary>
public sealed class PlayerTokenRequiredMetadata;

/// <summary>
/// Gives every controller action its entry from <see cref="ApiDocumentation"/>: summary, description, response
/// types and the player-token requirement. This is the same metadata the <c>[EndpointSummary]</c>,
/// <c>[ProducesResponseType]</c>, ... attributes would add, so OpenAPI and Scalar see no difference.
/// </summary>
public sealed class ApiDocumentationConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        var where = $"{action.Controller.ControllerName}Controller.{action.ActionName}";
        var name = action.Selectors.Select(s => s.AttributeRouteModel?.Name).FirstOrDefault(n => n is not null)
                   ?? throw new InvalidOperationException($"{where} needs a route name, e.g. [HttpGet(\"...\", Name = \"...\")].");
        if (!ApiDocumentation.Endpoints.TryGetValue(name, out var doc))
        {
            throw new InvalidOperationException($"{where} (\"{name}\") has no entry in ApiDocumentation.Endpoints.");
        }

        // A void type means "the action's declared ActionResult<T>".
        var successType = doc.SuccessType ?? typeof(void);
        action.Filters.Add(doc.ContentTypes is [var first, .. var others]
            ? new ProducesResponseTypeAttribute(successType, doc.Success, first, others)
            : new ProducesResponseTypeAttribute(successType, doc.Success));
        foreach (var status in doc.Errors ?? [])
        {
            action.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), status, "application/problem+json"));
        }

        foreach (var selector in action.Selectors)
        {
            selector.EndpointMetadata.Add(new EndpointSummaryAttribute(doc.Summary));
            if (doc.Description is not null)
            {
                selector.EndpointMetadata.Add(new EndpointDescriptionAttribute(doc.Description));
            }
            if (doc.PlayerToken)
            {
                selector.EndpointMetadata.Add(new PlayerTokenRequiredMetadata());
            }
            if (doc.Tag is not null)
            {
                selector.EndpointMetadata.Add(new TagsAttribute(doc.Tag));
            }
        }
    }
}
