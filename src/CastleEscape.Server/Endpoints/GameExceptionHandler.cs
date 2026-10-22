using CastleEscape.Contracts;
using CastleEscape.Game.Sessions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CastleEscape.Server.Endpoints;

/// <summary>
/// Turns <see cref="GameException"/> into a ProblemDetails response with the right status and a
/// machine-readable <c>code</c>. Malformed requests become 400. Everything else stays a 500.
/// </summary>
public sealed class GameExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public static int StatusFor(GameErrorCode code) => code switch
    {
        GameErrorCode.SessionNotFound or GameErrorCode.InvalidJoinCode => StatusCodes.Status404NotFound,
        GameErrorCode.SessionFull or GameErrorCode.AlreadyStarted or GameErrorCode.WrongPhase
            or GameErrorCode.NoLevelLoaded => StatusCodes.Status409Conflict,
        GameErrorCode.InvalidPlayerToken => StatusCodes.Status401Unauthorized,
        GameErrorCode.UnknownCharacter or GameErrorCode.InvalidRequest => StatusCodes.Status400BadRequest,
        GameErrorCode.LevelGenerationFailed => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, detail) = exception switch
        {
            GameException game => (StatusFor(game.Code), game.Code, game.Message),
            BadHttpRequestException bad => (StatusCodes.Status400BadRequest, GameErrorCode.InvalidRequest, bad.Message),
            _ => (0, default, ""),
        };
        if (status == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = code.ToString(),
                Detail = detail,
                Extensions = { ["code"] = code.ToString() },
            },
        });
    }
}
