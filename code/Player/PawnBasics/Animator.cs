namespace SUCH.Player;

public partial class Animator : EntityComponent<BasePawn>, ISingletonComponent
{
	public virtual void Simulate(IClient cl)
	{
		var player = Entity;
		var controller = player.Controller;
		CitizenAnimationHelper helper = new CitizenAnimationHelper( player );

		if ( cl.Components.Get<DevCamera>() == null || !cl.Components.Get<DevCamera>().Enabled )
			helper.WithLookAt( player.AimRay.Position + player.AimRay.Forward * 200 );

		helper.WithVelocity( player.Velocity );
		helper.WithWishVelocity( controller.GetWishVelocity() );

		helper.DuckLevel = Input.Down( "Duck" ) ? 0.75f : 0.0f;

		helper.IsGrounded = controller.GroundEntity != null;

		player.Rotation = player.LookInput.WithPitch(0).ToRotation();
	}

	public virtual void FrameSimulate( IClient cl )
	{
		
	}
}
