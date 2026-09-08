using AO;

public class WorldManager : Component
{
	private static WorldManager instance;
	public static WorldManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Entity.FindByName("GameManager").GetComponent<WorldManager>();
			}
			return instance;
		}
		private set => instance = value;
	}

	public List<World> Worlds = new();
	[Serialized] public World Lobby;
	public World CurrentWorld => Worlds[CurrentWorldIndex.Value];
	public SyncVar<int> CurrentWorldIndex = new();

	public override void Awake()
	{
		Instance = this;
	}

	public override void OnDestroy()
	{
		Instance = null;
	}
}