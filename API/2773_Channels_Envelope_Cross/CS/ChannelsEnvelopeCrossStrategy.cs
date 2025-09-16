namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Channels envelope crossover strategy converted from the MetaTrader Channels expert advisor.
/// The strategy monitors EMA based envelopes on hourly candles and trades breakouts of the fast EMA through the bands.
/// </summary>
public class ChannelsEnvelopeCrossStrategy : Strategy
{
	private const decimal Envelope003 = 0.3m / 100m;
	private const decimal Envelope007 = 0.7m / 100m;
	private const decimal Envelope010 = 1.0m / 100m;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useTradeHours;
	private readonly StrategyParam<int> _fromHour;
	private readonly StrategyParam<int> _toHour;
	private readonly StrategyParam<int> _stopLossBuyPips;
	private readonly StrategyParam<int> _stopLossSellPips;
	private readonly StrategyParam<int> _takeProfitBuyPips;
	private readonly StrategyParam<int> _takeProfitSellPips;
	private readonly StrategyParam<int> _trailingStopBuyPips;
	private readonly StrategyParam<int> _trailingStopSellPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFastClose;
	private EMA _emaFastOpen;
	private EMA _emaSlow;

	private bool _hasPreviousValues;
	private decimal _prevFastClose;
	private decimal _prevFastOpen;
	private decimal _prevSlow;
	private decimal _prevEnvLower03;
	private decimal _prevEnvUpper03;
	private decimal _prevEnvLower07;
	private decimal _prevEnvUpper07;
	private decimal _prevEnvLower10;
	private decimal _prevEnvUpper10;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enable trading only within the configured time window.
	/// </summary>
	public bool UseTradeHours
	{
		get => _useTradeHours.Value;
		set => _useTradeHours.Value = value;
	}

	/// <summary>
	/// Start hour of the trading window (inclusive).
	/// </summary>
	public int FromHour
	{
		get => _fromHour.Value;
		set => _fromHour.Value = value;
	}

