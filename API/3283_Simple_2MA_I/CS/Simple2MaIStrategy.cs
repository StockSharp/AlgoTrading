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
/// Linear weighted moving average crossover strategy with momentum and MACD filters.
/// The strategy mirrors the Simple 2 MA I expert logic with trailing stop and break-even management.
/// </summary>
public class Simple2MaIStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenOffsetPoints;
	private readonly StrategyParam<bool> _enableCandleTrailing;
	private readonly StrategyParam<decimal> _trailingActivationPoints;
	private readonly StrategyParam<decimal> _trailingPaddingPoints;
	private readonly StrategyParam<int> _maxNetVolume;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private RateOfChange _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private Order _stopOrder;
	private Order _takeProfitOrder;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal _point;

	private decimal? _momentumCurrent;
	private decimal? _momentumPrevious;
	private decimal? _momentumEarlier;
	private ICandleMessage _previousCandle;
	private ICandleMessage _earlierCandle;

	/// <summary>
	/// Initializes a new instance of the <see cref="Simple2MaIStrategy"/> class.
	/// </summary>
	public Simple2MaIStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for signals", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Moving Averages")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Moving Averages")
		.SetCanOptimize(true)
		.SetOptimize(30, 150, 5);

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Lookback for rate of change calculation", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 2);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Threshold", "Minimal absolute rate of change to confirm entries", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1.0m, 0.1m);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length inside the MACD", "Filters");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length inside the MACD", "Filters");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length inside the MACD", "Filters");

		_enableStopLoss = Param(nameof(EnableStopLoss), true)
		.SetDisplay("Use Stop-Loss", "Place protective stop orders", "Risk Management");

		_enableTakeProfit = Param(nameof(EnableTakeProfit), true)
		.SetDisplay("Use Take-Profit", "Place profit target orders", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop-Loss (points)", "Distance from entry for stop-loss", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take-Profit (points)", "Distance from entry for take-profit", "Risk Management");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Use Break-Even", "Move stop to break-even after sufficient profit", "Risk Management");

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Break-Even Trigger", "Profit in points required before break-even", "Risk Management");

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Break-Even Offset", "Offset in points added when moving stop", "Risk Management");

		_enableCandleTrailing = Param(nameof(EnableCandleTrailing), true)
		.SetDisplay("Use Candle Trailing", "Trail stops using candle extremes", "Risk Management");

		_trailingActivationPoints = Param(nameof(TrailingActivationPoints), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Activation", "Profit in points before candle trailing activates", "Risk Management");

		_trailingPaddingPoints = Param(nameof(TrailingPaddingPoints), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Padding", "Extra distance added below/above candle extremum", "Risk Management");

		_maxNetVolume = Param(nameof(MaxNetVolume), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Net Volume", "Maximum absolute net volume allowed", "General");
	}

	/// <summary>
	/// Selected candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	public bool EnableTakeProfit
	{
		get => _enableTakeProfit.Value;
		set => _enableTakeProfit.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	public decimal BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	public bool EnableCandleTrailing
	{
		get => _enableCandleTrailing.Value;
		set => _enableCandleTrailing.Value = value;
	}

	public decimal TrailingActivationPoints
	{
		get => _trailingActivationPoints.Value;
		set => _trailingActivationPoints.Value = value;
	}

	public decimal TrailingPaddingPoints
	{
		get => _trailingPaddingPoints.Value;
		set => _trailingPaddingPoints.Value = value;
	}

	public int MaxNetVolume
	{
		get => _maxNetVolume.Value;
		set => _maxNetVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stopOrder = null;
		_takeProfitOrder = null;
		_entryPrice = null;
		_stopPrice = null;
		_point = 0m;
		_momentumCurrent = null;
		_momentumPrevious = null;
		_momentumEarlier = null;
		_previousCandle = null;
		_earlierCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security.PriceStep ?? 1m;

		_fastMa = new WeightedMovingAverage
		{
			Length = FastMaLength,
			CandlePrice = CandlePrice.Typical,
		};

		_slowMa = new WeightedMovingAverage
		{
			Length = SlowMaLength,
			CandlePrice = CandlePrice.Typical,
		};

		_momentum = new RateOfChange { Length = MomentumLength };

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _fastMa, _slowMa, _momentum, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue momentumValue)
	{
		// Work only with finished candles to match original bar-on-close logic.
		if (candle.State != CandleStates.Finished)
		return;

		// Skip until all indicators are ready.
		if (!_macd.IsFormed || !_fastMa.IsFormed || !_slowMa.IsFormed || !_momentum.IsFormed)
		{
			UpdateMomentum(momentumValue);
			UpdateCandleHistory(candle);
			return;
		}

		UpdateMomentum(momentumValue);
		UpdateCandleHistory(candle);

		if (!macdValue.IsFinal || !fastValue.IsFinal || !slowValue.IsFinal)
		return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
		return;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal macdSignal)
		return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var momentum = _momentumCurrent;
		var prevMomentum = _momentumPrevious;
		var earlierMomentum = _momentumEarlier;

		if (momentum is null || prevMomentum is null || earlierMomentum is null)
		return;

		// The strategy requires at least one of the last three momentum values to exceed the threshold.
		var momentumFilter = Math.Abs(momentum.Value) >= MomentumThreshold
		|| Math.Abs(prevMomentum.Value) >= MomentumThreshold
		|| Math.Abs(earlierMomentum.Value) >= MomentumThreshold;

		if (!momentumFilter)
		return;

		// Check candle relation similar to the original low/high comparison.
		var hasHistory = _previousCandle is not null && _earlierCandle is not null;

		var bullishStructure = hasHistory && _earlierCandle!.LowPrice < _previousCandle!.HighPrice;
		var bearishStructure = hasHistory && _previousCandle!.LowPrice < _earlierCandle!.HighPrice;

		// Determine whether there is room for additional volume.
		var canIncreaseLong = Position <= 0 && Math.Abs(Position) < MaxNetVolume;
		var canIncreaseShort = Position >= 0 && Math.Abs(Position) < MaxNetVolume;

		var buySignal = canIncreaseLong && fast > slow && macdLine > macdSignal && bullishStructure;
		var sellSignal = canIncreaseShort && fast < slow && macdLine < macdSignal && bearishStructure;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (buySignal)
		{
			// Close short exposure before opening a new long position.
			if (Position < 0)
			BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
		}
		else if (sellSignal)
		{
			// Close long exposure before opening a new short position.
			if (Position > 0)
			SellMarket(Math.Abs(Position));

			SellMarket(Volume);
		}

		ManageRisk(candle);
	}

	private void UpdateMomentum(IIndicatorValue momentumValue)
	{
		if (!momentumValue.IsFinal)
		return;

		_momentumEarlier = _momentumPrevious;
		_momentumPrevious = _momentumCurrent;
		_momentumCurrent = Math.Abs(momentumValue.ToDecimal());
	}

	private void UpdateCandleHistory(ICandleMessage candle)
	{
		_earlierCandle = _previousCandle;
		_previousCandle = candle;
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (Position == 0)
		{
			CancelProtectionOrders();
			return;
		}

		if (_entryPrice is null)
		return;

		var entry = _entryPrice.Value;
		var volume = Math.Abs(Position);

		if (volume <= 0m)
		return;

		var stopDistance = StopLossPoints * _point;
		var takeDistance = TakeProfitPoints * _point;

		if (EnableStopLoss && stopDistance > 0m)
		{
			var desiredStop = Position > 0
			? entry - stopDistance
			: entry + stopDistance;

			if (_stopPrice is null)
			MoveStop(Position > 0 ? Sides.Sell : Sides.Buy, desiredStop, volume);
		}

		if (EnableTakeProfit && takeDistance > 0m && _takeProfitOrder is null)
		{
			var takeProfitPrice = Position > 0
			? entry + takeDistance
			: entry - takeDistance;

			_takeProfitOrder = Position > 0
			? SellLimit(volume, takeProfitPrice)
			: BuyLimit(volume, takeProfitPrice);
		}

		if (EnableBreakEven)
		ApplyBreakEven(candle, entry, volume);

		if (EnableCandleTrailing)
		ApplyCandleTrailing(candle, entry, volume);
	}

	private void ApplyBreakEven(ICandleMessage candle, decimal entry, decimal volume)
	{
		var trigger = BreakEvenTriggerPoints * _point;
		var offset = BreakEvenOffsetPoints * _point;

		if (trigger <= 0m)
		return;

		if (Position > 0)
		{
			var reached = candle.HighPrice - entry >= trigger;
			if (reached)
			{
				var newStop = entry + offset;
				if (_stopPrice is null || newStop > _stopPrice.Value)
				MoveStop(Sides.Sell, newStop, volume);
			}
		}
		else if (Position < 0)
		{
			var reached = entry - candle.LowPrice >= trigger;
			if (reached)
			{
				var newStop = entry - offset;
				if (_stopPrice is null || newStop < _stopPrice.Value)
				MoveStop(Sides.Buy, newStop, volume);
			}
		}
	}

	private void ApplyCandleTrailing(ICandleMessage candle, decimal entry, decimal volume)
	{
		var activation = TrailingActivationPoints * _point;
		var padding = TrailingPaddingPoints * _point;

		if (activation <= 0m)
		return;

		if (Position > 0)
		{
			if (candle.ClosePrice - entry < activation)
			return;

			var newStop = candle.LowPrice - padding;
			if (_stopPrice is null || newStop > _stopPrice.Value)
			MoveStop(Sides.Sell, newStop, volume);
		}
		else if (Position < 0)
		{
			if (entry - candle.ClosePrice < activation)
			return;

			var newStop = candle.HighPrice + padding;
			if (_stopPrice is null || newStop < _stopPrice.Value)
			MoveStop(Sides.Buy, newStop, volume);
		}
	}

	private void MoveStop(Sides side, decimal price, decimal volume)
	{
		if (volume <= 0m)
		return;

		if (_stopOrder != null)
		CancelOrder(_stopOrder);

		_stopOrder = side == Sides.Sell
		? SellStop(volume, price)
		: BuyStop(volume, price);

		_stopPrice = price;
	}

	private void CancelProtectionOrders()
	{
		if (_stopOrder != null)
		{
			CancelOrder(_stopOrder);
			_stopOrder = null;
		}

		if (_takeProfitOrder != null)
		{
			CancelOrder(_takeProfitOrder);
			_takeProfitOrder = null;
		}

		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade?.Order;
		var execution = trade?.Trade;

		if (order?.Security != Security || execution?.Price is null)
		return;

		var price = execution.Price.Value;

		// Update entry price when a new position is opened or reversed.
		if (order.Direction == Sides.Buy && Position > 0)
		{
			_entryPrice = price;
			CancelProtectionOrders();
		}
		else if (order.Direction == Sides.Sell && Position < 0)
		{
			_entryPrice = price;
			CancelProtectionOrders();
		}

		var candle = _previousCandle ?? CreateSyntheticCandle(price, execution.Time);
		ManageRisk(candle);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			CancelProtectionOrders();
			_entryPrice = null;
		}
	}

	private ICandleMessage CreateSyntheticCandle(decimal price, DateTimeOffset time)
	{
		return new TimeFrameCandleMessage
		{
			SecurityId = Security?.Id ?? default,
			OpenTime = time,
			CloseTime = time,
			OpenPrice = price,
			HighPrice = price,
			LowPrice = price,
			ClosePrice = price,
			State = CandleStates.Finished,
		};
	}
}

