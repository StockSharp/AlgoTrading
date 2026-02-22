using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Sweep Filter strategy based on Bollinger bands.
/// </summary>
public class LiquiditySweepFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;

	private SimpleMovingAverage _sma;
	private StandardDeviation _stdDev;
	private int _trend;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	public LiquiditySweepFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Base period", "Trend");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Band width multiplier", "Trend");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trend = 0;
		_sma = new SimpleMovingAverage { Length = Length };
		_stdDev = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, _stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || !_stdDev.IsFormed)
			return;

		var upper = smaVal + Multiplier * stdVal;
		var lower = smaVal - Multiplier * stdVal;

		var prevTrend = _trend;

		if (candle.ClosePrice > upper)
			_trend = 1;
		else if (candle.ClosePrice < lower)
			_trend = -1;

		if (prevTrend <= 0 && _trend > 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			if (Position <= 0)
				BuyMarket();
		}
		else if (prevTrend >= 0 && _trend < 0)
		{
			if (Position > 0)
				SellMarket(Position);
			if (Position >= 0)
				SellMarket();
		}
	}
}
