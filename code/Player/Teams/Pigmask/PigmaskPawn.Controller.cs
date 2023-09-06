namespace SUCH.Player;

public partial class PigmaskController : Controller, ISingletonComponent
{
	public override float StandingEyeHeight => 48.0f;
	public override float EyeCrouchHeight => 32.0f;
	public override float SprintSpeed => 375.0f;
	public override float DefaultSpeed => 200.0f;

	float runStamina;

	float maxStamina;

	TimeUntil timeBeforeRegain;

	TimeSince timeLastJump;

	float staminaDelta => 37.5f;

	bool isSprinting = false;

	public override string[] CollideTags => new string[]
	{
		"solid",
		"chimera"
	};

	public void AdjustStaminaValues(float newValue)
	{
		runStamina = newValue;
		maxStamina = newValue;
	}

	public override void Simulate( IClient cl )
	{
		if ( Input.Pressed( InputButton.Run ) && !isSprinting )
			isSprinting = true;

		base.Simulate( cl );

		DoCrouching( Input.Down( InputButton.Duck ) );

		if ( isSprinting )
		{
			runStamina -= staminaDelta * Time.Delta;
			runStamina = runStamina.Clamp( 0, maxStamina );

			if ( runStamina <= 0.0f )
			{
				isSprinting = false;
				timeBeforeRegain = 4.5f;
			}
		} 
		else if (!isSprinting && timeBeforeRegain <= 0.0f)
		{
			if ( runStamina >= maxStamina )
				return;

			runStamina += staminaDelta * Time.Delta;
			runStamina = runStamina.Clamp( 0, maxStamina );
		}
	}

	public void ForceSprint()
	{
		isSprinting = true;
	}

	public override void Jump()
	{
		if ( timeLastJump < 1.15f )
			return;

		timeLastJump = 0.0f;

		base.Jump();
	}

	public override float GetWishSpeed()
	{
		if ( Input.Down( InputButton.Duck ) )
			return CrouchSpeed;
		
		if ( isSprinting )
			return SprintSpeed;

		return base.GetWishSpeed();
	}
}

