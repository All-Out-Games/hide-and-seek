using AO;
using System.Collections;

public partial class GameManager : Component
{
    [AOIgnore] public static GameManager Instance;
    
    public const int RoleNameLayer = 200;
    public const int PlayersNeededToStartGame = 2;
    public const float HideTime = 10f;

    public SyncVar<float> Countdown = new();
    public SyncVar<float> HideTimer = new();
    public SyncVar<bool> BarrierEnabled = new();

    private SyncVar<int> _currentState = new();
    public GameState State
    {
        get => (GameState)_currentState.Value;
        set => _currentState.Set((int)value);
    }

    [Serialized] public Entity HunterSpawnsParent;
    [Serialized] public Entity PropSpawnsParent;
    [Serialized] public Entity HunterBarrier;

    public List<Entity> HunterSpawns = new();
    public List<Entity> PropSpawns = new();

    public Dictionary<PlayerRole, PlayerRoleDefinition> Roles = new Dictionary<PlayerRole, PlayerRoleDefinition>()
    {
        [PlayerRole.Spectator] = new () { ID = 0, roleName = "Spectator"},
        [PlayerRole.Hunter] = new () { ID = 1, roleName = "Hunter"},
        [PlayerRole.Prop]    = new () { ID = 2, roleName = "Prop"},
    };

    public override void Awake()
    {
        Instance = this;

        foreach (var c in HunterSpawnsParent.Children)
        {
            HunterSpawns.Add(c);
        }

        foreach (var c in PropSpawnsParent.Children)
        {
            PropSpawns.Add(c);
        }

        BarrierEnabled.OnSync += (_, v) => {
            HunterBarrier.LocalEnabled = v;
        };
    }

    public override void Start()
    {
        UI.SetLeaderboardOpen(false);
        Chat.RegisterChatCommandHandler(RunChatCommand);
        
        if (Network.IsServer)
        {
            State = GameState.WaitingForPlayers;   
        }
        else
        {
            Chat.SetChatMode(Chat.Mode.BubbleOnly);
        }
    }

    public override void OnDestroy()
    {
    }

    [ClientRpc]
    public void Noclip(HNSPlayer player, bool on)
    {
        // if (on)
        // {
        //     if (!player.HasEffect<NoClipEffect>())
        //     {
        //         player.AddEffect<NoClipEffect>();
        //     }
        // }
        // else
        // {
        //     player.RemoveEffect<NoClipEffect>(false);
        // }
    }

    public void RunChatCommand(Player p, string command)
    {
        var parts = command.Split(' ');
        var cmd = parts[0].ToLowerInvariant();
        HNSPlayer player = (HNSPlayer)p;
        var allowCommands = player.IsAdmin || Game.LaunchedFromEditor;
        if (!allowCommands && player.UserId == "65976031d3af49fc5eca9b3f") allowCommands = true; // Ian

        if (!allowCommands)
        {
            return;
        }

        switch (cmd)
        {
            case "help": 
            {
                Chat.SendMessage(p,"\nChat Commands:\n"+
                "/z[oom] <multiplier> : Set camera zoom\n"+
                "/s[tart] : start round immediately\n"+
                "/noclip [on]\n"+
                "/shadows [on]\n"+
                "/restart : Restart round immediately\n"+
                "/god [on | off] : Toggle godmode. Shadows off, speed 3, zoom 3, noclip.\n"+
                "");
                break;
            }
            case "die": 
            {
                // player.TakeDamage();
                break;
            }
            case "noclip":
            {
                // bool on = !player.HasEffect<NoClipEffect>();
                // if (parts.Length == 2)
                // {
                //     on = parts[1] == "on";
                // }
                // CallClient_Noclip(player, on);
                break;
            }
            case "god":
            {
                if (parts.Length == 1 || parts[1] == "on")
                {
                    RunChatCommand(p, "noclip on");
                    RunChatCommand(p, "zoom 3");
                    RunChatCommand(p, "sp 3");
                    RunChatCommand(p, "shadows off");
                }
                else
                {
                    RunChatCommand(p, "noclip off");
                    RunChatCommand(p, "zoom 1");
                    RunChatCommand(p, "sp 1");
                    RunChatCommand(p, "shadows on");
                }
                break;
            }
            case "s":
            case "start":
            {
                State = GameState.CountingDown;
                Countdown.Set(0);
                break;
            }
            case "z":
            case "zoom":
            {
                // if (parts.Length != 2)
                // {
                //     Chat.SendMessage(player, "/zoom needs a zoom value as a parameter.");
                //     break;
                // }
                // if (float.TryParse(parts[1], out var z))
                // {
                //     player.CurrentZoomLevel.Set(z);
                // }
                break;
            }
            case "shadows":
            {
                // bool on = !player.ShadowsEnabled;
                // if (parts.Length == 2)
                // {
                //     on = parts[1] == "on";
                // }
                // player.ShadowsEnabled.Set(on);
                break;
            }
            case "restart":
            {
                State = GameState.WaitingForPlayers;
                break;
            }
        }
    }

    [ClientRpc]
    public void ClearAllPlayerEffects()
    {
        foreach (var player in Scene.Components<HNSPlayer>(false))
        {
            player.ClearAllEffects();
        }
    }

