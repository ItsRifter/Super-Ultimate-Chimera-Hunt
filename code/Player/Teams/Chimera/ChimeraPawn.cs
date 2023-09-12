namespace SUCH.Player;

public partial class ChimeraPawn : BasePawn
{
	[Net] public bool FreezeMovement { get; set; } = false;
	[Net] public double RoarStamina { get; set; }

	public override void MoveToSpawnpoint()
	{
		Entity chimeraPoint = All.OfType<ChimeraSpawnpoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( chimeraPoint == null ) return;

		Transform = chimeraPoint.Transform;
		SetViewAngles( chimeraPoint.Rotation.Angles() );

		base.MoveToSpawnpoint();
	}

	[ClientRpc]
	public void SetViewAngles( Angles angle )
	{
		SetAngles = angle;
		SetView = true;
	}

	public override void CreateHull()
	{
		base.CreateHull();
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/player/chimera/chimera.vmdl" );
		CreateHull();
		Tags.Add( "chimera" );

		RoarStamina = 50.0f;

		LifeState = LifeState.Alive;

		Components.Create<ChimeraController>();
		Components.Create<Animator>();

		SetMaterialGroup( 1 );
	}

	TimeUntil timeActionFinish;

	void Bite()
	{
		Sound.FromWorld( "bite", EyePosition );
		SetAnimParameter( "b_bite", true );

		FreezeMovement = true;
		timeActionFinish = 0.85f;

		float biteRange = 54.0f;

		using(LagCompensation())
		{
			var tr = Trace.Ray( Position + Vector3.Up * 48, Position + Vector3.Up * 48 + Rotation.Forward * 56 )
				.Ignore( this )
				.Run();

			foreach(var ent in FindInSphere(tr.EndPosition, biteRange) )
			{
				if ( tr.Entity is BrushEntity brush && Game.IsServer )
					brush.Break();

				if( ent is PigmaskPawn pigmask )
				{
					DamageInfo dmgInfo = new DamageInfo();
					dmgInfo.Attacker = this;
					dmgInfo.Force = tr.Direction * 2500;

					pigmask.DmgInfo = dmgInfo;

					if ( Game.IsServer )
						pigmask.OnKilled();
				}

				if( ent is MrSaturn saturn )
				{
					saturn.BitByChimera( this );
				}
			}
		}
	}

	bool isRoaring;

	void Roar()
	{
		if ( RoarStamina < 50.0f ) return;

		Sound.FromWorld( "roar", EyePosition );

		SetAnimParameter( "b_roar", true );

		RoarStamina = 0.0f;
		FreezeMovement = true;
		timeActionFinish = 2.95f;

		isRoaring = true;
	}

	protected void DoStaminaRegain()
	{
		if ( RoarStamina < 50.0 )
		{
			RoarStamina += 5.0f * Time.Delta;
			RoarStamina = RoarStamina.Clamp( 0, 50 );
		}
	}

	protected void DoInputActions( IClient cl )
	{
		if ( Controller?.GroundEntity == null ) return;
		if ( SUCHGame.StaticRoundStatus != SUCHGame.RoundEnum.Active ) return;

		if ( Input.Pressed( "Attack1" ) )
			Bite();

		if ( Input.Pressed( "Attack2" ) )
			Roar();
	}

	public override void Simulate( IClient cl )
	{
		if(isRoaring)
		{
			foreach ( var ent in FindInSphere( Position, 256.0f ) )
			{
				if ( ent is PigmaskPawn piggy )
					piggy.BecomeScared();
			}

			if ( timeActionFinish <= 0.0f )
				isRoaring = false;
		}

		if ( timeActionFinish <= 0.0f && FreezeMovement )
			FreezeMovement = false;

		if ( FreezeMovement ) return;

		DoStaminaRegain();

		base.Simulate( cl );
		DoInputActions( cl );
	}

	protected Vector3 DoThirdPersonCamera(IClient cl)
	{
		Vector3 basePoint = EyePosition + Vector3.Up * 65;

		var pitch = EyePosition + Camera.Rotation.Backward * 125;

		var tr = Trace.Ray( basePoint, pitch + Vector3.Up * 35 )
			.Ignore( this )
			.Size( 25.0f )
			.Run();

		return tr.EndPosition;
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );
	}

	public override void CameraSimulate( IClient cl )
	{
		Camera.Position = DoThirdPersonCamera( cl );
		Camera.Rotation = EyeRotation;

		Camera.FirstPersonViewer = null;
	}

	protected void DisableChimera()
	{
		var ragdoll = new ModelEntity( GetModelName() );
		ragdoll.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		ragdoll.SetMaterialGroup( 2 );
		ragdoll.PhysicsEnabled = true;

		ragdoll.Position = Position;
		ragdoll.Rotation = Rotation;

		ragdoll.Tags.Clear();

		Corpse = ragdoll;

		ragdoll.DeleteAsync( 10.0f );
	}

	public override void OnKilled()
	{
		base.OnKilled();
		Sound.FromWorld( "button", Position );
		LifeState = LifeState.Dead;
		DisableChimera();
	}
}
