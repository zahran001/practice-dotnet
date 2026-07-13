namespace WordFreq.Core;

public sealed record WordFreqOptions(
	int MinLength = 1,
	bool IgnoreCase = false);
