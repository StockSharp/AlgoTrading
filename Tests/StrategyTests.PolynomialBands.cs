namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using MathNet.Numerics;
using MathNet.Numerics.LinearRegression;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;

public abstract partial class StrategyTests
{
	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(false, 100, 2, 2.0)]
	[DataRow(false, 100, 1, 2.0)]
	[DataRow(true, 40, 3, 1.5)]
	[DataRow(false, 30, 0, 1.2)]
	[DataRow(false, 40, 4, 1.8)]
	[DataRow(true, 40, 6, 1.8)]
	public async Task S1172_PolynomialEndpointAndResidualBandsDetermineEveryOrder(bool secondary, int length, int degree, double multiplier)
	{
		const string key = "1172_Polynomial_Regression_Bands_Channel";
		var prices = new List<decimal>();
		var violations = new List<string>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var filledPosition = 0m;
		var signals = 0;
		var orders = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var residualsDifferFromRawPrices = 0;
		var curvatureChangesSignals = 0;
		var nearbySignals = 0;
		DateTime? lastSignal = null;
		await Replay(key, (strategy, alternateSecurity) =>
		{
			if (secondary)
				strategy.Security = alternateSecurity;
			foreach (var (name, value) in new (string, object)[] { ("Length", 100), ("Degree", 2), ("StdDevMultiplier", 2m) })
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"The polynomial residual channel requires {name}, not a fixed EMA crossover.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			SetParam(strategy, "Length", length);
			SetParam(strategy, "Degree", degree);
			SetParam(strategy, "StdDevMultiplier", (decimal)multiplier);
			strategy.OwnTradeReceived += (_, trade) => filledPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add($"{candle.OpenTime:O}: an eligible channel entry was missed.");
				expectedSide = null;
				prices.Add(candle.ClosePrice);
				if (prices.Count < length)
					return;
				if (prices.Count > length)
					prices.RemoveAt(0);
				var (center, deviation) = PolynomialProjection(prices, degree);
				var width = (decimal)multiplier * deviation;
				var sign = candle.ClosePrice < center - width ? 1 : candle.ClosePrice > center + width ? -1 : 0;
				var (mean, rawDeviation) = PolynomialProjection(prices, 0);
				var rawSign = candle.ClosePrice < mean - (decimal)multiplier * rawDeviation ? 1
					: candle.ClosePrice > mean + (decimal)multiplier * rawDeviation ? -1 : 0;
				if (sign != rawSign)
					residualsDifferFromRawPrices++;
				if (degree >= 2)
				{
					var (linear, linearDeviation) = PolynomialProjection(prices, 1);
					var linearSign = candle.ClosePrice < linear - (decimal)multiplier * linearDeviation ? 1
						: candle.ClosePrice > linear + (decimal)multiplier * linearDeviation ? -1 : 0;
					if (sign != linearSign)
						curvatureChangesSignals++;
				}
				if (sign > 0 && strategy.Position <= 0m)
				{
					expectedSide = Sides.Buy;
					buys++;
				}
				else if (sign < 0 && strategy.Position >= 0m)
				{
					expectedSide = Sides.Sell;
					sells++;
				}
				if (expectedSide is null)
					return;
				signals++;
				expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
				if (strategy.Position != 0m)
					reversals++;
				if (lastSignal is DateTime previous && candle.OpenTime - previous < TimeSpan.FromHours(6))
					nearbySignals++;
				lastSignal = candle.OpenTime;
			};
			strategy.OrderRegistering += order =>
			{
				orders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market || strategy.Position != filledPosition)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume} from polynomial residual bands, filled position {filledPosition}.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null);
		AreEqual(signals, orders);
		IsTrue(buys > 0 && sells > 0 && reversals > 0 && nearbySignals > 0);
		if (degree > 0)
			IsTrue(residualsDifferFromRawPrices > 0, "Residual deviation and the regression endpoint must change signals from ordinary price bands.");
		if (degree >= 2)
			IsTrue(curvatureChangesSignals > 0, "Polynomial curvature must affect real archive entries.");
		TestContext.WriteLine($"{key}: degree {degree}, {orders} orders ({buys}/{sells}), {reversals} reversals, {nearbySignals} nearby entries, {residualsDifferFromRawPrices} signals changed from raw-price bands, {curvatureChangesSignals} changed by curvature.");
	}

	private static (decimal Center, decimal Deviation) PolynomialProjection(IReadOnlyList<decimal> prices, int degree)
	{
		if (degree > 3)
		{
			var grid = Enumerable.Range(0, prices.Count).Select(i => 2.0 * i / (prices.Count - 1) - 1.0).ToArray();
			var values = prices.Select(price => (double)(price - prices[0])).ToArray();
			var coefficients = Fit.Polynomial(grid, values, degree, DirectRegressionMethod.QR);
			var fittedValues = grid.Select(x => Polynomial.Evaluate(x, coefficients)).ToArray();
			var squaredError = values.Select((value, i) => Math.Pow(value - fittedValues[i], 2)).Sum();
			return (prices[0] + (decimal)fittedValues[^1], (decimal)Math.Sqrt(squaredError / prices.Count));
		}
		// Closed-form discrete orthogonal polynomials on a symmetric, equally spaced grid.
		// This oracle does not call the implementation's numerical QR solver.
		var count = prices.Count;
		var x = Enumerable.Range(0, count).Select(i => (decimal)(2 * i - count + 1)).ToArray();
		var squares = x.Select(v => v * v).ToArray();
		var meanSquare = squares.Average();
		var mean = prices.Average();
		var quadratic = squares.Select(v => v - meanSquare).ToArray();
		var cubicWeight = squares.Sum(v => v * v) / squares.Sum();
		var cubic = x.Select(v => v * v * v - cubicWeight * v).ToArray();
		var linearCoefficient = degree >= 1 ? x.Select((v, i) => v * prices[i]).Sum() / squares.Sum() : 0m;
		var quadraticCoefficient = degree >= 2 ? quadratic.Select((v, i) => v * prices[i]).Sum() / quadratic.Sum(v => v * v) : 0m;
		var cubicCoefficient = degree >= 3 ? cubic.Select((v, i) => v * prices[i]).Sum() / cubic.Sum(v => v * v) : 0m;
		var fitted = x.Select((v, i) => mean + linearCoefficient * v + quadraticCoefficient * quadratic[i] + cubicCoefficient * cubic[i]).ToArray();
		var variance = prices.Select((value, i) => (value - fitted[i]) * (value - fitted[i])).Sum() / count;
		return (fitted[^1], (decimal)Math.Sqrt((double)variance));
	}
}
