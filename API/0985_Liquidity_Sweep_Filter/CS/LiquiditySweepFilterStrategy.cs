using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
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
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _sma;
	private StandardDeviation _stdDev;
	private int _trend;
	private int _barsSinceSignal;

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

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public LiquiditySweepFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Base period", "Trend");

		_multiplier = Param(nameof(Multiplier), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Band width multiplier", "Trend");

		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trend = 0;
		_barsSinceSignal = 0;
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

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_sma = null;
		_stdDev = null;
		_trend = 0;
		_barsSinceSignal = 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!_sma.IsFormed || !_stdDev.IsFormed)
			return;

		var upper = smaVal + Multiplier * stdVal;
		var lower = smaVal - Multiplier * stdVal;

		var prevTrend = _trend;

		// Determine trend based on band crossover with reset at SMA
		if (candle.ClosePrice > upper)
			_trend = 1;
		else if (candle.ClosePrice < lower)
			_trend = -1;
		else
			_trend = 0;

		if (_barsSinceSignal < CooldownBars)
			return;

		if (prevTrend != 1 && _trend == 1 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
		else if (prevTrend != -1 && _trend == -1 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
	}
}
