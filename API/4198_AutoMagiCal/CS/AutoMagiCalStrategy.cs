using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using System.Globalization;
using System.Text;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "AutoMagiCal" MetaTrader script that generates a deterministic magic number from the instrument symbol.
/// </summary>
public class AutoMagiCalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _firstDividerThreshold;
	private readonly StrategyParam<decimal> _secondDividerThreshold;

	private int? _magicNumber;
	public AutoMagiCalStrategy()
	{
		_firstDividerThreshold = Param(nameof(FirstDividerThreshold), 999999999m)
			.SetGreaterThanZero()
			.SetDisplay("First Divider Threshold", "Upper bound before the first divider is applied", "Calculation");

		_secondDividerThreshold = Param(nameof(SecondDividerThreshold), 9999999999m)
			.SetGreaterThanZero()
			.SetDisplay("Second Divider Threshold", "Upper bound before the second divider is applied", "Calculation");
	}

	/// <summary>
	/// Value above which the intermediate number is divided by ten.
	/// </summary>
	public decimal FirstDividerThreshold
	{
		get => _firstDividerThreshold.Value;
		set => _firstDividerThreshold.Value = value;
	}

	/// <summary>
	/// Value above which the intermediate number is divided by one hundred.
	/// </summary>
	public decimal SecondDividerThreshold
	{
		get => _secondDividerThreshold.Value;
		set => _secondDividerThreshold.Value = value;
	}


	/// <summary>
	/// Gets the most recently calculated magic number.
	/// </summary>
	public int? MagicNumber => _magicNumber;

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_magicNumber = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		CalculateMagicNumber();
	}

	/// <summary>
	/// Calculates the magic number using the ASCII codes of the symbol characters, mimicking the original script.
	/// </summary>
	private void CalculateMagicNumber()
	{
		var symbol = Security?.Id ?? Security?.Code;

		if (symbol.IsEmpty())
		{
			LogError("Security is not assigned. Unable to generate magic number.");
			return;
		}

		var digits = new StringBuilder();

		for (var index = 1; index < 5 && index < symbol.Length; index++)
		{
			var charCode = (int)symbol[index];
			digits.Append(charCode);
		}

		if (digits.Length == 0)
		{
			LogError($"Symbol '{symbol}' is too short to extract digits for the magic number.");
			return;
		}

		if (!decimal.TryParse(digits.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
		{
			LogError($"Failed to parse intermediate number '{digits}'.");
			return;
		}

		if (value > FirstDividerThreshold)
		{
			value /= 10m;
		}

		if (value > SecondDividerThreshold)
		{
			value /= 100m;
		}

		var magic = (int)Math.Round(value, MidpointRounding.AwayFromZero);
		_magicNumber = magic;

		LogInfo($"Magic No. = {magic}");
	}
}
