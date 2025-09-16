using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Percentage Crossover indicator.
/// </summary>
public class PercentageCrossoverStrategy : Strategy
{
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<int> _colorHistory = new();

	private decimal? _previousMiddle;
	private int? _lastColor;

	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	public decimal Percent
	{
		get => _percent.Value;
		set => _percent.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PercentageCrossoverStrategy()
	{
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Enable Buy Entries", "Allow opening long positions", "General");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Enable Sell Entries", "Allow opening short positions", "General");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Enable Buy Exits", "Allow closing long positions", "General");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Enable Sell Exits", "Allow closing short positions", "General");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Restrict trading to specific hours", "Time Filter");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading window start hour", "Time Filter");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetDisplay("Start Minute", "Trading window start minute", "Time Filter");

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Trading window end hour", "Time Filter");

		_endMinute = Param(nameof(EndMinute), 59)
			.SetDisplay("End Minute", "Trading window end minute", "Time Filter");

		_percent = Param(nameof(Percent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Percent", "Percentage offset for the indicator", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Closed bars to look back for the signal", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for signal candles", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_colorHistory.Clear();
		_previousMiddle = null;
		_lastColor = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var percentFactor = Percent / 100m;

		if (_previousMiddle is null)
		{
			_previousMiddle = close;
			_lastColor = 0;
			_colorHistory.Clear();
			_colorHistory.Add(0);
			return;
		}

		var previousMiddle = _previousMiddle.Value;
		var lowerBoundary = close * (1 - percentFactor);
		var upperBoundary = close * (1 + percentFactor);

		var middle = previousMiddle;

		if (lowerBoundary > previousMiddle)
			middle = lowerBoundary;
		else if (upperBoundary < previousMiddle)
			middle = upperBoundary;

		var color = _lastColor ?? 0;

		if (middle > previousMiddle)
			color = 0;
		else if (middle < previousMiddle)
			color = 1;

		_previousMiddle = middle;
		_lastColor = color;

		_colorHistory.Add(color);
		var maxSize = Math.Max(SignalBar + 2, 4);
		if (_colorHistory.Count > maxSize)
			_colorHistory.RemoveRange(0, _colorHistory.Count - maxSize);

		var currentIndex = _colorHistory.Count - SignalBar;
		if (currentIndex <= 0)
			return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var currentColor = _colorHistory[currentIndex];
		var previousColor = _colorHistory[previousIndex];

		var buyOpen = BuyPosOpen && currentColor == 0 && previousColor == 1;
		var sellOpen = SellPosOpen && currentColor == 1 && previousColor == 0;
		var buyClose = BuyPosClose && currentColor == 1;
		var sellClose = SellPosClose && currentColor == 0;

		var inTradingWindow = !UseTimeFilter || IsTradingTime(candle.CloseTime);

		if (UseTimeFilter && !inTradingWindow)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();

			return;
		}

		if (buyClose && Position > 0)
			SellMarket();

		if (sellClose && Position < 0)
			BuyMarket();

		if (!inTradingWindow)
			return;

		if (buyOpen && Position <= 0)
			BuyMarket();
		else if (sellOpen && Position >= 0)
			SellMarket();
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		var hour = time.Hour;
		var minute = time.Minute;

		if (StartHour < EndHour)
		{
			if (hour == StartHour && minute >= StartMinute)
				return true;

			if (hour > StartHour && hour < EndHour)
				return true;

			if (hour > StartHour && hour == EndHour && minute < EndMinute)
				return true;

			return false;
		}

		if (StartHour == EndHour)
		{
			return hour == StartHour && minute >= StartMinute && minute < EndMinute;
		}

		if (hour >= StartHour && minute >= StartMinute)
			return true;

		if (hour < EndHour)
			return true;

		if (hour == EndHour && minute < EndMinute)
			return true;

		return false;
	}
}
