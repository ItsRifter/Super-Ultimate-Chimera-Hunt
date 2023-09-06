namespace SUCH.Player;

public partial class PigmaskPawn : BasePawn
{
	public enum PigRankEnum
	{
		Ensign,
		Captain,
		Major,
		Colonel
	}

	[Net] public PigRankEnum PigRank { get; set; }

	[Net] public bool IsScared { get; set; } = false;

	[Net] public MrSaturn SaturnEntity { get; set; }

	TimeUntil timeRecoverShock;

	public override void MoveToSpawnpoint()
	{
		Entity pigPoint = All.OfType<StandardSpawnpoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( pigPoint == null ) return;

		Transform = pigPoint.Transform;
		SetViewAngles( pigPoint.Rotation.Angles() );

		base.MoveToSpawnpoint();
	}

	[ClientRpc]
	public void SetViewAngles( Angles angle )
	{
		SetAngles = angle;
		SetView = true;
	}

	public override void CreatePhysHull()
	{
		base.CreatePhysHull();
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3(-16, -16, 0), new Vector3(16, 16, 64 ) );

		Tags.Add( "pigmask" );
	}

	void AttemptChimeraPress()
	{
		if ( SUCHGame.StaticRoundStatus != SUCHGame.RoundEnum.Active ) return;

		using ( LagCompensation() )
		{
			var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 75 )
				.Ignore( this )
				.UseHitboxes()
				.Size( 5f )
				.Run();

			if(tr.Entity is ChimeraPawn chimera && tr.Hitbox.HasTag("button"))
			{
				if ( Game.IsServer )
				{
					chimera.OnKilled();
					SUCHGame.PigRankUp( Client );
					SUCHGame.Instance.EndRound( SUCHGame.WinEnum.Pigmask );
				}
			}
		}
	}

	bool FindOrThrowSaturn()
	{
		if( SaturnEntity == null )
		{
			var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 75 )
				.Ignore( this )
				.UseHitboxes()
				.Size( 3f )
				.Run();

			if(tr.Entity is MrSaturn saturn)
			{
				saturn.PickUp( this );
				return true;
			}
		} 
		else
		{
			SaturnEntity.Throw();
			return true;
		}

		return false;
	}

	[Net] bool isTaunting { get; set; }
	TimeSince timeLastTaunt;

	void Taunt()
	{
		if ( Controller.GroundEntity == null ) return;

		if ( PigRank == PigRankEnum.Colonel )
		{
			SetAnimParameter( "b_taunt_special", true );
			timeLastTaunt = 0.0f;
		}
		else
		{
			SetAnimParameter( "b_taunt", true );
			timeLastTaunt = 1.25f;
		}

		isTaunting = true;
	}

	protected void DoMouseInputs()
	{
		if ( IsScared || isTaunting ) return;
		
		if( Input.Pressed(InputButton.PrimaryAttack) )
		{
			if( FindOrThrowSaturn() )
				return;
		}

		if ( Input.Pressed( InputButton.PrimaryAttack ) || Input.Pressed( InputButton.Use ) )
			AttemptChimeraPress();

		if ( Input.Pressed( InputButton.SecondaryAttack ) )
			Taunt();
	}

	public override void Simulate( IClient cl )
	{
		if(!isTaunting)
			base.Simulate( cl );
		else
		{
			if( timeLastTaunt > 4f )
				isTaunting = false;
		}

		if ( IsScared && timeRecoverShock <= 0.0f )
			IsScared = false;

		SetAnimParameter( "b_scared", IsScared );

		DoMouseInputs();
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/player/pigmask/pigmask.vmdl" );
		CreatePhysHull();

		LifeState = LifeState.Alive;

		Components.Create<PigmaskController>();
		Components.Create<Animator>();

		AssignRankMaterial();
		AdjustStaminaOnRank();

		isTaunting = false;
	}

	public void AssignPigRank(IClient cl)
	{
		PigRank = SUCHGame.GetPigRank( cl );
		AssignRankMaterial();
		AdjustStaminaOnRank();
	}

	public void BecomeScared()
	{
		if ( IsScared ) return;

		PlaySound( "squeal" );

		(Controller as PigmaskController).AdjustStaminaValues(0.0f);

		IsScared = true;
		timeRecoverShock = 6.5f;
	}

	protected void AdjustStaminaOnRank()
	{
		var controller = Controller as PigmaskController;
		
		switch(PigRank)
		{
			case PigRankEnum.Ensign:
				controller.AdjustStaminaValues(50.0f);
				break;
			case PigRankEnum.Captain:
				controller.AdjustStaminaValues( 75.0f );
				break;
			case PigRankEnum.Major:
				controller.AdjustStaminaValues( 100.0f );
				break;
			case PigRankEnum.Colonel:
				controller.AdjustStaminaValues( 125.0f );
				break;
		}
	}

	protected void AssignRankMaterial()
	{
		SetMaterialGroup( (int)PigRank + 1 );

		switch ( PigRank )
		{
			case PigRankEnum.Captain: SetBodyGroup( 1, 1 ); break;
			case PigRankEnum.Major: SetBodyGroup( 1, 2 ); break;
			case PigRankEnum.Colonel: SetBodyGroup( 1, 2 ); break;

			default: SetBodyGroup( 1, 0 ); break;
		}

		if ( PigRank == PigRankEnum.Colonel )
			SetBodyGroup( 0, 1 );
		else
			SetBodyGroup( 0, 0 );
	}

	protected void PigmaskRagdoll()
	{
		var ragdoll = new ModelEntity( GetModelName() );
		ragdoll.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		ragdoll.SetMaterialGroup( (int)PigRank + 1 );
		ragdoll.PhysicsEnabled = true;

		ragdoll.Tags.Clear();
		ragdoll.Tags.Add( "ragdoll" );

		ragdoll.ApplyAbsoluteImpulse( DmgInfo.Force );

		ragdoll.Position = Position;
		ragdoll.Rotation = Rotation;

		Corpse = ragdoll;

		ragdoll.DeleteAsync( 10.0f );
	}

	public DamageInfo DmgInfo;

	public override void OnKilled()
	{
		base.OnKilled();

		PlaySound( "squeal" );
		isTaunting = false;

		if ( Game.IsClient )
			PlaySound( "die" );

		SUCHGame.Instance.StopMusicToUser( To.Single(this) );

		PigmaskRagdoll();
		LifeState = LifeState.Dead;

		SUCHGame.ResetPigRank( Client );
		SUCHGame.CheckRoundConditions();

		SUCHGame.SwapPawn( this, new GhostPawn(), true );
	}

	protected Vector3 DoThirdPersonCamera( IClient cl )
	{
		Vector3 basePoint = EyePosition + Vector3.Up * 35;

		var pitch = EyePosition + Camera.Rotation.Backward * 85;

		var tr = Trace.Ray( basePoint, pitch + Vector3.Up * 25 )
			.Ignore( this )
			.Run();

		return tr.EndPosition;
	}

	public override void CameraSimulate( IClient cl )
	{
		if ( !IsScared && !isTaunting )
			base.CameraSimulate( cl );
		else
		{
			Controller?.FrameSimulate( cl );

			Camera.Position = DoThirdPersonCamera( cl ); 
			Camera.Rotation = EyeRotation;
			Camera.FirstPersonViewer = null;
		}
	}
}
