namespace SUCH.Player;

public partial class Controller : EntityComponent<BasePawn>, ISingletonComponent
{
	[ConVar.Replicated( "debug_controller" )]
	public static bool Debug { get; set; } = false;
	public Vector3 LastVelocity { get; set; }
	public Entity LastGroundEntity { get; set; }
	public Entity GroundEntity { get; set; }
	public Vector3 BaseVelocity { get; set; }
	public Vector3 GroundNormal { get; set; }
	public float CurrentGroundAngle { get; set; }
	public virtual float StopSpeed => 150f;
	public virtual float StepSize => 18.0f;
	public virtual float GroundAngle => 46.0f;
	public virtual float DefaultSpeed => 200f;
	public virtual float WalkSpeed => 140f;
	public virtual float GroundFriction => 4.0f;
	public virtual float MaxNonJumpVelocity => 140.0f;
	public virtual float SurfaceFriction { get; set; } = 1f;
	public virtual float Acceleration => 6f;
	public virtual float DuckAcceleration => 5f;
	public virtual float Gravity => 800.0f;
	public virtual float AirControl => 30.0f;
	public virtual float AirAcceleration => 35.0f;
	public virtual float SprintSpeed => 135f;
	public virtual float CrouchSpeed => 110f;
	public virtual float WishCrouchSpeed => 120f;
	public virtual float EyeCrouchHeight => 24.0f;
	public virtual float StandingEyeHeight => 48.0f;
	public virtual float JumpAmount => 350f;

	public bool IsNoclipping = false;
	[Predicted] public float CurrentEyeHeight { get; set; }
	public BasePawn Player => Entity;

	public virtual string[] CollideTags { get; set; } = new string[]
	{
		"solid"
	};

	protected Vector3 mins;
	protected Vector3 maxs;

	[ConCmd.Server( "noclip" )]
	public static void DoNoclip()
	{
		if ( !SUCHGame.SUCHDebug ) return;

		var player = ConsoleSystem.Caller.Pawn as BasePawn;
		if ( player == null ) return;

		player.Controller.IsNoclipping = !player.Controller.IsNoclipping;
	}

	public Vector3 Position
	{
		get => Player.Position;
		set => Player.Position = value;
	}

	public Vector3 Velocity
	{
		get => Player.Velocity;
		set => Player.Velocity = value;
	}
	public float BodyGirth => 32f;

	public BBox Hull
	{
		get
		{
			var girth = BodyGirth * 0.5f;
			var baseHeight = CurrentEyeHeight;

			var mins = new Vector3( -girth, -girth, 0 );
			var maxs = new Vector3( +girth, +girth, baseHeight * 1.1f );

			return new BBox( mins, maxs );
		}
	}

	protected override void OnActivate()
	{
		CurrentEyeHeight = StandingEyeHeight;
		curEyeHeight = StandingEyeHeight;

		base.OnActivate();
	}

	protected void SimulateEyes()
	{
		Player.EyeRotation = Player.LookInput.ToRotation();
		Player.EyeLocalPosition = Vector3.Up * CurrentEyeHeight;
	}

	public virtual float? WishSpeed => 200f;

