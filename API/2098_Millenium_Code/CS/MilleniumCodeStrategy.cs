using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Millenium Code positional strategy.
/// </summary>
public class MilleniumCodeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _highLowBars;
	private readonly StrategyParam<bool> _reverseSignal;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _openMinute;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _closeMinute;
	private readonly StrategyParam<int> _tradeDuration;
	private readonly StrategyParam<bool> _sunday;
	private readonly StrategyParam<bool> _monday;
	private readonly StrategyParam<bool> _tuesday;
	private readonly StrategyParam<bool> _wednesday;
	private readonly StrategyParam<bool> _thursday;
	private readonly StrategyParam<bool> _friday;

	private SMA _fast = null!;
	private SMA _slow = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _openedToday;
	private DateTime _currentDay;
	private decimal _entryPrice;
	private decimal _stopDistance;
	private decimal _takeDistance;
	private DateTimeOffset _entryTime;

	public MilleniumCodeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_fastLength = Param(nameof(FastLength), 15)
			.SetDisplay("Fast MA", "Fast moving average length", "Indicators");
		_slowLength = Param(nameof(SlowLength), 14)
			.SetDisplay("Slow MA", "Slow moving average length", "Indicators");
		_highLowBars = Param(nameof(HighLowBars), 10)
			.SetDisplay("HighLow Bars", "Bars count for high/low search", "Indicators");
		_reverseSignal = Param(nameof(ReverseSignal), true)
			.SetDisplay("Reverse", "Reverse buy/sell logic", "General");
		_stopLoss = Param(nameof(StopLoss), 1100m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 400m)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk");
		_openHour = Param(nameof(OpenHour), 16)
			.SetDisplay("Open Hour", "Hour to start trading (-1 disables)", "Timing");
		_openMinute = Param(nameof(OpenMinute), 5)
			.SetDisplay("Open Minute", "Minute to start trading", "Timing");
		_closeHour = Param(nameof(CloseHour), 17)
			.SetDisplay("Close Hour", "Hour to stop trading (-1 disables)", "Timing");
		_closeMinute = Param(nameof(CloseMinute), 55)
			.SetDisplay("Close Minute", "Minute to stop trading", "Timing");
		_tradeDuration = Param(nameof(TradeDuration), 0)
			.SetDisplay("Duration", "Trade duration in hours", "Timing");
		_sunday = Param(nameof(Sunday), true).SetDisplay("Sunday", "Allow trading on Sunday", "Days");
		_monday = Param(nameof(Monday), true).SetDisplay("Monday", "Allow trading on Monday", "Days");
		_tuesday = Param(nameof(Tuesday), true).SetDisplay("Tuesday", "Allow trading on Tuesday", "Days");
		_wednesday = Param(nameof(Wednesday), true).SetDisplay("Wednesday", "Allow trading on Wednesday", "Days");
		_thursday = Param(nameof(Thursday), true).SetDisplay("Thursday", "Allow trading on Thursday", "Days");
		_friday = Param(nameof(Friday), true).SetDisplay("Friday", "Allow trading on Friday", "Days");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int HighLowBars { get => _highLowBars.Value; set => _highLowBars.Value = value; }
	public bool ReverseSignal { get => _reverseSignal.Value; set => _reverseSignal.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int OpenHour { get => _openHour.Value; set => _openHour.Value = value; }
	public int OpenMinute { get => _openMinute.Value; set => _openMinute.Value = value; }
	public int CloseHour { get => _closeHour.Value; set => _closeHour.Value = value; }
	public int CloseMinute { get => _closeMinute.Value; set => _closeMinute.Value = value; }
	public int TradeDuration { get => _tradeDuration.Value; set => _tradeDuration.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_openedToday = false;
		_currentDay = default;
		_entryPrice = 0m;
		_stopDistance = 0m;
		_takeDistance = 0m;
		_entryTime = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fast = new SMA { Length = FastLength };
		_slow = new SMA { Length = SlowLength };
		_highest = new Highest { Length = HighLowBars };
		_lowest = new Lowest { Length = HighLowBars };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fast, _slow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fast);
			DrawIndicator(area, _slow);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var high = _highest.Process(candle.HighPrice).ToDecimal();
		var low = _lowest.Process(candle.LowPrice).ToDecimal();

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_openedToday = false;
		}

		if (Position != 0)
		{
			CheckExit(candle);
		}

		if (_openedToday || !IsFormedAndOnlineAndAllowTrading() || !IsAllowedDay(candle.OpenTime) || !IsOpenTime(candle.OpenTime))
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		bool crossUp = _prevFast < _prevSlow && fast > slow && candle.ClosePrice > fast && candle.ClosePrice > slow && low < fast && low < slow;
		bool crossDown = _prevFast > _prevSlow && fast < slow && candle.ClosePrice < fast && candle.ClosePrice < slow && high > fast && high > slow;

		int dir = 0;
		if (crossUp)
			dir = ReverseSignal ? -1 : 1;
		else if (crossDown)
			dir = ReverseSignal ? 1 : -1;

		if (dir == 1 && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.CloseTime;
			_openedToday = true;
			CalcDistances();
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Opened long at {candle.ClosePrice}");
		}
		else if (dir == -1 && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.CloseTime;
			_openedToday = true;
			CalcDistances();
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Opened short at {candle.ClosePrice}");
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private void CalcDistances()
	{
		var point = Security?.PriceStep ?? 1m;
		_stopDistance = StopLoss * point;
		_takeDistance = TakeProfit * point;
	}

	private void CheckExit(ICandleMessage candle)
	{
		if (TradeDuration > 0 && candle.CloseTime >= _entryTime + TimeSpan.FromHours(TradeDuration))
		{
			ClosePosition();
			return;
		}

		if (IsCloseTime(candle.CloseTime))
		{
			ClosePosition();
			return;
		}

		if (Position > 0)
		{
			if (_stopDistance > 0 && candle.LowPrice <= _entryPrice - _stopDistance)
				ClosePosition();
			else if (_takeDistance > 0 && candle.HighPrice >= _entryPrice + _takeDistance)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (_stopDistance > 0 && candle.HighPrice >= _entryPrice + _stopDistance)
				ClosePosition();
			else if (_takeDistance > 0 && candle.LowPrice <= _entryPrice - _takeDistance)
				ClosePosition();
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	private bool IsAllowedDay(DateTimeOffset time)
	{
		return time.DayOfWeek switch
		{
			DayOfWeek.Sunday => _sunday.Value,
			DayOfWeek.Monday => _monday.Value,
			DayOfWeek.Tuesday => _tuesday.Value,
			DayOfWeek.Wednesday => _wednesday.Value,
			DayOfWeek.Thursday => _thursday.Value,
			DayOfWeek.Friday => _friday.Value,
			_ => true
		};
	}

	private bool IsOpenTime(DateTimeOffset time)
	{
		if (OpenHour < 0)
			return true;
		var open = time.Date.AddHours(OpenHour).AddMinutes(OpenMinute);
		return time >= open;
	}

	private bool IsCloseTime(DateTimeOffset time)
	{
		if (CloseHour < 0)
			return false;
		var close = time.Date.AddHours(CloseHour).AddMinutes(CloseMinute);
		return time >= close;
	}
}
