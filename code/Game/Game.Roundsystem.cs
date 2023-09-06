namespace SUCH;

public partial class SUCHGame
{
	public enum GameEnum
	{
		Idle,
		Starting,
		Active,
		Post,
		Mapchange
	}

	public enum RoundEnum
	{
		Idle,
		Active,
		Post,
	}

	[Net] public GameEnum GameStatus { get; set; }
	[Net] public RoundEnum RoundStatus { get; set; }

	public static GameEnum StaticGameStatus => Instance?.GameStatus ?? GameEnum.Idle;
	public static RoundEnum StaticRoundStatus => Instance?.RoundStatus ?? RoundEnum.Idle;

	public TimeUntil RoundTimer;

	TimeUntil timeForSaturnAppear;
	bool saturnAppeared;
	float[] randTimerArray = new float[]
	{
		25f,
		45f,
		65f,
		85f,
	};

	[Net] public float ClientTimer { get; set; }

	[Event.Tick.Server]
	protected void GameTick()
	{
		if ( GameStatus == GameEnum.Idle ) return;
		if ( GameFreeze ) return;

		ClientTimer = RoundTimer;

		Log.Info( timeForSaturnAppear );
		
		if( timeForSaturnAppear <= 0.0f && RoundStatus == RoundEnum.Active && !saturnAppeared )
			SpawnMrSaturn();

		if ( RoundTimer > 0.0f ) return;

		if ( GameStatus == GameEnum.Starting )
			StartGame();
		else
		{
			switch( RoundStatus )
			{
				case RoundEnum.Active:
					EndRound(WinEnum.Draw);
					break;

				case RoundEnum.Post:
					StartRound();
					break;
			}
		}
	}

	public void SpawnMrSaturn()
	{
		new MrSaturn();
		PlaySoundToUser( To.Everyone, "saturn_appear" );
		saturnAppeared = true;
	}

	public void ResetSaturnTimer()
	{
		saturnAppeared = false;
		timeForSaturnAppear = randTimerArray[Game.Random.Int( 0, randTimerArray.Length - 1 )];
	}

	public double SetRandomTime(double min, double max)
	{
		return Game.Random.Double(min, max);
	}

	public bool CanPlayGame()
	{
		if ( GameStatus != GameEnum.Idle ) return false;

		if ( Game.Clients.Count <= 1 ) return false;

		return true;
	}

	public void StopGame()
	{
		GameStatus = GameEnum.Idle;
		RoundStatus = RoundEnum.Idle;
	}

	public void StartPreGame()
	{
		RoundTimer = 15.0f;
		GameStatus = GameEnum.Starting;
	}

	public void StartGame()
	{
		GameStatus = GameEnum.Active;

		StartRound();
	}

	public void StartRound()
	{
		Game.ResetMap( All.Where( x => x is BasePawn ).ToArray() );

		ResetSaturnTimer();

		ResetPlayers();
		AssignChimera();
		AssignPigmasks();

		RoundTimer = 60.0f * 3;
		RoundStatus = RoundEnum.Active;

		_ = AwaitTimer( 5.0f );

		PlayMusicToUser( To.Everyone, "music_active" );
	}

	public static void CheckRoundConditions()
	{
		if ( StaticRoundStatus != RoundEnum.Active ) return;

		if ( Instance.GetPlayers( typeof(PigmaskPawn) ).Where(x => x.LifeState == LifeState.Alive).Count() <= 0 )
			Instance.EndRound( WinEnum.Chimera );

		if ( Instance.GetPlayers( typeof( ChimeraPawn ) ).Count <= 0 )
			Instance.EndRound( WinEnum.Pigmask );
	}

	public enum WinEnum
	{
		Draw,
		Pigmask,
		Chimera
	}

	public void EndRound( WinEnum winners )
	{
		StopMusicToUser( To.Everyone );

		switch ( winners ) 
		{
			case WinEnum.Draw:
				PlayMusicToUser( To.Everyone, "roundend_draw" );
				break;
			case WinEnum.Pigmask:
				PlayMusicToUser( To.Multiple(Game.Clients.Where(x => x.Pawn is PigmaskPawn || x.Pawn is GhostPawn ) ), "roundend_pigmask_win" );
				PlayMusicToUser( To.Multiple( Game.Clients.Where( x => x.Pawn is ChimeraPawn ) ), "roundend_chimera_lose" );
				break;
			case WinEnum.Chimera:
				PlayMusicToUser( To.Multiple( Game.Clients.Where( x => x.Pawn is PigmaskPawn || x.Pawn is GhostPawn ) ), "roundend_pigmask_lose" );
				PlayMusicToUser( To.Multiple( Game.Clients.Where( x => x.Pawn is ChimeraPawn ) ), "roundend_chimera_win" );
				break;
		}

		RoundTimer = 10.0f;
		RoundStatus = RoundEnum.Post;
	}
}
