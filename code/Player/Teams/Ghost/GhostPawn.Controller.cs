namespace SUCH.Player;

public partial class GhostController : Controller, ISingletonComponent
{
	public override float StandingEyeHeight => 48.0f;
	public override float DefaultSpeed => 275.0f;
	public override float WalkSpeed => 130.0f;

	public override void Simulate( IClient cl )
	{
		if ( Input.Down( "Duck" ) )
			DoGhostFloat();

		float moveZ = DoGhostMovement();

		if ( moveZ != 0.0f )
			Player.Velocity = Player.Velocity.WithZ( moveZ * 10 );

		base.Simulate( cl );
	}

	/// <summary>
	/// Handles movement when in air and returns any velocity
	/// </summary>
	protected float DoGhostMovement()
	{
		float velocity = 0.0f;

		if ( Input.Down( "Jump" ) )
			velocity = 15.0f;

		return velocity;
	}

	/// <summary>
	/// Makes the player float in the air
	/// </summary>
	protected void DoGhostFloat() => Player.Velocity = Player.Velocity.WithZ( 8.5f );
}

