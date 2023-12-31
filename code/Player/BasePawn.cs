﻿namespace SUCH.Player;

public partial class BasePawn : AnimatedEntity
{
	[ClientInput] public Vector2 MoveInput { get; protected set; }
	[ClientInput] public Angles LookInput { get; protected set; }
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[BindComponent] public Controller Controller { get; }
	[BindComponent] public Animator Animator { get; }

	public ModelEntity Corpse { get; set; }

	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	[Net, Predicted] public Vector3 EyeLocalPosition { get; set; }
	[Net, Predicted] public Rotation EyeLocalRotation { get; set; }

	/// <summary>
	/// Move the player to a specific spawnpoint, you can handle other logic here
	/// </summary>
	public virtual void MoveToSpawnpoint()
	{
		ResetInterpolation();
	}

	/// <summary>
	/// Sets up a hull for the player
	/// </summary>
	public virtual void CreateHull()
	{
		if ( PhysicsBody.IsValid() ) return;

		EnableHitboxes = true;
		EnableLagCompensation = true;
		EnableAllCollisions = true;
	}

	/// <summary>
	/// Clears any hull the player has
	/// </summary>
	public virtual void ClearHull()
	{
		EnableHitboxes = false;
		EnableLagCompensation = false;
		EnableAllCollisions = false;
		PhysicsClear();
	}

	public override void Spawn()
	{
		EnableDrawing = true;
		MoveToSpawnpoint();

		EnableShadowInFirstPerson = true;
		EnableHideInFirstPerson = true;
	}

	public bool SetView;
	public Angles SetAngles;

	public override void BuildInput()
	{
		if ( SetView )
		{
			LookInput = SetAngles;
			SetView = false;
		}

		InputDirection = Input.AnalogMove;

		MoveInput = Input.AnalogMove;
		var lookInput = (LookInput + Input.AnalogLook).Normal;

		LookInput = lookInput.WithPitch( lookInput.pitch.Clamp( -90f, 90f ) );
	}

	public virtual float FootstepVolume()
	{
		float soundScale = (this is not ChimeraPawn)? 2.5f : 5.0f;

		if ( Input.Down( "Duck" ) && Controller.GroundEntity != null )
			soundScale = 0.0f;

		return Velocity.WithZ( 0 ).Length.LerpInverse( 0.0f, 200.0f ) * soundScale;
	}

	TimeSince timeSinceLastFootstep;

	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( !Game.IsClient )
			return;

		if ( timeSinceLastFootstep < 0.2f )
			return;

		volume *= FootstepVolume();

		timeSinceLastFootstep = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		if ( this is ChimeraPawn )
			Sound.FromWorld( "step", tr.EndPosition ).SetVolume( volume );
		else if (this is PigmaskPawn)
			tr.Surface.DoFootstep( this, tr, foot, volume );
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		TickPlayerUse();

		Animator?.Simulate( cl );
		Controller?.Simulate( cl );
	}

	public virtual void CameraSimulate( IClient cl )
	{
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 75 );
		//Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		Controller?.FrameSimulate( cl );

		CameraSimulate( cl );
	}

	public override void OnKilled()
	{
		EnableDrawing = false;
		ClearHull();
	}

	/// <summary>
	/// Performs an eye trace from where the player is looking
	/// </summary>
	/// <param name="dist">How far can the trace go</param>
	/// <param name="size">The size of the trace</param>
	/// <param name="useHitbox">Should the trace collide with hitboxes</param>
	/// <returns>The resulted trace ray</returns>
	public TraceResult GetEyeTrace( float dist, float size = 1.0f, bool useHitbox = false )
	{
		TraceResult tr;
		
		tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * dist )
			.Ignore( this )
			.Size( size )
			.UseHitboxes( useHitbox )
			.Run();

		return tr;
	}
}
