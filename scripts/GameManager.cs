using AO;
using System.Collections;

public partial class GameManager : Component
{
    public const int IntroLayer = 1000;

    [AOIgnore] public static GameManager Instance;
    
    public const int RoleNameLayer = 200;
    public const int PlayersNeededToStartGame = 2;
    public const float HideTime = 15f;
    public const float SeekTime = 60 * 3;

    public float CurrentTimer;
    public SyncVar<int> Countdown = new();
    public SyncVar<int> HideTimer = new();
    public SyncVar<int> SeekTimer = new();
    public SyncVar<int> EndRoundTime = new();
    public SyncVar<bool> BarrierEnabled = new();

    public float SeekTimeCountdownTime;

    public float VignetteFader;

    public SyncVar<bool> VoiceChatEnabled = new(false);

    private SyncVar<int> _currentState = new();
    public GameState State
    {
        get => (GameState)_currentState.Value;
        set => _currentState.Set((int)value);
    }

    private SyncVar<int> _currentWinner = new();
    public PlayerRole Winner
    {
        get => (PlayerRole)_currentWinner.Value;
        set => _currentWinner.Set((int)value);
    }

    public Dictionary<PlayerRole, PlayerRoleDefinition> Roles = new Dictionary<PlayerRole, PlayerRoleDefinition>()
    {
        [PlayerRole.Spectator] = new () { ID = 0, RoleName = "Spectator", RoleColor = new Vector4(0.35f, 0.76f, 0.98f, 1f)},
        [PlayerRole.Seeker]    = new () { ID = 1, RoleName = "Seeker",    RoleColor = new Vector4(1, 0, 0, 1)},
        [PlayerRole.Prop]      = new () { ID = 2, RoleName = "Hider",     RoleColor = new Vector4(0, 1, 1, 1)},
    };

