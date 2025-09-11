using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Library of helper functions for working with label and line arrays.
/// Converted from TradingView script.
/// </summary>
public class CommonLabelLineArrayFunctionsStrategy : Strategy
{
	private readonly List<string> _labelTexts = new();
	private readonly List<(decimal X1, decimal Y1, decimal X2, decimal Y2)> _linePoints = new();

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Example initialization
		CreateLabelArray(["Label 1", "Label 2"]);
		CreateLineArray([(0m, 0m, 1m, 1m), (1m, 1m, 2m, 2m)]);
	}

	/// <summary>
	/// Creates labels from provided texts.
	/// </summary>
	public void CreateLabelArray(IEnumerable<string> texts)
	{
		_labelTexts.Clear();
		_labelTexts.AddRange(texts);
	}

	/// <summary>
	/// Joins label texts into single string.
	/// </summary>
	public string JoinLabelArray(string delimiter) => string.Join(delimiter, _labelTexts);

	/// <summary>
	/// Removes all labels.
	/// </summary>
	public void DeleteLabelArray() => _labelTexts.Clear();

	/// <summary>
	/// Creates line definitions from provided points.
	/// </summary>
	public void CreateLineArray(IEnumerable<(decimal X1, decimal Y1, decimal X2, decimal Y2)> lines)
	{
		_linePoints.Clear();
		_linePoints.AddRange(lines);
	}

	/// <summary>
	/// Removes all lines.
	/// </summary>
	public void DeleteLineArray() => _linePoints.Clear();
}
