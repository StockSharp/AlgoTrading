namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ATR Slope Breakout Strategy.
/// Enters when ATR slope breaks out above average, uses price/EMA for direction.
/// </summary>
public class AtrSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAtr;
	private decimal _slopeSum;
	private int _slopeCount;
	private readonly Queue<decimal> _slopeQueue = new();

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
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

	public AtrSlopeBreakoutStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicator");

		_slopePeriod = Param(nameof(SlopePeriod), 20)
			.SetDisplay("Slope Period", "Period for slope statistics", "Indicator");

		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 1.5m)
			.SetDisplay("Breakout Multiplier", "Multiplier for slope breakout", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAtr = 0;
		_slopeSum = 0;
		_slopeCount = 0;
		_slopeQueue.Clear();

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var ema = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevAtr == 0)
		{
			_prevAtr = atrValue;
			return;
		}

		// Calculate ATR slope (change)
		var slope = atrValue - _prevAtr;
		_prevAtr = atrValue;

		// Update slope statistics
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

		// Simplified std dev estimation
		var absAvgSlope = Math.Abs(slopeAvg);
		var threshold = absAvgSlope > 0 ? absAvgSlope * BreakoutMultiplier : atrValue * 0.01m;

		var priceAboveEma = candle.ClosePrice > emaValue;

		// ATR slope breakout - volatility expanding
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
		// Slope returning to mean - exit
		else if (slope < slopeAvg)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
		}
	}
}