    public void SetUpRound()
    {
        Util.Assert(Network.IsServer, "SetUpRound can only be called on the server");
        Util.Assert(Player.AllPlayers.Count >= 2, "We need at least 2 players to start the game!");

        Log.Info("RESETTING ROUND ------------------");

        var players = new List<HNSPlayer>(Player.AllPlayers.Cast<HNSPlayer>());
        players.Shuffle();
        
        var huntersCount = (int) (players.Count * 0.2f);
        huntersCount = Math.Max(1, huntersCount);

        var hunterSpawns = new List<Entity>(HunterSpawns);
        var propSpawns = new List<Entity>(PropSpawns);

        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            player.PlayerRole = i < huntersCount ? PlayerRole.Hunter : PlayerRole.Prop;

            if (player.PlayerRole == PlayerRole.Hunter)
            {
                var spawn = hunterSpawns[0];
                hunterSpawns.RemoveAt(0);
                if (hunterSpawns.Count == 0)
                {
                    hunterSpawns = new List<Entity>(HunterSpawns);
                }
                player.Teleport(spawn.Position);
            }
            else
            {
                var spawn = propSpawns[0];
                propSpawns.RemoveAt(0); 
                if (propSpawns.Count == 0)
                {
                    propSpawns = new List<Entity>(PropSpawns);
                }
                player.Teleport(spawn.Position);
            }
        }
    }

    public void MessageAllPlayers(string message)
    {
        foreach (var player in Player.AllPlayers)
        {
            Chat.SendMessage(player, message);
        }
    }

    public override void Update()
    {
        if (Network.IsServer)
        {
            switch (State)
            {
                case GameState.WaitingForPlayers:
                {
                    if (Player.AllPlayers.Count >= PlayersNeededToStartGame)
                    {
                        MessageAllPlayers("STARTING ROUND IN 30 SECONDS");
                        State = GameState.CountingDown;
                        Countdown.Set(30f);
                        BarrierEnabled.Set(false);
                    }
                    break;
                }
                case GameState.CountingDown:
                {
                    Countdown.Set(Countdown - Time.DeltaTime);
                    if (Countdown < 0f)
                    {
                        State = GameState.StartRound;
                    }
                    break;
                }
                case GameState.StartRound:
                {
                    try 
                    {
                        MessageAllPlayers("STARTING ROUND!!!");
                        SetUpRound();
                        Countdown.Set(0);
                        HideTimer.Set(HideTime);
                        State = GameState.Hiding;
                        BarrierEnabled.Set(true);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                        State = GameState.WaitingForPlayers;
                    }
                    
                    break;
                }
                case GameState.Hiding:
                {
                    HideTimer.Set(HideTimer - Time.DeltaTime);
                    if (HideTimer < 0f)
                    {
                        MessageAllPlayers("Ready or not, here we come!");
                        State = GameState.Round;
                        BarrierEnabled.Set(false);
                    }
                    break;
                }
                case GameState.Round:
                {
                    break;
                }
            }
        }

        var localPlayer = (HNSPlayer)Network.LocalPlayer;
        if (localPlayer != null)
        {
            var topBarRect = UI.ScreenRect.CutTop(200);
            var midBarRect  = UI.ScreenRect.SubRect(0.5f, 0.8f, 0.5f, 0.8f);
            var midBarRect2 = UI.ScreenRect.SubRect(0.5f, 0.2f, 0.5f, 0.2f);

            var bottomBarRect = UI.ScreenRect.CutBottom(350);

            using var _ = UI.PUSH_LAYER(RoleNameLayer);

            // if (localPlayer.HideHudReasons.Count == 0 && localPlayer.PlayerRole != PlayerRole.Spectator)
            // {
            //     UI.Text(topBarRect, localPlayer.Region, GetTextSettings(42, 0f, null));
            // }

            switch (State)
            {
                case GameState.WaitingForPlayers:
                {
                    UI.Text(bottomBarRect, $"Waiting for players ({Player.AllPlayers.Count}/{PlayersNeededToStartGame})",GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
                case GameState.CountingDown:
                {
                    UI.Text(bottomBarRect,("Round starts in "+Math.Round(Countdown)).ToString()+" seconds...",GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
                case GameState.StartRound:
                {
                    UI.Text(bottomBarRect,"Starting round...",GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
                case GameState.Hiding:
                {
                    UI.Text(bottomBarRect,"Hiding: " + Math.Round(HideTimer) + "s", GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
                case GameState.Round:
                {
                    break;
                }
            }
        }
    }

    public UI.TextSettings GetTextSettings(float size, float offset = 0, FontAsset font = null,UI.HorizontalAlignment halign = UI.HorizontalAlignment.Center,UI.VerticalAlignment valign = UI.VerticalAlignment.Center)
    {
        if (font == null)
        {
            font = UI.Fonts.BarlowBold;
        }
        var ts = new UI.TextSettings()
        {
            Font = font,
            Size = size,
            Color = Vector4.White,
            DropShadow = true,
            DropShadowColor = new Vector4(0f,0f,0.02f,0.5f),
            DropShadowOffset = new Vector2(0f,-3f),
            HorizontalAlignment = halign,
            VerticalAlignment = valign,
            WordWrap = false,
            WordWrapOffset = 0,
            Outline = true,
            OutlineThickness = 3,
            Offset = new Vector2(0, offset),
        };
        return ts;
    }

    public Vector2 GetOnCircle(float angleDegrees, float radius)
    {
        // initialize calculation variables
        float _x = 0;
        float _y = 0;
        float angleRadians = 0;
        Vector2 _returnVector;

        // convert degrees to radians
        angleRadians = angleDegrees * (float)Math.PI / 180.0f;

        // get the 2D dimensional coordinates
        _x = radius * (float)Math.Cos(angleRadians);
        _y = radius * (float)Math.Sin(angleRadians);

        // derive the 2D vector
        _returnVector = new Vector2(_x, _y);

        // return the vector info
        return _returnVector;
    }
    
    public class PlayerRoleDefinition
    {
        public int ID;
        public string roleName;
    }
}

public enum GameState
{
    WaitingForPlayers,
    CountingDown,
    StartRound,
    Hiding,
    Round,
}

public enum PlayerRole
{
    Spectator,
    Hunter,
    Prop,
}
