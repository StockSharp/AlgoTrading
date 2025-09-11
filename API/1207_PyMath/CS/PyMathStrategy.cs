using System;

namespace StockSharp.Samples.Strategies;

public static class PyMathStrategy
{
	public const double PosInfProxy = 1e300;

	public static bool IsInf(this double value)
	{
		return double.IsInfinity(value) || Math.Abs(value) >= PosInfProxy;
	}

	public static bool IsFinite(this double value)
	{
		return !double.IsNaN(value) && !value.IsInf();
	}

	public static double Fmod(this double value, double divisor)
	{
		if (double.IsNaN(value) || double.IsNaN(divisor) || divisor == 0.0)
			return double.NaN;

		if (double.IsInfinity(divisor))
			return value.IsFinite() ? value : double.NaN;

		if (double.IsInfinity(value))
			return double.NaN;

		return value - Math.Truncate(value / divisor) * divisor;
	}
}
