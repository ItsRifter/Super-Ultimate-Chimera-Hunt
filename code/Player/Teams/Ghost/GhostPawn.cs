namespace SUCH.Player;

public partial class GhostPawn : BasePawn
{
	public override void MoveToSpawnpoint()
	{
		Entity ghostPoint = All.OfType<StandardSpawnpoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( ghostPoint == null ) return;

		Transform = ghostPoint.Transform;
		SetViewAngles( ghostPoint.Rotation.Angles() );

		base.MoveToSpawnpoint();
	}

	[ClientRpc]
	public void SetViewAngles( Angles angle )
	{
		SetAngles = angle;
		SetView = true;
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/player/ghost/ghost.vmdl" );
		CreatePhysHull();

		Components.Create<GhostController>();
		Components.Create<Animator>();

		SUCHGame.DoVisibilty();
	}
}
