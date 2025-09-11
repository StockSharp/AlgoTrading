using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Augmented Dickey-Fuller test (ADF) utilities.
/// </summary>
public class FunctionAdfStrategy : Strategy
{
	public static (double adf, double crit, int nobs) AdfTest(IList<double> data, int nLag, string conf)
	{
		if (nLag >= data.Count / 2 - 2)
			throw new ArgumentOutOfRangeException(nameof(nLag), "Maximum lag must be less than half the data length minus two.");

		var nobs = data.Count - nLag - 1;

		var y = new double[nobs];
		var x = new double[nobs];
		double meanX = 0;

		for (var i = 0; i < nobs; i++)
		{
			var v0 = data[i];
			var v1 = data[i + 1];
			meanX += v1;
			y[i] = v0 - v1;
			x[i] = v1;
		}

		meanX /= nobs;

		var m = 2 + Math.Max(0, nLag);
		var X = new double[nobs, m];

		for (var i = 0; i < nobs; i++)
		{
			X[i, 0] = x[i];
			X[i, m - 1] = 1.0;
		}

		if (nLag > 0)
		{
			for (var n = 1; n <= nLag; n++)
			{
				for (var i = 0; i < nobs; i++)
					X[i, n] = data[i + n] - data[i + n + 1];
			}
		}

		var XtX = new double[m, m];
		var Xty = new double[m];

		for (var i = 0; i < nobs; i++)
		{
			for (var j = 0; j < m; j++)
			{
				Xty[j] += X[i, j] * y[i];

				for (var k = 0; k < m; k++)
					XtX[j, k] += X[i, j] * X[i, k];
			}
		}

		var coeff = Solve(XtX, Xty);

		var yhat = new double[nobs];

		for (var i = 0; i < nobs; i++)
		{
			for (var j = 0; j < m; j++)
				yhat[i] += X[i, j] * coeff[j];
		}

		double sum1 = 0;
		double sum2 = 0;

		for (var i = 0; i < nobs; i++)
		{
			var resid = y[i] - yhat[i];
			sum1 += resid * resid;

			var diff = x[i] - meanX;
			sum2 += diff * diff;
		}

		sum1 /= nobs - m;
		var se = Math.Sqrt(sum1 / sum2);

		var adf = coeff[0] / se;

		var nobsSq = nobs * nobs;
		var nobsCu = nobsSq * nobs;

		var crit = conf switch
		{
			"90%" => -2.56677 - 1.5384 / nobs - 2.809 / nobsSq,
			"95%" => -2.86154 - 2.8903 / nobs - 4.234 / nobsSq - 40.040 / nobsCu,
			"99%" => -3.43035 - 6.5393 / nobs - 16.786 / nobsSq - 79.433 / nobsCu,
			_ => -2.56677 - 1.5384 / nobs - 2.809 / nobsSq,
		};

		return (adf, crit, nobs);
	}

	private static double[] Solve(double[,] a, double[] b)
	{
		var n = b.Length;
		var mat = new double[n, n + 1];

		for (var i = 0; i < n; i++)
		{
			for (var j = 0; j < n; j++)
				mat[i, j] = a[i, j];
			mat[i, n] = b[i];
		}

		for (var i = 0; i < n; i++)
		{
			var maxRow = i;

			for (var k = i + 1; k < n; k++)
			{
				if (Math.Abs(mat[k, i]) > Math.Abs(mat[maxRow, i]))
					maxRow = k;
			}

			if (maxRow != i)
			{
				for (var k = i; k <= n; k++)
				{
					var tmp = mat[i, k];
					mat[i, k] = mat[maxRow, k];
					mat[maxRow, k] = tmp;
				}
			}

			var pivot = mat[i, i];

			for (var k = i; k <= n; k++)
				mat[i, k] /= pivot;

			for (var j = 0; j < n; j++)
			{
				if (j == i)
					continue;

				var factor = mat[j, i];

				for (var k = i; k <= n; k++)
					mat[j, k] -= factor * mat[i, k];
			}
		}

		var result = new double[n];

		for (var i = 0; i < n; i++)
			result[i] = mat[i, n];

		return result;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield break;
	}
}

