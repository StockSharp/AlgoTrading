using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that compares the current candle range against the previous one.
/// Opens a position when the current range slightly exceeds the previous range
/// and optionally reverses direction after a losing trade.
/// </summary>
public class BreakthroughVolatilityStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<bool> _onlyOnePositionPerBar;
	private readonly StrategyParam<bool> _useAutoDigits;
	private readonly StrategyParam<bool> _reverseAfterStop;
	private readonly StrategyParam<int> _maxReverseOrders;
	private readonly StrategyParam<decimal> _takeProfitIncrease;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousRange;
	private DateTimeOffset? _lastBuyBarTime;
	private DateTimeOffset? _lastSellBarTime;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private Sides? _activeSide;
	private int _activeReverseCount;
	private Sides? _pendingReverseSide;
	private int _pendingReverseCount;

	/// <summary>
	/// Trade volume for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum move in points required before shifting the trailing stop.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Limit to a single entry per bar when enabled.
	/// </summary>
	public bool OnlyOnePositionPerBar
	{
		get => _onlyOnePositionPerBar.Value;
		set => _onlyOnePositionPerBar.Value = value;
	}

	/// <summary>
	/// Apply automatic digit adjustment for pip calculations.
	/// </summary>
	public bool UseAutoDigits
	{
		get => _useAutoDigits.Value;
		set => _useAutoDigits.Value = value;
	}

	/// <summary>
	/// Enable reversing after a losing trade.
	/// </summary>
	public bool ReverseAfterStop
	{
		get => _reverseAfterStop.Value;
		set => _reverseAfterStop.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive reverse orders.
	/// </summary>
	public int MaxReverseOrders
	{
		get => _maxReverseOrders.Value;
		set => _maxReverseOrders.Value = value;
	}

	/// <summary>
	/// Additional take-profit points applied to each reverse order.
	/// </summary>
	public decimal TakeProfitIncrease
	{
		get => _takeProfitIncrease.Value;
		set => _takeProfitIncrease.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BreakthroughVolatilityStrategy"/>.
	/// </summary>
	public BreakthroughVolatilityStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base trade volume", "Orders")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 1m, 0.05m);

		_stopLossPoints = Param(nameof(StopLossPoints), 20)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 25)
			.SetDisplay("Trailing Stop", "Trailing distance in points", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5)
			.SetDisplay("Trailing Step", "Minimum trailing increment", "Risk");

		_onlyOnePositionPerBar = Param(nameof(OnlyOnePositionPerBar), true)
			.SetDisplay("One Position Per Bar", "Prevent multiple entries per bar", "Orders");

		_useAutoDigits = Param(nameof(UseAutoDigits), true)
			.SetDisplay("Auto Digits", "Adjust pip value automatically", "General");

		_reverseAfterStop = Param(nameof(ReverseAfterStop), true)
			.SetDisplay("Reverse After Loss", "Reverse direction after stop loss", "Orders");

		_maxReverseOrders = Param(nameof(MaxReverseOrders), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Reverses", "Maximum consecutive reverse orders", "Orders");

		_takeProfitIncrease = Param(nameof(TakeProfitIncrease), 100m)
			.SetDisplay("TP Increase", "Extra take-profit points per reverse", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for analysis", "General");
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
		_previousRange = null;
		_lastBuyBarTime = null;
		_lastSellBarTime = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_activeSide = null;
		_activeReverseCount = 0;
		_pendingReverseSide = null;
		_pendingReverseCount = 0;
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

		TryExecutePendingReverse(candle);
		ManageOpenPosition(candle);

		var currentRange = candle.HighPrice - candle.LowPrice;
		if (_previousRange.HasValue)
		{
			var pip = GetPipSize();
			var maxRange = _previousRange.Value + 2m * pip;
			var expanded = currentRange > _previousRange.Value && currentRange < maxRange;

			if (expanded && IsFormedAndOnlineAndAllowTrading())
			{
				if (candle.ClosePrice > candle.OpenPrice)
					TryEnterLong(candle);
				else if (candle.ClosePrice < candle.OpenPrice)
					TryEnterShort(candle);
			}
		}

		_previousRange = currentRange;
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (OnlyOnePositionPerBar && Position != 0)
			return;

		if (_activeSide == Sides.Sell)
			return;

		if (OnlyOnePositionPerBar && _lastBuyBarTime == candle.OpenTime)
			return;

		if (!OnlyOnePositionPerBar && Position > 0)
			return;

		OpenPosition(Sides.Buy, candle.ClosePrice, 0);
		_lastBuyBarTime = candle.OpenTime;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (OnlyOnePositionPerBar && Position != 0)
			return;

		if (_activeSide == Sides.Buy)
			return;

		if (OnlyOnePositionPerBar && _lastSellBarTime == candle.OpenTime)
			return;

		if (!OnlyOnePositionPerBar && Position < 0)
			return;

		OpenPosition(Sides.Sell, candle.ClosePrice, 0);
		_lastSellBarTime = candle.OpenTime;
	}

	private void OpenPosition(Sides side, decimal price, int reverseCount)
	{
		var volume = TradeVolume + Math.Abs(Position);
		if (volume <= 0)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_activeSide = side;
		_activeReverseCount = reverseCount;
		_entryPrice = price;

		var pip = GetPipSize();
		_stopPrice = StopLossPoints > 0
			? side == Sides.Buy
				? price - StopLossPoints * pip
				: price + StopLossPoints * pip
			: (decimal?)null;

		var tpPoints = TakeProfitPoints + TakeProfitIncrease * reverseCount;
		_takeProfitPrice = tpPoints > 0
			? side == Sides.Buy
				? price + tpPoints * pip
				: price - tpPoints * pip
			: (decimal?)null;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (_activeSide == null || !_entryPrice.HasValue)
			return;

		UpdateTrailingStop(candle);

		var exit = false;
		var exitPrice = candle.ClosePrice;
		var isLoss = false;

		switch (_activeSide)
		{
			case Sides.Buy:
				if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
				{
					exit = true;
					exitPrice = _stopPrice.Value;
					isLoss = exitPrice <= _entryPrice.Value;
				}
				else if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
				{
					exit = true;
					exitPrice = _takeProfitPrice.Value;
					isLoss = exitPrice <= _entryPrice.Value;
				}
				break;
			case Sides.Sell:
				if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
				{
					exit = true;
					exitPrice = _stopPrice.Value;
					isLoss = exitPrice >= _entryPrice.Value;
				}
				else if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
				{
					exit = true;
					exitPrice = _takeProfitPrice.Value;
					isLoss = exitPrice >= _entryPrice.Value;
				}
				break;
		}

		if (!exit)
			return;

		ClosePosition();
		ScheduleReverse(isLoss);
		ResetPositionState();
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0 || _activeSide == null || !_entryPrice.HasValue)
			return;

		var pip = GetPipSize();
		var trailingDistance = TrailingStopPoints * pip;
		var stepDistance = TrailingStepPoints > 0 ? TrailingStepPoints * pip : 0m;

		if (_activeSide == Sides.Buy)
		{
			var move = candle.ClosePrice - _entryPrice.Value;
			if (move <= trailingDistance)
				return;

			var newStop = candle.ClosePrice - trailingDistance;
			if (!_stopPrice.HasValue || newStop > _stopPrice.Value + stepDistance)
				_stopPrice = _stopPrice.HasValue ? Math.Max(_stopPrice.Value, newStop) : newStop;
		}
		else
		{
			var move = _entryPrice.Value - candle.ClosePrice;
			if (move <= trailingDistance)
				return;

			var newStop = candle.ClosePrice + trailingDistance;
			if (!_stopPrice.HasValue || newStop < _stopPrice.Value - stepDistance)
				_stopPrice = _stopPrice.HasValue ? Math.Min(_stopPrice.Value, newStop) : newStop;
		}
	}

	private void ScheduleReverse(bool isLoss)
	{
		if (!isLoss)
		{
			_activeReverseCount = 0;
			_pendingReverseSide = null;
			_pendingReverseCount = 0;
			return;
		}

		if (!ReverseAfterStop || _activeSide == null)
		{
			_activeReverseCount = 0;
			_pendingReverseSide = null;
			_pendingReverseCount = 0;
			return;
		}

		if (_activeReverseCount >= MaxReverseOrders)
		{
			_activeReverseCount = 0;
			_pendingReverseSide = null;
			_pendingReverseCount = 0;
			return;
		}

		_pendingReverseSide = _activeSide == Sides.Buy ? Sides.Sell : Sides.Buy;
		_pendingReverseCount = _activeReverseCount + 1;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_activeSide = null;
	}

	private void TryExecutePendingReverse(ICandleMessage candle)
	{
		if (_pendingReverseSide == null || Position != 0)
			return;

		OpenPosition(_pendingReverseSide.Value, candle.ClosePrice, _pendingReverseCount);
		_pendingReverseSide = null;
		_pendingReverseCount = 0;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		if (step <= 0)
			step = 0.0001m;

		if (!UseAutoDigits)
			return step;

		var decimals = security.Decimals ?? (int)Math.Round(Math.Log10((double)(1m / step)));
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}
}