	/// <summary>
	/// End hour of the trading window (inclusive).
	/// </summary>
	public int ToHour
	{
		get => _toHour.Value;
		set => _toHour.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long positions expressed in pips.
	/// </summary>
	public int StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short positions expressed in pips.
	/// </summary>
	public int StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long positions expressed in pips.
	/// </summary>
	public int TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short positions expressed in pips.
	/// </summary>
	public int TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop size for long positions expressed in pips.
	/// </summary>
	public int TrailingStopBuyPips
	{
		get => _trailingStopBuyPips.Value;
		set => _trailingStopBuyPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop size for short positions expressed in pips.
	/// </summary>
	public int TrailingStopSellPips
	{
		get => _trailingStopSellPips.Value;
		set => _trailingStopSellPips.Value = value;
	}

	/// <summary>
	/// Minimum increment for trailing adjustments expressed in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ChannelsEnvelopeCrossStrategy"/>.
	/// </summary>
	public ChannelsEnvelopeCrossStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume in lots", "Trading");

		_useTradeHours = Param(nameof(UseTradeHours), false)
		.SetDisplay("Use Trade Hours", "Restrict trading to specified hours", "Trading");

		_fromHour = Param(nameof(FromHour), 0)
		.SetDisplay("From Hour", "Start hour for trading window", "Trading");

		_toHour = Param(nameof(ToHour), 23)
		.SetDisplay("To Hour", "End hour for trading window", "Trading");

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 0)
		.SetDisplay("SL BUY (pips)", "Stop loss distance for long positions", "Risk");

		_stopLossSellPips = Param(nameof(StopLossSellPips), 0)
		.SetDisplay("SL SELL (pips)", "Stop loss distance for short positions", "Risk");

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 0)
		.SetDisplay("TP BUY (pips)", "Take profit distance for long positions", "Risk");

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 0)
		.SetDisplay("TP SELL (pips)", "Take profit distance for short positions", "Risk");

		_trailingStopBuyPips = Param(nameof(TrailingStopBuyPips), 30)
		.SetDisplay("Trail BUY (pips)", "Trailing stop for long positions", "Risk");

		_trailingStopSellPips = Param(nameof(TrailingStopSellPips), 30)
		.SetDisplay("Trail SELL (pips)", "Trailing stop for short positions", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 1)
		.SetDisplay("Trailing Step (pips)", "Minimum increment for trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations", "General");
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

		_hasPreviousValues = false;
		_prevFastClose = 0m;
		_prevFastOpen = 0m;
		_prevSlow = 0m;
		_prevEnvLower03 = 0m;
		_prevEnvUpper03 = 0m;
		_prevEnvLower07 = 0m;
		_prevEnvUpper07 = 0m;
		_prevEnvLower10 = 0m;
		_prevEnvUpper10 = 0m;

		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;

		_emaFastClose?.Reset();
		_emaFastOpen?.Reset();
		_emaSlow?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFastClose = new EMA { Length = 2 };
		_emaFastOpen = new EMA { Length = 2 };
		_emaSlow = new EMA { Length = 220 };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	if (UseTradeHours && !IsWithinTradeHours(candle.OpenTime))
	return;

	if (candle.State != CandleStates.Finished)
	return;

	var fastCloseValue = _emaFastClose.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
	var fastOpenValue = _emaFastOpen.Process(new CandleIndicatorValue(candle, candle.OpenPrice));
	var slowValue = _emaSlow.Process(new CandleIndicatorValue(candle, candle.ClosePrice));

	var fastClose = fastCloseValue.GetValue<decimal>();
	var fastOpen = fastOpenValue.GetValue<decimal>();
	var slow = slowValue.GetValue<decimal>();

	var envLower03 = slow * (1m - Envelope003);
	var envUpper03 = slow * (1m + Envelope003);
	var envLower07 = slow * (1m - Envelope007);
	var envUpper07 = slow * (1m + Envelope007);
	var envLower10 = slow * (1m - Envelope010);
	var envUpper10 = slow * (1m + Envelope010);

	if (!_emaSlow.IsFormed || !_emaFastClose.IsFormed || !_emaFastOpen.IsFormed)
	{
	UpdatePreviousValues(fastClose, fastOpen, slow, envLower03, envUpper03, envLower07, envUpper07, envLower10, envUpper10);
	return;
	}

	if (!_hasPreviousValues)
	{
	UpdatePreviousValues(fastClose, fastOpen, slow, envLower03, envUpper03, envLower07, envUpper07, envLower10, envUpper10);
	_hasPreviousValues = true;
	return;
	}

	var buySignal =
	(fastClose > envLower10 && _prevFastClose <= _prevEnvLower10) ||
	(fastClose > envLower07 && _prevFastClose <= _prevEnvLower07) ||
	(fastClose < envLower03 && _prevFastClose < _prevEnvLower03) ||
	(fastClose > slow && _prevFastClose <= _prevSlow) ||
	(fastClose > envUpper03 && _prevFastClose <= _prevEnvUpper03) ||
	(fastClose > envUpper07 && _prevFastClose <= _prevEnvUpper07);

	var sellSignal =
	(fastOpen < envUpper10 && _prevFastOpen >= _prevEnvUpper10) ||
	(fastOpen < envUpper07 && _prevFastOpen >= _prevEnvUpper07) ||
	(fastOpen < envUpper03 && _prevFastOpen >= _prevEnvUpper03) ||
	(fastOpen < slow && _prevFastOpen >= _prevSlow) ||
	(fastOpen < envLower03 && _prevFastOpen >= _prevEnvLower03) ||
	(fastOpen < envLower07 && _prevFastOpen >= _prevEnvLower07);

	if (Position > 0)
	{
	ManageLongPosition(candle);
	}
	else if (Position < 0)
	{
	ManageShortPosition(candle);
	}

	if (Position == 0)
	{
	if (buySignal)
	{
	BuyMarket(OrderVolume);
	SetEntryState(true, candle.ClosePrice);
	}
	else if (sellSignal)
	{
	SellMarket(OrderVolume);
	SetEntryState(false, candle.ClosePrice);
	}
	}

	UpdatePreviousValues(fastClose, fastOpen, slow, envLower03, envUpper03, envLower07, envUpper07, envLower10, envUpper10);
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
	if (_entryPrice is null)
	return;

	var pip = GetPipSize();
	var trailingDistance = TrailingStopBuyPips * pip;
	var trailingStep = TrailingStepPips * pip;

	var profit = candle.ClosePrice - _entryPrice.Value;

	if (TrailingStopBuyPips > 0 && profit > trailingDistance + trailingStep)
	{
	var threshold = candle.ClosePrice - (trailingDistance + trailingStep);
	if (!_stopLossPrice.HasValue || _stopLossPrice.Value < threshold)
	_stopLossPrice = candle.ClosePrice - trailingDistance;
	}

	var exitVolume = Position;

	if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
	{
	SellMarket(exitVolume);
	ResetPositionState();
	return;
	}

	if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
	{
	SellMarket(exitVolume);
	ResetPositionState();
	}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
	if (_entryPrice is null)
	return;

	var pip = GetPipSize();
	var trailingDistance = TrailingStopSellPips * pip;
	var trailingStep = TrailingStepPips * pip;

	var profit = _entryPrice.Value - candle.ClosePrice;

	if (TrailingStopSellPips > 0 && profit > trailingDistance + trailingStep)
	{
	var threshold = candle.ClosePrice + (trailingDistance + trailingStep);
	if (!_stopLossPrice.HasValue || _stopLossPrice.Value > threshold)
	_stopLossPrice = candle.ClosePrice + trailingDistance;
	}

	var exitVolume = -Position;

	if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
	{
	BuyMarket(exitVolume);
	ResetPositionState();
	return;
	}

	if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
	{
	BuyMarket(exitVolume);
	ResetPositionState();
	}
	}

	private void SetEntryState(bool isLong, decimal entryPrice)
	{
	_entryPrice = entryPrice;

	var pip = GetPipSize();

	_stopLossPrice = isLong && StopLossBuyPips > 0
	? entryPrice - StopLossBuyPips * pip
	: !isLong && StopLossSellPips > 0
	? entryPrice + StopLossSellPips * pip
	: null;

	_takeProfitPrice = isLong && TakeProfitBuyPips > 0
	? entryPrice + TakeProfitBuyPips * pip
	: !isLong && TakeProfitSellPips > 0
	? entryPrice - TakeProfitSellPips * pip
	: null;
	}

	private void ResetPositionState()
	{
	_entryPrice = null;
	_stopLossPrice = null;
	_takeProfitPrice = null;
	}

	private void UpdatePreviousValues(decimal fastClose, decimal fastOpen, decimal slow, decimal envLower03, decimal envUpper03, decimal envLower07, decimal envUpper07, decimal envLower10, decimal envUpper10)
	{
	_prevFastClose = fastClose;
	_prevFastOpen = fastOpen;
	_prevSlow = slow;
	_prevEnvLower03 = envLower03;
	_prevEnvUpper03 = envUpper03;
	_prevEnvLower07 = envLower07;
	_prevEnvUpper07 = envUpper07;
	_prevEnvLower10 = envLower10;
	_prevEnvUpper10 = envUpper10;
	}

	private bool IsWithinTradeHours(DateTimeOffset time)
	{
	var hour = time.Hour;

	if (FromHour == ToHour)
	return hour == FromHour;

	if (FromHour < ToHour)
	return hour >= FromHour && hour <= ToHour;

	return hour >= FromHour || hour <= ToHour;
	}

	private decimal GetPipSize()
	{
	var step = Security?.PriceStep ?? 0.0001m;

	if (Security?.Decimals is int decimals && (decimals == 3 || decimals == 5))
	return step * 10m;

	return step;
	}
}
