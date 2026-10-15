using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pearson's R oscillator strategy.
/// On every candle the closes of the last MinPeriod, MinPeriod + Step, ... MaxPeriod bars are fitted with a linear regression
/// and the period with the strongest Pearson correlation is chosen. When that correlation is at least IdealPositive or at most
/// IdealNegative, a channel is drawn around the regression line at Deviations standard deviations of the residuals. A close
/// crossing above the upper line goes long and a close crossing below the lower line goes short, reversing an opposite
/// position. A long closes when the close crosses below the midline and a short when it crosses above it.
/// </summary>
public class PearsonsROscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _minPeriod;
	private readonly StrategyParam<int> _maxPeriod;
	private readonly StrategyParam<int> _step;
	private readonly StrategyParam<decimal> _idealPositive;
	private readonly StrategyParam<decimal> _idealNegative;
	private readonly StrategyParam<decimal> _deviations;
	private readonly StrategyParam<DataType> _candleType;

	// Prefix sums of the closes (shifted by the first close), their squares and index-weighted values.
	private readonly List<double> _sumY = [];
	private readonly List<double> _sumYy = [];
	private readonly List<double> _sumIy = [];
	private double? _origin;
	private double? _prevClose;
	private (double upper, double mid, double lower)? _prevChannel;
	private (double upper, double mid, double lower)? _lastChannel;

	/// <summary>
	/// Shortest regression period.
	/// </summary>
	public int MinPeriod
	{
		get => _minPeriod.Value;
		set => _minPeriod.Value = value;
	}

	/// <summary>
	/// Longest regression period.
	/// </summary>
	public int MaxPeriod
	{
		get => _maxPeriod.Value;
		set => _maxPeriod.Value = value;
	}

	/// <summary>
	/// Increment between tested periods.
	/// </summary>
	public int Step
	{
		get => _step.Value;
		set => _step.Value = value;
	}

	/// <summary>
	/// Correlation that confirms an upward channel.
	/// </summary>
	public decimal IdealPositive
	{
		get => _idealPositive.Value;
		set => _idealPositive.Value = value;
	}

	/// <summary>
	/// Correlation that confirms a downward channel.
	/// </summary>
	public decimal IdealNegative
	{
		get => _idealNegative.Value;
		set => _idealNegative.Value = value;
	}

	/// <summary>
	/// Channel width in standard deviations.
	/// </summary>
	public decimal Deviations
	{
		get => _deviations.Value;
		set => _deviations.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public PearsonsROscillatorStrategy()
	{
		_minPeriod = Param(nameof(MinPeriod), 48)
			.SetGreaterThanZero()
			.SetDisplay("Min Period", "Shortest regression period", "Regression");

		_maxPeriod = Param(nameof(MaxPeriod), 360)
			.SetGreaterThanZero()
			.SetDisplay("Max Period", "Longest regression period", "Regression");

		_step = Param(nameof(Step), 12)
			.SetGreaterThanZero()
			.SetDisplay("Step", "Increment between tested periods", "Regression");

		_idealPositive = Param(nameof(IdealPositive), 0.85m)
			.SetDisplay("Ideal Positive", "Correlation that confirms an upward channel", "Regression");

		_idealNegative = Param(nameof(IdealNegative), -0.85m)
			.SetDisplay("Ideal Negative", "Correlation that confirms a downward channel", "Regression");

		_deviations = Param(nameof(Deviations), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviations", "Channel width in standard deviations", "Regression");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_sumY.Clear();
		_sumYy.Clear();
		_sumIy.Clear();
		_origin = null;
		_prevClose = null;
		_prevChannel = null;
		_lastChannel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = (double)candle.ClosePrice;
		_origin ??= close;
		var y = close - _origin.Value;
		var index = _sumY.Count;

		_sumY.Add((index > 0 ? _sumY[index - 1] : 0d) + y);
		_sumYy.Add((index > 0 ? _sumYy[index - 1] : 0d) + y * y);
		_sumIy.Add((index > 0 ? _sumIy[index - 1] : 0d) + index * y);

		var channel = FindChannel(index);

		var prevClose = _prevClose;
		var prevChannel = _prevChannel;
		_prevClose = close;
		_prevChannel = channel ?? _lastChannel;

		if (channel != null)
			_lastChannel = channel;

		if (prevClose is not double pc || prevChannel is not (double prevUpper, double prevMid, double prevLower) || _lastChannel is not (double upper, double mid, double lower))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (channel != null && pc <= prevUpper && close > upper && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (channel != null && pc >= prevLower && close < lower && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && pc >= prevMid && close < mid)
			SellMarket(Position);
		else if (Position < 0 && pc <= prevMid && close > mid)
			BuyMarket(-Position);
	}

	private (double upper, double mid, double lower)? FindChannel(int last)
	{
		var bestR = 0d;
		var bestPeriod = 0;
		var bestSlope = 0d;
		var bestIntercept = 0d;
		var bestStd = 0d;

		for (var period = MinPeriod; period <= MaxPeriod; period += Step)
		{
			if (period > last + 1)
				break;

			var start = last - period + 1;
			double Range(List<double> sums) => sums[last] - (start > 0 ? sums[start - 1] : 0d);

			var n = (double)period;
			var sy = Range(_sumY);
			var syy = Range(_sumYy);
			// Local x runs from 0 to period - 1.
			var sxy = Range(_sumIy) - start * sy;
			var sx = n * (n - 1) / 2;
			var sxx = (n - 1) * n * (2 * n - 1) / 6;

			var covXy = sxy - sx * sy / n;
			var varX = sxx - sx * sx / n;
			var varY = syy - sy * sy / n;

			if (varX <= 0 || varY <= 0)
				continue;

			var r = covXy / Math.Sqrt(varX * varY);

			if (Math.Abs(r) <= Math.Abs(bestR))
				continue;

			var slope = covXy / varX;
			bestR = r;
			bestPeriod = period;
			bestSlope = slope;
			bestIntercept = (sy - slope * sx) / n;
			bestStd = Math.Sqrt(Math.Max(0d, varY - slope * covXy) / n);
		}

		if (bestPeriod == 0 || (bestR < (double)IdealPositive && bestR > (double)IdealNegative))
			return null;

		var mid = bestIntercept + bestSlope * (bestPeriod - 1) + _origin.Value;
		var width = (double)Deviations * bestStd;
		return (mid + width, mid, mid - width);
	}
}
