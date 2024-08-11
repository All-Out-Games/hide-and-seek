using AO;

public partial class VoteManager : Component
{
    private static VoteManager instance;
    public static VoteManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Entity.FindByName("GameManager").GetComponent<VoteManager>();
            }
            return instance;
        }
        private set => instance = value;
    }

    private SyncVar<int> _firstMapRandom = new();
    private SyncVar<int> _secondMapRandom = new();
    private SyncVar<int> _thirdMapRandom = new();

    public List<SyncVar<int>> MapsSelected;

    public override void Awake()
    {
        MapsSelected = new List<SyncVar<int>>
        {
            _firstMapRandom,
            _secondMapRandom,
            _thirdMapRandom
        };
    }

    public void SelectRandomMaps()
    {
        if (Network.IsServer)
        {
            var maps = WorldManager.Instance.Worlds;
            var mapIndexes = new List<int>();
            for (int i = 0; i < maps.Count; i++)
            {
                mapIndexes.Add(i);
            }
            foreach(var RandomMap in MapsSelected)
            {
                var rand = new Random();
                var randomInt = rand.Next(0, mapIndexes.Count);
                RandomMap.Set(randomInt);
                mapIndexes.Remove(randomInt);
            }
        }
    }
}