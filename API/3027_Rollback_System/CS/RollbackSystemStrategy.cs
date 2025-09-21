using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily rollback strategy converted from the MetaTrader "Rollback system" expert advisor.
/// </summary>
public class RollbackSystemStrategy : Strategy
{
	private const int HistorySize = 25;

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _rollbackPips;
	private readonly StrategyParam<decimal> _channelOpenClosePips;
	private readonly StrategyParam<decimal> _channelRollbackPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _openHistory = new decimal[HistorySize];
	private readonly decimal[] _closeHistory = new decimal[HistorySize];
	private int _historyCount;
	private int _historyStart;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _pipValue;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _entryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="RollbackSystemStrategy"/> class.
	/// </summary>
	public RollbackSystemStrategy()
	{
	_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Volume", "Trade volume in lots", "Trading")
		.SetGreaterThanZero();

	_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
		.SetNotNegative();

	_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk")
		.SetNotNegative();

	_rollbackPips = Param(nameof(RollbackPips), 20m)
		.SetDisplay("Rollback", "Minimum pullback size", "Signals")
		.SetNotNegative();

	_channelOpenClosePips = Param(nameof(ChannelOpenClosePips), 18m)
		.SetDisplay("Channel Open-Close", "Required day change", "Signals")
		.SetNotNegative();

	_channelRollbackPips = Param(nameof(ChannelRollbackPips), 3m)
		.SetDisplay("Channel Rollback", "Rollback tolerance", "Signals")
		.SetNotNegative();

	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe", "General");
	}

