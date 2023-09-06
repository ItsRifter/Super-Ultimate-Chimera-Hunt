namespace SUCH.UI;

public class PawnStats : Panel
{
	public Panel statLayout;

	public PawnStats()
	{
		StyleSheet.Load( "UI/Styles/PawnStats.scss" );

		statLayout = Add.Panel();
	}

	public override void Tick()
	{
		var player = Game.LocalPawn as BasePawn;
		if ( player == null ) return;

		statLayout.SetClass( "pigmask", true );
	}
}
