using AO;

public class WorldLobby : World
{
	public override void Awake()
	{
		WorldManager.Instance.Lobby = this;

		foreach (var c in PropSpawnsParent.Children)
		{
			PropSpawns.Add(c);
		}
	}
}