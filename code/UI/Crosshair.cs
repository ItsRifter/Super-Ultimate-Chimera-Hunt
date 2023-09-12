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
			SetClass( "clicked", Input.Pressed( "Attack1" ) );
		}
	}

	void SetClassOnPigRank( PigmaskPawn.PigRanks rank )
	{
		SetClass( "ensign", rank is PigmaskPawn.PigRanks.Ensign );
		SetClass( "captain", rank is PigmaskPawn.PigRanks.Captain );
		SetClass( "major", rank is PigmaskPawn.PigRanks.Major );
		SetClass( "colonel", rank is PigmaskPawn.PigRanks.Colonel );
	}
}
