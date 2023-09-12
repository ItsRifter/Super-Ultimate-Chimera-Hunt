using Sandbox;

namespace SUCH.Entities;

public class MrSaturn : AnimatedEntity, IUse
{
	public NPCPathSteer PathSteering { get; set; }

	public PigmaskPawn PigOwner;
	PigmaskPawn lastPigOwner;

	TimeSince timeThrownDropped;

	bool canBePickedUp;

	public override void Spawn()
	{
		base.Spawn();

		timeThrownDropped = 7.5f;
		canBePickedUp = true;

		SetModel( "models/entities/saturn/mrsaturn.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Tags.Add( "pigmask" );

		Position = SpawnAtPoint();

		PathSteering = new NPCPathSteer();
		PathSteering.FindNewTarget( Position );
	}

	Vector3 SpawnAtPoint()
	{
		Entity point = All.OfType<StandardSpawnpoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		return point.Position + Vector3.Up * 50;
	}

	[GameEvent.Tick.Server]
	protected void TickPathSteering()
	{
		if ( timeThrownDropped > 7.5f && Owner == null )
		{
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			PathSteering.CanNavigate = true;
			hasCollided = false;
		}

		if ( Owner != null || !PathSteering.CanNavigate ) return;

		PathSteering.Tick( Position );

		if ( !PathSteering.Output.Finished )
		{
			InputVelocity = PathSteering.Output.Direction.Normal;
			Velocity = Velocity.AddClamped( InputVelocity * Time.Delta * 500, 50 );
		} 
		else
		{
			PathSteering.FindNewTarget( Position );
		}

		Move( Time.Delta );

		var walkVelocity = Velocity.WithZ( 0 );
		if ( walkVelocity.Length > 0.5f )
		{
			var turnSpeed = walkVelocity.Length.LerpInverse( 0, 100, true );
			var targetRotation = Rotation.LookAt( walkVelocity.Normal, Vector3.Up );
			Rotation = Rotation.Lerp( Rotation, targetRotation, turnSpeed * Time.Delta * 20.0f );
		}

		var animHelper = new SaturnAnimationHelper( this );
	}

	Vector3 InputVelocity;
	//Vector3 _lookDir;

	protected void Move( float timeDelta )
	{
		var bbox = BBox.FromHeightAndRadius( 4, 4 );
		//DebugOverlay.Box( Position, bbox.Mins, bbox.Maxs, Color.Green );

		MoveHelper move = new( Position, Velocity );
		move.MaxStandableAngle = 50;
		move.Trace = move.Trace.Ignore( this ).Size( bbox );

		if ( !Velocity.IsNearlyZero( 0.001f ) )
		{
			move.TryMoveWithStep( timeDelta, 4 );
		}

		var tr = move.TraceDirection( Vector3.Down * 10.0f );

		if ( move.IsFloor( tr ) )
		{
			GroundEntity = tr.Entity;

			if ( !tr.StartedSolid )
			{
				move.Position = tr.EndPosition;
			}

			if ( InputVelocity.Length > 0 )
			{
				var movement = move.Velocity.Dot( InputVelocity.Normal );
				move.Velocity = move.Velocity - movement * InputVelocity.Normal;
				move.ApplyFriction( tr.Surface.Friction * 10.0f, timeDelta );
				move.Velocity += movement * InputVelocity.Normal;
			}
			else
			{
				move.ApplyFriction( tr.Surface.Friction * 10.0f, timeDelta );
			}
		}
		else
		{
			GroundEntity = null;
			move.Velocity += Vector3.Down * 900 * timeDelta;
		}

		Position = move.Position;
		Velocity = move.Velocity;

		SetAnimParameter( "b_walking", true );
	}

	public bool IsUsable( Entity user )
	{
		return user is PigmaskPawn && Owner == null && canBePickedUp;
	}

	public bool OnUse( Entity user )
	{
		if ( !IsUsable( user ) ) return false;

		Owner = user as PigmaskPawn;

		PickUp(Owner);

		return false;
	}

	public void PickUp(Entity picker)
	{
		if ( !canBePickedUp ) return;

		if ( Owner == null )
			Owner = picker as PigmaskPawn;

		hasCollided = false;

		PigOwner = Owner as PigmaskPawn;
		lastPigOwner = PigOwner;
		PigOwner.SaturnEntity = this;

		if ( Game.IsServer )
		{
			PathSteering.CanNavigate = false;
		}

		SetAnimParameter( "b_walking", false );
		SetParent( PigOwner, "bip01_r_hand", new Transform(new Vector3(0, 0, -8), Rotation.Identity) );
		EnableHideInFirstPerson = true;
	}

	public void Drop()
	{
		if ( Game.IsClient ) return;

		PigOwner.SaturnEntity = null;
		Owner = null;
		SetParent( null );

		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		timeThrownDropped = 0;
	}

	public void Throw()
	{
		if ( Game.IsClient ) return;

		SetParent( null );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		Position = PigOwner.EyePosition + PigOwner.EyeRotation.Forward * 5;
		Velocity = PigOwner.EyeRotation.Forward * 750;

		PigOwner.SaturnEntity = null;
		Owner = null;
		PigOwner = null;

		timeThrownDropped = 0;
	}

	bool hasCollided = false;
	TimeSince timeLastBump;

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		base.OnPhysicsCollision( eventData );

		if ( eventData.Other.Entity is PigmaskPawn ) return;

		if(eventData.Other.Entity is ChimeraPawn chimera && !hasCollided && canBePickedUp )
		{
			if ( SUCHGame.StaticRoundStatus != SUCHGame.RoundEnum.Active ) return;

			chimera.OnKilled();
			lastPigOwner.IncrementRank();

			SUCHGame.Instance.EndRound( SUCHGame.WinEnum.Pigmask );
			SUCHGame.Instance.PlaySoundToUser( To.Everyone, "roundend_pigmask_win_saturn" );
		}

		if( !hasCollided )
		{
			Sound.FromWorld( "saturn_hit", eventData.Position );
			hasCollided = true;
		}

		if( timeLastBump > 0.1f)
		{
			Sound.FromWorld( "saturn_collide", eventData.Position );
			timeLastBump = 0;
		}
	}

	public void BitByChimera( ChimeraPawn chimera )
	{
		if ( Game.IsClient ) return;

		canBePickedUp = false;
		PathSteering.CanNavigate = false;

		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		Position = chimera.EyePosition + chimera.Rotation.Forward * 5;
		Velocity = chimera.Rotation.Forward * 750 + Vector3.Up * 250;

		DeleteAsync( 3.0f );
	}
}
