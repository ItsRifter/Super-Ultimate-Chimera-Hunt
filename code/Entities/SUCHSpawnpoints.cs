namespace SUCH.Entities;

[Library( "such_player_standard" )]
[HammerEntity]
[EditorModel( "models/player/pigmask/pigmask.vmdl", "white", "white", FixedBounds = true )]
[Title( "Pigmask/Ghost Spawnpoint" )]
[Category( "Player" )]
[Icon( "place" )]
public class StandardSpawnpoint : Entity
{

}

[Library( "such_player_chimera" )]
[HammerEntity]
[EditorModel( "models/player/chimera/chimera.vmdl", "white", "white", FixedBounds = true )]
[Title( "Chimera Spawnpoint" )]
[Category( "Player" )]
[Icon( "place" )]
public class ChimeraSpawnpoint : Entity
{

}
