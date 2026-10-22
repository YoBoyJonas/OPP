using CastleEscape.Contracts;
using CastleEscape.Contracts.Gameplay;
using CastleEscape.Contracts.Realtime;
using CastleEscape.Contracts.Sessions;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.Messaging;
using CastleEscape.Game.Sessions;
using CastleEscape.Server.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace CastleEscape.Server.Endpoints;

/// <summary>
/// REST mirror of the hub, so everything can be tried from Scalar: send input, restart, and read
/// the state, layout and HUD. Reads use the session's latest snapshot and never lock the world.
/// </summary>
public static class GameplayEndpoints
{
    public static IEndpointRouteBuilder MapGameplayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions/{sessionId:guid}").WithTags("Gameplay");

        group.MapPost("/input", SubmitInput)
            .RequirePlayerToken();

        group.MapPost("/restart", Restart)
            .RequirePlayerToken();

        group.MapGet("/state", GetState);

        group.MapGet("/messages", GetMessages)
            .WithTags("Realtime");

        group.MapGet("/level", GetLevel);

        group.MapGet("/hud", GetHud);

        return app;
    }

    private static Accepted SubmitInput(Guid sessionId, DirectionRequest request, HttpContext http, GameFacade game)
    {
        game.SubmitDirection(sessionId, http.PlayerToken(), request.Direction);
        return TypedResults.Accepted((string?)null);
    }

    private static Accepted Restart(Guid sessionId, HttpContext http, GameFacade game)
    {
        game.RequestRestart(sessionId, http.PlayerToken());
        return TypedResults.Accepted((string?)null);
    }

    /// <summary>JSON or XML through the Adapter (IMessageSerializer): <c>?format=</c> wins over the Accept header.</summary>
    private static IResult GetState(Guid sessionId, string? format, HttpRequest request, GameFacade game)
    {
        var state = game.GetState(sessionId);
        var serializer = ChooseSerializer(format, request);
        return Results.Text(serializer.Serialize(state), serializer.ContentType, System.Text.Encoding.UTF8);
    }

    private static Ok<PolledMessagesResponse> GetMessages(Guid sessionId, long? afterSeq, string? format, HttpRequest request,
        GameFacade game, PollingBufferChannel polling, IOptions<RealtimeOptions> realtime)
    {
        game.GetSession(sessionId); // 404 for an unknown session
        if (!realtime.Value.EnabledChannels.Contains(RealtimeChannel.Polling))
        {
            throw new GameException(GameErrorCode.InvalidRequest, "The polling channel is disabled (Realtime:EnabledChannels).");
        }
        var serializer = ChooseSerializer(format, request);
        var after = afterSeq ?? 0;
        var messages = polling.Read(sessionId, after)
            .Select(m => new PolledMessageDto(m.Seq, m.Method, serializer.Serialize(m.Message)))
            .ToArray();
        return TypedResults.Ok(new PolledMessagesResponse(sessionId, serializer.ContentType,
            messages.Length > 0 ? messages[^1].Seq : after, messages));
    }

    private static IMessageSerializer ChooseSerializer(string? format, HttpRequest request)
    {
        try
        {
            return MessageSerializers.Choose(format, request.Headers.Accept);
        }
        catch (ArgumentException ex)
        {
            throw new GameException(GameErrorCode.InvalidRequest, ex.Message);
        }
    }

    private static Ok<LevelLayoutResponse> GetLevel(Guid sessionId, GameFacade game) => TypedResults.Ok(game.GetLevelLayout(sessionId));

    private static Ok<HudResponse> GetHud(Guid sessionId, GameFacade game) => TypedResults.Ok(game.GetHud(sessionId));
}
