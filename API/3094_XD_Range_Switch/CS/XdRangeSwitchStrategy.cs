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
/// XD-RangeSwitch strategy converted from the MetaTrader 5 expert advisor.
/// The logic monitors channel breakouts identified by the XD-RangeSwitch indicator
/// and optionally flips the trading direction based on the <see cref="XdRangeSwitchTradeDirection"/> parameter.
/// </summary>
public class XdRangeSwitchStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _peaks;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<XdRangeSwitchTradeDirection> _tradeDirection;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();
	private readonly List<XdRangeSwitchValue> _indicatorHistory = new();

	private decimal? _previousUpperBand;
	private decimal? _previousLowerBand;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="XdRangeSwitchStrategy"/> class.
	/// </summary>
	public XdRangeSwitchStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for the XD-RangeSwitch calculations", "General");

		_peaks = Param(nameof(Peaks), 4)
		.SetGreaterThanZero()
		.SetDisplay("Peaks", "Number of extremes tracked by the indicator", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "How many completed bars back to read the indicator buffers", "Indicator");

		_tradeDirection = Param(nameof(TradeDirection), XdRangeSwitchTradeDirection.AgainstSignal)
		.SetDisplay("Trade Direction", "Trade with or against the XD-RangeSwitch signals", "Trading");

		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
		.SetDisplay("Allow Buy Entry", "Enable opening of long positions", "Trading");

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
		.SetDisplay("Allow Sell Entry", "Enable opening of short positions", "Trading");

		_allowBuyExit = Param(nameof(AllowBuyExit), true)
		.SetDisplay("Allow Buy Exit", "Enable closing of existing long positions", "Trading");

		_allowSellExit = Param(nameof(AllowSellExit), true)
		.SetDisplay("Allow Sell Exit", "Enable closing of existing short positions", "Trading");

		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop Loss", "Enable protective stop loss handling", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetNotNegative()
		.SetDisplay("Stop Loss Points", "Distance in price units for stop loss management", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take Profit", "Enable protective take profit handling", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetNotNegative()
		.SetDisplay("Take Profit Points", "Distance in price units for take profit management", "Risk");
	}

	/// <summary>
	/// Working timeframe used by the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of peaks (N parameter) replicated from the MT5 script.
	/// </summary>
	public int Peaks
	{
		get => _peaks.Value;
		set => _peaks.Value = value;
	}

	/// <summary>
	/// Bar shift applied when reading the XD-RangeSwitch buffers.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Determines whether trades follow or fade the indicator signals.
	/// </summary>
	public XdRangeSwitchTradeDirection TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Enable opening of long positions.
	/// </summary>
	public bool AllowBuyEntry
	{
		get => _allowBuyEntry.Value;
		set => _allowBuyEntry.Value = value;
	}

	/// <summary>
	/// Enable opening of short positions.
	/// </summary>
	public bool AllowSellEntry
	{
		get => _allowSellEntry.Value;
		set => _allowSellEntry.Value = value;
	}

	/// <summary>
	/// Allow closing of existing long positions.
	/// </summary>
	public bool AllowBuyExit
	{
		get => _allowBuyExit.Value;
		set => _allowBuyExit.Value = value;
	}

	/// <summary>
	/// Allow closing of existing short positions.
	/// </summary>
	public bool AllowSellExit
	{
		get => _allowSellExit.Value;
		set => _allowSellExit.Value = value;
	}

	/// <summary>
	/// Toggle stop-loss management.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Distance between the entry price and the protective stop.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Toggle take-profit management.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Distance between the entry price and the take-profit target.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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

		_highHistory.Clear();
		_lowHistory.Clear();
		_indicatorHistory.Clear();
		_previousUpperBand = null;
		_previousLowerBand = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ManageRisk(candle);

		// Apply protective stops before indicator-driven logic.
		var indicatorValue = CalculateIndicator(candle);
		_indicatorHistory.Add(indicatorValue);

		var maxHistory = Math.Max(GetRequiredHistoryLength(), 10);
		if (_indicatorHistory.Count > maxHistory)
		_indicatorHistory.RemoveRange(0, _indicatorHistory.Count - maxHistory);

		var index = _indicatorHistory.Count - 1 - SignalBar;
		if (index < 0)
		return;

		var reference = _indicatorHistory[index];

		decimal? upTrend;
		decimal? upSignal;
		decimal? downTrend;
		decimal? downSignal;

		if (TradeDirection == XdRangeSwitchTradeDirection.WithSignal)
		{
		upTrend = reference.LowerBand;
		upSignal = reference.DownSignal;
		downTrend = reference.UpperBand;
		downSignal = reference.UpSignal;
		}
		else
		{
		upTrend = reference.UpperBand;
		upSignal = reference.UpSignal;
		downTrend = reference.LowerBand;
		downSignal = reference.DownSignal;
		}

		var shouldCloseLong = false;
		var shouldCloseShort = false;
		var shouldOpenLong = false;
		var shouldOpenShort = false;

		if (upSignal.HasValue)
		{
		if (AllowBuyEntry)
		shouldOpenLong = true;

		if (AllowSellExit)
		shouldCloseShort = true;
		}
		else if (upTrend.HasValue && AllowSellExit)
		{
		shouldCloseShort = true;
		}

		if (downSignal.HasValue)
		{
		if (AllowSellEntry)
		shouldOpenShort = true;

		if (AllowBuyExit)
		shouldCloseLong = true;
		}
		else if (downTrend.HasValue && AllowBuyExit)
		{
		shouldCloseLong = true;
		}

		// Execute exits before evaluating fresh entries to mirror the MT5 sequence.
		if (shouldCloseLong && Position > 0)
		{
		SellMarket(Position);
		_longEntryPrice = null;
		}

		if (shouldCloseShort && Position < 0)
		{
		BuyMarket(-Position);
		_shortEntryPrice = null;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Open new exposure only when trading conditions remain valid.

		if (shouldOpenLong && Position <= 0)
		{
		var volume = Volume + Math.Max(0m, -Position);
		if (volume > 0)
		{
		BuyMarket(volume);
		_longEntryPrice = candle.ClosePrice;
		_shortEntryPrice = null;
		}
		}

		if (shouldOpenShort && Position >= 0)
		{
		var volume = Volume + Math.Max(0m, Position);
		if (volume > 0)
		{
		SellMarket(volume);
		_shortEntryPrice = candle.ClosePrice;
		_longEntryPrice = null;
		}
		}
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (Position > 0 && _longEntryPrice is decimal longEntry)
		{
		var stopLossReached = UseStopLoss && StopLossPoints > 0m && candle.LowPrice <= longEntry - StopLossPoints;
		var takeProfitReached = UseTakeProfit && TakeProfitPoints > 0m && candle.HighPrice >= longEntry + TakeProfitPoints;

		if (stopLossReached || takeProfitReached)
		{
		SellMarket(Position);
		_longEntryPrice = null;
		}
		}
		else if (Position < 0 && _shortEntryPrice is decimal shortEntry)
		{
		var stopLossReached = UseStopLoss && StopLossPoints > 0m && candle.HighPrice >= shortEntry + StopLossPoints;
		var takeProfitReached = UseTakeProfit && TakeProfitPoints > 0m && candle.LowPrice <= shortEntry - TakeProfitPoints;

		if (stopLossReached || takeProfitReached)
		{
		BuyMarket(-Position);
		_shortEntryPrice = null;
		}
		}
		else if (Position == 0)
		{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		}
	}

	private XdRangeSwitchValue CalculateIndicator(ICandleMessage candle)
	{
		// Maintain rolling windows for highs and lows, mirroring the MT5 indicator buffers.
		_highHistory.Add(candle.HighPrice);
		_lowHistory.Add(candle.LowPrice);

		var maxLength = Peaks + 1;
		if (maxLength < 1)
		maxLength = 1;

		if (_highHistory.Count > maxLength)
		_highHistory.RemoveRange(0, _highHistory.Count - maxLength);

		if (_lowHistory.Count > maxLength)
		_lowHistory.RemoveRange(0, _lowHistory.Count - maxLength);

		decimal? upperBand = null;
		decimal? lowerBand = null;
		decimal? upSignal = null;
		decimal? downSignal = null;

		if (_highHistory.Count > Peaks && Peaks > 0)
		{
		var previousCount = Math.Min(Peaks, _highHistory.Count - 1);
		var highestPrevious = decimal.MinValue;
		var lowestPrevious = decimal.MaxValue;

		var startPrev = _highHistory.Count - 1 - previousCount;
		var endPrev = _highHistory.Count - 1;

		for (var i = startPrev; i < endPrev; i++)
		{
		var high = _highHistory[i];
		if (high > highestPrevious)
		highestPrevious = high;

		var low = _lowHistory[i];
		if (low < lowestPrevious)
		lowestPrevious = low;
		}

		var recentCount = Math.Min(Peaks, _highHistory.Count);
		var highestRecent = decimal.MinValue;
		var lowestRecent = decimal.MaxValue;
		var startRecent = _highHistory.Count - recentCount;

		for (var i = startRecent; i < _highHistory.Count; i++)
		{
		var high = _highHistory[i];
		if (high > highestRecent)
		highestRecent = high;

		var low = _lowHistory[i];
		if (low < lowestRecent)
		lowestRecent = low;
		}

		var prevUpper = _previousUpperBand;
		var prevLower = _previousLowerBand;
		var close = candle.ClosePrice;

		if (close > highestPrevious)
		{
		lowerBand = lowestRecent;
		upperBand = null;
		}
		else if (close < lowestPrevious)
		{
		upperBand = highestRecent;
		lowerBand = null;
		}
		else
		{
		upperBand = prevUpper;
		lowerBand = prevLower;
		}

		if (prevUpper is null && upperBand is not null)
		upSignal = upperBand;

		if (prevLower is null && lowerBand is not null)
		downSignal = lowerBand;

		_previousUpperBand = upperBand;
		_previousLowerBand = lowerBand;
		}
		else
		{
		_previousUpperBand = null;
		_previousLowerBand = null;
		}

		var candleTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		return new XdRangeSwitchValue(candleTime, candle.ClosePrice, upperBand, lowerBand, upSignal, downSignal);
	}

	private int GetRequiredHistoryLength()
	{
	var baseLength = Math.Max(Peaks + 1, SignalBar + 2);
	return baseLength + 5;
	}

	private sealed class XdRangeSwitchValue
	{
	public XdRangeSwitchValue(DateTimeOffset time, decimal closePrice, decimal? upperBand, decimal? lowerBand, decimal? upSignal, decimal? downSignal)
	{
		Time = time;
		ClosePrice = closePrice;
		UpperBand = upperBand;
		LowerBand = lowerBand;
		UpSignal = upSignal;
		DownSignal = downSignal;
	}

	public DateTimeOffset Time { get; }
	public decimal ClosePrice { get; }
	public decimal? UpperBand { get; }
	public decimal? LowerBand { get; }
	public decimal? UpSignal { get; }
	public decimal? DownSignal { get; }
	}
}

/// <summary>
/// Trade direction selection matching the MT5 expert advisor input.
/// </summary>
public enum XdRangeSwitchTradeDirection
{
	/// <summary>
	/// Counter-trend logic: buy on downward channel breaks and sell on upward breaks.
	/// </summary>
	AgainstSignal,

	/// <summary>
	/// Trend-following logic: align with the XD-RangeSwitch arrows.
	/// </summary>
	WithSignal
}

