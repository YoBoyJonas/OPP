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
            options.AddDocumentTransformer(AddPlayerTokenScheme);
            options.AddOperationTransformer(AddPlayerTokenRequirement);
        });

    /// <summary>Documents the endpoint as needing the player token, so Scalar sends the header.</summary>
    public static TBuilder RequirePlayerToken<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new PlayerTokenRequiredMetadata());

    private static Task AddPlayerTokenScheme(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
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
