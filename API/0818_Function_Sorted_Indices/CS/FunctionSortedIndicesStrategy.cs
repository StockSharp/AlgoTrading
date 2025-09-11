using System;
using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

public class FunctionSortedIndicesStrategy : Strategy
{
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var values = new decimal[] { 55m, 66m, 44m, 77m, 80m, -90m, 1m };
		var (indices, ordered) = SortIndices(values, true);

		LogInfo($"Original: {string.Join(", ", values)}");
		LogInfo($"Sorted: {string.Join(", ", ordered)}");
		LogInfo($"Indices: {string.Join(", ", indices)}");
	}

	private static (int[] indices, decimal[] ordered) SortIndices(decimal[] sample, bool forward)
	{
		var size = sample.Length;
		var ordered = new decimal[size];
		var indices = new int[size];

		for (var i = 0; i < size; i++)
		{
			ordered[i] = sample[i];
			indices[i] = i;
		}

		for (var i = 0; i < size - 1; i++)
		{
			var ai = ordered[i];
			var iai = indices[i];

			for (var j = i + 1; j < size; j++)
			{
				var aj = ordered[j];
				var iaj = indices[j];

				if (aj < ai)
				{
					ordered[j] = ai;
					indices[j] = iai;
					ordered[i] = aj;
					indices[i] = iaj;
					ai = aj;
					iai = iaj;
				}
			}
		}

		if (!forward)
		{
			Array.Reverse(indices);
			Array.Reverse(ordered);
		}

		return (indices, ordered);
	}
}
