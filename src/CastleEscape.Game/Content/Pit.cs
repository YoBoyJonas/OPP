namespace CastleEscape.Game.Content;

public class Pit : Obstacle
{
    public Pit()
    {
    }

    protected Pit(Pit template) : base(template)
    {
    }

    public override ObstacleKind Kind => ObstacleKind.Pit;
}
