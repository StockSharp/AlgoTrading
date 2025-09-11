using System;
using System.IO;
using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Logs file names from an external list.
/// </summary>
public class FilesToRenameStrategy : Strategy
{
	private readonly StrategyParam<string> _filePath;

	/// <summary>
	/// Path to the source text file.
	/// </summary>
	public string FilePath
	{
		get => _filePath.Value;
		set => _filePath.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FilesToRenameStrategy()
	{
		_filePath = Param(nameof(FilePath), "TradingView/0374_files_to_rename.txt")
			.SetDisplay("File Path", "Path to the text file containing file names", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (!File.Exists(FilePath))
			return;

		foreach (var line in File.ReadLines(FilePath))
			LogInfo(line);
	}
}
