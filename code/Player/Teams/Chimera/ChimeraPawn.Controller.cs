namespace SUCH.Player;

public partial class ChimeraController : Controller, ISingletonComponent
{
	public override float StandingEyeHeight => 32.0f;
	public override float DefaultSpeed => 125.0f;
	public override float SprintSpeed => 300.0f;
	public override float AirAcceleration => 15.0f;
	public override float Acceleration => 50.0f;

	public float RunStamina { get; set; } = 150.0f;
	float sprintDelta => 17.5f;

	TimeSince timeLastSprinted;
	TimeSince timeLastJump;

	public override string[] CollideTags => new string[]
	{
		"solid",
		"pigmask"
	};

	public override void Simulate( IClient cl )
	{
		if( RunStamina < 150.0f && !Input.Down( "Run" ) )
		{
			RunStamina += sprintDelta * Time.Delta;
			RunStamina = RunStamina.Clamp( 0, 150.0f );
		}

		Entity.SetAnimParameter( "b_running", Velocity.LengthSquared > 0.1f && RunStamina > 0.0f && Input.Down( "Run" ) );

		if ( GroundEntity == null && Input.Pressed( "Jump" ) )
		{
			FlapWings();
			return;
		}

		base.Simulate( cl );
	}

	public override float GetWishSpeed()
	{
		if(Input.Down( "Run" ) && RunStamina > 0.0f )
		{
			RunStamina -= sprintDelta * Time.Delta;
			RunStamina = RunStamina.Clamp( 0, 150.0f );

			timeLastSprinted = 0;

			return SprintSpeed;
		}

		return base.GetWishSpeed();
	}

	public override void Jump()
	{
		if ( timeLastJump < 0.70f )
			return;

		timeLastJump = 0.0f;

		base.Jump();
	}

	protected void FlapWings()
	{
		if ( timeLastJump < 0.70f )
			return;

		timeLastJump = 0.0f;

		float flGroundFactor = 1.0f;

		Velocity = Velocity.WithZ( JumpAmount * flGroundFactor );
		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
	}
}

