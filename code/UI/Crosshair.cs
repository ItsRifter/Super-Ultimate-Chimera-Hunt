namespace SUCH.UI;

public class Crosshair : Panel
{
	Panel crosshair;

	public Crosshair()
	{
		StyleSheet.Load( "UI/Styles/Crosshair.scss" );

		crosshair = Add.Panel();
	}

	public override void Tick()
	{
		var player = Game.LocalPawn as BasePawn;
		if ( player == null ) return;

		SetClass( "show", player is PigmaskPawn );

		if ( player is PigmaskPawn pig )
		{
			SetClassOnPigRank( pig.PigRank );
			SetClass( "clicked", Input.Pressed( InputButton.PrimaryAttack ) );
		}
	}

	void SetClassOnPigRank( PigmaskPawn.PigRankEnum rank )
	{
		SetClass( "ensign", rank is PigmaskPawn.PigRankEnum.Ensign );
		SetClass( "captain", rank is PigmaskPawn.PigRankEnum.Captain );
		SetClass( "major", rank is PigmaskPawn.PigRankEnum.Major );
		SetClass( "colonel", rank is PigmaskPawn.PigRankEnum.Colonel );
	}
}
