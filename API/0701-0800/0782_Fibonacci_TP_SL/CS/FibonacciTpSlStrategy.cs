using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci TP SL strategy.
/// Retracement levels are measured down from the highest high of the last Lookback candles towards the lowest low. While flat it
/// goes long when the close lies between the 78.6% and 38.2% levels and short when it lies between the 61.8% and 23.6% levels,
/// provided at least MinBarsBetweenTrades candles passed since the last entry. A position closes at an ATR-based stop set at entry
/// or at a percent take profit. No new trades open in a week once the week's return reaches MaxWeeklyReturn.
/// </summary>
public class FibonacciTpSlStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _minBarsBetweenTrades;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _maxWeeklyReturn;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;
	private int? _lastEntryBar;
	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTime _currentWeek;
	private decimal _weekStartPnL;
	private decimal _weekStartValue;

	/// <summary>
	/// Take profit in percent of the entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Minimum candles between entries.
	/// </summary>
	public int MinBarsBetweenTrades
	{
		get => _minBarsBetweenTrades.Value;
		set => _minBarsBetweenTrades.Value = value;
	}

	/// <summary>
	/// Candles the Fibonacci range spans.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Weekly return (fraction of the account value) after which no new trades open.
	/// </summary>
	public decimal MaxWeeklyReturn
	{
		get => _maxWeeklyReturn.Value;
		set => _maxWeeklyReturn.Value = value;
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
	public FibonacciTpSlStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit in percent of the entry price", "Risk");

		_minBarsBetweenTrades = Param(nameof(MinBarsBetweenTrades), 10)
			.SetNotNegative()
			.SetDisplay("Min Bars Between Trades", "Minimum candles between entries", "General");

		_lookback = Param(nameof(Lookback), 100)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Candles the Fibonacci range spans", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the stop distance", "Risk");

		_maxWeeklyReturn = Param(nameof(MaxWeeklyReturn), 0.15m)
			.SetNotNegative()
			.SetDisplay("Max Weekly Return", "Weekly return after which no new trades open", "Risk");

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
		_barIndex = 0;
		_lastEntryBar = null;
		_stopPrice = 0m;
		_takePrice = 0m;
		_currentWeek = default;
		_weekStartPnL = 0m;
		_weekStartValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var highest = new Highest { Length = Lookback };
		var lowest = new Lowest { Length = Lookback };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bar = _barIndex++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var weekCapReached = UpdateWeek(candle.OpenTime);

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || (_takePrice > 0 && candle.HighPrice >= _takePrice))
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || (_takePrice > 0 && candle.LowPrice <= _takePrice))
				BuyMarket(-Position);

			return;
		}

		if (weekCapReached)
			return;

		if (_lastEntryBar is int lastBar && bar - lastBar < MinBarsBetweenTrades)
			return;

		var range = high - low;
		if (range <= 0)
			return;

		var fib236 = high - range * 0.236m;
		var fib382 = high - range * 0.382m;
		var fib618 = high - range * 0.618m;
		var fib786 = high - range * 0.786m;

		var close = candle.ClosePrice;
		var stopDistance = atr * AtrMultiplier;

		if (close <= fib382 && close >= fib786)
		{
			_stopPrice = close - stopDistance;
			_takePrice = TakeProfitPercent > 0 ? close * (1 + TakeProfitPercent / 100m) : 0m;
			_lastEntryBar = bar;
			BuyMarket();
		}
		else if (close <= fib236 && close >= fib618)
		{
			_stopPrice = close + stopDistance;
			_takePrice = TakeProfitPercent > 0 ? close * (1 - TakeProfitPercent / 100m) : 0m;
			_lastEntryBar = bar;
			SellMarket();
		}
	}

	private bool UpdateWeek(DateTime time)
	{
		var date = time.Date;
		var week = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

		if (_currentWeek != week)
		{
			_currentWeek = week;
			_weekStartPnL = PnL;
			_weekStartValue = Portfolio?.CurrentValue ?? 0m;
		}

		// Without a known account value there is nothing to measure the return against.
		return _weekStartValue > 0 && MaxWeeklyReturn > 0 && (PnL - _weekStartPnL) / _weekStartValue >= MaxWeeklyReturn;
	}
}
