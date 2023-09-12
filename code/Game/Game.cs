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

		PlayMusicToUser( To.Single( client ), "music_waiting" );
		//DoVisibilty();

		if ( CanPlayGame() )
			StartPreGame();
	}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
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

	/// <summary>
	/// Changes player pawn and deletes their original pawn
	/// </summary>
	/// <param name="client">The player to change their pawn</param>
	/// <param name="teamRole">The new role to assign to the player</param>
	/// <param name="atDeathPos">Spawn where the player died</param>
	public static void SwapPawn(IClient client, AssignType teamRole, bool atDeathPos = false)
	{
		IEntity pawn = client.Pawn;
		Vector3 pos = pawn.Position;

		//Set the new pawn based on 'teamRole'
		client.Pawn = GetNewPawn(teamRole);
		
		//Delete the old pawn
		pawn.Delete();

		if ( atDeathPos )
			client.Pawn.Position = pos;

		//DoVisibilty();
	}

	static IEntity GetNewPawn( AssignType role )
	{
		return role switch
		{
			AssignType.Ghost => new GhostPawn(),
			AssignType.Pigmask => new PigmaskPawn(),
			AssignType.Chimera => new ChimeraPawn(),

			_ => new GhostPawn()
		};
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
