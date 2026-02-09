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

	private SyncVar<int> _firstMapVotes = new();
	private SyncVar<int> _secondMapVotes = new();
	private SyncVar<int> _thirdMapVotes = new();
	
	public List<SyncVar<int>> MapsSelected;
	public List<SyncVar<int>> MapsVotes;

	public override void Awake()
	{
		MapsSelected = new List<SyncVar<int>>
		{
			_firstMapRandom,
			_secondMapRandom,
			_thirdMapRandom
		};
		MapsVotes = new List<SyncVar<int>>
		{
			_firstMapVotes,
			_secondMapVotes,
			_thirdMapVotes
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
			Log.Info($"Count {mapIndexes.Count}");
			foreach(var RandomMap in MapsSelected)
			{
				var rand = new Random();
				var randomInt = rand.Next(0, mapIndexes.Count);
				var mapWorld = 0;
				if (randomInt < mapIndexes.Count)
				{
					mapWorld = mapIndexes[randomInt];
				}
				RandomMap.Set(mapWorld);
				if (mapIndexes.Count > 0)
				{
					mapIndexes.RemoveAt(randomInt);
				}
				
				Log.Info($"RandomInt {randomInt} ////Map {mapIndexes.Count}");
			}
		}
	}


	public void SetupVoteManager() 
	{
		foreach(var player in Scene.Components<HNSPlayer>())
		{
			if (player.HasEffect<VotingEffect>()) continue;
			CallClient_AddVoteEffect(player);
		}
	}
	public void RemoveVoteEffect() 
	{
		foreach(var player in Scene.Components<HNSPlayer>())
		{
			if (player.HasEffect<VotingEffect>())
			{
				CallClient_RemoveVoteEffect(player);
			}
		}
	}
	public void ResetVotes()
	{
		foreach (var mapVote in MapsVotes)
		{
			mapVote.Set(0);
		}
	}
	public int GetVotedWorld()
	{
		int currentIndex = 0;
		int lastHighestVote = -1;
		List<int> selectedMapsIndex = new List<int>();
		foreach (var mapVote in MapsVotes)
		{
			if (lastHighestVote < mapVote.Value)
			{
				lastHighestVote = mapVote.Value;
			}
		}
		Log.Info($"Highest vote is: {lastHighestVote}");
		foreach (var mapVote in MapsVotes)
		{
			if (lastHighestVote == mapVote.Value)
			{
				selectedMapsIndex.Add(currentIndex);
				Log.Info($"Index added is: {currentIndex}");
			}
			currentIndex++;
		}
		Log.Info($"maps count voted: {selectedMapsIndex.Count}");
		Random rand = new Random();
		int randomIndex = rand.Next(0, selectedMapsIndex.Count);
		int actualIndex = selectedMapsIndex[randomIndex];
		
		var selected = MapsSelected[actualIndex];
		Log.Info($"actual vote index: {actualIndex}");
		return selected.Value;
	}
	[ClientRpc]
	public void RemoveVoteEffect(Player player)
	{
		player.RemoveEffect<VotingEffect>(true);
	}
	[ClientRpc]
	public void AddVoteEffect(Player player)
	{
		player.AddEffect<VotingEffect>();
	}
}

public partial class VotingEffect : MyEffect
{
	public override bool IsActiveEffect => true;
	public override bool FreezePlayer => false;

	public float[] Hold;
	public bool PlayerHasVoted = false;

	public override void OnEffectStart(bool isDropIn)
	{
		Hold = new float[3]
		{
			0.0f,
			0.0f,
			0.0f	
		};
		// Hold = new float[2]
		// {
		// 	0.0f,
		// 	0.0f
		// };
	}

	public override void OnEffectEnd(bool interrupt)
	{
	}

	[ServerRpc]
	public static void ServerPlayerVoted(int voteIndex)
	{
		var player = Network.GetRemoteCallContextPlayer();
		if (player == null) return;
		var effectVoting = player.GetEffect<VotingEffect>();
		if (effectVoting == null) return;
		if (effectVoting.PlayerHasVoted == true) return;
		effectVoting.PlayerHasVoted = true;
		var mapVote = VoteManager.Instance.MapsVotes[voteIndex];
		mapVote.Set(mapVote.Value + 1);
		CallClient_ClientPlayedVoted(player);
	}
	[ClientRpc]
	public static void ClientPlayedVoted(Player player)
	{
		var effectVoting = player.GetEffect<VotingEffect>();
		if (effectVoting == null) return;
		effectVoting.PlayerHasVoted = true;
	}

