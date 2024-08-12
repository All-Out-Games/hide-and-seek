using AO;

public class World : Component
{
	[Serialized] public Entity HunterSpawnsParent;
	[Serialized] public Entity PropSpawnsParent;
	[Serialized] public Entity HunterBarrier;
	[Serialized] public Entity PropsParent;

	public List<Entity> HunterSpawns = new();
	public List<Entity> PropSpawns = new();
	public List<Sprite_Renderer> Props = new();
	
	[Serialized] public string MapName;


	public override void Awake()
	{
		WorldManager.Instance.Worlds.Add(this);
		foreach (var c in HunterSpawnsParent.Children)
		{
			HunterSpawns.Add(c);
		}

		foreach (var c in PropSpawnsParent.Children)
		{
			PropSpawns.Add(c);
		}

		foreach (var c in PropsParent.Children)
		{
			Props.Add(c.GetComponent<Sprite_Renderer>());
		}
	}

	public Sprite_Renderer GetPristineProp(int index)
	{
		return Props[index % Props.Count];
	}
}