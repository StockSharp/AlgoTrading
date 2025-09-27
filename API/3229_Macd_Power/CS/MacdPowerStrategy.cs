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
/// MACD Power strategy converted from the original MetaTrader implementation.
/// The logic combines multi-timeframe MACD confirmation with momentum and moving average filters.
/// </summary>
public class MacdPowerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingActivationPoints;
	private readonly StrategyParam<decimal> _trailingOffsetPoints;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenOffsetPoints;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _orderVolume;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private MovingAverageConvergenceDivergence _macdPrimary = null!;
	private MovingAverageConvergenceDivergence _macdSecondary = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macroMacd = null!;

	private readonly Queue<decimal> _momentumBuffer = new();

	private decimal _pointValue;
	private decimal? _entryPrice;
	private Sides? _entrySide;
	private decimal _highestPriceSinceEntry;
	private decimal _lowestPriceSinceEntry;
	private bool _breakEvenActive;
	private decimal _breakEvenPrice;
	private bool _macroBullish;
	private bool _macroBearish;
	private bool _macroReady;
	private bool _momentumReady;
	private int _tradesOpened;

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdPowerStrategy"/> class.
	/// </summary>
	public MacdPowerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(4, 12, 2);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(60, 120, 5);

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetDisplay("Momentum Length", "Number of periods for the momentum confirmation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 1);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetDisplay("Momentum Buy Threshold", "Minimum absolute distance from 100 for bullish momentum", "Filters");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetDisplay("Momentum Sell Threshold", "Minimum absolute distance from 100 for bearish momentum", "Filters");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
		.SetDisplay("Take Profit (points)", "Profit target expressed in instrument points", "Risk management");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
		.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in instrument points", "Risk management");

		_trailingActivationPoints = Param(nameof(TrailingActivationPoints), 40m)
		.SetDisplay("Trailing Activation", "Required profit in points before the trailing logic starts", "Risk management");

		_trailingOffsetPoints = Param(nameof(TrailingOffsetPoints), 40m)
		.SetDisplay("Trailing Offset", "Distance between the trailing stop and the best price", "Risk management");

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 30m)
		.SetDisplay("Break-even Trigger", "Profit distance that enables break-even protection", "Risk management");

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 30m)
		.SetDisplay("Break-even Offset", "Offset in points added to the entry price when locking profits", "Risk management");

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetDisplay("Max Trades", "Maximum number of trades allowed per session", "Trading limits");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Base order volume used for entries", "General");
	}

	/// <summary>
	/// Primary candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the fast linear weighted moving average.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow linear weighted moving average.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum calculation length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum distance from 100 required for bullish momentum confirmation.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum distance from 100 required for bearish momentum confirmation.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit in points required before the trailing stop becomes active.
	/// </summary>
	public decimal TrailingActivationPoints
	{
		get => _trailingActivationPoints.Value;
		set => _trailingActivationPoints.Value = value;
	}

	/// <summary>
	/// Offset in points between the trailing stop and the best achieved price.
	/// </summary>
	public decimal TrailingOffsetPoints
	{
		get => _trailingOffsetPoints.Value;
		set => _trailingOffsetPoints.Value = value;
	}

	/// <summary>
	/// Profit distance in points that enables break-even protection.
	/// </summary>
	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Offset in points applied to the entry price when placing the break-even stop.
	/// </summary>
	public decimal BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Maximum number of trades allowed per run.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Base order volume used when opening positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	if (Security != null)
	yield return (Security, CandleType);

	if (Security != null)
	{
	yield return (Security, GetMomentumCandleType());
	yield return (Security, GetMacroCandleType());
	}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_fastMa = null!;
	_slowMa = null!;
	_macdPrimary = null!;
	_macdSecondary = null!;
	_momentum = null!;
	_macroMacd = null!;

	_momentumBuffer.Clear();
	_entryPrice = null;
	_entrySide = null;
	_highestPriceSinceEntry = 0m;
	_lowestPriceSinceEntry = 0m;
	_breakEvenActive = false;
	_breakEvenPrice = 0m;
	_macroBullish = false;
	_macroBearish = false;
	_macroReady = false;
	_momentumReady = false;
	_tradesOpened = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_pointValue = Security?.PriceStep ?? 1m;
	if (_pointValue <= 0m)
	_pointValue = 1m;

	Volume = OrderVolume;

	_fastMa = new WeightedMovingAverage
	{
	Length = FastMaLength,
	CandlePrice = CandlePrice.Typical
	};

	_slowMa = new WeightedMovingAverage
	{
	Length = SlowMaLength,
	CandlePrice = CandlePrice.Typical
	};

	_macdPrimary = new MovingAverageConvergenceDivergence
	{
	ShortPeriod = 12,
	LongPeriod = 26,
	SignalPeriod = 1
	};

	_macdSecondary = new MovingAverageConvergenceDivergence
	{
	ShortPeriod = 6,
	LongPeriod = 13,
	SignalPeriod = 1
	};

	_momentum = new Momentum
	{
	Length = MomentumLength
	};

	_macroMacd = new MovingAverageConvergenceDivergenceSignal
	{
		// Monthly MACD used as trend confirmation filter
	Macd =
	{
	ShortMa = { Length = 12 },
	LongMa = { Length = 26 }
	},
	SignalMa = { Length = 9 }
	};

	// Subscribe to the primary timeframe and bind indicator pipelines
	var mainSubscription = SubscribeCandles(CandleType);
	mainSubscription
	.Bind(_fastMa, _slowMa, _macdPrimary, _macdSecondary, ProcessMainCandle)
	.Start();

	// Higher timeframe momentum subscription for divergence confirmation
	var momentumSubscription = SubscribeCandles(GetMomentumCandleType());
	momentumSubscription
	.Bind(_momentum, ProcessMomentum)
	.Start();

	// Monthly MACD subscription for long-term bias
	var macroSubscription = SubscribeCandles(GetMacroCandleType());
	macroSubscription
	.BindEx(_macroMacd, ProcessMacroMacd)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, mainSubscription);
	DrawIndicator(area, _fastMa);
	DrawIndicator(area, _slowMa);
	DrawOwnTrades(area);
	}
	}

	private DataType GetMomentumCandleType()
	{
	var timeFrame = CandleType.TimeFrame ?? TimeSpan.FromMinutes(15);

	var momentumFrame = timeFrame.TotalMinutes switch
	{
	<= 1 => TimeSpan.FromMinutes(15),
	5 => TimeSpan.FromMinutes(30),
	15 => TimeSpan.FromMinutes(60),
	30 => TimeSpan.FromHours(4),
	60 => TimeSpan.FromDays(1),
	240 => TimeSpan.FromDays(7),
	1440 => TimeSpan.FromDays(30),
	_ => timeFrame
	};

	return momentumFrame.TimeFrame();
	}

	private DataType GetMacroCandleType()
	{
	return TimeSpan.FromDays(30).TimeFrame();
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!_momentum.IsFormed)
	return;

	var diff = Math.Abs(100m - momentumValue);
	_momentumBuffer.Enqueue(diff);

	while (_momentumBuffer.Count > 3)
	_momentumBuffer.Dequeue();

	_momentumReady = _momentumBuffer.Count == 3;
	}

	private void ProcessMacroMacd(ICandleMessage candle, IIndicatorValue value)
	{
	if (!value.IsFinal)
	return;

	var macdValue = (MovingAverageConvergenceDivergenceSignalValue)value;

	if (macdValue.Macd is not decimal macd || macdValue.Signal is not decimal signal)
	return;

	_macroBullish = macd > signal;
	_macroBearish = macd < signal;
	_macroReady = true;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal macdPrimary, decimal macdSecondary)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_macdPrimary.IsFormed || !_macdSecondary.IsFormed)
	return;

	if (!_macroReady || !_momentumReady)
	return;

	// Manage protective logic before evaluating new entries
	if (Position > 0m)
	{
	ManageLongPosition(candle);
	}
	else if (Position < 0m)
	{
	ManageShortPosition(candle);
	}

	if (_tradesOpened >= MaxTrades)
	return;

	if (_momentumBuffer.Count < 3)
	return;

	// Momentum buffer holds the three latest higher-timeframe readings
	var momentumValues = _momentumBuffer.ToArray();
	var hasBullMomentum = momentumValues[0] >= MomentumBuyThreshold ||
	momentumValues[1] >= MomentumBuyThreshold ||
	momentumValues[2] >= MomentumBuyThreshold;

	var hasBearMomentum = momentumValues[0] >= MomentumSellThreshold ||
	momentumValues[1] >= MomentumSellThreshold ||
	momentumValues[2] >= MomentumSellThreshold;

	var volume = Volume;
	if (volume <= 0m)
	return;

	if (macdPrimary > 0m && macdSecondary > 0m && fastMa < slowMa && hasBullMomentum && _macroBullish && Position <= 0m)
	{
	var orderVolume = volume + Math.Abs(Position);
	if (orderVolume > 0m)
	BuyMarket(orderVolume);
	}
	else if (macdPrimary < 0m && macdSecondary < 0m && fastMa < slowMa && hasBearMomentum && _macroBearish && Position >= 0m)
	{
	var orderVolume = volume + Math.Abs(Position);
	if (orderVolume > 0m)
	SellMarket(orderVolume);
	}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		// Exit and protection logic for long positions
	var entryPrice = _entryPrice;
	if (entryPrice == null)
	return;

	_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, candle.HighPrice);
	_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, candle.LowPrice);

	var takeProfit = TakeProfitPoints > 0m ? entryPrice.Value + TakeProfitPoints * _pointValue : (decimal?)null;
	var stopLoss = StopLossPoints > 0m ? entryPrice.Value - StopLossPoints * _pointValue : (decimal?)null;

	if (takeProfit != null && candle.HighPrice >= takeProfit)
	{
	SellMarket(Position);
	return;
	}

	if (stopLoss != null && candle.LowPrice <= stopLoss)
	{
	SellMarket(Position);
	return;
	}

	if (TrailingOffsetPoints > 0m)
	{
	var activation = TrailingActivationPoints * _pointValue;
	var offset = TrailingOffsetPoints * _pointValue;
	var move = _highestPriceSinceEntry - entryPrice.Value;

	if (move >= activation)
	{
	var trailingStop = _highestPriceSinceEntry - offset;
	if (candle.LowPrice <= trailingStop)
	{
	SellMarket(Position);
	return;
	}
	}
	}

	if (!_breakEvenActive && BreakEvenTriggerPoints > 0m)
	{
	var trigger = entryPrice.Value + BreakEvenTriggerPoints * _pointValue;
	if (candle.HighPrice >= trigger)
	{
	_breakEvenActive = true;
	_breakEvenPrice = entryPrice.Value + BreakEvenOffsetPoints * _pointValue;
	}
	}

	if (_breakEvenActive && candle.LowPrice <= _breakEvenPrice)
	{
	SellMarket(Position);
	}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		// Exit and protection logic for short positions
	var entryPrice = _entryPrice;
	if (entryPrice == null)
	return;

	_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, candle.HighPrice);
	_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, candle.LowPrice);

	var takeProfit = TakeProfitPoints > 0m ? entryPrice.Value - TakeProfitPoints * _pointValue : (decimal?)null;
	var stopLoss = StopLossPoints > 0m ? entryPrice.Value + StopLossPoints * _pointValue : (decimal?)null;

	if (takeProfit != null && candle.LowPrice <= takeProfit)
	{
	BuyMarket(-Position);
	return;
	}

	if (stopLoss != null && candle.HighPrice >= stopLoss)
	{
	BuyMarket(-Position);
	return;
	}

	if (TrailingOffsetPoints > 0m)
	{
	var activation = TrailingActivationPoints * _pointValue;
	var offset = TrailingOffsetPoints * _pointValue;
	var move = entryPrice.Value - _lowestPriceSinceEntry;

	if (move >= activation)
	{
	var trailingStop = _lowestPriceSinceEntry + offset;
	if (candle.HighPrice >= trailingStop)
	{
	BuyMarket(-Position);
	return;
	}
	}
	}

	if (!_breakEvenActive && BreakEvenTriggerPoints > 0m)
	{
	var trigger = entryPrice.Value - BreakEvenTriggerPoints * _pointValue;
	if (candle.LowPrice <= trigger)
	{
	_breakEvenActive = true;
	_breakEvenPrice = entryPrice.Value - BreakEvenOffsetPoints * _pointValue;
	}
	}

	if (_breakEvenActive && candle.HighPrice >= _breakEvenPrice)
	{
	BuyMarket(-Position);
	}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		// Track entry statistics to drive trailing and break-even logic
	base.OnNewMyTrade(trade);

	if (Position == 0m)
	{
	_entryPrice = null;
	_entrySide = null;
	_highestPriceSinceEntry = 0m;
	_lowestPriceSinceEntry = 0m;
	_breakEvenActive = false;
	_breakEvenPrice = 0m;
	return;
	}

	var tradePrice = trade.Trade.Price;

	// Manage protective logic before evaluating new entries
	if (Position > 0m)
	{
	if (_entrySide != Sides.Buy)
	{
	_entrySide = Sides.Buy;
	_entryPrice = tradePrice;
	_highestPriceSinceEntry = tradePrice;
	_lowestPriceSinceEntry = tradePrice;
	_breakEvenActive = false;
	_breakEvenPrice = 0m;

	if (_tradesOpened < MaxTrades)
	_tradesOpened++;
	}
	else
	{
	_entryPrice = tradePrice;
	_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, tradePrice);
	_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, tradePrice);
	}
	}
	else if (Position < 0m)
	{
	if (_entrySide != Sides.Sell)
	{
	_entrySide = Sides.Sell;
	_entryPrice = tradePrice;
	_highestPriceSinceEntry = tradePrice;
	_lowestPriceSinceEntry = tradePrice;
	_breakEvenActive = false;
	_breakEvenPrice = 0m;

	if (_tradesOpened < MaxTrades)
	_tradesOpened++;
	}
	else
	{
	_entryPrice = tradePrice;
	_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, tradePrice);
	_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, tradePrice);
	}
	}
	}
}

