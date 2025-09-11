using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class FinishRenamingStrategy : Strategy
{
	private readonly StrategyParam<int> _start;
	private readonly StrategyParam<string> _path;

	public int StartNumber
	{
		get => _start.Value;
		set => _start.Value = value;
	}

	public string DirectoryPath
	{
		get => _path.Value;
		set => _path.Value = value;
	}

	public FinishRenamingStrategy()
	{
		_start = Param(nameof(StartNumber), 2888)
		.SetGreaterThanZero()
		.SetDisplay("Start Number", "Initial counter value", "Renaming");
		_path = Param(nameof(DirectoryPath), Path.Combine(Environment.CurrentDirectory, "TradingView"))
		.SetDisplay("Directory Path", "Path containing text files", "Renaming");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return Array.Empty<(Security, DataType)>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var dir = DirectoryPath;
		var counter = StartNumber;

		if (!Directory.Exists(dir))
		{
			LogError($"Directory '{dir}' not found.");
			return;
		}

		var files = new DirectoryInfo(dir)
		.GetFiles("*.txt")
		.Where(f => !Regex.IsMatch(f.Name, "^[0-9]{4}_"))
		.OrderBy(f => f.Name);
		var success = 0;
		var errors = 0;

		foreach (var f in files)
		{
			var newName = $"{counter:D4}_{f.Name}";
			LogInfo($"[{counter}] {f.Name}");
			try
			{
				f.MoveTo(Path.Combine(dir, newName));
				success++;
			}
			catch (Exception ex)
			{
				LogError($"ERROR: {ex.Message}");
				errors++;
			}
			counter++;
		}

		LogInfo("Final renaming completed!");
		LogInfo($"Successful renames: {success}");
		LogInfo($"Errors: {errors}");
		LogInfo($"Final counter: {counter}");
	}
}
