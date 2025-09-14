using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Random entry strategy with trailing stop management.
/// </summary>
public class RandomTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minStopLevel;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<int> _sleepMinutes;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _volume;

	private readonly Random _random = new();
	private DateTimeOffset _nextTradeTime;
	private decimal? _stopPrice;

	/// <summary>
	/// Minimal distance for stop orders.
	/// </summary>
	public decimal MinStopLevel
	{
		get => _minStopLevel.Value;
		set => _minStopLevel.Value = value;
	}

	/// <summary>
	/// Step for trailing stop update.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Pause before the next trade in minutes.
	/// </summary>
	public int SleepMinutes
	{
		get => _sleepMinutes.Value;
		set => _sleepMinutes.Value = value;
	}

	/// <summary>
	/// SMA period used for biasing random side.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="RandomTrailingStopStrategy"/>.
	/// </summary>
	public RandomTrailingStopStrategy()
	{
		_minStopLevel = Param(nameof(MinStopLevel), 0.00036m)
			.SetGreaterThanZero()
			.SetDisplay("Min Stop Level", "Minimal stop distance", "Trading");

		_trailingStep = Param(nameof(TrailingStep), 0.00001m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Trailing stop adjustment step", "Trading");

		_sleepMinutes = Param(nameof(SleepMinutes), 5)
			.SetGreaterThanZero()
			.SetDisplay("Sleep Minutes", "Pause before next trade in minutes", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Simple moving average period", "Indicators");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.TimeFrame(TimeSpan.FromMinutes(1)))];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var subscription = SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(1)));

		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, sma, "SMA");
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			if (candle.CloseTime < _nextTradeTime)
				return;

			var side = GetRandomSide(candle.ClosePrice, smaValue);
			_stopPrice = null;

			if (side == Sides.Buy)
				BuyMarket(Volume);
			else
				SellMarket(Volume);

			_nextTradeTime = candle.CloseTime + TimeSpan.FromMinutes(SleepMinutes);

			return;
		}

		if (_stopPrice == null)
		{
			if (Position > 0)
				_stopPrice = candle.ClosePrice - MinStopLevel;
			else
				_stopPrice = candle.ClosePrice + MinStopLevel;

			return;
		}

		if (Position > 0)
		{
			if (candle.ClosePrice - _stopPrice >= TrailingStep)
				_stopPrice = candle.ClosePrice - MinStopLevel;

			if (candle.LowPrice <= _stopPrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (_stopPrice - candle.ClosePrice >= TrailingStep)
				_stopPrice = candle.ClosePrice + MinStopLevel;

			if (candle.HighPrice >= _stopPrice)
				BuyMarket(-Position);
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