	public override void OnEffectUpdate()
	{
		if (Player.IsLocal)
		{
			using var _1 = UI.PUSH_LAYER(GameManager.IntroLayer);
			int offset = -400;
			//int offset = -200;
			int currentIndex = 0;
			foreach (var mapVotes in VoteManager.Instance.MapsSelected)
			{
				using var _ = UI.PUSH_ID(currentIndex);
				var currentMap = WorldManager.Instance.Worlds[mapVotes.Value];
				var currentVote = VoteManager.Instance.MapsVotes[currentIndex];
				//Log.Info("{CurrentMap}");
				var backgroundVoteMap = UI.SafeRect.CenterRect().Offset(offset, 0).Grow(220, 150, 220, 150);
					//offset += 400;
				offset += 400;
				UI.Image(backgroundVoteMap, null, new Vector4(0, 0, 0, 0.9f));
	
				if (currentMap != null)
				{
					var buttonSettings = new UI.ButtonSettings() {};
				
					var button = UI.BeginButton(backgroundVoteMap, $"{currentIndex}", buttonSettings, new UI.TextSettings());
					
					UI.TextAsync(backgroundVoteMap.CutTop(50).Offset(0, -10), $"{currentMap.MapName}", GameManager.Instance.GetTextSettings(60, 0f, null, UI.HorizontalAlignment.Center));


					// the map image
					var boxImage = backgroundVoteMap.CutTop(250).InsetRight(10).InsetLeft(10).Offset(0,0).FitAspect(currentMap.MapThumbnail.Aspect);
					// var nineSlice = new UI.NineSlice
					// {
					// 	slice = new Vector4(boxImage.Center.X, boxImage.Center.Y, currentMap.MapThumbnail.Width, currentMap.MapThumbnail.Height),
					// 	sliceScale = currentMap.MapThumbnail.Aspect
					// };
					UI.Image(boxImage, currentMap.MapThumbnail, Vector4.One);
			
					//var button = UI.Button(backgroundVoteMap.BottomCenterRect().Offset(0, voteOffset).Grow(75, 75, 75, 75), $"{currentIndex}", buttonSettings, new UI.TextSettings());
					//UI.Image(button.Rect, null, new Vector4(0, 0, 0, 0.9f));
					if (button.Pressed)
					{
						Hold[currentIndex] += Time.DeltaTime;
					}
					else
					{
						if (Hold[currentIndex] < 1) Hold[currentIndex] = 0;
					}

					if (Hold[currentIndex] >= 1)
					{
						CallServer_ServerPlayerVoted(currentIndex);
					}

					Hold[currentIndex] = (float)Math.Clamp(Hold[currentIndex], 0, 1);
					var ts = GameManager.Instance.GetTextSettings(52);
					ts.Color = new Vector4(1, 1, 1, 1);
					float voteOffset = 100.0f;
					var holdRectBg = backgroundVoteMap.BottomCenterRect().Offset(0, voteOffset).Grow(10, 75, 10, 75);
					if (!PlayerHasVoted)
					{
						var holdRect = holdRectBg.Inset(2, 2, 2, 2).SubRect(Hold[currentIndex], 0, 1, 1);
						UI.Image(holdRectBg, null, new Vector4(0.8f, 0.8f, 0.8f, 1));
						UI.Image(holdRect, null, new Vector4(0.1f, 0.1f, 0.1f, 1));
					}
			
					ts.Size = 28;
					var str = "Click and hold to vote";
					if (Game.IsMobile)
					{
						str = "Tap and hold to vote";
					}
					if (PlayerHasVoted)
					{
						str = "Already voted";
					}
					UI.TextAsync(holdRectBg.Offset(0, PlayerHasVoted ? 0 : 35), str, ts);
					ts.Color = Vector4.Green;
					ts.Size = 50;
					UI.TextAsync(backgroundVoteMap.BottomCenterRect().Offset(0, 50), $"Votes: {currentVote.Value}", ts);
					UI.EndButton();
				}
				currentIndex +=1;
			}

			var timeLeft = GameManager.Instance.Countdown.Value;
			var timeLeftRect = UI.ScreenRect.CutBottom(300);
			UI.TextAsync(timeLeftRect, $"Time left to vote: {timeLeft}", GameManager.Instance.GetTextSettings(60, 0f, null, UI.HorizontalAlignment.Center));

		}
	}
}