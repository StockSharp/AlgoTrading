using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Random entry strategy with trailing stop management.
/// Biases trade direction using SMA trend filter.
/// </summary>
public class RandomTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minStopLevel;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<int> _sleepBars;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Random _random = new();
	private int _barsSinceLastTrade;
	private decimal? _stopPrice;

	public decimal MinStopLevel { get => _minStopLevel.Value; set => _minStopLevel.Value = value; }
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }
	public int SleepBars { get => _sleepBars.Value; set => _sleepBars.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RandomTrailingStopStrategy()
	{
		_minStopLevel = Param(nameof(MinStopLevel), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Min Stop %", "Minimal stop distance percent", "Trading");

		_trailingStep = Param(nameof(TrailingStep), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step %", "Trailing stop adjustment step percent", "Trading");

		_sleepBars = Param(nameof(SleepBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Sleep Bars", "Pause before next trade in bars", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Simple moving average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_barsSinceLastTrade = 0;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, sma);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceLastTrade++;

		if (Position == 0)
		{
			if (_barsSinceLastTrade < SleepBars)
				return;

			_stopPrice = null;

			var side = GetRandomSide(candle.ClosePrice, smaValue);

			if (side == Sides.Buy)
				BuyMarket();
			else
				SellMarket();

			_barsSinceLastTrade = 0;
			return;
		}

		var stopDist = candle.ClosePrice * MinStopLevel / 100m;
		var trailDist = candle.ClosePrice * TrailingStep / 100m;

		if (_stopPrice == null)
		{
			if (Position > 0)
				_stopPrice = candle.ClosePrice - stopDist;
			else
				_stopPrice = candle.ClosePrice + stopDist;

			return;
		}

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - stopDist;
			if (newStop - _stopPrice >= trailDist)
				_stopPrice = newStop;

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				_barsSinceLastTrade = 0;
			}
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + stopDist;
			if (_stopPrice - newStop >= trailDist)
				_stopPrice = newStop;

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				_barsSinceLastTrade = 0;
			}
		}
	}

	private Sides GetRandomSide(decimal price, decimal smaValue)
	{
		var rnd = _random.Next(5);
		if (price > smaValue)
			return rnd == 0 ? Sides.Sell : Sides.Buy;
		else
			return rnd == 1 ? Sides.Buy : Sides.Sell;
	}
}
