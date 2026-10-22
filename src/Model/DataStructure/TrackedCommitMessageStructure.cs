using DeployItSmart.Model.DataStructure.Enums;

namespace DeployItSmart.Model.DataStructure;

public record class TrackedCommitMessageStructure
{
	public string CommitId { get; init; } = default!;
	public string OriginalMessage { get; init; } = string.Empty;
	public Dictionary<TrackerReleaseType, List<string>> TokenMessages { get; init; } = new()
	{
		[TrackerReleaseType.Breaking] = new List<string>(),
		[TrackerReleaseType.Feature] = new List<string>(),
		[TrackerReleaseType.Fix] = new List<string>(),
		[TrackerReleaseType.Note] = new List<string>(),
		[TrackerReleaseType.Sponsor] = new List<string>(),
		[TrackerReleaseType.Thanks] = new List<string>(),
	};
	public Dictionary<TrackerDirtyType, List<string>> TokenDirtyMessages { get; init; } = new()
	{
		[TrackerDirtyType.Trace] = new List<string>(),
		[TrackerDirtyType.TraceNoTrigger] = new List<string>(),
		[TrackerDirtyType.WorkInProgress] = new List<string>(),
	};
	public List<string> AffectedFiles { get; init; } = [];
	public string Author { get; init; } = string.Empty;
	public int CountReleaseToken()
	{
		int count = 0;
		foreach (var item in TokenMessages)
			count += item.Value.Count;
		return count;
	}

	public int CountDirtyToken()
	{
		int count = 0;
		foreach (var item in TokenDirtyMessages)
			count += item.Value.Count;
		return count;
	}
}
