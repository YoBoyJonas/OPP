using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CastleEscape.Server.OpenApi;

/// <summary>Marks an endpoint that acts as a player and needs the <c>X-Player-Token</c> header.</summary>
public sealed class PlayerTokenRequiredMetadata;

public static class OpenApiSetup
{
    public const string PlayerTokenScheme = "PlayerToken";
    public const string PlayerTokenHeader = "X-Player-Token";

    public static IServiceCollection AddCastleEscapeOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer(DescribeDocument);
            options.AddOperationTransformer(AddPlayerTokenRequirement);
        });

    /// <summary>Documents the endpoint as needing the player token, so Scalar sends the header.</summary>
    public static TBuilder RequirePlayerToken<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new PlayerTokenRequiredMetadata());

    private static Task DescribeDocument(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Castle Escape API",
            Version = "v1",
            Description = "Authoritative game server for Castle Escape (KTU T120B516). "
                          + "Lobby and inspection over REST, real-time play over the SignalR hub at /hubs/game. "
                          + "Endpoints that act as a player need the X-Player-Token header returned by create/join.",
        };

        document.Tags = new HashSet<OpenApiTag>(
            ApiTags.Groups.SelectMany(g => g.Tags).Select(t => new OpenApiTag { Name = t.Name, Description = t.Description }));

        // Scalar renders these as the sidebar sections Lobby / Gameplay / Content / Course / Ops.
        var groups = new JsonArray();
        foreach (var group in ApiTags.Groups)
        {
            var tags = new JsonArray();
            foreach (var tag in group.Tags)
            {
                tags.Add(tag.Name);
            }
            groups.Add(new JsonObject { ["name"] = group.Name, ["tags"] = tags });
        }
        document.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        document.Extensions["x-tagGroups"] = new JsonNodeExtension(groups);

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[PlayerTokenScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = PlayerTokenHeader,
            Description = "The playerToken returned by POST /api/sessions or POST /api/sessions/join.",
        };

        return Task.CompletedTask;
    }

    private static Task AddPlayerTokenRequirement(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<PlayerTokenRequiredMetadata>().Any())
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(PlayerTokenScheme, context.Document)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
