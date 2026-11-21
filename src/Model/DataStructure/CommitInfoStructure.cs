using DeployItSmart.Model.DataStructure.Enums;

namespace DeployItSmart.Model.DataStructure;

public record class CommitInfoStructure
{
	public string CommitId { get; init; } = default!;
	public bool IsRemote { get; init; }
	public string CommitMessage { get; init; } = string.Empty;
	public List<string> AffectedFiles { get; init; } = [];
	public string Author { get; init; } = string.Empty;
}
