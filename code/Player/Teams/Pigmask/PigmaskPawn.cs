namespace SUCH.Player;

public partial class PigmaskPawn : BasePawn
{
	[Net] public bool IsScared { get; set; } = false;

	[Net] public MrSaturn SaturnEntity { get; set; }

	TimeUntil timeRecoverShock;

	//Setup
	#region
	public override void MoveToSpawnpoint()
	{
		Entity pigPoint = All.OfType<StandardSpawnpoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( pigPoint == null ) return;

		Transform = pigPoint.Transform;
		SetViewAngles( pigPoint.Rotation.Angles() );

		base.MoveToSpawnpoint();
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/player/pigmask/pigmask.vmdl" );
		CreateHull();

		LifeState = LifeState.Alive;

		Components.Create<PigmaskController>();
		Components.Create<Animator>();

		AssignRankMaterial();
		AdjustStaminaOnRank();

		IsTaunting = false;

		DoRankSpawnSound( To.Single(this) );
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
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3(-16, -16, 0), new Vector3(16, 16, 64 ) );

		Tags.Add( "pigmask" );
	}
	#endregion

	//Interactions
	#region
	protected void DoMouseInputs()
	{
		if ( IsScared || IsTaunting ) return;

		if ( Input.Pressed( "Attack1" ) || Input.Pressed( "Use" ) )
		{
			if ( !SaturnEntity.IsValid() )
			{
				if ( !FindSaturn() )
					TryChimeraButton();
			}
			else
				ThrowSaturn();
		}

		if ( Input.Pressed( "Attack2" ) )
			Taunt();
	}

	void TryChimeraButton()
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

					IncrementRank();

					SUCHGame.Instance.EndRound( SUCHGame.WinEnum.Pigmask );
				}
			}
		}
	}

	void ThrowSaturn() => SaturnEntity.Throw();

	/// <summary>
	/// Finds Mr Saturn from player aim trace
	/// </summary>
	/// <returns>If the player picked up Mr Saturn</returns>
	bool FindSaturn()
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

		return false;
	}
	#endregion

	//Taunting + Scared method
	#region
	[Net] bool IsTaunting { get; set; }
	TimeSince timeLastTaunt;

	void Taunt()
	{
		if ( Controller.GroundEntity == null ) return;

		if ( PigRank == PigRanks.Colonel )
		{
			SetAnimParameter( "b_taunt_special", true );
			timeLastTaunt = 0.0f;
		}
		else
		{
			SetAnimParameter( "b_taunt", true );
			timeLastTaunt = 1.25f;
		}

		Controller.Velocity = Vector3.Zero;
		IsTaunting = true;
	}

	public void BecomeScared()
	{
		if ( IsScared ) return;

		PlaySound( "squeal" );

		(Controller as PigmaskController).AdjustStaminaValues( 0.0f );

		IsScared = true;
		timeRecoverShock = 6.5f;
	}
	#endregion

	public override void Simulate( IClient cl )
	{
		if(!IsTaunting)
			base.Simulate( cl );
		else
		{
			if( timeLastTaunt > 4f )
				IsTaunting = false;
		}

		if ( IsScared && timeRecoverShock <= 0.0f )
			IsScared = false;

		SetAnimParameter( "b_scared", IsScared );

		DoMouseInputs();
	}

	//Pig Ranks
	#region
	/// <summary>
	/// Assigns the pigmask rank to this player
	/// </summary>
	/// <param name="cl"></param>
	public void AssignPigRank(IClient cl)
	{
		AssignRankMaterial();
		AdjustStaminaOnRank();
	}

	/// <summary>
	/// Adjusts the stamina based on rank
	/// </summary>
	protected void AdjustStaminaOnRank()
	{
		var controller = Controller as PigmaskController;

		switch ( PigRank )
		{
			case PigRanks.Ensign:
				controller.AdjustStaminaValues( 50.0f );
				break;
			case PigRanks.Captain:
				controller.AdjustStaminaValues( 75.0f );
				break;
			case PigRanks.Major:
				controller.AdjustStaminaValues( 100.0f );
				break;
			case PigRanks.Colonel:
				controller.AdjustStaminaValues( 125.0f );
				break;
		}
	}

	/// <summary>
	/// Assigns the approrpriate material based on pigmask rank
	/// </summary>
	protected void AssignRankMaterial()
	{
		//With pigmask enum (and ranks in code) starting at 0
		//Up it by 1 without changing the rank itself
		SetMaterialGroup( (int)PigRank + 1 );

		switch ( PigRank )
		{
			case PigRanks.Captain: SetBodyGroup( 1, 1 ); break;
			case PigRanks.Major: SetBodyGroup( 1, 2 ); break;
			case PigRanks.Colonel: SetBodyGroup( 1, 2 ); break;

			default: SetBodyGroup( 1, 0 ); break;
		}

		//If the player is a colonel rank, show the cape
		//Else hide the cape
		if ( PigRank == PigRanks.Colonel )
			SetBodyGroup( 0, 1 );
		else
			SetBodyGroup( 0, 0 );
	}
	#endregion

	//OnKilled methods
	#region
	public DamageInfo DmgInfo;

	public override void OnKilled()
	{
		base.OnKilled();

		PlaySound( "squeal" );
		IsTaunting = false;

		if ( Game.IsClient )
			PlaySound( "die" );

		SUCHGame.Instance.StopMusicToUser( To.Single( this ) );

		PigmaskRagdoll();
		LifeState = LifeState.Dead;

		ResetRank();
		SUCHGame.CheckRoundConditions();

		SUCHGame.SwapPawn( Client, SUCHGame.AssignType.Ghost, true );
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
	#endregion

	//Camera
	#region
	public override void CameraSimulate( IClient cl )
	{
		base.CameraSimulate( cl );

		Vector3 eyePos = EyePosition;
		Rotation eyeRot = EyeRotation;

		bool doThirdPerson = IsScared || IsTaunting;

		if ( doThirdPerson )
			eyePos = DoThirdPersonCamera( cl );

		Camera.Position = eyePos;
		Camera.Rotation = eyeRot;
		Camera.FirstPersonViewer = !doThirdPerson ? this : null;
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
	#endregion
}
