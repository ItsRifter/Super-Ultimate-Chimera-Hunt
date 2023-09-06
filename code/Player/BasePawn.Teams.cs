namespace SUCH.Player;

public partial class BasePawn
{
	public enum TeamEnum
	{
		Unspecified,
		Ghosts,
		Pigmask,
		Chimera
	}

	[Net] public TeamEnum Team { get; set; } = TeamEnum.Unspecified;

	[Net] public bool IsFancyGhost { get; set; }
}