    public override void Awake()
    {
        Instance = this;

        VoiceChatEnabled.OnSync += (_, enabled) => {
            if (Network.LocalPlayer != null)
            {
                if (enabled && Network.LocalPlayer.HasEffect<SpectatorEffect>() == false)
                {
                    Game.SetVoiceEnabled(true);
                }
            }

            if (enabled == false)
            {
                Game.SetVoiceEnabled(false);
            }
        };

        SeekTimer.OnSync += (old, value) =>
        {
            if (value <= 10)
            {
                SeekTimeCountdownTime = Time.TimeSinceStartup;
            }
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

        BarrierEnabled.OnSync += (_, v) =>
        {
            WorldManager.Instance.CurrentWorld.HunterBarrier.LocalEnabled = v;
        };

        Leaderboard.RegisterSortCallback((Player[] players) =>
        {
            Array.Sort(players, (a, b) =>
            {
                return ((HNSPlayer)b).WinsSync.Value.CompareTo(((HNSPlayer)a).WinsSync);
            });
        });

        Leaderboard.Register("Wins", (Player[] players, string[] scores) =>
        {
            for (int i = 0; i < players.Length; i++)
            {
                var player = (HNSPlayer)players[i];
                scores[i] = $"{player.WinsSync:N0}";
            }
        });

        Leaderboard.Register("Role", (Player[] players, string[] scores) =>
        {
            for (int i = 0; i < players.Length; i++)
            {
                var player = (HNSPlayer)players[i];
                scores[i] = Roles[player.PlayerRole].RoleName;
            }
        });
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

    // public static UI.TextSettings GetTextSettings(float size, float offset = 0, FontAsset font = null, UI.HorizontalAlignment halign = UI.HorizontalAlignment.Center)
    // {
    //     if (font == null)
    //     {
    //         font = UI.Fonts.BarlowBold;
    //     }
    //     var ts = new UI.TextSettings()
    //     {
    //         Font = font,
    //         Size = size,
    //         Color = Vector4.White,
    //         DropShadowColor = new Vector4(0f,0f,0f,0.5f),
    //         DropShadowOffset = new Vector2(0f,-3f),
    //         HorizontalAlignment = halign,
    //         VerticalAlignment = UI.VerticalAlignment.Center,
    //         WordWrap = false,
    //         WordWrapOffset = 0,
    //         Outline = true,
    //         OutlineThickness = 3,
    //         Offset = new Vector2(0, offset),
    //     };
    //     return ts;
    // }

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
                "/voice : Toggles voice chat on and off.\n"+
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
            case "voice":
            {
                VoiceChatEnabled.Set(!VoiceChatEnabled);
                break;
            }
            case "s":
            case "start":
            {
                State = GameState.CountingDown;
                CurrentTimer = 0;
                Countdown.Set((int)CurrentTimer);
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

    [ClientRpc]
    public void StartRoundForPlayers()
    {
        foreach (var p in Player.AllPlayers)
        {
            var player = (HNSPlayer)p;
            player.PreparePlayerForRound();
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

        var hunterSpawns = new List<Entity>(WorldManager.Instance.CurrentWorld.HunterSpawns);
        var propSpawns = new List<Entity>(WorldManager.Instance.CurrentWorld.PropSpawns);

        CallClient_ClearAllPlayerEffects();

        foreach (var corpse in Scene.Components<PlayerCorpse>(true))
        {
            Network.Despawn(corpse.Entity);
            corpse.Entity.Destroy();
        }

        foreach (var prop in Scene.Components<DecoyProp>(true))
        {
            Network.Despawn(prop.Entity);
            prop.Entity.Destroy();
        }

        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            player.PlayerRole = i < huntersCount ? PlayerRole.Seeker : PlayerRole.Prop;

            if (player.PlayerRole == PlayerRole.Seeker)
            {
                var spawn = hunterSpawns[0];
                hunterSpawns.RemoveAt(0);
                if (hunterSpawns.Count == 0)
                {
                    hunterSpawns = new List<Entity>(WorldManager.Instance.CurrentWorld.HunterSpawns);
                }
                player.Teleport(spawn.Position);
            }
            else
            {
                var spawn = propSpawns[0];
                propSpawns.RemoveAt(0); 
                if (propSpawns.Count == 0)
                {
                    propSpawns = new List<Entity>(WorldManager.Instance.CurrentWorld.PropSpawns);
                }
                player.Teleport(spawn.Position);
            }
        }

        CallClient_StartRoundForPlayers();
        var rand = new Random();
        foreach (var p in players)
        {
            p.CurrentPropIndex.Set(rand.Next(0, WorldManager.Instance.CurrentWorld.Props.Count));
            p.CallClient_RoundStart();
        }
    }

    public void MessageAllPlayers(string message)
    {
        foreach (var player in Player.AllPlayers)
        {
            Chat.SendMessage(player, message);
        }
    }

    public UI.TextSettings GetTextSettingsColor(float size,Vector4 textColor, float offset = 0, FontAsset font = null,UI.HorizontalAlignment halign = UI.HorizontalAlignment.Center,UI.VerticalAlignment valign = UI.VerticalAlignment.Center)
    {
        if (font == null)
        {
            font = UI.Fonts.BarlowBold;
        }
        var ts = new UI.TextSettings()
        {
            Font = font,
            Size = size,
            Color = textColor,
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

    public UI.TextSettings GetSimpleTextSettings(float size, float offset = 0, FontAsset font = null)
    {
        if (font == null)
        {
            font = UI.Fonts.BarlowBold;
        }
        var ts = new UI.TextSettings()
        {
            Font = font,
            Size = size,
            Color = new Vector4(0f,0f,0f,0f),
            HorizontalAlignment = UI.HorizontalAlignment.Center,
            VerticalAlignment = UI.VerticalAlignment.Center,
            WordWrap = false,
            WordWrapOffset = 0,
            Outline = false,
            OutlineThickness = 3,
            Offset = new Vector2(0, offset),
        };
        return ts;
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
                        State = GameState.CountingDown;
                        CurrentTimer = 15;
                        Countdown.Set((int)CurrentTimer);

                        var rand = new Random();
                        WorldManager.Instance.CurrentWorldIndex.Set(rand.Next(0, WorldManager.Instance.Worlds.Count));
                    }
                    break;
                }
                case GameState.CountingDown:
                {
                    CurrentTimer -= Time.DeltaTime;
                    Countdown.Set((int)CurrentTimer);
                    if (CurrentTimer < 0f)
                    {
                        State = GameState.StartRound;
                    }
                    break;
                }
                case GameState.StartRound:
                {
                    try 
                    {
                        SetUpRound();
                        CurrentTimer = HideTime;
                        HideTimer.Set((int)CurrentTimer);
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
                    CurrentTimer -= Time.DeltaTime;
                    HideTimer.Set((int)CurrentTimer);
                    if (CurrentTimer < 0f)
                    {
                        CurrentTimer = SeekTime;
                        SeekTimer.Set((int)CurrentTimer);
                        State = GameState.Round;
                        BarrierEnabled.Set(false);
                    }
                    break;
                }
                case GameState.Round:
                {
                    CurrentTimer -= Time.DeltaTime;
                    SeekTimer.Set((int)CurrentTimer);
                    if (CurrentTimer < 0f)
                    {
                        Winner = PlayerRole.Prop;
                        State = GameState.EndRound;
                        CurrentTimer = 10;
                        EndRoundTime.Set((int)CurrentTimer);

                        foreach (var player in Player.AllPlayers.Cast<HNSPlayer>())
                        {
                            if (player.PlayerRole == PlayerRole.Prop)
                            {
                                player.Wins += 1;
                            }
                        }
                    }
                    else
                    {
                        var seekersWin = true;
                        foreach (var p in Player.AllPlayers)
                        {
                            var player = (HNSPlayer)p;
                            if (player.PlayerRole == PlayerRole.Prop)
                            {
                                seekersWin = false;
                                break;
                            }
                        }

                        if (seekersWin)
                        {
                            Winner = PlayerRole.Seeker;
                            State = GameState.EndRound;
                            CurrentTimer = 10;
                            EndRoundTime.Set((int)CurrentTimer);

                            foreach (var player in Player.AllPlayers.Cast<HNSPlayer>())
                            {
                                if (player.PlayerRole == PlayerRole.Seeker)
                                {
                                    player.Wins += 1;
                                }
                            }
                        }
                    }
                    break;
                }
                case GameState.EndRound:
                {
                    CurrentTimer -= Time.DeltaTime;
                    EndRoundTime.Set((int)CurrentTimer);
                    if (CurrentTimer < 0f)
                    {
                        State = GameState.WaitingForPlayers;
                    }
                    break;
                }
            }
        }

        var vignetteIsShowing = false;

        var localPlayer = (HNSPlayer)Network.LocalPlayer;
        if (localPlayer != null)
        {
            var vignetteSerial = IM.GetNextSerial();

            var topBarRect = UI.ScreenRect.CutTop(150);
            var midBarRect  = UI.ScreenRect.SubRect(0.5f, 0.8f, 0.5f, 0.8f);
            var midBarRect2 = UI.ScreenRect.SubRect(0.5f, 0.2f, 0.5f, 0.2f);

            var bottomBarRect = UI.ScreenRect.CutBottom(300);

            using var _ = UI.PUSH_LAYER(RoleNameLayer);

            // if (localPlayer.HideHudReasons.Count == 0 && localPlayer.PlayerRole != PlayerRole.Spectator)
            // {
            //     UI.Text(topBarRect, localPlayer.Region, GetTextSettings(42, 0f, null));
            // }

            if (State != GameState.WaitingForPlayers && State != GameState.CountingDown)
            {
                Vector4 roleColor = Roles[localPlayer.PlayerRole].RoleColor;
                var roleText = localPlayer.PlayerRole == PlayerRole.Spectator ? "Spectating" : Roles[localPlayer.PlayerRole].RoleName;
                UI.Text(topBarRect, roleText, GetTextSettingsColor(56, roleColor, 0f, null));
                UI.Text(topBarRect, roleText, GetTextSettingsColor(56, roleColor, 0f, null));
                if (localPlayer.PlayerRole == PlayerRole.Spectator)
                {
                    UI.Text(topBarRect.Grow(0, 0, 100, 0), "(Fly around till the next round starts!)", GetTextSettings(36, 0f, null));
                }
            }

            switch (State)
            {
                case GameState.WaitingForPlayers:
                {
                    UI.Text(bottomBarRect, $"Waiting for players ({Player.AllPlayers.Count}/{PlayersNeededToStartGame})",GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
                case GameState.CountingDown:
                {
                    UI.Text(bottomBarRect,("Round starts in "+Countdown).ToString()+" seconds...",GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
                case GameState.StartRound:
                {
                    UI.Text(bottomBarRect,"Starting round...",GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
                case GameState.Hiding:
                {
                    UI.Text(bottomBarRect,"Hiding: " + HideTimer + "s", GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
                case GameState.Round:
                {
                    var seconds = SeekTimer.Value;
                    var minutes = seconds / 60;
                    seconds -= minutes * 60;
                    var str = "";
                    if (minutes >= 1)
                    {
                        str += $"{minutes}m ";
                    }
                    str += $"{seconds}s";

                    if (Game.IsPhone)
                    {
                        UI.PushScaleFactor(UI.ScreenScaleFactor * 1.5f);
                    }
                    var size01 = Ease.T(Time.TimeSinceStartup - SeekTimeCountdownTime, 1f);
                    var timeRect = bottomBarRect.Offset(0, -50);
                    var color = new Vector4(1, 1, 0, 1);
                    if (SeekTimer <= 10)
                    {
                        color = new Vector4(1, 0, 0, 1);
                    }
                    UI.PushScaleFactor(UI.ScreenScaleFactor * AOMath.Lerp(2f, 1f, Ease.OutQuart(size01)));
                    var timeTextRect = UI.Text(timeRect, str, GetTextSettingsColor(60, color, 0f, null, UI.HorizontalAlignment.Center));
                    UI.PopScaleFactor();
                    UI.Text(timeRect.Offset(0, 70), "Time Left", GetTextSettings(40, 0f, null, UI.HorizontalAlignment.Center));

                    if (Game.IsPhone)
                    {
                        UI.PopScaleFactor();
                    }

                    break;
                }
                case GameState.EndRound:
                {
                    if (localPlayer.Alive())
                    {
                        vignetteIsShowing = true;
                        if (localPlayer.WasPresentAtRoundStart)
                        {
                            if (localPlayer.PlayerRole == Winner)
                            {
                                IM.SetNextSerial(vignetteSerial);
                                UI.Image(UI.ScreenRect, null, new Vector4(0, 1, 0, 1) * 0.6f * VignetteFader);
                                UI.Text(UI.ScreenRect.CenterRect().Offset(0, 250), "YOU WIN", GetTextSettingsColor(100, new Vector4(1, 1, 1, 1)));
                            }
                            else
                            {
                                IM.SetNextSerial(vignetteSerial);
                                UI.Image(UI.ScreenRect, null, new Vector4(1, 0, 0, 1) * 0.6f * VignetteFader);
                                UI.Text(UI.ScreenRect.CenterRect().Offset(0, 250), "YOU LOSE", GetTextSettingsColor(100, new Vector4(1, 1, 1, 1)));
                            }
                        }
                    }
                    var str = "Seekers";
                    if (Winner == PlayerRole.Prop)
                    {
                        str = "Hiders";
                    }
                    UI.Text(bottomBarRect,$"{str} Win! Next round in {EndRoundTime}s.", GetTextSettings(42,0f,null,UI.HorizontalAlignment.Center));
                    break;
                }
            }
        }

        if (vignetteIsShowing)
        {
            VignetteFader = MathF.Min(1, VignetteFader + Time.DeltaTime * 4.0f);
        }
        else
        {
            VignetteFader = MathF.Max(0, VignetteFader - Time.DeltaTime * 4.0f);
        }
    }

    // public UI.TextSettings GetTextSettings(float size, float offset = 0, FontAsset font = null,UI.HorizontalAlignment halign = UI.HorizontalAlignment.Center,UI.VerticalAlignment valign = UI.VerticalAlignment.Center)
    // {
    //     if (font == null)
    //     {
    //         font = UI.Fonts.BarlowBold;
    //     }
    //     var ts = new UI.TextSettings()
    //     {
    //         Font = font,
    //         Size = size,
    //         Color = Vector4.White,
    //         DropShadow = true,
    //         DropShadowColor = new Vector4(0f,0f,0.02f,0.5f),
    //         DropShadowOffset = new Vector2(0f,-3f),
    //         HorizontalAlignment = halign,
    //         VerticalAlignment = valign,
    //         WordWrap = false,
    //         WordWrapOffset = 0,
    //         Outline = true,
    //         OutlineThickness = 3,
    //         Offset = new Vector2(0, offset),
    //     };
    //     return ts;
    // }

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
        public string RoleName;
        public Vector4 RoleColor;
    }
}

public enum GameState
{
    WaitingForPlayers,
    CountingDown,
    StartRound,
    Hiding,
    Round,
    EndRound,
}

public enum PlayerRole
{
    Spectator,
    Seeker,
    Prop,
}
