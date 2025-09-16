using System;
using System.Globalization;
using System.Text;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "AutoMagiCal" MetaTrader script that generates a deterministic magic number from the instrument symbol.
/// </summary>
public class AutoMagiCalStrategy : Strategy
{
	private const decimal FirstDividerThreshold = 999999999m;
	private const decimal SecondDividerThreshold = 9999999999m;

	private int? _magicNumber;

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

		if (string.IsNullOrEmpty(symbol))
		{
			AddErrorLog("Security is not assigned. Unable to generate magic number.");
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
			AddErrorLog($"Symbol '{symbol}' is too short to extract digits for the magic number.");
			return;
		}

		if (!decimal.TryParse(digits.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
		{
			AddErrorLog($"Failed to parse intermediate number '{digits}'.");
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

		AddInfoLog($"Magic No. = {magic}");
	}
}
