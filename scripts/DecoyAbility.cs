using AO;

public class DecoyAbility : MyAbility
{
    public override TargettingMode TargettingMode => TargettingMode.Self;
    public override float Cooldown => 10f;

    public override bool OnTryActivate(List<Player> targetPlayers, Vector2 positionOrDirection, float magnitude)
    {
        if (Network.IsServer)
        {
            var propClone = Player.PropEntity.Clone();
            propClone.SetParent(null, false);
            propClone.LocalPosition = Player.PropEntity.Position;
            propClone.LocalScale = Player.PropEntity.Scale;
            propClone.AddComponent<DecoyProp>();
            Network.Spawn(propClone);
        }
        return true;
    }
}

public class DecoyProp : Component
{
    public Vector2 StartScale;
    public float t;
    public override void Awake()
    {
        StartScale = Entity.LocalScale;
    }

    public override void Update()
    {
        t += Time.DeltaTime * 5;
        t = Math.Clamp(t, 0, 1);
        Entity.LocalScale = StartScale * Ease.OutBack(t);
    }
}