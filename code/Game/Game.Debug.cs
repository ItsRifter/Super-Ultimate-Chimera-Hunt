namespace SUCH;

public partial class SUCHGame
{
	public static SUCHGame Instance => Current as SUCHGame;

	[ConVar.Replicated("such.debug")]
	public static bool SUCHDebug { get; set; }

	public static bool GameFreeze { get; set; }

	[ConCmd.Server("such.round.start")]
	public static void StartRoundCMD()
	{
		if ( !SUCHDebug ) return;
		if ( StaticRoundStatus == RoundEnum.Active ) return;

		Instance.StartRound();
	}

	[ConCmd.Server( "such.round.restart" )]
	public static void RestartRoundCMD()
	{
		if ( !SUCHDebug ) return;
		if ( StaticRoundStatus != RoundEnum.Active ) return;

		Instance.EndRound( WinEnum.Draw );
	}

	[ConCmd.Server( "such.game.start" )]
	public static void StartGameCMD()
	{
		if ( !SUCHDebug ) return;
		if ( StaticGameStatus != GameEnum.Idle ) return;

		Instance.StartGame();
	}

	[ConCmd.Server( "such.game.end" )]
	public static void EndGameCMD()
	{
		if ( !SUCHDebug ) return;
		if ( StaticGameStatus == GameEnum.Idle ) return;

		Instance.StopGame();
	}

	[ConCmd.Server("such.game.freeze")]
	public static void FreezeGameCMD()
	{
		if ( !SUCHDebug ) return;

		GameFreeze = !GameFreeze;
	}

	public enum BotAssignEnum
	{
		Ghost,
		Pigmask,
		Chimera
	}

	[ConCmd.Server("such.role.assign")]
	public static void AssignRole(string targetName, BotAssignEnum assign)
	{
		if ( !SUCHDebug ) return;

		IClient target = null;

		foreach ( IClient cl in Game.Clients )
		{
			if ( cl.Name.ToLower().Contains( targetName.ToLower() ) )
				target = cl;
		}

		if ( !target.IsValid ) return;

		var oldPawn = target.Pawn;

		switch ( assign )
		{
			case BotAssignEnum.Ghost:
				target.Pawn = new GhostPawn();
				break;
			case BotAssignEnum.Pigmask:
				target.Pawn = new PigmaskPawn();
				(target.Pawn as PigmaskPawn).AssignPigRank( target );
				break;
			case BotAssignEnum.Chimera:
				target.Pawn = new ChimeraPawn();
				break;
			default:
				return;
		}

		oldPawn?.Delete();
		DoVisibilty();
	}

	[ConCmd.Server("such.saturn.spawn")]
	public static void SpawnSaturnCMD()
	{
		if ( !SUCHDebug ) return;

		new MrSaturn();
	}
}
