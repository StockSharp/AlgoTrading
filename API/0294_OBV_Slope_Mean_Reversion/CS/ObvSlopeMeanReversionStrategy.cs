namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// OBV Slope Mean Reversion Strategy.
/// Trades based on On-Balance Volume slope reversions to the mean.
/// </summary>
public class ObvSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevObv;
	private bool _hasPrev;
	private decimal _slopeSum;
	private decimal _slopeSqSum;
	private int _slopeCount;
	private readonly Queue<decimal> _slopeQueue = new();

	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ObvSlopeMeanReversionStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetDisplay("Lookback Period", "Period for slope statistics", "Strategy");

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.5m)
			.SetDisplay("Deviation Multiplier", "Multiplier for std dev threshold", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevObv = 0;
		_hasPrev = false;
		_slopeSum = 0;
		_slopeSqSum = 0;
		_slopeCount = 0;
		_slopeQueue.Clear();

		var obv = new OnBalanceVolume();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(obv, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevObv = obvValue;
			_hasPrev = true;
			return;
		}

		var slope = obvValue - _prevObv;
		_prevObv = obvValue;

		_slopeQueue.Enqueue(slope);
		_slopeSum += slope;
		_slopeSqSum += slope * slope;
		_slopeCount++;

		if (_slopeCount > LookbackPeriod)
		{
			var removed = _slopeQueue.Dequeue();
			_slopeSum -= removed;
			_slopeSqSum -= removed * removed;
			_slopeCount = LookbackPeriod;
		}

		if (_slopeCount < LookbackPeriod)
			return;

		var avg = _slopeSum / _slopeCount;
		var variance = (_slopeSqSum / _slopeCount) - (avg * avg);
		var stdDev = variance <= 0 ? 0m : (decimal)Math.Sqrt((double)variance);

		var lowerThreshold = avg - DeviationMultiplier * stdDev;
		var upperThreshold = avg + DeviationMultiplier * stdDev;

		if (slope < lowerThreshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (slope > upperThreshold && Position >= 0)
		{
			SellMarket();
		}
		else if (Position > 0 && slope > avg)
		{
			SellMarket();
		}
		else if (Position < 0 && slope < avg)
		{
			BuyMarket();
		}
	}
}