	/// <summary>
	/// Trading volume per position.
	/// </summary>
	public decimal TradeVolume
	{
	get => _tradeVolume.Value;
	set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
	get => _stopLossPips.Value;
	set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
	get => _takeProfitPips.Value;
	set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Base rollback distance measured in pips.
	/// </summary>
	public decimal RollbackPips
	{
	get => _rollbackPips.Value;
	set => _rollbackPips.Value = value;
	}

	/// <summary>
	/// Required change between the open 24 bars ago and the latest close.
	/// </summary>
	public decimal ChannelOpenClosePips
	{
	get => _channelOpenClosePips.Value;
	set => _channelOpenClosePips.Value = value;
	}

	/// <summary>
	/// Additional tolerance applied to the rollback condition.
	/// </summary>
	public decimal ChannelRollbackPips
	{
	get => _channelRollbackPips.Value;
	set => _channelRollbackPips.Value = value;
	}

	/// <summary>
	/// Working candle type (defaults to 1 hour).
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	Array.Clear(_openHistory, 0, _openHistory.Length);
	Array.Clear(_closeHistory, 0, _closeHistory.Length);
	_historyCount = 0;
	_historyStart = 0;
	_stopPrice = null;
	_takeProfitPrice = null;
	_entryPrice = 0m;
	_pipValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	Volume = TradeVolume;

	// Prepare high/low trackers for the previous 24 hourly bars.
	_highest = new Highest
	{
	Length = 24,
	CandlePrice = CandlePrice.High
	};

	_lowest = new Lowest
	{
	Length = 24,
	CandlePrice = CandlePrice.Low
	};

	_pipValue = CalculatePipValue();

	var subscription = SubscribeCandles(CandleType);
	subscription
		.Bind(_highest, _lowest, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
	// Only finished candles are processed to mimic the MQL new-bar logic.
	if (candle.State != CandleStates.Finished)
	return;

	AddToHistory(candle.OpenPrice, candle.ClosePrice);

	// Manage the active trade before searching for new signals.
	if (ManagePosition(candle))
	return;

	if (Position != 0)
	return;

	if (_historyCount < HistorySize || !_highest.IsFormed || !_lowest.IsFormed)
	return;

	if (!IsTradingWindow(candle.CloseTime))
	return;

	if (!TryGetHistoryValues(out var open24, out var lastClose))
	return;

	// Convert pip-based parameters to price offsets using the detected pip value.
	if (_pipValue <= 0m)
	_pipValue = CalculatePipValue();

	var channelOpenClose = ChannelOpenClosePips * _pipValue;
	var rollback = RollbackPips * _pipValue;
	var channelRollback = ChannelRollbackPips * _pipValue;
	var stopOffset = StopLossPips * _pipValue;
	var takeOffset = TakeProfitPips * _pipValue;

	var open24MinusClose1 = open24 - lastClose;
	var close1MinusOpen24 = lastClose - open24;
	var close1MinusLowest = lastClose - lowest;
	var highestMinusClose1 = highest - lastClose;

	// Long entry if the market fell strongly during the last day and closed near the extreme low.
	if (open24MinusClose1 > channelOpenClose && close1MinusLowest < (rollback - channelRollback))
	{
	TryEnterLong(lastClose, stopOffset, takeOffset);
	return;
	}

	// Long entry if the market rallied but closed far below the daily high, expecting a rollback.
	if (close1MinusOpen24 > channelOpenClose && highestMinusClose1 > (rollback + channelRollback))
	{
	TryEnterLong(lastClose, stopOffset, takeOffset);
	return;
	}

	// Short entry when the instrument rallied and the close is near the daily high.
	if (close1MinusOpen24 > channelOpenClose && highestMinusClose1 < (rollback - channelRollback))
	{
	TryEnterShort(lastClose, stopOffset, takeOffset);
	return;
	}

	// Short entry when the instrument declined but closed far above the daily low.
	if (open24MinusClose1 > channelOpenClose && close1MinusLowest > (rollback + channelRollback))
	{
	TryEnterShort(lastClose, stopOffset, takeOffset);
	}
	}

	private void TryEnterLong(decimal closePrice, decimal stopOffset, decimal takeOffset)
	{
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (TradeVolume <= 0m)
	return;

	var stop = StopLossPips > 0m ? closePrice - stopOffset : (decimal?)null;
	if (stop.HasValue && stop.Value >= closePrice)
	return;

	var target = TakeProfitPips > 0m ? closePrice + takeOffset : (decimal?)null;

	BuyMarket(TradeVolume);

	_entryPrice = closePrice;
	_stopPrice = stop;
	_takeProfitPrice = target;
	}

	private void TryEnterShort(decimal closePrice, decimal stopOffset, decimal takeOffset)
	{
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (TradeVolume <= 0m)
	return;

	var stop = StopLossPips > 0m ? closePrice + stopOffset : (decimal?)null;
	if (stop.HasValue && stop.Value <= closePrice)
	return;

	var target = TakeProfitPips > 0m ? closePrice - takeOffset : (decimal?)null;

	SellMarket(TradeVolume);

	_entryPrice = closePrice;
	_stopPrice = stop;
	_takeProfitPrice = target;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
	if (Position > 0)
	{
	// Exit long positions when either the stop-loss or take-profit is touched intrabar.
	if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
	{
	SellMarket(Math.Abs(Position));
	ResetProtection();
	return true;
	}

	if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
	{
	SellMarket(Math.Abs(Position));
	ResetProtection();
	return true;
	}
	}
	else if (Position < 0)
	{
	// Exit short positions when protective boundaries are violated.
	if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
	{
	BuyMarket(Math.Abs(Position));
	ResetProtection();
	return true;
	}

	if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
	{
	BuyMarket(Math.Abs(Position));
	ResetProtection();
	return true;
	}
	}
	else if (_stopPrice.HasValue || _takeProfitPrice.HasValue)
	{
	// Clean up state if the position was closed externally.
	ResetProtection();
	}

	return false;
	}

	private void ResetProtection()
	{
	_stopPrice = null;
	_takeProfitPrice = null;
	_entryPrice = 0m;
	}

	private void AddToHistory(decimal open, decimal close)
	{
	var index = (_historyStart + _historyCount) % HistorySize;
	_openHistory[index] = open;
	_closeHistory[index] = close;

	if (_historyCount < HistorySize)
	{
	_historyCount++;
	}
	else
	{
	_historyStart = (_historyStart + 1) % HistorySize;
	}
	}

	private bool TryGetHistoryValues(out decimal open24, out decimal lastClose)
	{
	if (_historyCount < HistorySize)
	{
	open24 = 0m;
	lastClose = 0m;
	return false;
	}

	open24 = _openHistory[_historyStart];
	var lastIndex = (_historyStart + _historyCount - 1) % HistorySize;
	lastClose = _closeHistory[lastIndex];
	return true;
	}

	private static bool IsTradingWindow(DateTimeOffset time)
	{
	// Execute logic only at the start of a new trading day around midnight, except Monday and Friday.
	return time.Hour == 0
	&& time.Minute <= 3
	&& time.DayOfWeek != DayOfWeek.Monday
	&& time.DayOfWeek != DayOfWeek.Friday;
	}

	private decimal CalculatePipValue()
	{
	var step = Security?.PriceStep ?? 1m;

	if (step <= 0m)
	return 1m;

	var ratio = 1m / step;
	var digits = Math.Log10((double)ratio);
	var pipMultiplier = 1m;

	if (!double.IsNaN(digits) && !double.IsInfinity(digits))
	{
	var rounded = Math.Round(digits);

	if (Math.Abs(digits - rounded) < 1e-6 && (rounded == 3 || rounded == 5))
	pipMultiplier = 10m;
	}

	return step * pipMultiplier;
	}
}
