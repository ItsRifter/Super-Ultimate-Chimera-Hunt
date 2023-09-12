using static SUCH.Player.PigmaskPawn;

namespace SUCH;

public partial class SUCHGame
{
	public static SUCHGame Instance => Current as SUCHGame;

	[ConVar.Replicated("such.debug")]
	public static bool SUCHDebug { get; set; }

	public static bool GameFreeze { get; set; }

	//Gameplay (incl rounds)
	#region
	[ConCmd.Server( "such.game.start", Help = "Forces the game to start" )]
	public static void StartGameCMD()
	{
		if ( !SUCHDebug ) return;
		if ( StaticGameStatus != GameEnum.Idle ) return;

		Instance.StartGame();
	}

	[ConCmd.Server( "such.game.end", Help = "Forces the game to stop" )]
	public static void EndGameCMD()
	{
		if ( !SUCHDebug ) return;
		if ( StaticGameStatus == GameEnum.Idle ) return;

		Instance.StopGame();
	}

	[ConCmd.Server( "such.game.freeze", Help = "Freezes the timer during gameplay, this does not freeze players" )]
	public static void FreezeGameCMD()
	{
		if ( !SUCHDebug ) return;

		GameFreeze = !GameFreeze;
	}

	[ConCmd.Server("such.round.start", Help = "Forces the round to start")]
	public static void StartRoundCMD()
	{
		if ( !SUCHDebug ) return;
		if ( StaticRoundStatus == RoundEnum.Active ) return;

		Instance.StartRound();
	}

	[ConCmd.Server( "such.round.restart", Help = "Forces the round to restart, this ends the round in a draw" )]
	public static void RestartRoundCMD()
	{
		if ( !SUCHDebug ) return;
		if ( StaticRoundStatus != RoundEnum.Active ) return;

		Instance.EndRound( WinEnum.Draw );
	}
	#endregion

	public enum AssignType
	{
		Ghost,
		Pigmask,
		Chimera
	}

	//Player
	#region
	[ConCmd.Server("such.player.role.assign", Help = "Assigns a team role to yourself or with a player name")]
	public static void AssignRole( AssignType assign, string targetName = "" )
	{
		if ( !SUCHDebug ) return;

		IClient target = null;
		IEntity oldPawn = null;
		
		//Player is targeting someone
		if( !string.IsNullOrEmpty( targetName ) )
		{
			//Find all clients with the given target name
			var clients = Game.Clients
				.Where( n => n.Name.ToLower().Contains( targetName ) )
				.ToList();

			//If no player exists with target name
			if ( !clients.Any() )
				Log.Error( "SUCH: No client with target name exists." );

			//If more than 1 player exists with target name
			if ( clients.Count > 1 )
				Log.Error( "SUCH: More than one client exists with target name, be more specific." );

			//If the player targeted themself with the parameter filled
			if ( ConsoleSystem.Caller.Name.ToLower().Contains( targetName ) )
				Log.Warning( "SUCH: Appears you targeted yourself, you can leave this parameter empty next time." );

			bool success = clients.Any() && clients.Count == 1;
			if ( !success ) return;

			if ( !target.IsValid ) return;
			oldPawn = target.Pawn;
		}
		//Player is targeting themselves
		else
		{
			target = ConsoleSystem.Caller;

			if ( !target.IsValid ) return;
			oldPawn = target.Pawn;
		}

		//Give the assignment to the player
		switch ( assign )
		{
			case AssignType.Ghost:
				target.Pawn = new GhostPawn();
				break;
			case AssignType.Pigmask:
				target.Pawn = new PigmaskPawn();
				(target.Pawn as PigmaskPawn).AssignPigRank( target );
				break;
			case AssignType.Chimera:
				target.Pawn = new ChimeraPawn();
				break;
			default:
				return;
		}

		//Delete the old pawn after successful change
		oldPawn?.Delete();

		//DoVisibilty();
	}
	
	enum RankHandling
	{
		Increment,
		Set,
		Reset
	}

	static void DoPigRanks( PigmaskPawn piggy, RankHandling handle, bool respawn, int newRank = -1 )
	{
		if( piggy == null )
		{
			Log.Error( "SUCH: Player is not a pigmask" );
			return;
		}

		switch ( handle )
		{
			case RankHandling.Increment: piggy.IncrementRank(); break;
			case RankHandling.Set: piggy.SetRank( (PigRanks)newRank ); break;
			case RankHandling.Reset: piggy.ResetRank(); break;
		}

		if ( respawn )
			piggy.Spawn();
	}

	[ConCmd.Server( "such.player.pigrank.up", Help = "Increments the player's pig rank with an optional parameter for respawn" )]
	public static void IncrementRankCMD(bool shouldRespawn = false)
	{
		if ( !SUCHDebug ) return;
		DoPigRanks( ConsoleSystem.Caller.Pawn as PigmaskPawn, RankHandling.Increment, shouldRespawn);
	}

	[ConCmd.Server("such.player.pigrank.reset")]
	public static void ResetRankCMD( bool shouldRespawn = false )
	{
		if ( !SUCHDebug ) return;
		DoPigRanks( ConsoleSystem.Caller.Pawn as PigmaskPawn, RankHandling.Reset, shouldRespawn);
	}

	[ConCmd.Server( "such.player.pigrank.set", Help = "Sets the player's pig rank to a value" )]
	public static void SetRankCMD( int newRank, bool shouldRespawn = false )
	{
		if ( !SUCHDebug ) return;

		int highVal = Enum.GetValues( typeof( PigRanks ) ).Length + 1;

		if(newRank < 0 || newRank > highVal )
		{
			Log.Error( "SUCH: Invalid value" );
			return;
		}

		DoPigRanks( ConsoleSystem.Caller.Pawn as PigmaskPawn, RankHandling.Reset, shouldRespawn, newRank+1 );
	}
	#endregion

	[ConCmd.Server("such.saturn.spawn", Help = "Spawns Mr. Saturn")]
	public static void SpawnSaturnCMD()
	{
		if ( !SUCHDebug ) return;

		_ = new MrSaturn();
	}
}
