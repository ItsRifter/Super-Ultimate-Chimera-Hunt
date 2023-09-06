namespace SUCH.Player;

public partial class GhostController : Controller, ISingletonComponent
{
	public override float StandingEyeHeight => 48.0f;
	public override void Simulate( IClient cl )
	{
		if ( Input.Down( InputButton.Run ) )
			DoFloatingVelocity();

		float moveZ = SimulateGhostMovement();

		if ( moveZ != 0.0f )
			Player.Velocity = Player.Velocity.WithZ( moveZ * 10 );

		base.Simulate( cl );
	}

	protected float SimulateGhostMovement()
	{
		float velocity = 0.0f;

		if ( Input.Down( InputButton.Jump ) )
			velocity = 25.0f;

		if ( Input.Down( InputButton.Duck ) )
			velocity = -25.0f;

		return velocity;
	}

	protected void DoFloatingVelocity()
	{
		Player.Velocity = Player.Velocity.WithZ( 8.5f );
	}
}

