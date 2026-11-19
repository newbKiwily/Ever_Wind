namespace EverWind.Gameplay
{
public sealed class MonsterServerTransmitter : AliveServerTransmitter
{
    public int Definition { get; private set; }

    protected override void InitializeEntity(ServerMessage spawn)
    {
        Definition = spawn.Definition;

        base.InitializeEntity(spawn);
    }
}
}
