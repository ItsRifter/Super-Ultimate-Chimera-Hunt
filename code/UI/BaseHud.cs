namespace SUCH.UI;

public class SUCHHud : RootPanel
{
	public static SUCHHud Current { get; set; }

	public SUCHHud()
	{
		Current?.Delete();
		Current = null;

		StyleSheet.Load( "UI/Styles/Hud.scss" );

		AddChild<Chat>();
		AddChild<Scoreboard<ScoreboardEntry>>();
		AddChild<PawnStats>();
		AddChild<PlayerHoverInfo>();
		AddChild<Crosshair>();

		Current = this;
	}
}
