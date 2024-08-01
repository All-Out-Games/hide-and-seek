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

public class World : Component
{
    [Serialized] public Entity HunterSpawnsParent;
    [Serialized] public Entity PropSpawnsParent;
    [Serialized] public Entity HunterBarrier;
    [Serialized] public Entity PropsParent;

    public List<Entity> HunterSpawns = new();
    public List<Entity> PropSpawns = new();
    public List<Sprite_Renderer> Props = new();

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