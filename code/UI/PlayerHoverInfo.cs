namespace SUCH.UI;

public class PlayerHoverInfo : Panel
{
	Label playerInfo;

	public PlayerHoverInfo()
	{
		StyleSheet.Load( "UI/Styles/PlayerHoverInfo.scss" );

		playerInfo = Add.Label("???");
	}

	public override void Tick()
	{
		var player = Game.LocalPawn as BasePawn;
		if ( player == null ) return;

		var ent = player.GetEyeTrace( 256.0f, 1.0f ).Entity;

		SetClass( "show", ent is BasePawn || (player is GhostPawn && ent is GhostPawn) );
		playerInfo.SetText( (ent as BasePawn)?.Client.Name );

		SetClass( "chimera", ent is ChimeraPawn );

		if ( ent is PigmaskPawn piggy )
			SetClassOnPigRank( piggy.PigRank );
	}

	void SetClassOnPigRank( PigmaskPawn.PigRankEnum rank )
	{
		SetClass( "pigmask_ensign", rank is PigmaskPawn.PigRankEnum.Ensign );
		SetClass( "pigmask_captain", rank is PigmaskPawn.PigRankEnum.Captain );
		SetClass( "pigmask_major", rank is PigmaskPawn.PigRankEnum.Major );
		SetClass( "pigmask_colonel", rank is PigmaskPawn.PigRankEnum.Colonel );
	}
}
