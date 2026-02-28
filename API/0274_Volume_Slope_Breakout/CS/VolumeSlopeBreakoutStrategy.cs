namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Volume Slope Breakout Strategy.
/// Trades when volume slope breaks out, using price/EMA for direction.
/// </summary>
public class VolumeSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevVolume;
	private decimal _slopeSum;
	private int _slopeCount;
	private readonly Queue<decimal> _slopeQueue = new();

	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	public int SlopePeriod
	{
		get => _slopePeriod.Value;
		set => _slopePeriod.Value = value;
	}

	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public VolumeSlopeBreakoutStrategy()
	{
		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetDisplay("Volume Avg Period", "Period for volume averaging", "Indicator");

		_slopePeriod = Param(nameof(SlopePeriod), 20)
			.SetDisplay("Slope Period", "Period for slope statistics", "Indicator");

		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 1.5m)
			.SetDisplay("Breakout Multiplier", "Multiplier for breakout threshold", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevVolume = 0;
		_slopeSum = 0;
		_slopeCount = 0;
		_slopeQueue.Clear();

		var ema = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var vol = candle.TotalVolume;

		if (_prevVolume == 0)
		{
			_prevVolume = vol;
			return;
		}

		// Calculate volume slope (change)
		var slope = vol - _prevVolume;
		_prevVolume = vol;

		_slopeQueue.Enqueue(slope);
		_slopeSum += slope;
		_slopeCount++;

		if (_slopeCount > SlopePeriod)
		{
			_slopeSum -= _slopeQueue.Dequeue();
			_slopeCount = SlopePeriod;
		}

		if (_slopeCount < SlopePeriod)
			return;

		var slopeAvg = _slopeSum / _slopeCount;
		var absAvgSlope = Math.Abs(slopeAvg);
		var threshold = absAvgSlope > 0 ? absAvgSlope * BreakoutMultiplier : vol * 0.1m;

		var priceAboveEma = candle.ClosePrice > emaValue;

		// Volume slope breakout
		if (slope > slopeAvg + threshold)
		{
			if (priceAboveEma && Position <= 0)
			{
				BuyMarket();
			}
			else if (!priceAboveEma && Position >= 0)
			{
				SellMarket();
			}
		}
		// Volume declining - exit
		else if (slope < slopeAvg - threshold)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
		}
	}
}
