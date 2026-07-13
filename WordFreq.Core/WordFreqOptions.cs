namespace WordFreq.Core;

public sealed class WordFreqOptions
{
	public const string SectionName = "WordFreq";

	public int MinLength { get; set; } = 1;
	public bool IgnoreCase { get; set; } = false;
	public int MaxTextLength { get; set; } = 1_000_000;
	public int DefaultTop { get; set; } = 10;
}
