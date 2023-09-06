namespace SUCH.UI;

public partial class MapIcon : Panel
{
	public string Ident { get; set; }
	public int Votes { get; set; }

	private Package data;

	protected void VoteMap() => MapVoteEntity.SetVote( Ident );

	protected override async Task OnParametersSetAsync()
	{
		var package = await Package.Fetch( Ident, true );
		if ( package is null || package.PackageType != Package.Type.Map )
		{
			Delete();
			return;
		}

		data = package;
		StateHasChanged();
	}

	protected override int BuildHash() => HashCode.Combine( Votes );
}
