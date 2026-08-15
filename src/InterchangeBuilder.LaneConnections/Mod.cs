using Game;
using Game.Modding;

namespace InterchangeBuilder.LaneConnections;

public sealed class Mod : IMod
{
    public void OnLoad(UpdateSystem updateSystem) => Bootstrap.Install(updateSystem);

    public void OnDispose() => Bootstrap.Uninstall();
}
