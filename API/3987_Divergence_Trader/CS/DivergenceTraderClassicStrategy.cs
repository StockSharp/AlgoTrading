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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Classic divergence trading strategy converted from the MetaTrader 4 "Divergence Trader" expert.
/// The strategy compares a fast and a slow simple moving average and monitors how the spread between
/// them changes from bar to bar. A widening spread to the upside triggers long trades while a widening
/// spread to the downside triggers short trades. Risk management mimics the original MQL behaviour with
/// optional profit targets, stop-loss, trailing stop, break-even shift and basket level exits.
/// </summary>
public class DivergenceTraderClassicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<CandlePrice> _appliedPrice;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _stayOutThreshold;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<decimal> _breakEvenBufferPips;
	private readonly StrategyParam<decimal> _basketProfitCurrency;
	private readonly StrategyParam<decimal> _basketLossCurrency;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;
	private decimal? _previousSpread;
	private decimal _pipSize;
	private decimal _maxBasketPnL;
	private decimal _minBasketPnL;
	private decimal? _breakEvenPrice;
	private decimal? _trailingStopPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="DivergenceTraderClassicStrategy"/>.
	/// </summary>
	public DivergenceTraderClassicStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
		.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMA", "Period for the fast simple moving average.", "Indicators")
		.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 88)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMA", "Period for the slow simple moving average.", "Indicators")
		.SetCanOptimize(true);

		_appliedPrice = Param(nameof(AppliedPrice), CandlePrice.Open)
		.SetDisplay("Applied Price", "Price component forwarded into the moving averages.", "Indicators");

		_buyThreshold = Param(nameof(BuyThreshold), 0.0011m)
		.SetDisplay("Buy Threshold", "Minimal divergence needed to allow long entries.", "Signals")
		.SetCanOptimize(true);

		_stayOutThreshold = Param(nameof(StayOutThreshold), 0.0079m)
		.SetDisplay("Stay Out Threshold", "Upper divergence bound disabling new entries.", "Signals")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
		.SetDisplay("Take Profit (pips)", "Distance in pips used to exit winners.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetDisplay("Stop Loss (pips)", "Maximum adverse excursion tolerated.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 9999m)
		.SetDisplay("Trailing Stop (pips)", "Trailing distance; 9999 disables trailing just like the EA.", "Risk");

		_breakEvenPips = Param(nameof(BreakEvenPips), 9999m)
		.SetDisplay("Break-Even Trigger (pips)", "Profit in pips required before moving the stop to break-even.", "Risk");

		_breakEvenBufferPips = Param(nameof(BreakEvenBufferPips), 2m)
		.SetDisplay("Break-Even Buffer (pips)", "Buffer in pips added to the break-even stop.", "Risk");

		_basketProfitCurrency = Param(nameof(BasketProfitCurrency), 75m)
		.SetDisplay("Basket Profit", "Floating profit that forces closing all positions.", "Basket");

		_basketLossCurrency = Param(nameof(BasketLossCurrency), 9999m)
		.SetDisplay("Basket Loss", "Floating loss that forces closing all positions.", "Basket");

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Hour when trading becomes active (0-23).", "Schedule");

		_stopHour = Param(nameof(StopHour), 24)
		.SetDisplay("Stop Hour", "Hour when trading stops accepting new entries (1-24).", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used to calculate signals.", "General");
	}

	/// <summary>
	/// Base volume for new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Period for the fast moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Period for the slow moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Price component forwarded into both moving averages.
	/// </summary>
	public CandlePrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Divergence value required before long trades can be opened.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Maximum divergence that still allows trades. Above this value trading is skipped.
	/// </summary>
	public decimal StayOutThreshold
	{
		get => _stayOutThreshold.Value;
		set => _stayOutThreshold.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Zero keeps the trade open until an opposite signal.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Trailing stop distance in pips. Use a very large value to disable the trail.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Profit trigger for moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Additional buffer applied when shifting the stop to break-even.
	/// </summary>
	public decimal BreakEvenBufferPips
	{
		get => _breakEvenBufferPips.Value;
		set => _breakEvenBufferPips.Value = value;
	}

	/// <summary>
	/// Basket profit threshold in account currency.
	/// </summary>
	public decimal BasketProfitCurrency
	{
		get => _basketProfitCurrency.Value;
		set => _basketProfitCurrency.Value = value;
	}

	/// <summary>
	/// Basket loss threshold in account currency.
	/// </summary>
	public decimal BasketLossCurrency
	{
		get => _basketLossCurrency.Value;
		set => _basketLossCurrency.Value = value;
	}

	/// <summary>
	/// Hour of the day when new trades are allowed.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour of the day when new trades are blocked.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Candle type (timeframe) used by the strategy.
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

	_fastSma = null;
	_slowSma = null;
	_previousSpread = null;
	_pipSize = 0m;
	_maxBasketPnL = 0m;
	_minBasketPnL = 0m;
	_breakEvenPrice = null;
	_trailingStopPrice = null;
	_highestPrice = 0m;
	_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_pipSize = CalculatePipSize();

	_fastSma = new SimpleMovingAverage
	{
	Length = FastPeriod,
	CandlePrice = AppliedPrice
	};

	_slowSma = new SimpleMovingAverage
	{
	Length = SlowPeriod,
	CandlePrice = AppliedPrice
	};

	_previousSpread = null;
	_breakEvenPrice = null;
	_trailingStopPrice = null;
	_highestPrice = 0m;
	_lowestPrice = 0m;

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_fastSma, _slowSma, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _fastSma);
	DrawIndicator(area, _slowSma);
	DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
	// Work only with fully formed candles.
	if (candle.State != CandleStates.Finished)
	return;

	// Update trailing logic for existing positions before acting on new signals.
	ManageOpenPosition(candle);

	// Respect basket limits from the legacy EA.
	if (EvaluateBasketPnL(candle.ClosePrice))
	{
	_previousSpread = fastValue - slowValue;
	return;
	}

	if (_fastSma == null || _slowSma == null)
	return;

	if (!_fastSma.IsFormed || !_slowSma.IsFormed)
	{
	_previousSpread = fastValue - slowValue;
	return;
	}

	var currentSpread = fastValue - slowValue;
	var divergence = _previousSpread.HasValue ? currentSpread - _previousSpread.Value : 0m;
	_previousSpread = currentSpread;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (!IsWithinTradingHours(candle.CloseTime))
	return;

	if (OrderVolume <= 0m)
	return;

	// Avoid over-hedging: only reverse when the signal changes direction.
	if (divergence >= BuyThreshold && divergence <= StayOutThreshold)
	{
	if (Position < 0m)
	{
	BuyMarket(Math.Abs(Position));
	}

	if (Position <= 0m)
	{
	ResetPositionTracking();
	BuyMarket(OrderVolume);
	}
	}
	else if (divergence <= -BuyThreshold && divergence >= -StayOutThreshold)
	{
	if (Position > 0m)
	{
	SellMarket(Position);
	}

	if (Position >= 0m)
	{
	ResetPositionTracking();
	SellMarket(OrderVolume);
	}
	}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
	if (Position == 0m)
	{
	ResetPositionTracking();
	return;
	}

	var entryPrice = PositionPrice;
	if (entryPrice == 0m)
	return;

	var pipSize = EnsurePipSize();
	var takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * pipSize : 0m;
	var stopLossDistance = StopLossPips > 0m ? StopLossPips * pipSize : 0m;
	var breakEvenDistance = BreakEvenPips > 0m && BreakEvenPips < 9000m ? BreakEvenPips * pipSize : 0m;
	var breakEvenBuffer = BreakEvenBufferPips > 0m ? BreakEvenBufferPips * pipSize : 0m;
	var trailingDistance = TrailingStopPips > 0m && TrailingStopPips < 9000m ? TrailingStopPips * pipSize : 0m;
	var absPosition = Math.Abs(Position);

	if (Position > 0m)
	{
	_highestPrice = Math.Max(_highestPrice == 0m ? entryPrice : _highestPrice, candle.HighPrice);

	var profitDistance = candle.ClosePrice - entryPrice;

	if (breakEvenDistance > 0m && profitDistance >= breakEvenDistance && _breakEvenPrice == null)
	_breakEvenPrice = entryPrice + breakEvenBuffer;

	if (_breakEvenPrice is decimal bePrice && candle.LowPrice <= bePrice)
	{
	SellMarket(absPosition);
	ResetPositionTracking();
	return;
	}

	if (trailingDistance > 0m && profitDistance >= trailingDistance)
	{
	var candidate = _highestPrice - trailingDistance;
	if (_trailingStopPrice == null || candidate > _trailingStopPrice)
	_trailingStopPrice = candidate;

	if (_trailingStopPrice is decimal trailing && candle.LowPrice <= trailing)
	{
	SellMarket(absPosition);
	ResetPositionTracking();
	return;
	}
	}

	if (takeProfitDistance > 0m && profitDistance >= takeProfitDistance)
	{
	SellMarket(absPosition);
	ResetPositionTracking();
	return;
	}

	if (stopLossDistance > 0m && candle.LowPrice <= entryPrice - stopLossDistance)
	{
	SellMarket(absPosition);
	ResetPositionTracking();
	}
	}
	else if (Position < 0m)
	{
	_lowestPrice = Math.Min(_lowestPrice == 0m ? entryPrice : _lowestPrice, candle.LowPrice);

	var profitDistance = entryPrice - candle.ClosePrice;

	if (breakEvenDistance > 0m && profitDistance >= breakEvenDistance && _breakEvenPrice == null)
	_breakEvenPrice = entryPrice - breakEvenBuffer;

	if (_breakEvenPrice is decimal bePrice && candle.HighPrice >= bePrice)
	{
	BuyMarket(absPosition);
	ResetPositionTracking();
	return;
	}

	if (trailingDistance > 0m && profitDistance >= trailingDistance)
	{
	var candidate = _lowestPrice + trailingDistance;
	if (_trailingStopPrice == null || candidate < _trailingStopPrice)
	_trailingStopPrice = candidate;

	if (_trailingStopPrice is decimal trailing && candle.HighPrice >= trailing)
	{
	BuyMarket(absPosition);
	ResetPositionTracking();
	return;
	}
	}

	if (takeProfitDistance > 0m && profitDistance >= takeProfitDistance)
	{
	BuyMarket(absPosition);
	ResetPositionTracking();
	return;
	}

	if (stopLossDistance > 0m && candle.HighPrice >= entryPrice + stopLossDistance)
	{
	BuyMarket(absPosition);
	ResetPositionTracking();
	}
	}
	}

	private bool EvaluateBasketPnL(decimal lastPrice)
	{
	if (BasketProfitCurrency <= 0m && BasketLossCurrency <= 0m)
	return false;

	if (Position == 0m)
	return false;

	var entryPrice = PositionPrice;
	if (entryPrice == 0m)
	return false;

	var step = EnsurePipSize();
	var stepValue = Security?.StepPrice ?? step;

	var priceMove = Position > 0m ? lastPrice - entryPrice : entryPrice - lastPrice;
	var pipMove = step > 0m ? priceMove / step : priceMove;
	var currencyPnL = pipMove * stepValue * Math.Abs(Position);

	_maxBasketPnL = Math.Max(_maxBasketPnL, currencyPnL);
	_minBasketPnL = Math.Min(_minBasketPnL, currencyPnL);

	var shouldCloseForProfit = BasketProfitCurrency > 0m && currencyPnL >= BasketProfitCurrency;
	var shouldCloseForLoss = BasketLossCurrency > 0m && currencyPnL <= -BasketLossCurrency;

	if (shouldCloseForProfit || shouldCloseForLoss)
	{
	CloseAllPositions();
	return true;
	}

	return false;
	}

	private void CloseAllPositions()
	{
	if (Position > 0m)
	{
	SellMarket(Position);
	}
	else if (Position < 0m)
	{
	BuyMarket(Math.Abs(Position));
	}

	ResetPositionTracking();
	}

	private void ResetPositionTracking()
	{
	_breakEvenPrice = null;
	_trailingStopPrice = null;
	_highestPrice = 0m;
	_lowestPrice = 0m;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
	var hour = time.LocalDateTime.Hour;

	if (StartHour == StopHour)
	return true;

	if (StartHour < StopHour)
	return hour >= StartHour && hour < StopHour;

	// Overnight window that crosses midnight.
	return hour >= StartHour || hour < StopHour;
	}

	private decimal CalculatePipSize()
	{
	var step = Security?.PriceStep ?? 0m;
	return step > 0m ? step : 0.0001m;
	}

	private decimal EnsurePipSize()
	{
	if (_pipSize <= 0m)
	_pipSize = CalculatePipSize();

	return _pipSize;
	}
}

