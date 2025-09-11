using System;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates discrete linear convolution of two sequences.
/// </summary>
public class MathSpecialFunctionsConvolve1DStrategy : Strategy
{
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var random = new Random();
		var signal = new decimal[20];
		var filter = new decimal[20];

		for (var i = 0; i < signal.Length; i++)
		{
			signal[i] = (decimal)random.NextDouble();
			filter[i] = (decimal)Math.Cos(i * Math.PI);
		}

		var result = Convolve(signal, filter);
		this.AddInfoLog(string.Join(", ", result));
	}

	private static decimal[] Convolve(decimal[] signal, decimal[] filter)
	{
		if (signal.Length == 0)
			throw new ArgumentException("Signal must have at least one element.", nameof(signal));

		if (filter.Length == 0)
			throw new ArgumentException("Filter must have at least one element.", nameof(filter));

		var outputSize = signal.Length + filter.Length - 1;
		var output = new decimal[outputSize];

		for (var k = 0; k < outputSize; k++)
		{
			var sum = 0m;
			for (var n = 0; n < signal.Length; n++)
			{
				var i = k - n;
				if (i >= 0 && i < filter.Length)
					sum += signal[n] * filter[i];
			}

			output[k] = sum;
		}

		return output;
	}
}
