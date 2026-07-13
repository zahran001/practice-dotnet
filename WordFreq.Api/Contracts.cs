namespace WordFreq.Api
{
	// The HTTP contract and domain model are allowed to diverge
	public sealed record AnalyzeRequest(string Text, int? Top);
	public sealed record WordCount(string Word, int Count);
	public sealed record AnalyzeResponse(
		int TotalWords,
		int DistinctWords,
		IReadOnlyList<WordCount> Top);
}
