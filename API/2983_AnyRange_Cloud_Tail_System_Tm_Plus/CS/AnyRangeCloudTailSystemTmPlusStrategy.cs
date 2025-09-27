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
/// Breakout strategy based on the AnyRange Cloud Tail indicator ported from MQL5.
/// </summary>
public class AnyRangeCloudTailSystemTmPlusStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<int> _exitAfterMinutes;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _rangeLookbackDays;
	private readonly StrategyParam<TimeSpan> _rangeStart;
	private readonly StrategyParam<TimeSpan> _rangeEnd;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleInfo> _history = new();
	private readonly List<int> _colorHistory = new();

	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;

	/// <summary>
	/// Initializes <see cref="AnyRangeCloudTailSystemTmPlusStrategy"/>.
	/// </summary>
	public AnyRangeCloudTailSystemTmPlusStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Base order size for new entries", "Trading");

		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
		.SetDisplay("Allow Buy Entries", "Enable breakout entries on the upper boundary", "Trading");

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
		.SetDisplay("Allow Sell Entries", "Enable breakout entries on the lower boundary", "Trading");

		_allowBuyExit = Param(nameof(AllowBuyExit), true)
		.SetDisplay("Allow Buy Exit", "Allow long positions to be closed by bearish breakouts", "Trading");

		_allowSellExit = Param(nameof(AllowSellExit), true)
		.SetDisplay("Allow Sell Exit", "Allow short positions to be closed by bullish breakouts", "Trading");

		_useTimeExit = Param(nameof(UseTimeExit), true)
		.SetDisplay("Use Time Exit", "Close positions after a fixed holding time", "Risk");

		_exitAfterMinutes = Param(nameof(ExitAfterMinutes), 1500)
		.SetDisplay("Exit After (min)", "Holding time in minutes before a time-based exit", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss (points)", "Protective stop in price steps", "Protection");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit (points)", "Profit target in price steps", "Protection");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar Offset", "Number of bars back used for breakout detection", "Indicator");

		_rangeLookbackDays = Param(nameof(RangeLookbackDays), 1)
		.SetDisplay("Range Lookback Days", "Maximum number of past sessions to search for a completed range", "Indicator");

		_rangeStart = Param(nameof(RangeStartTime), TimeSpan.FromHours(2))
		.SetDisplay("Range Start", "Start time of the intraday reference range", "Indicator");

		_rangeEnd = Param(nameof(RangeEndTime), TimeSpan.FromHours(7))
		.SetDisplay("Range End", "End time of the intraday reference range", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations", "General");
	}

	/// <summary>
	/// Order volume for new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enable long entries on bullish breakouts.
	/// </summary>
	public bool AllowBuyEntry
	{
		get => _allowBuyEntry.Value;
		set => _allowBuyEntry.Value = value;
	}

	/// <summary>
	/// Enable short entries on bearish breakouts.
	/// </summary>
	public bool AllowSellEntry
	{
		get => _allowSellEntry.Value;
		set => _allowSellEntry.Value = value;
	}

	/// <summary>
	/// Allow long positions to be closed by bearish breakouts.
	/// </summary>
	public bool AllowBuyExit
	{
		get => _allowBuyExit.Value;
		set => _allowBuyExit.Value = value;
	}

	/// <summary>
	/// Allow short positions to be closed by bullish breakouts.
	/// </summary>
	public bool AllowSellExit
	{
		get => _allowSellExit.Value;
		set => _allowSellExit.Value = value;
	}

	/// <summary>
	/// Enable time-based position exit.
	/// </summary>
	public bool UseTimeExit
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	/// <summary>
	/// Holding time before a forced exit.
	/// </summary>
	public int ExitAfterMinutes
	{
		get => _exitAfterMinutes.Value;
		set => _exitAfterMinutes.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of bars back used for breakout validation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Maximum number of past sessions scanned for a completed range.
	/// </summary>
	public int RangeLookbackDays
	{
		get => _rangeLookbackDays.Value;
		set => _rangeLookbackDays.Value = value;
	}

	/// <summary>
	/// Start time of the reference range.
	/// </summary>
	public TimeSpan RangeStartTime
	{
		get => _rangeStart.Value;
		set => _rangeStart.Value = value;
	}

	/// <summary>
	/// End time of the reference range.
	/// </summary>
	public TimeSpan RangeEndTime
	{
		get => _rangeEnd.Value;
		set => _rangeEnd.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_history.Clear();
		_colorHistory.Clear();
		_longEntryTime = null;
		_shortEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		var step = Security?.PriceStep ?? 1m;
		Unit takeProfit = null;
		if (TakeProfitPoints > 0)
			takeProfit = new Unit(TakeProfitPoints * step, UnitTypes.Price);

		Unit stopLoss = null;
		if (StopLossPoints > 0)
			stopLoss = new Unit(StopLossPoints * step, UnitTypes.Price);

		StartProtection(takeProfit, stopLoss, useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store incoming candle data for range calculations.
		_history.Add(new CandleInfo(candle.OpenTime, candle.HighPrice, candle.LowPrice));

		var hasRange = TryGetRange(candle.OpenTime, out var rangeHigh, out var rangeLow);

		// Determine the current color code produced by the original indicator logic.
		var color = hasRange ? GetColor(candle, rangeHigh, rangeLow) : 4;
		_colorHistory.Add(color);

		// Keep only recent history needed for calculations.
		TrimHistory(candle.OpenTime);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ApplyTimeExit(candle.CloseTime);

		if (!hasRange)
			return;

		if (_colorHistory.Count < SignalBar + 2)
			return;

		var signalIndex = _colorHistory.Count - 2 - SignalBar;
		if (signalIndex < 0)
			return;

		var signalColor = _colorHistory[signalIndex];
		var nextColor = _colorHistory[signalIndex + 1];

		var isSignalUp = signalColor == 2 || signalColor == 3;
		var isSignalDown = signalColor == 0 || signalColor == 1;
		var nextIsUp = nextColor == 2 || nextColor == 3;
		var nextIsDown = nextColor == 0 || nextColor == 1;

		// Exit rules mirror the MQL implementation.
		if (AllowBuyExit && isSignalDown && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_longEntryTime = null;
		}

		if (AllowSellExit && isSignalUp && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_shortEntryTime = null;
		}

		// Entry logic fires when a breakout appears on the signal bar
		// and the more recent bar does not repeat the same color.
		if (AllowBuyEntry && isSignalUp && !nextIsUp && Position <= 0)
		{
			var volume = Volume;
			if (Position < 0)
				volume += Math.Abs(Position);

			BuyMarket(volume);
			_longEntryTime = candle.CloseTime;
			_shortEntryTime = null;
		}

		if (AllowSellEntry && isSignalDown && !nextIsDown && Position >= 0)
		{
			var volume = Volume;
			if (Position > 0)
				volume += Math.Abs(Position);

			SellMarket(volume);
			_shortEntryTime = candle.CloseTime;
			_longEntryTime = null;
		}
	}

	private void ApplyTimeExit(DateTimeOffset currentTime)
	{
		if (!UseTimeExit || ExitAfterMinutes <= 0 || Position == 0)
			return;

		var lifetime = TimeSpan.FromMinutes(ExitAfterMinutes);

		if (Position > 0 && _longEntryTime != null && currentTime - _longEntryTime >= lifetime)
		{
			SellMarket(Math.Abs(Position));
			_longEntryTime = null;
		}
		else if (Position < 0 && _shortEntryTime != null && currentTime - _shortEntryTime >= lifetime)
		{
			BuyMarket(Math.Abs(Position));
			_shortEntryTime = null;
		}
	}

	private static int GetColor(ICandleMessage candle, decimal rangeHigh, decimal rangeLow)
	{
		if (candle.ClosePrice > rangeHigh)
			return candle.ClosePrice >= candle.OpenPrice ? 3 : 2;

		if (candle.ClosePrice < rangeLow)
			return candle.ClosePrice <= candle.OpenPrice ? 0 : 1;

		return 4;
	}

	private (DateTimeOffset Start, DateTimeOffset End) GetRangeWindow(DateTime date, TimeSpan offset)
	{
		var start = new DateTimeOffset(date.Add(RangeStartTime), offset);
		var endDate = date.Add(RangeEndTime);

		if (RangeStartTime <= RangeEndTime)
			return (start, new DateTimeOffset(endDate, offset));

		return (start, new DateTimeOffset(endDate, offset).AddDays(1));
	}

	private bool TryGetRange(DateTimeOffset candleTime, out decimal rangeHigh, out decimal rangeLow)
	{
		var offset = candleTime.Offset;
		var date = candleTime.Date;
		var daysToCheck = Math.Max(0, RangeLookbackDays);

		for (var dayOffset = 0; dayOffset <= daysToCheck; dayOffset++)
		{
			var targetDate = date.AddDays(-dayOffset);
			var window = GetRangeWindow(targetDate, offset);

			if (window.End > candleTime)
				continue;

			var hasData = false;
			decimal high = default;
			decimal low = default;

			foreach (var item in _history)
			{
				if (item.OpenTime < window.Start || item.OpenTime >= window.End)
					continue;

				if (!hasData)
				{
					high = item.High;
					low = item.Low;
					hasData = true;
				}
				else
				{
					if (item.High > high)
						high = item.High;

					if (item.Low < low)
						low = item.Low;
				}
			}

			if (hasData)
			{
				rangeHigh = high;
				rangeLow = low;
				return true;
			}
		}

		rangeHigh = 0m;
		rangeLow = 0m;
		return false;
	}

	private void TrimHistory(DateTimeOffset currentTime)
	{
		var minDate = currentTime.Date.AddDays(-RangeLookbackDays - 3);
		var threshold = new DateTimeOffset(minDate, currentTime.Offset);

		while (_history.Count > SignalBar + 5 && _history.Count > 0 && _history[0].OpenTime < threshold)
		{
			_history.RemoveAt(0);
			_colorHistory.RemoveAt(0);
		}
	}

	private readonly struct CandleInfo
	{
		public CandleInfo(DateTimeOffset openTime, decimal high, decimal low)
		{
			OpenTime = openTime;
			High = high;
			Low = low;
		}

		public DateTimeOffset OpenTime { get; }
		public decimal High { get; }
		public decimal Low { get; }
	}
}