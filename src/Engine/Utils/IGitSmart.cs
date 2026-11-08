using DeployItSmart.Model.DataStructure;
using System;

namespace DeployItSmart.Engine.Utils;
/*

    Create a front to Git Library to mock unit testing
    Add Config with Promotion Stategry listing branches from Stable to Bottom
    MAke should any get history we get histories of all branches target and merge then insi single commit history listing
    1. Alpha Request history from a tage it doesn't own, you move to beta when beta is asked and doesnt find move to main when main find we find last common commit hash across
    alpha - main and return the history in a straight line.
    2. alpha has now ALPHA main and beta then will continue ahead since the last stable
*/
public interface IGitSmart
{
    Task<List<string>> GetTagHistory();
    Task<List<CommitInfoStructure>> GetHistory(string? sinceTag);
}
