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
/// Port of the MetaTrader expert advisor "EXP_FIBO_ZZ_V1en".
/// Trades breakouts of the last ZigZag corridor using Fibonacci based stop and take-profit distances.
/// </summary>
public class ExpFiboZzStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<decimal> _zigZagDeviationPips;
	private readonly StrategyParam<int> _zigZagBackstep;
	private readonly StrategyParam<int> _entryOffsetPips;
	private readonly StrategyParam<int> _minCorridorPips;
	private readonly StrategyParam<int> _maxCorridorPips;
	private readonly StrategyParam<decimal> _fiboStopLoss;
	private readonly StrategyParam<decimal> _fiboTakeProfit;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<int> _stopMinute;
	private readonly StrategyParam<bool> _useBalanceForRisk;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerPips;
	private readonly StrategyParam<int> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _drawCorridorLevels;

	private decimal _currentPivot;
	private decimal _previousPivot;
	private decimal _olderPivot;
	private int _zigZagDirection;
	private int _barsSincePivot;

	private decimal _pipSize;
	private decimal _zigZagDeviation;
	private decimal _entryOffset;
	private decimal _minCorridorSize;
	private decimal _maxCorridorSize;
	private decimal _plannedStopDistance;
	private decimal _plannedTakeDistance;
	private decimal _breakEvenTrigger;
	private decimal _breakEvenOffset;
	private decimal _stopLevelBuffer;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpFiboZzStrategy"/> class.
	/// </summary>
	public ExpFiboZzStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for corridor detection.", "General");

		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
			.SetDisplay("ZigZag Depth", "Number of candles required between swing points.", "ZigZag")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_zigZagDeviationPips = Param(nameof(ZigZagDeviationPips), 5m)
			.SetDisplay("ZigZag Deviation", "Minimum price deviation in pips before accepting a new swing.", "ZigZag")
			.SetNotNegative()
			.SetCanOptimize(true);

		_zigZagBackstep = Param(nameof(ZigZagBackstep), 3)
			.SetDisplay("ZigZag Backstep", "Minimum bars before switching swing direction.", "ZigZag")
			.SetNotNegative()
			.SetCanOptimize(true);

		_entryOffsetPips = Param(nameof(EntryOffsetPips), 5)
			.SetDisplay("Entry Offset", "Distance from the corridor in pips used for stop orders.", "Orders")
			.SetNotNegative()
			.SetCanOptimize(true);

		_minCorridorPips = Param(nameof(MinCorridorPips), 20)
			.SetDisplay("Min Corridor", "Minimum corridor height in pips required for trading.", "Orders")
			.SetNotNegative()
			.SetCanOptimize(true);

		_maxCorridorPips = Param(nameof(MaxCorridorPips), 100)
			.SetDisplay("Max Corridor", "Maximum corridor height in pips allowed for trading.", "Orders")
			.SetNotNegative()
			.SetCanOptimize(true);

		_fiboStopLoss = Param(nameof(FiboStopLoss), 61.8m)
			.SetDisplay("Stop Loss %", "Fibonacci percentage applied to the corridor for the stop distance.", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_fiboTakeProfit = Param(nameof(FiboTakeProfit), 161.8m)
			.SetDisplay("Take Profit %", "Fibonacci percentage applied to the corridor for the take-profit target.", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading window start hour.", "Trading Window")
			.SetNotNegative()
			.SetCanOptimize(true);

		_startMinute = Param(nameof(StartMinute), 1)
			.SetDisplay("Start Minute", "Trading window start minute.", "Trading Window")
			.SetNotNegative()
			.SetCanOptimize(true);

		_stopHour = Param(nameof(StopHour), 23)
			.SetDisplay("Stop Hour", "Trading window end hour.", "Trading Window")
			.SetNotNegative()
			.SetCanOptimize(true);

		_stopMinute = Param(nameof(StopMinute), 59)
			.SetDisplay("Stop Minute", "Trading window end minute.", "Trading Window")
			.SetNotNegative()
			.SetCanOptimize(true);

		_useBalanceForRisk = Param(nameof(UseBalanceForRisk), true)
			.SetDisplay("Use Balance", "When true use equity, otherwise rely on available cash for risk sizing.", "Money Management");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetDisplay("Risk %", "Risk percentage applied to the selected capital source.", "Money Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetDisplay("Fixed Volume", "Fallback lot size when risk based sizing is disabled or unavailable.", "Money Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
			.SetDisplay("Enable BreakEven", "Move the stop to break-even after sufficient profit.", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 13)
			.SetDisplay("BreakEven Trigger", "Profit in pips required before stop adjustment.", "Risk")
			.SetNotNegative()
			.SetCanOptimize(true);

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 2)
			.SetDisplay("BreakEven Offset", "Offset in pips added beyond the entry when moving the stop.", "Risk")
			.SetNotNegative()
			.SetCanOptimize(true);

		_drawCorridorLevels = Param(nameof(DrawCorridorLevels), false)
			.SetDisplay("Draw Corridor", "Render the current ZigZag corridor on the chart.", "Visuals");
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum number of candles between ZigZag pivots.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	/// <summary>
	/// Minimum deviation in pips required to register a new ZigZag swing.
	/// </summary>
	public decimal ZigZagDeviationPips
	{
		get => _zigZagDeviationPips.Value;
		set => _zigZagDeviationPips.Value = value;
	}

	/// <summary>
	/// Minimum number of bars before direction can flip again.
	/// </summary>
	public int ZigZagBackstep
	{
		get => _zigZagBackstep.Value;
		set => _zigZagBackstep.Value = value;
	}

	/// <summary>
	/// Offset above/below the corridor expressed in pips.
	/// </summary>
	public int EntryOffsetPips
	{
		get => _entryOffsetPips.Value;
		set => _entryOffsetPips.Value = value;
	}

	/// <summary>
	/// Minimum corridor size in pips.
	/// </summary>
	public int MinCorridorPips
	{
		get => _minCorridorPips.Value;
		set => _minCorridorPips.Value = value;
	}

	/// <summary>
	/// Maximum corridor size in pips.
	/// </summary>
	public int MaxCorridorPips
	{
		get => _maxCorridorPips.Value;
		set => _maxCorridorPips.Value = value;
	}

	/// <summary>
	/// Fibonacci percentage for the protective stop.
	/// </summary>
	public decimal FiboStopLoss
	{
		get => _fiboStopLoss.Value;
		set => _fiboStopLoss.Value = value;
	}

	/// <summary>
	/// Fibonacci percentage for the take-profit target.
	/// </summary>
	public decimal FiboTakeProfit
	{
		get => _fiboTakeProfit.Value;
		set => _fiboTakeProfit.Value = value;
	}

	/// <summary>
	/// Trading window start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading window start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Trading window end hour.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Trading window end minute.
	/// </summary>
	public int StopMinute
	{
		get => _stopMinute.Value;
		set => _stopMinute.Value = value;
	}

	/// <summary>
	/// When true risk is calculated from equity, otherwise from available cash.
	/// </summary>
	public bool UseBalanceForRisk
	{
		get => _useBalanceForRisk.Value;
		set => _useBalanceForRisk.Value = value;
	}

	/// <summary>
	/// Risk percentage applied to the selected capital source.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fallback fixed trading volume.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Enable break-even stop adjustments.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in pips required before moving the stop.
	/// </summary>
	public int BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Offset in pips beyond the entry when moving the stop.
	/// </summary>
	public int BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Draw corridor boundaries on the chart.
	/// </summary>
	public bool DrawCorridorLevels
	{
		get => _drawCorridorLevels.Value;
		set => _drawCorridorLevels.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentPivot = 0m;
		_previousPivot = 0m;
		_olderPivot = 0m;
		_zigZagDirection = 0;
		_barsSincePivot = 0;

		_pipSize = 0m;
		_zigZagDeviation = 0m;
		_entryOffset = 0m;
		_minCorridorSize = 0m;
		_maxCorridorSize = 0m;
		_plannedStopDistance = 0m;
		_plannedTakeDistance = 0m;
		_breakEvenTrigger = 0m;
		_breakEvenOffset = 0m;
		_stopLevelBuffer = 0m;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeScaling();

		var highest = new Highest { Length = ZigZagDepth };
		var lowest = new Lowest { Length = ZigZagDepth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void InitializeScaling()
	{
		_pipSize = CalculatePipSize();
		_zigZagDeviation = ZigZagDeviationPips * _pipSize;
		_entryOffset = EntryOffsetPips * _pipSize;
		_minCorridorSize = MinCorridorPips * _pipSize;
		_maxCorridorSize = MaxCorridorPips * _pipSize;
		_breakEvenTrigger = BreakEvenTriggerPips * _pipSize;
		_breakEvenOffset = BreakEvenOffsetPips * _pipSize;

		var security = Security;
		var step = security?.PriceStep ?? 0m;
		var minStop = security?.MinPriceStep ?? 0m;
		_stopLevelBuffer = Math.Max(step, minStop);
		if (_stopLevelBuffer <= 0m)
		_stopLevelBuffer = _pipSize;
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateBreakEven(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWithinTradingWindow(candle.OpenTime))
		{
			CancelEntryOrders();
			return;
		}

		UpdatePivots(candle, highest, lowest);

		if (_olderPivot == 0m || _previousPivot == 0m)
		return;

		var high = Math.Max(_previousPivot, _olderPivot);
		var low = Math.Min(_previousPivot, _olderPivot);
		var corridor = high - low;

		if (corridor <= 0m)
		{
			CancelEntryOrders();
			return;
		}

		var lastPivot = _currentPivot;
		var corridorOk = corridor >= _minCorridorSize && corridor <= _maxCorridorSize;
		var above = high > lastPivot + _stopLevelBuffer;
		var below = low < lastPivot - _stopLevelBuffer;

		if (!corridorOk || !above || !below || Position != 0m)
		{
			CancelEntryOrders();
			return;
		}

		var stopDistance = corridor * (FiboStopLoss / 100m);
		var takeDistance = corridor * (FiboTakeProfit / 100m - 1m);
		if (takeDistance < 0m) takeDistance = 0m;

		var buyPrice = RoundPrice(high + _entryOffset);
		var sellPrice = RoundPrice(low - _entryOffset);

		var referencePrice = candle.ClosePrice > 0m ? candle.ClosePrice : GetReferencePrice();
		var volume = CalculateOrderVolume(referencePrice);

		UpdateEntryOrders(buyPrice, sellPrice, stopDistance, takeDistance, volume);

		if (DrawCorridorLevels)
		DrawLine(candle.OpenTime, high, candle.OpenTime, low);
	}

	private void UpdatePivots(ICandleMessage candle, decimal highest, decimal lowest)
	{
		_barsSincePivot++;

		var deviationOk = _zigZagDeviation <= 0m;

		if (candle.HighPrice >= highest)
		{
			var deviation = Math.Abs(candle.HighPrice - _currentPivot);
			if (_currentPivot != 0m && _zigZagDirection != 1 && _barsSincePivot < ZigZagBackstep)
				return;

			if (_currentPivot != 0m && !deviationOk && deviation < _zigZagDeviation)
				return;

			RegisterPivot(candle.HighPrice);
			_zigZagDirection = 1;
			_barsSincePivot = 0;
			return;
		}

		if (candle.LowPrice <= lowest)
		{
			var deviation = Math.Abs(candle.LowPrice - _currentPivot);
			if (_currentPivot != 0m && _zigZagDirection != -1 && _barsSincePivot < ZigZagBackstep)
				return;

			if (_currentPivot != 0m && !deviationOk && deviation < _zigZagDeviation)
				return;

			RegisterPivot(candle.LowPrice);
			_zigZagDirection = -1;
			_barsSincePivot = 0;
		}
	}

	private void RegisterPivot(decimal value)
	{
		if (_currentPivot == 0m)
		{
			_currentPivot = value;
			return;
		}

		if (_previousPivot == 0m)
		{
			_previousPivot = _currentPivot;
			_currentPivot = value;
			return;
		}

		_olderPivot = _previousPivot;
		_previousPivot = _currentPivot;
		_currentPivot = value;
	}

	private void UpdateEntryOrders(decimal buyPrice, decimal sellPrice, decimal stopDistance, decimal takeDistance, decimal volume)
	{
		if (volume <= 0m)
		{
			CancelEntryOrders();
			return;
		}

		if (buyPrice > 0m)
		{
			if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			{
				if (!ArePricesEqual(_buyStopOrder.Price, buyPrice) || !AreVolumesEqual(_buyStopOrder.Volume, volume))
				{
					CancelOrder(_buyStopOrder);
					_buyStopOrder = BuyStop(volume, buyPrice);
				}
			}
			else
			{
				_buyStopOrder = BuyStop(volume, buyPrice);
			}
		}

		if (sellPrice > 0m)
		{
			if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			{
				if (!ArePricesEqual(_sellStopOrder.Price, sellPrice) || !AreVolumesEqual(_sellStopOrder.Volume, volume))
				{
					CancelOrder(_sellStopOrder);
					_sellStopOrder = SellStop(volume, sellPrice);
				}
			}
			else
			{
				_sellStopOrder = SellStop(volume, sellPrice);
			}
		}

		_plannedStopDistance = stopDistance;
		_plannedTakeDistance = takeDistance;
	}

	private void UpdateBreakEven(ICandleMessage candle)
	{
		if (!EnableBreakEven || Position == 0m || _breakEvenTrigger <= 0m)
		return;

		if (_stopLossOrder == null)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
		return;

		var tolerance = _pipSize > 0m ? _pipSize / 2m : 0m;
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (Position > 0m)
		{
			var profit = candle.HighPrice - entryPrice;
			if (profit < _breakEvenTrigger)
			return;

			var currentStop = _stopLossOrder.Price;
			if (currentStop >= entryPrice + _breakEvenOffset - tolerance)
			return;

			CancelStopLossIfActive();
			var newStop = RoundPrice(entryPrice + _breakEvenOffset);
			if (newStop < entryPrice)
			newStop = entryPrice;
			_stopLossOrder = SellStop(volume, newStop);
		}
		else if (Position < 0m)
		{
			var profit = entryPrice - candle.LowPrice;
			if (profit < _breakEvenTrigger)
			return;

			var currentStop = _stopLossOrder.Price;
			if (currentStop <= entryPrice - _breakEvenOffset + tolerance)
			return;

			CancelStopLossIfActive();
			var newStop = RoundPrice(entryPrice - _breakEvenOffset);
			if (newStop > entryPrice)
			newStop = entryPrice;
			_stopLossOrder = BuyStop(volume, newStop);
		}
	}

	private void CancelStopLossIfActive()
	{
		if (_stopLossOrder != null && _stopLossOrder.State == OrderStates.Active)
		CancelOrder(_stopLossOrder);
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			_plannedStopDistance = 0m;
			_plannedTakeDistance = 0m;
			return;
		}

		CancelEntryOrders();
		RegisterProtection(Position > 0m);
	}

	private void RegisterProtection(bool isLong)
	{
		CancelProtectionOrders();

		if (_plannedStopDistance <= 0m)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
		return;

		var stopPrice = isLong
		? RoundPrice(entryPrice - _plannedStopDistance)
		: RoundPrice(entryPrice + _plannedStopDistance);

		_stopLossOrder = isLong
		? SellStop(volume, stopPrice)
		: BuyStop(volume, stopPrice);

		if (_plannedTakeDistance > 0m)
		{
			var takePrice = isLong
			? RoundPrice(entryPrice + _plannedTakeDistance)
			: RoundPrice(entryPrice - _plannedTakeDistance);

			_takeProfitOrder = isLong
			? SellLimit(volume, takePrice)
			: BuyLimit(volume, takePrice);
		}
	}

	private void CancelEntryOrders()
	{
		if (_buyStopOrder != null)
		{
			if (_buyStopOrder.State == OrderStates.Active)
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null)
		{
			if (_sellStopOrder.State == OrderStates.Active)
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}
	}

	private void CancelProtectionOrders()
	{
		if (_stopLossOrder != null)
		{
			if (_stopLossOrder.State == OrderStates.Active)
			CancelOrder(_stopLossOrder);
			_stopLossOrder = null;
		}

		if (_takeProfitOrder != null)
		{
			if (_takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);
			_takeProfitOrder = null;
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var stop = new TimeSpan(StopHour, StopMinute, 0);
		var current = time.TimeOfDay;

		if (stop > start)
		return current >= start && current <= stop;

		return current >= start || current <= stop;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return price;

		var ratio = price / step;
		var rounded = Math.Round(ratio, MidpointRounding.AwayFromZero);
		return rounded * step;
	}

	private bool ArePricesEqual(decimal left, decimal right)
	{
		var tolerance = (Security?.PriceStep ?? 0m) / 2m;
		if (tolerance <= 0m)
		tolerance = 0.0000001m;

		return Math.Abs(left - right) <= tolerance;
	}

	private bool AreVolumesEqual(decimal left, decimal right)
	{
		var tolerance = (Security?.VolumeStep ?? 0m) / 2m;
		if (tolerance <= 0m)
		tolerance = 0.0000001m;

		return Math.Abs(left - right) <= tolerance;
	}

	private decimal CalculateOrderVolume(decimal referencePrice)
	{
		var baseVolume = FixedVolume;

		if (RiskPercent <= 0m)
		return AdjustVolume(baseVolume);

		var security = Security;
		var portfolio = Portfolio;
		if (security == null || portfolio == null)
		return AdjustVolume(baseVolume);

		var capital = UseBalanceForRisk
		? (portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m)
		: (portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m) - (portfolio.BlockedValue ?? 0m);

		if (capital <= 0m || referencePrice <= 0m)
		return AdjustVolume(baseVolume);

		var volume = capital * (RiskPercent / 100m) / referencePrice;
		if (volume <= 0m)
		volume = baseVolume;

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep;
		if (step.HasValue && step.Value > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step.Value, MidpointRounding.AwayFromZero));
			volume = steps * step.Value;
		}

		var minVolume = security.MinVolume;
		if (minVolume.HasValue && minVolume.Value > 0m && volume < minVolume.Value)
		volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
		volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		return 1m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 1m;

		return security.Decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private decimal GetReferencePrice()
	{
		var security = Security;
		if (security == null)
		return 0m;

		if (security.LastPrice is decimal lastPrice && lastPrice > 0m)
		return lastPrice;

		var bid = security.BestBid?.Price ?? 0m;
		var ask = security.BestAsk?.Price ?? 0m;

		if (bid > 0m && ask > 0m)
		return (bid + ask) / 2m;

		if (bid > 0m)
		return bid;

		if (ask > 0m)
		return ask;

		return 0m;
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (_buyStopOrder != null && order == _buyStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_buyStopOrder = null;

		if (_sellStopOrder != null && order == _sellStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_sellStopOrder = null;

		if (_stopLossOrder != null && order == _stopLossOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_stopLossOrder = null;

		if (_takeProfitOrder != null && order == _takeProfitOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnOrderRegisterFailed(OrderFail fail, bool calcRisk)
	{
		base.OnOrderRegisterFailed(fail, calcRisk);

		if (_buyStopOrder != null && fail.Order == _buyStopOrder)
		_buyStopOrder = null;

		if (_sellStopOrder != null && fail.Order == _sellStopOrder)
		_sellStopOrder = null;

		if (_stopLossOrder != null && fail.Order == _stopLossOrder)
		_stopLossOrder = null;

		if (_takeProfitOrder != null && fail.Order == _takeProfitOrder)
		_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelEntryOrders();
		CancelProtectionOrders();
		base.OnStopped();
	}
}
