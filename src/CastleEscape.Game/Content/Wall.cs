namespace CastleEscape.Game.Content;

public class Wall : Obstacle
{
    public Wall()
    {
    }

    protected Wall(Wall template) : base(template)
    {
    }

    public override ObstacleKind Kind => ObstacleKind.Wall;
}
