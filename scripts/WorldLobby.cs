using AO;

public class WorldLobby : World
{
	public override void Awake()
	{
		foreach (var c in PropSpawnsParent.Children)
		{
			PropSpawns.Add(c);
		}
	}
}