	public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f, float liftHead = 0.0f )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		if ( liftHead > 0 )
		{
			end += Vector3.Up * liftHead;
		}

		var tr = Trace.Ray( start, end )
			.Size( mins, maxs )
			.WithAnyTags( "solid", "playerclip" )
			.WithAnyTags( CollideTags )
			.Ignore( Player )
			.Run();

		return tr;
	}

	public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f, float liftHead = 0.0f )
	{
		var hull = Hull;
		return TraceBBox( start, end, hull.Mins, hull.Maxs, liftFeet, liftHead );
	}

	public Vector3 GetWishVelocity( bool zeroPitch = false )
	{
		var result = new Vector3( Player.MoveInput.x, Player.MoveInput.y, 0 );
		result *= Vector3.One;

		var inSpeed = result.Length.Clamp( 0, 1 );
		result *= Player.LookInput.WithPitch( 0f ).ToRotation();

		if ( zeroPitch )
			result.z = 0;

		float speed = GetWishSpeed();

		result = result.Normal * inSpeed;
		result *= speed;

		var ang = CurrentGroundAngle.Remap( 0, 45, 1, 0.6f );
		result *= ang;

		return result;
	}

	public void NoclipSimulate()
	{
		var fwd = Player.MoveInput.x.Clamp( -1f, 1f );
		var left = Player.MoveInput.y.Clamp( -1f, 1f );
		var rotation = Player.LookInput.ToRotation();

		var vel = (rotation.Forward * fwd) + (rotation.Left * left);

		if ( Input.Down( "Jump" ) )
			vel += Vector3.Up * 1;

		vel = vel.Normal * 2000;

		if ( Input.Down( "Run" ) )
			vel *= 5.0f;

		if ( Input.Down( "Duck" ) )
			vel *= 0.2f;

		Velocity += vel * Time.Delta;

		if ( Velocity.LengthSquared > 0.01f )
			Position += Velocity * Time.Delta;

		Velocity = Velocity.Approach( 0, Velocity.Length * Time.Delta * 5.0f );

		GroundEntity = null;
		BaseVelocity = Vector3.Zero;
	}

	public virtual float GetWishSpeed()
	{
		return DefaultSpeed;
	}

	public void Accelerate( Vector3 wishdir, float wishspeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishspeed > speedLimit )
			wishspeed = speedLimit;

		var currentspeed = Velocity.Dot( wishdir );
		var addspeed = wishspeed - currentspeed;

		if ( addspeed <= 0 )
			return;

		var accelspeed = acceleration * Time.Delta * wishspeed;

		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Velocity += wishdir * accelspeed;
	}

	public void ApplyFriction( float stopSpeed, float frictionAmount = 1.0f )
	{
		var speed = Velocity.Length;
		if ( speed.AlmostEqual( 0f ) ) return;

		var control = (speed < stopSpeed) ? stopSpeed : speed;
		var drop = control * Time.Delta * frictionAmount;

		// Scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;

		if ( newspeed != speed )
		{
			newspeed /= speed;
			Velocity *= newspeed;
		}
	}

	public void StepMove( float groundAngle = 46f, float stepSize = 18f )
	{
		MoveHelper mover = new MoveHelper( Position, Velocity );
		mover.Trace = mover.Trace.Size( Hull )
			.Ignore( Player )
			.WithAnyTags( CollideTags );

		mover.MaxStandableAngle = groundAngle;

		mover.TryMoveWithStep( Time.Delta, stepSize );

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	public void Move( float groundAngle = 46f )
	{
		MoveHelper mover = new MoveHelper( Position, Velocity );

		mover.Trace = mover.Trace.Size( Hull )
			.Ignore( Player );

		mover.MaxStandableAngle = groundAngle;

		mover.TryMove( Time.Delta );

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	public virtual void Jump()
	{
		float flGroundFactor = 1.0f;
		float startz = Velocity.z;

		Velocity = Velocity.WithZ( startz + JumpAmount * flGroundFactor );
		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

		ClearGroundEntity();
	}

	public virtual void AirMove()
	{
		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
		Velocity += new Vector3( 0, 0, BaseVelocity.z ) * Time.Delta;
		BaseVelocity = BaseVelocity.WithZ( 0 );

		var groundedAtStart = GroundEntity.IsValid();
		if ( groundedAtStart )
			return;

		var wishVel = GetWishVelocity( true );
		var wishdir = wishVel.Normal;
		var wishspeed = wishVel.Length;

		Accelerate( wishdir, wishspeed, AirControl, AirAcceleration );
		Velocity += BaseVelocity;
		Move();
		Velocity -= BaseVelocity;
		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
	}

	float crouchSpeed = 5.0f;
	float curEyeHeight;

	public void DoCrouching( bool should )
	{
		if ( should )
		{
			curEyeHeight = MathX.Lerp( curEyeHeight, EyeCrouchHeight, Time.Delta * crouchSpeed );
		}
		else
		{
			curEyeHeight = MathX.Lerp( curEyeHeight, StandingEyeHeight, Time.Delta * crouchSpeed );

			if ( curEyeHeight > (StandingEyeHeight - 0.1f) )
				curEyeHeight = StandingEyeHeight;
		}

		CurrentEyeHeight = curEyeHeight;
	}

	public virtual void Simulate( IClient cl )
	{
		SimulateEyes();

		if ( IsNoclipping )
		{
			NoclipSimulate();
			return;
		}

		CheckLadder();

		if ( Input.Pressed( "Jump" ) && GroundEntity != null )
			Jump();

		if ( IsTouchingLadder )
		{
			LadderMove();
		}
		else if ( GroundEntity != null )
		{
			WalkMove();
		}
		else if ( GroundEntity == null )
		{
			AirMove();
		}

		//DoCrouching( Input.Down( InputButton.Duck ) );

		CategorizePosition( GroundEntity != null );

		if ( Debug )
		{
			var hull = Hull;
			DebugOverlay.Box( Position, hull.Mins, hull.Maxs, Color.Red );
			DebugOverlay.Box( Position, hull.Mins, hull.Maxs, Color.Blue );

			var lineOffset = 0;

			DebugOverlay.ScreenText( $"Player Controller", ++lineOffset );
			DebugOverlay.ScreenText( $"       Position: {Position}", ++lineOffset );
			DebugOverlay.ScreenText( $"        Velocity: {Velocity}", ++lineOffset );
			DebugOverlay.ScreenText( $"    BaseVelocity: {BaseVelocity}", ++lineOffset );
			DebugOverlay.ScreenText( $"    GroundEntity: {GroundEntity} [{GroundEntity?.Velocity}]", ++lineOffset );
			DebugOverlay.ScreenText( $"           Speed: {Velocity.Length}", ++lineOffset );
		}
	}

	public void ClearGroundEntity()
	{
		if ( GroundEntity == null ) return;

		LastGroundEntity = GroundEntity;
		GroundEntity = null;
		SurfaceFriction = 1.0f;
	}

	public void SetGroundEntity( Entity entity )
	{
		LastGroundEntity = GroundEntity;
		LastVelocity = Velocity;

		GroundEntity = entity;

		if ( GroundEntity != null )
		{
			Velocity = Velocity.WithZ( 0 );
			BaseVelocity = GroundEntity.Velocity;
		}
	}

	private void UpdateGroundEntity( TraceResult tr )
	{
		GroundNormal = tr.Normal;

		SurfaceFriction = tr.Surface.Friction * 1.25f;
		if ( SurfaceFriction > 1 ) SurfaceFriction = 1;

		SetGroundEntity( tr.Entity );
	}

	public void CategorizePosition( bool bStayOnGround )
	{
		SurfaceFriction = 1.0f;

		var point = Position - Vector3.Up * 2;
		var vBumpOrigin = Position;
		bool bMovingUpRapidly = Velocity.z > MaxNonJumpVelocity;
		bool bMoveToEndPos = false;

		if ( GroundEntity != null )
		{
			bMoveToEndPos = true;
			point.z -= StepSize;
		}
		else if ( bStayOnGround )
		{
			bMoveToEndPos = true;
			point.z -= StepSize;
		}

		if ( bMovingUpRapidly )
		{
			ClearGroundEntity();
			return;
		}

		var pm = TraceBBox( vBumpOrigin, point, 4.0f );

		var angle = Vector3.GetAngle( Vector3.Up, pm.Normal );
		CurrentGroundAngle = angle;

		if ( pm.Entity == null || Vector3.GetAngle( Vector3.Up, pm.Normal ) > GroundAngle )
		{
			ClearGroundEntity();
			bMoveToEndPos = false;

			if ( Velocity.z > 0 )
				SurfaceFriction = 0.25f;
		}
		else
		{
			UpdateGroundEntity( pm );
		}

		if ( bMoveToEndPos && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
		{
			Position = pm.EndPosition;
		}
	}

	public virtual void FrameSimulate( IClient cl )
	{
		SimulateEyes();
	}

	bool IsTouchingLadder = false;
	Vector3 LadderNormal;

	public virtual void LadderMove()
	{
		var velocity = GetWishVelocity( true );
		float normalDot = velocity.Dot( LadderNormal );
		var cross = LadderNormal * normalDot;
		Player.Velocity = (velocity - cross) + (-normalDot * LadderNormal.Cross( Vector3.Up.Cross( LadderNormal ).Normal ));

		Move();
	}

	public virtual void CheckLadder()
	{
		var wishvel = GetWishVelocity( true );
		wishvel *= Player.LookInput.WithPitch( 0 ).ToRotation();
		wishvel = wishvel.Normal;

		if ( IsTouchingLadder )
		{
			if ( Input.Pressed( "Jump" ) )
			{
				Player.Velocity = LadderNormal * 100.0f;
				IsTouchingLadder = false;
				return;

			}
			else if ( Player.GroundEntity != null && LadderNormal.Dot( wishvel ) > 0 )
			{
				IsTouchingLadder = false;
				return;
			}
		}

		const float ladderDistance = 1.0f;
		var start = Player.Position;
		Vector3 end = start + (IsTouchingLadder ? (LadderNormal * -1.0f) : wishvel) * ladderDistance;

		var pm = Trace.Ray( start, end )
			.Size( Hull )
			.WithTag( "ladder" )
			.Ignore( Player )
			.Run();

		IsTouchingLadder = false;

		if ( pm.Hit )
		{
			IsTouchingLadder = true;
			LadderNormal = pm.Normal;
		}
	}

	private void StayOnGround()
	{
		var start = Position + Vector3.Up * 2;
		var end = Position + Vector3.Down * StepSize;

		// See how far up we can go without getting stuck
		var trace = TraceBBox( Position, start );
		start = trace.EndPosition;

		// Now trace down from a known safe position
		trace = TraceBBox( start, end );

		if ( trace.Fraction <= 0 ) return;
		if ( trace.Fraction >= 1 ) return;
		if ( trace.StartedSolid ) return;
		if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle ) return;

		Position = trace.EndPosition;
	}

	private void WalkMove()
	{
		var wishVel = GetWishVelocity( true );
		var wishdir = wishVel.Normal;
		var wishspeed = wishVel.Length;
		var friction = GroundFriction * SurfaceFriction;

		Velocity = Velocity.WithZ( 0 );
		ApplyFriction( StopSpeed, friction );

		var accel = Acceleration;

		Velocity = Velocity.WithZ( 0 );
		Accelerate( wishdir, wishspeed, 0, accel );
		Velocity = Velocity.WithZ( 0 );

		// Add in any base velocity to the current velocity.
		Velocity += BaseVelocity;

		StepMove();
		StayOnGround();
	}
}

