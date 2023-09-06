namespace SUCH;

public partial class SUCHGame : GameManager
{
	[Net] public IList<string> MapVoteIdents { get; set; }

	public SUCHGame()
	{
		SUCHDebug = Game.IsEditor;

		if ( Game.IsServer )
		{
			GameStatus = GameEnum.Idle;
			RoundStatus = RoundEnum.Idle;
			PigRanks = new Dictionary<IClient, PigmaskPawn.PigRankEnum>();
		}

		if ( Game.IsClient )
		{
			_ = new SUCHHud();
		}
	}

	[Event.Hotload]
	protected void HotloadGame()
	{
		if ( Game.IsServer )
		{

		}

		if ( Game.IsClient )
		{
			_ = new SUCHHud();
		}
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		var pawn = new GhostPawn();
		client.Pawn = pawn;

		PigRanks.Add( client, PigmaskPawn.PigRankEnum.Ensign );

		PlayMusicToUser( To.Single( client ), "music_waiting" );
		DoVisibilty();

		if ( CanPlayGame() )
			StartPreGame();
	}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		PigRanks.Remove( cl );

		base.ClientDisconnect( cl, reason );

		CheckRoundConditions();

		if ( Game.Clients.Count < 2 )
			StopGame();
	}

	public async Task AwaitTimer(float time)
	{
		await Task.DelayRealtimeSeconds( time );
	}

	public override async void PostLevelLoaded()
	{
		base.PostLevelLoaded();

		var mapList = await GetRemoteMapIdents();

		MapVoteIdents = mapList;
	}

	static async Task<List<string>> GetRemoteMapIdents()
	{
		var queryResult = await Package.FindAsync( $"type:map game:{Game.Server.GameIdent.Replace( "#local", "" )}", take: 99 );
		return queryResult.Packages.Select( ( p ) => p.FullIdent ).ToList();
	}

	public static void SwapPawn(BasePawn oldPawn, BasePawn newPawn, bool gotoPos = false)
	{
		var client = oldPawn.Client;
		Vector3 pos = oldPawn.Position;

		oldPawn.Delete();

		client.Pawn = newPawn;

		if ( gotoPos )
			newPawn.Position = pos;

		DoVisibilty();
	}

	public static void DoVisibilty()
	{
		var living = Game.Clients.Where( x => x.Pawn is not GhostPawn ).ToList();
		var ghosts = Game.Clients.Where( x => x.Pawn is GhostPawn ).ToList();

		foreach ( var ghost in ghosts )
		{
			ChangeVisibiltyOnClient( To.Multiple( living ), ghost, false );
			ChangeVisibiltyOnClient( To.Multiple( ghosts ), ghost, true );
		}
	}

	[ClientRpc]
	public static void ChangeVisibiltyOnClient( IClient target, bool show )
	{
		if ( target == null ) return;

		(target.Pawn as BasePawn).EnableDrawing = show;
	}
}
