using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy that opens a position during a short time window.
/// Uses the previous daily candle direction to decide whether to buy or sell.
/// Applies configurable take-profit and stop-loss levels expressed in price steps.
/// </summary>
public class SheKanskigorDailyStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<int> _tradeWindowMinutes;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<DataType> _intradayCandleType;

	private readonly DataType _dailyCandleType;

	private DateTime _currentDate;
	private bool _tradePlaced;
	private bool _dailyReady;
	private decimal _previousOpen;
	private decimal _previousClose;
	private decimal _entryPrice;

	/// <summary>
	/// Start time of the trading window.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Width of the trading window in minutes.
	/// </summary>
	public int TradeWindowMinutes
	{
		get => _tradeWindowMinutes.Value;
		set => _tradeWindowMinutes.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in security price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in security price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Intraday candle type used to evaluate the trading window.
	/// </summary>
	public DataType IntradayCandleType
	{
		get => _intradayCandleType.Value;
		set => _intradayCandleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="SheKanskigorDailyStrategy"/>.
	/// </summary>
	public SheKanskigorDailyStrategy()
	{
		Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 35m)
			.SetDisplay("Take Profit", "Profit target in steps", "Risk")
			.SetCanOptimize(true);

		_stopLossSteps = Param(nameof(StopLossSteps), 55m)
			.SetDisplay("Stop Loss", "Loss limit in steps", "Risk")
			.SetCanOptimize(true);

		_startTime = Param(nameof(StartTime), new TimeSpan(0, 5, 0))
			.SetDisplay("Start Time", "Time of day to evaluate entries", "Schedule");

		_tradeWindowMinutes = Param(nameof(TradeWindowMinutes), 5)
			.SetDisplay("Window (min)", "Trading window duration in minutes", "Schedule")
			.SetCanOptimize(true);

		_intradayCandleType = Param(nameof(IntradayCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Intraday Candle", "Candle type for intraday checks", "Data");

		_dailyCandleType = TimeSpan.FromDays(1).TimeFrame();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, IntradayCandleType), (Security, _dailyCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentDate = default;
		_tradePlaced = false;
		_dailyReady = false;
		_previousOpen = 0m;
		_previousClose = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var intraday = SubscribeCandles(IntradayCandleType);
		intraday.Bind(ProcessIntraday).Start();

		var daily = SubscribeCandles(_dailyCandleType);
		daily.Bind(ProcessDaily).Start();

		StartProtection();
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the direction of the last completed daily candle.
		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
		_dailyReady = true;
	}

	private void ProcessIntraday(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openTime = candle.OpenTime;

		if (openTime.Date != _currentDate)
		{
			_currentDate = openTime.Date;
			_tradePlaced = false;
		}

		ManagePosition(candle.ClosePrice);

		var start = StartTime;
		var end = start.Add(TimeSpan.FromMinutes(TradeWindowMinutes));
		var currentTod = openTime.TimeOfDay;

		if (currentTod < start || currentTod > end)
			return;

		if (_tradePlaced)
			return;

		if (!_dailyReady)
			return;

		if (Position != 0)
		{
			_tradePlaced = true;
			return;
		}

		if (_previousOpen > _previousClose)
		{
			BuyMarket(Volume);
			_tradePlaced = true;
		}
		else if (_previousOpen < _previousClose)
		{
			SellMarket(Volume);
			_tradePlaced = true;
		}
		else
		{
			// Skip trading when the previous day closed unchanged.
			_tradePlaced = true;
		}
	}

	private void ManagePosition(decimal closePrice)
	{
		if (Position == 0)
			return;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m || _entryPrice == 0m)
			return;

		if (Position > 0)
		{
			var target = _entryPrice + TakeProfitSteps * step;
			var stop = _entryPrice - StopLossSteps * step;

			if (TakeProfitSteps > 0m && closePrice >= target)
			{
				SellMarket(Position);
				return;
			}

			if (StopLossSteps > 0m && closePrice <= stop)
			{
				SellMarket(Position);
			}
		}
		else
		{
			var target = _entryPrice - TakeProfitSteps * step;
			var stop = _entryPrice + StopLossSteps * step;

			if (TakeProfitSteps > 0m && closePrice <= target)
			{
				BuyMarket(-Position);
				return;
			}

			if (StopLossSteps > 0m && closePrice >= stop)
			{
				BuyMarket(-Position);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		// Track the latest fill price to evaluate protective exits.
		_entryPrice = trade.Trade.Price;
	}
}

