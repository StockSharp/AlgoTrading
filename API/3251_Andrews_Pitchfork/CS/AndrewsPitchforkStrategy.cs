using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Andrew's Pitchfork" MetaTrader expert advisor.
/// </summary>
public class AndrewsPitchforkStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _maxPyramids;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<int> _takeProfitSteps;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<int> _trailingTriggerSteps;
	private readonly StrategyParam<int> _trailingDistanceSteps;
	private readonly StrategyParam<int> _trailingPadSteps;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerSteps;
	private readonly StrategyParam<int> _breakEvenOffsetSteps;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<decimal> _momentumHistory = new();

	private decimal? _fastValue;
	private decimal? _slowValue;
	private decimal? _macdLine;
	private decimal? _macdSignal;
	private DateTimeOffset? _trendCandleTime;
	private DateTimeOffset? _macdCandleTime;
	private DateTimeOffset? _processedCandleTime;
	private ICandleMessage? _lastCandle;

	private Order? _stopOrder;
	private Order? _takeProfitOrder;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="AndrewsPitchforkStrategy"/> class.
	/// </summary>
	public AndrewsPitchforkStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for trading signals", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetRange(1, 100)
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Indicators")
		.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetRange(2, 200)
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Indicators")
		.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetRange(5, 60)
		.SetDisplay("Momentum Length", "Lookback for the Momentum indicator", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetDisplay("Buy Momentum Threshold", "Minimum |Momentum - 100| for long entries", "Filters");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetDisplay("Sell Momentum Threshold", "Minimum |Momentum - 100| for short entries", "Filters");

		_maxPyramids = Param(nameof(MaxPyramids), 1)
		.SetRange(1, 10)
		.SetDisplay("Max Position Units", "How many base lots can be stacked in the same direction", "Risk");

		_stopLossSteps = Param(nameof(StopLossSteps), 20)
		.SetRange(5, 200)
		.SetDisplay("Stop Loss (steps)", "Initial stop loss distance expressed in price steps", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50)
		.SetRange(5, 400)
		.SetDisplay("Take Profit (steps)", "Initial take profit distance expressed in price steps", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Enables dynamic trailing stop management", "Risk");

		_trailingTriggerSteps = Param(nameof(TrailingTriggerSteps), 40)
		.SetRange(1, 500)
		.SetDisplay("Trailing Trigger", "Profit in steps required before trailing starts", "Risk");

		_trailingDistanceSteps = Param(nameof(TrailingDistanceSteps), 40)
		.SetRange(1, 500)
		.SetDisplay("Trailing Distance", "Distance in steps between price extreme and trailing stop", "Risk");

		_trailingPadSteps = Param(nameof(TrailingPadSteps), 10)
		.SetRange(0, 200)
		.SetDisplay("Trailing Pad", "Additional buffer in steps for the trailing stop", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Enable Break-Even", "Moves stop loss to break-even after sufficient profit", "Risk");

		_breakEvenTriggerSteps = Param(nameof(BreakEvenTriggerSteps), 30)
		.SetRange(1, 500)
		.SetDisplay("Break-Even Trigger", "Profit in steps required before moving to break-even", "Risk");

		_breakEvenOffsetSteps = Param(nameof(BreakEvenOffsetSteps), 30)
		.SetRange(0, 500)
		.SetDisplay("Break-Even Offset", "Offset in steps beyond entry when break-even triggers", "Risk");
	}

	/// <summary>
	/// Primary candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator lookback.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation to allow long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation to allow short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of base position units that can be accumulated.
	/// </summary>
	public int MaxPyramids
	{
		get => _maxPyramids.Value;
		set => _maxPyramids.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Enables dynamic trailing stop handling.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit in steps required before trailing starts.
	/// </summary>
	public int TrailingTriggerSteps
	{
		get => _trailingTriggerSteps.Value;
		set => _trailingTriggerSteps.Value = value;
	}

	/// <summary>
	/// Distance between price extreme and trailing stop in steps.
	/// </summary>
	public int TrailingDistanceSteps
	{
		get => _trailingDistanceSteps.Value;
		set => _trailingDistanceSteps.Value = value;
	}

	/// <summary>
	/// Additional padding applied to the trailing stop in steps.
	/// </summary>
	public int TrailingPadSteps
	{
		get => _trailingPadSteps.Value;
		set => _trailingPadSteps.Value = value;
	}

	/// <summary>
	/// Enables moving the stop to break-even.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit required before moving the stop to break-even.
	/// </summary>
	public int BreakEvenTriggerSteps
	{
		get => _breakEvenTriggerSteps.Value;
		set => _breakEvenTriggerSteps.Value = value;
	}

	/// <summary>
	/// Offset placed beyond entry price when break-even activates.
	/// </summary>
	public int BreakEvenOffsetSteps
	{
		get => _breakEvenOffsetSteps.Value;
		set => _breakEvenOffsetSteps.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new LinearWeightedMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new LinearWeightedMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(_fastMa, _slowMa, _momentum, ProcessTrendValues)
		.BindEx(_macd, ProcessMacd)
		.Start();

		StartProtectionIfNeeded();
	}

	private void ProcessTrendValues(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_fastValue = fastValue;
		_slowValue = slowValue;
		_lastCandle = candle;
		_trendCandleTime = candle.CloseTime;

		UpdateMomentumHistory(momentumValue);

		TryProcessSignal();
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		_macdLine = typed.Macd;
		_macdSignal = typed.Signal;
		_macdCandleTime = candle.CloseTime;

		TryProcessSignal();
	}

	private void TryProcessSignal()
	{
		if (_lastCandle is not { } candle)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_fastValue.HasValue || !_slowValue.HasValue || !_macdLine.HasValue || !_macdSignal.HasValue)
		return;

		if (_trendCandleTime != _macdCandleTime)
		return;

		if (_processedCandleTime == _trendCandleTime)
		return;

		_processedCandleTime = _trendCandleTime;

		ProcessSignals(candle);
	}

	private void ProcessSignals(ICandleMessage candle)
	{
		var fast = _fastValue!.Value;
		var slow = _slowValue!.Value;
		var macd = _macdLine!.Value;
		var signal = _macdSignal!.Value;

		var bullishMa = fast > slow;
		var bearishMa = fast < slow;

		var bullishMomentum = HasMomentumImpulse(MomentumBuyThreshold);
		var bearishMomentum = HasMomentumImpulse(MomentumSellThreshold);

		var bullishMacd = macd > signal;
		var bearishMacd = macd < signal;

		var maxVolume = Volume * MaxPyramids;

		if (bullishMa && bullishMomentum && bullishMacd)
		{
			EnterLong(candle, maxVolume);
		}
		else if (bearishMa && bearishMomentum && bearishMacd)
		{
			EnterShort(candle, maxVolume);
		}

		ManageOpenPosition(candle);
	}

	private void EnterLong(ICandleMessage candle, decimal maxVolume)
	{
		if (Volume <= 0)
		return;

		if (Position >= maxVolume)
		return;

		CancelProtectionOrders();

		var volume = Volume;
		if (Position < 0)
		volume += Math.Abs(Position);

		BuyMarket(volume);

		_entryPrice = candle.ClosePrice;
		_highestPrice = candle.ClosePrice;
		_lowestPrice = candle.ClosePrice;

		PlaceInitialProtection(true);
	}

	private void EnterShort(ICandleMessage candle, decimal maxVolume)
	{
		if (Volume <= 0)
		return;

		if (-Position >= maxVolume)
		return;

		CancelProtectionOrders();

		var volume = Volume;
		if (Position > 0)
		volume += Position;

		SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		_highestPrice = candle.ClosePrice;
		_lowestPrice = candle.ClosePrice;

		PlaceInitialProtection(false);
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
		return;

		var step = GetPriceStep();
		if (step <= 0)
		return;

		if (_entryPrice is not decimal entry)
		return;

		_highestPrice = Math.Max(_highestPrice, candle.HighPrice ?? candle.ClosePrice);
		_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice ?? candle.ClosePrice);

		if (EnableBreakEven)
		{
			ApplyBreakEven(entry, step);
		}

		if (EnableTrailing)
		{
			ApplyTrailing(entry, step);
		}
	}

	private void ApplyBreakEven(decimal entryPrice, decimal step)
	{
		if (Position > 0)
		{
			var trigger = entryPrice + BreakEvenTriggerSteps * step;
			var offsetPrice = entryPrice + BreakEvenOffsetSteps * step;

			if (_highestPrice > trigger)
			{
				if (_stopPrice is null || offsetPrice > _stopPrice.Value)
				MoveStop(Sides.Sell, offsetPrice);
			}
		}
		else if (Position < 0)
		{
			var trigger = entryPrice - BreakEvenTriggerSteps * step;
			var offsetPrice = entryPrice - BreakEvenOffsetSteps * step;

			if (_lowestPrice < trigger)
			{
				if (_stopPrice is null || offsetPrice < _stopPrice.Value)
				MoveStop(Sides.Buy, offsetPrice);
			}
		}
	}

	private void ApplyTrailing(decimal entryPrice, decimal step)
	{
		var triggerDistance = TrailingTriggerSteps * step;
		var trailDistance = TrailingDistanceSteps * step;
		var pad = TrailingPadSteps * step;

		if (Position > 0)
		{
			var profit = _highestPrice - entryPrice;
			if (profit > triggerDistance)
			{
				var desired = _highestPrice - trailDistance - pad;
				if (_stopPrice is null || desired > _stopPrice.Value)
				MoveStop(Sides.Sell, desired);
			}
		}
		else if (Position < 0)
		{
			var profit = entryPrice - _lowestPrice;
			if (profit > triggerDistance)
			{
				var desired = _lowestPrice + trailDistance + pad;
				if (_stopPrice is null || desired < _stopPrice.Value)
				MoveStop(Sides.Buy, desired);
			}
		}
	}

	private void UpdateMomentumHistory(decimal momentumValue)
	{
		var deviation = Math.Abs(100m - momentumValue);
		_momentumHistory.Enqueue(deviation);

		while (_momentumHistory.Count > 3)
		_momentumHistory.Dequeue();
	}

	private bool HasMomentumImpulse(decimal threshold)
	{
		foreach (var value in _momentumHistory)
		{
			if (value >= threshold)
			return true;
		}

		return false;
	}

	private void PlaceInitialProtection(bool isLong)
	{
		var step = GetPriceStep();
		if (step <= 0 || _entryPrice is not decimal entry)
		return;

		if (isLong)
		{
			var stop = entry - StopLossSteps * step;
			var take = entry + TakeProfitSteps * step;
			MoveStop(Sides.Sell, stop);
			PlaceTakeProfit(Sides.Sell, take);
		}
		else
		{
			var stop = entry + StopLossSteps * step;
			var take = entry - TakeProfitSteps * step;
			MoveStop(Sides.Buy, stop);
			PlaceTakeProfit(Sides.Buy, take);
		}
	}

	private void MoveStop(Sides side, decimal price)
	{
		CancelStop();

		var volume = Math.Abs(Position);
		if (volume <= 0)
		return;

		_stopOrder = side == Sides.Sell
		? SellStop(volume, price)
		: BuyStop(volume, price);

		_stopPrice = price;
	}

	private void PlaceTakeProfit(Sides side, decimal price)
	{
		CancelTakeProfit();

		var volume = Math.Abs(Position);
		if (volume <= 0)
		return;

		_takeProfitOrder = side == Sides.Sell
		? SellLimit(volume, price)
		: BuyLimit(volume, price);

		_takeProfitPrice = price;
	}

	private void CancelProtectionOrders()
	{
		CancelStop();
		CancelTakeProfit();
	}

	private void CancelStop()
	{
		if (_stopOrder is null)
		return;

		CancelOrder(_stopOrder);
		_stopOrder = null;
		_stopPrice = null;
	}

	private void CancelTakeProfit()
	{
		if (_takeProfitOrder is null)
		return;

		CancelOrder(_takeProfitOrder);
		_takeProfitOrder = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			ResetState();
		}
	}

	private void ResetState()
	{
		CancelProtectionOrders();
		_entryPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_processedCandleTime = null;
		_trendCandleTime = null;
		_macdCandleTime = null;
		_fastValue = null;
		_slowValue = null;
		_macdLine = null;
		_macdSignal = null;
		_lastCandle = null;
		_momentumHistory.Clear();
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 0m;
	}

	private void StartProtectionIfNeeded()
	{
		if (Position == 0)
		return;

		_entryPrice ??= Security?.LastTrade?.Price;

		if (_entryPrice is null)
		return;

		PlaceInitialProtection(Position > 0);
	}
}
