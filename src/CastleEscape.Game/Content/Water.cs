namespace CastleEscape.Game.Content;

public class Water : Obstacle
{
    public Water()
    {
    }

    protected Water(Water template) : base(template)
    {
    }

    public override ObstacleKind Kind => ObstacleKind.Water;
}
