namespace StockSharp.Samples.Strategies;

using System;
using System.IO;
using System.Text;

using StockSharp.Algo.Strategies;

public class HpcsInter1Strategy : Strategy
{
	private readonly StrategyParam<string> _csvFileName;
	private readonly StrategyParam<char> _separator;
	private string _resolvedPath;

	/// <summary>
	/// Name of the CSV file that will be read during initialization.
	/// </summary>
	public string CsvFileName
	{
		get => _csvFileName.Value;
		set => _csvFileName.Value = value;
	}

	/// <summary>
	/// Character that splits the first line into individual tokens.
	/// </summary>
	public char Separator
	{
		get => _separator.Value;
		set => _separator.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HpcsInter1Strategy"/> class.
	/// </summary>
	public HpcsInter1Strategy()
	{
		_csvFileName = Param(nameof(CsvFileName), "Third.csv")
			.SetDisplay("CSV File", "Name of the CSV file that will be read when the strategy starts.", "General");

		_separator = Param(nameof(Separator), '_')
			.SetDisplay("Separator", "Character that is used to split the first line into tokens.", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResolveFilePath();

		if (_resolvedPath is null)
			return;

		ReadAndLogCsv();
	}

	private void ResolveFilePath()
	{
		var fileName = CsvFileName;

		if (string.IsNullOrWhiteSpace(fileName))
		{
			LogWarning("CSV file name is empty. Nothing to read.");
			_resolvedPath = null;
			return;
		}

		try
		{
			_resolvedPath = Path.GetFullPath(fileName);
		}
		catch (Exception ex)
		{
			LogError($"Failed to resolve file path '{fileName}'. Error: {ex.Message}");
			_resolvedPath = null;
			return;
		}

		if (!File.Exists(_resolvedPath))
		{
			LogWarning($"File '{_resolvedPath}' was not found. Strategy will stay idle.");
			_resolvedPath = null;
		}
	}

	private void ReadAndLogCsv()
	{
		var path = _resolvedPath;

		if (path is null)
			return;

		try
		{
			using var reader = new StreamReader(path, Encoding.UTF8, true);
			var line = reader.ReadLine();

			if (string.IsNullOrWhiteSpace(line))
			{
				LogWarning($"File '{path}' is empty or the first line contains only whitespace.");
				return;
			}

			LogInfo($"Original line: {line}");

			var tokens = line.Split(Separator);

			if (tokens.Length == 0)
			{
				LogWarning($"Separator '{Separator}' did not produce any tokens.");
				return;
			}

			for (var i = 0; i < tokens.Length; i++)
			{
				var token = tokens[i];
				LogInfo($"Token {i + 1}: {token}");
			}
		}
		catch (Exception ex)
		{
			LogError($"Failed to read file '{path}'. Error: {ex.Message}");
		}
	}
}
