namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;

/// <summary>
/// Harmonic pattern detection helper methods.
/// </summary>
public class HarmonicPatternStrategy : Strategy
{
	/// <summary>
	/// Compute the price rate of line AB divided by line BC.
	/// </summary>
	public static decimal LinePriceRate(decimal pointC, decimal pointB, decimal pointA)
	{
		return (pointA - pointB) / (pointB - pointC);
	}

	/// <summary>
	/// Compute the time rate of line AB divided by line BC.
	/// </summary>
	public static decimal LineTimeRate(decimal pointC, decimal pointB, decimal pointA)
	{
		return (0m - (pointA - pointB)) / (pointB - pointC);
	}

	/// <summary>
	/// Check if value is within tolerance range.
	/// </summary>
	public static bool IsInRange(decimal value, decimal min, decimal max, decimal marginOfError = 0.05m, string calculationType = "additive")
	{
		return calculationType switch
		{
			"additive" => value <= (max + marginOfError) && value >= (min - marginOfError),
			"multiplicative" => value <= (max * (1m + marginOfError)) && value >= (min * (1m - marginOfError)),
			_ => throw new ArgumentException("HarmonicPattern -> IsInRange(): undefined margin calculation type."),
		};
	}

	/// <summary>
	/// Check if rate corresponds to a harmonic triangle pattern.
	/// </summary>
	public static bool IsHarmonicTriangle(decimal rateCba, decimal marginOfError = 0.05m, string calculationType = "additive")
	{
		const decimal Phi = 1.618033988749895m;
		for (var i = 1; i <= 12; i++)
		{
			var phid = -(decimal)Math.Pow((double)Phi, -5 + i);
			if (IsInRange(rateCba, phid, phid, marginOfError, calculationType))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Check if rate corresponds to a Double Top/Bottom pattern.
	/// </summary>
	public static bool Is2Tap(decimal rateCba, decimal marginOfError = 0.05m, string calculationType = "additive")
	{
		return IsInRange(rateCba, -1.000m, -1.000m, marginOfError, calculationType);
	}

	/// <summary>
	/// Check if rates correspond to a Triple Top/Bottom pattern.
	/// </summary>
	public static bool Is3Tap(decimal rateEdc, decimal rateCba, decimal marginOfError = 0.05m, string calculationType = "additive")
	{
		var isEdc = IsInRange(rateEdc, -1.000m, -1.000m, marginOfError, calculationType);
		var isCba = IsInRange(rateCba, -1.000m, -1.000m, marginOfError, calculationType);
		return isEdc && isCba;
	}

	/// <summary>
	/// Check if rates correspond to a Quadruple Top/Bottom pattern.
	/// </summary>
	public static bool Is4Tap(decimal rateGfe, decimal rateEdc, decimal rateCba, decimal marginOfError = 0.05m, string calculationType = "additive")
	{
		var isGfe = IsInRange(rateGfe, -1.000m, -1.000m, marginOfError, calculationType);
		var isEdc = IsInRange(rateEdc, -1.000m, -1.000m, marginOfError, calculationType);
		var isCba = IsInRange(rateCba, -1.000m, -1.000m, marginOfError, calculationType);
		return isGfe && isEdc && isCba;
	}

	/// <summary>
	/// Check if rates correspond to an AB=CD pattern.
	/// </summary>
	public static bool IsABCD(decimal rateCba, decimal rateDcb, decimal marginOfError = 0.05m, string calculationType = "additive")
	{
		var isCba = IsInRange(rateCba, -1.618m, -1.270m, marginOfError, calculationType);
		var isDcb = IsInRange(rateDcb, -0.786m, -0.618m, marginOfError, calculationType);
		return isCba && isDcb;
	}
}
