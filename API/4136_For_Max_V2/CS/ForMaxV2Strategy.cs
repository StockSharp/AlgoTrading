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
/// Port of the MetaTrader 4 expert advisor for_max_v2.
/// Places symmetrical stop orders around recent candles and manages trailing exits.
/// </summary>
public class ForMaxV2Strategy : Strategy
{
	private readonly StrategyParam<int> _buyTakeProfitPoints;
	private readonly StrategyParam<int> _sellTakeProfitPoints;
	private readonly StrategyParam<int> _gapPoints;
	private readonly StrategyParam<int> _maxSearchBars;
	private readonly StrategyParam<int> _orderExpirationFactor;
	private readonly StrategyParam<int> _breakEvenTriggerPoints;
	private readonly StrategyParam<int> _breakEvenOffsetPoints;
	private readonly StrategyParam<int> _trailingBufferLongPoints;
	private readonly StrategyParam<int> _trailingBufferShortPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<bool> _trailOnlyAfterProfit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleSnapshot> _history = new();
	private readonly PendingStraddle _type1Straddle = new();
	private readonly PendingStraddle _type2Straddle = new();

	private decimal _pointSize;
	private decimal _longHighestPrice;
	private decimal _shortLowestPrice;
	private decimal _previousPosition;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _nextLongStop;
	private decimal? _nextLongTake;
	private decimal? _nextShortStop;
	private decimal? _nextShortTake;
	private bool _longBreakEvenApplied;
	private bool _shortBreakEvenApplied;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	public ForMaxV2Strategy()
	{

		_buyTakeProfitPoints = Param(nameof(BuyTakeProfitPoints), 100)
			.SetDisplay("Buy Take Profit (points)", "Take-profit distance for long trades expressed in points.", "Entries")
			.SetCanOptimize(true);

		_sellTakeProfitPoints = Param(nameof(SellTakeProfitPoints), 100)
			.SetDisplay("Sell Take Profit (points)", "Take-profit distance for short trades expressed in points.", "Entries")
			.SetCanOptimize(true);

		_gapPoints = Param(nameof(GapPoints), 1)
			.SetDisplay("Gap (points)", "Offset added to breakout stop orders beyond the reference candle extremum.", "Entries")
			.SetCanOptimize(true);

		_maxSearchBars = Param(nameof(MaxSearchBars), 100)
			.SetDisplay("Search Depth", "Number of historical candles scanned for the engulfing setup.", "Entries")
			.SetCanOptimize(true);

		_orderExpirationFactor = Param(nameof(OrderExpirationFactor), 8)
			.SetDisplay("Order Expiry (bars)", "Lifetime of pending orders measured in candle multiples.", "Entries")
			.SetCanOptimize(true);

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 25)
			.SetDisplay("Break-even Trigger (points)", "Profit in points required before the stop is moved to secure profit.", "Risk Management")
			.SetCanOptimize(true);

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 1)
			.SetDisplay("Break-even Offset (points)", "Distance from the entry used when arming the break-even stop.", "Risk Management")
			.SetCanOptimize(true);

		_trailingBufferLongPoints = Param(nameof(TrailingBufferLongPoints), 50)
			.SetDisplay("Long Trailing Buffer (points)", "Distance maintained between the price extreme and the long stop.", "Risk Management")
			.SetCanOptimize(true);

		_trailingBufferShortPoints = Param(nameof(TrailingBufferShortPoints), 35)
			.SetDisplay("Short Trailing Buffer (points)", "Distance maintained between the price extreme and the short stop.", "Risk Management")
			.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 3)
			.SetDisplay("Trailing Step (points)", "Minimum improvement required before tightening the trailing stop.", "Risk Management")
			.SetCanOptimize(true);

		_trailOnlyAfterProfit = Param(nameof(TrailOnlyAfterProfit), true)
			.SetDisplay("Trail Only After Profit", "Enable trailing only once the trade is already in profit.", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that drives the strategy calculations.", "General");
	}


	/// <summary>
	/// Take-profit distance for long trades expressed in points.
	/// </summary>
	public int BuyTakeProfitPoints
	{
		get => _buyTakeProfitPoints.Value;
		set => _buyTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades expressed in points.
	/// </summary>
	public int SellTakeProfitPoints
	{
		get => _sellTakeProfitPoints.Value;
		set => _sellTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Offset added to breakout stop orders beyond the reference candle extremum.
	/// </summary>
	public int GapPoints
	{
		get => _gapPoints.Value;
		set => _gapPoints.Value = value;
	}

	/// <summary>
	/// Number of historical candles scanned for the engulfing setup.
	/// </summary>
	public int MaxSearchBars
	{
		get => _maxSearchBars.Value;
		set => _maxSearchBars.Value = value;
	}

	/// <summary>
	/// Lifetime of pending orders measured in candle multiples.
	/// </summary>
	public int OrderExpirationFactor
	{
		get => _orderExpirationFactor.Value;
		set => _orderExpirationFactor.Value = value;
	}

	/// <summary>
	/// Profit in points required before the stop is moved to secure profit.
	/// </summary>
	public int BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Distance from the entry used when arming the break-even stop.
	/// </summary>
	public int BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Distance maintained between the price extreme and the long stop.
	/// </summary>
	public int TrailingBufferLongPoints
	{
		get => _trailingBufferLongPoints.Value;
		set => _trailingBufferLongPoints.Value = value;
	}

	/// <summary>
	/// Distance maintained between the price extreme and the short stop.
	/// </summary>
	public int TrailingBufferShortPoints
	{
		get => _trailingBufferShortPoints.Value;
		set => _trailingBufferShortPoints.Value = value;
	}

	/// <summary>
	/// Minimum improvement required before tightening the trailing stop.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enable trailing only once the trade is already in profit.
	/// </summary>
	public bool TrailOnlyAfterProfit
	{
		get => _trailOnlyAfterProfit.Value;
		set => _trailOnlyAfterProfit.Value = value;
	}

	/// <summary>
	/// Timeframe that drives the strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_history.Clear();
		_type1Straddle.Reset();
		_type2Straddle.Reset();

		_pointSize = 0m;
		_longHighestPrice = 0m;
		_shortLowestPrice = 0m;
		_previousPosition = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_nextLongStop = null;
		_nextLongTake = null;
		_nextShortStop = null;
		_nextShortTake = null;
		_longBreakEvenApplied = false;
		_shortBreakEvenApplied = false;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order is null)
			return;

		switch (order.State)
		{
			case OrderStates.Done:
				HandleOrderDone(order);
				break;
			case OrderStates.Canceled:
			case OrderStates.Failed:
				HandleOrderTerminated(order);
				break;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		var previous = _previousPosition;

		base.OnPositionReceived(position);

		var current = Position;

		if (current > 0m)
		{
			if (previous <= 0m)
			{
				InitializeLongState();
				CancelStraddleOrders(_type1Straddle, false, true);
				CancelStraddleOrders(_type2Straddle, false, true);
			}
		}
		else if (current < 0m)
		{
			if (previous >= 0m)
			{
				InitializeShortState();
				CancelStraddleOrders(_type1Straddle, true, false);
				CancelStraddleOrders(_type2Straddle, true, false);
			}
		}
		else if (previous != 0m)
		{
			ResetPositionTracking();
		}

		_previousPosition = current;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdatePointSize();
		UpdateHistory(candle);
		CancelExpiredStraddles(candle.CloseTime);

		if (HandlePositionExits(candle))
			return;

		UpdateTrailing(candle);
		CancelOppositeOrdersIfNeeded();

		if (Volume <= 0m)
			return;

		if (Position != 0m)
			return;

		if (!_type1Straddle.HasOrders && CheckType1Signal())
			PlaceType1Orders(candle.CloseTime);

		if (!_type2Straddle.HasOrders && CheckType2Signal())
			PlaceType2Orders(candle.CloseTime);
	}

	private void HandleOrderDone(Order order)
	{
		if (order == _type1Straddle.BuyOrder)
		{
			_nextLongStop = _type1Straddle.BuyStopLoss;
			_nextLongTake = _type1Straddle.BuyTakeProfit;
			_type1Straddle.BuyOrder = null;
			_type1Straddle.BuyStopLoss = null;
			_type1Straddle.BuyTakeProfit = null;
			_type1Straddle.ClearIfEmpty();
		}
		else if (order == _type1Straddle.SellOrder)
		{
			_nextShortStop = _type1Straddle.SellStopLoss;
			_nextShortTake = _type1Straddle.SellTakeProfit;
			_type1Straddle.SellOrder = null;
			_type1Straddle.SellStopLoss = null;
			_type1Straddle.SellTakeProfit = null;
			_type1Straddle.ClearIfEmpty();
		}
		else if (order == _type2Straddle.BuyOrder)
		{
			_nextLongStop = _type2Straddle.BuyStopLoss;
			_nextLongTake = _type2Straddle.BuyTakeProfit;
			_type2Straddle.BuyOrder = null;
			_type2Straddle.BuyStopLoss = null;
			_type2Straddle.BuyTakeProfit = null;
			_type2Straddle.ClearIfEmpty();
		}
		else if (order == _type2Straddle.SellOrder)
		{
			_nextShortStop = _type2Straddle.SellStopLoss;
			_nextShortTake = _type2Straddle.SellTakeProfit;
			_type2Straddle.SellOrder = null;
			_type2Straddle.SellStopLoss = null;
			_type2Straddle.SellTakeProfit = null;
			_type2Straddle.ClearIfEmpty();
		}
	}

	private void HandleOrderTerminated(Order order)
	{
		if (order == _type1Straddle.BuyOrder)
		{
			_type1Straddle.BuyOrder = null;
			_type1Straddle.BuyStopLoss = null;
			_type1Straddle.BuyTakeProfit = null;
			_type1Straddle.ClearIfEmpty();
		}
		else if (order == _type1Straddle.SellOrder)
		{
			_type1Straddle.SellOrder = null;
			_type1Straddle.SellStopLoss = null;
			_type1Straddle.SellTakeProfit = null;
			_type1Straddle.ClearIfEmpty();
		}
		else if (order == _type2Straddle.BuyOrder)
		{
			_type2Straddle.BuyOrder = null;
			_type2Straddle.BuyStopLoss = null;
			_type2Straddle.BuyTakeProfit = null;
			_type2Straddle.ClearIfEmpty();
		}
		else if (order == _type2Straddle.SellOrder)
		{
			_type2Straddle.SellOrder = null;
			_type2Straddle.SellStopLoss = null;
			_type2Straddle.SellTakeProfit = null;
			_type2Straddle.ClearIfEmpty();
		}
	}

	private void InitializeLongState()
	{
		_longStop = _nextLongStop;
		_longTake = _nextLongTake;
		_nextLongStop = null;
		_nextLongTake = null;
		_longHighestPrice = PositionPrice;
		_longBreakEvenApplied = false;
		_longExitRequested = false;

		_shortStop = null;
		_shortTake = null;
		_shortLowestPrice = 0m;
		_shortBreakEvenApplied = false;
		_shortExitRequested = false;
	}

	private void InitializeShortState()
	{
		_shortStop = _nextShortStop;
		_shortTake = _nextShortTake;
		_nextShortStop = null;
		_nextShortTake = null;
		_shortLowestPrice = PositionPrice;
		_shortBreakEvenApplied = false;
		_shortExitRequested = false;

		_longStop = null;
		_longTake = null;
		_longHighestPrice = 0m;
		_longBreakEvenApplied = false;
		_longExitRequested = false;
	}

	private void ResetPositionTracking()
	{
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_longHighestPrice = 0m;
		_shortLowestPrice = 0m;
		_nextLongStop = null;
		_nextLongTake = null;
		_nextShortStop = null;
		_nextShortTake = null;
		_longBreakEvenApplied = false;
		_shortBreakEvenApplied = false;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		var snapshot = new CandleSnapshot(candle.CloseTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);

		if (_history.Count > 0)
		{
			var lastIndex = _history.Count - 1;
			if (_history[lastIndex].CloseTime == candle.CloseTime)
			{
				_history[lastIndex] = snapshot;
			}
			else
			{
				_history.Add(snapshot);
			}
		}
		else
		{
			_history.Add(snapshot);
		}

		var maxSize = Math.Max(3, MaxSearchBars + 5);
		while (_history.Count > maxSize)
			_history.RemoveAt(0);
	}

	private void UpdatePointSize()
	{
		if (_pointSize > 0m)
			return;

		var step = Security?.PriceStep;
		if (step.HasValue && step.Value > 0m)
		{
			_pointSize = step.Value;
			return;
		}

		var decimals = Security?.Decimals;
		if (decimals.HasValue && decimals.Value > 0)
		{
			_pointSize = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (_pointSize <= 0m)
			_pointSize = 0.0001m;
	}

	private void CancelExpiredStraddles(DateTimeOffset currentTime)
	{
		CancelExpiredStraddle(_type1Straddle, currentTime);
		CancelExpiredStraddle(_type2Straddle, currentTime);
	}

	private void CancelExpiredStraddle(PendingStraddle straddle, DateTimeOffset currentTime)
	{
		if (!straddle.HasOrders)
			return;

		if (!straddle.Expiration.HasValue)
			return;

		if (currentTime < straddle.Expiration.Value)
			return;

		CancelStraddleOrders(straddle, true, true);
	}

	private void CancelStraddleOrders(PendingStraddle straddle, bool cancelBuy, bool cancelSell)
	{
		if (cancelBuy && straddle.BuyOrder != null)
		{
			CancelOrder(straddle.BuyOrder);
			straddle.BuyOrder = null;
			straddle.BuyStopLoss = null;
			straddle.BuyTakeProfit = null;
		}

		if (cancelSell && straddle.SellOrder != null)
		{
			CancelOrder(straddle.SellOrder);
			straddle.SellOrder = null;
			straddle.SellStopLoss = null;
			straddle.SellTakeProfit = null;
		}

		if (!straddle.HasOrders)
		{
			straddle.BuyStopLoss = null;
			straddle.BuyTakeProfit = null;
			straddle.SellStopLoss = null;
			straddle.SellTakeProfit = null;
			straddle.Expiration = null;
		}
	}

	private void CancelOppositeOrdersIfNeeded()
	{
		if (Position > 0m)
		{
			CancelStraddleOrders(_type1Straddle, false, true);
			CancelStraddleOrders(_type2Straddle, false, true);
		}
		else if (Position < 0m)
		{
			CancelStraddleOrders(_type1Straddle, true, false);
			CancelStraddleOrders(_type2Straddle, true, false);
		}
	}

	private bool HandlePositionExits(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		if (Position > 0m)
		{
			if (_longExitRequested)
				return false;

			if (_longStop.HasValue && _longStop.Value > 0m && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(volume);
				_longExitRequested = true;
				return true;
			}

			if (_longTake.HasValue && _longTake.Value > 0m && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(volume);
				_longExitRequested = true;
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (_shortExitRequested)
				return false;

			if (_shortStop.HasValue && _shortStop.Value > 0m && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				return true;
			}

			if (_shortTake.HasValue && _shortTake.Value > 0m && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				return true;
			}
		}

		return false;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		var point = _pointSize;
		if (point <= 0m)
			return;

		if (Position > 0m)
		{
			if (candle.HighPrice > _longHighestPrice)
				_longHighestPrice = candle.HighPrice;

			ApplyLongBreakEven();
			ApplyLongTrailing(point);
		}
		else if (Position < 0m)
		{
			if (_shortLowestPrice == 0m || candle.LowPrice < _shortLowestPrice)
				_shortLowestPrice = candle.LowPrice;

			ApplyShortBreakEven();
			ApplyShortTrailing(point);
		}
	}

	private void ApplyLongBreakEven()
	{
		if (_longBreakEvenApplied)
			return;

		if (BreakEvenTriggerPoints <= 0 || BreakEvenOffsetPoints < 0)
			return;

		var entry = PositionPrice;
		if (entry <= 0m)
			return;

		var triggerPrice = entry + BreakEvenTriggerPoints * _pointSize;
		if (_longHighestPrice < triggerPrice)
			return;

		var newStop = NormalizePrice(entry + BreakEvenOffsetPoints * _pointSize);
		if (newStop <= 0m)
			return;

		if (!_longStop.HasValue || newStop > _longStop.Value)
			_longStop = newStop;

		_longBreakEvenApplied = true;
	}

	private void ApplyLongTrailing(decimal point)
	{
		if (TrailingBufferLongPoints <= 0)
			return;

		var entry = PositionPrice;
		if (entry <= 0m)
			return;

		var buffer = TrailingBufferLongPoints * point;
		var newStop = NormalizePrice(_longHighestPrice - buffer);
		if (newStop <= 0m)
			return;

		if (TrailOnlyAfterProfit && _longHighestPrice - entry <= buffer)
			return;

		var minImprovement = TrailingStepPoints > 1 ? (TrailingStepPoints - 1) * point : 0m;
		var threshold = newStop - minImprovement;

		if (!_longStop.HasValue || _longStop.Value < threshold)
			_longStop = newStop;
	}

	private void ApplyShortBreakEven()
	{
		if (_shortBreakEvenApplied)
			return;

		if (BreakEvenTriggerPoints <= 0 || BreakEvenOffsetPoints < 0)
			return;

		var entry = PositionPrice;
		if (entry <= 0m)
			return;

		var triggerPrice = entry - BreakEvenTriggerPoints * _pointSize;
		if (_shortLowestPrice > triggerPrice)
			return;

		var newStop = NormalizePrice(entry - BreakEvenOffsetPoints * _pointSize);
		if (newStop <= 0m)
			return;

		if (!_shortStop.HasValue || _shortStop.Value == 0m || newStop < _shortStop.Value)
			_shortStop = newStop;

		_shortBreakEvenApplied = true;
	}

	private void ApplyShortTrailing(decimal point)
	{
		if (TrailingBufferShortPoints <= 0)
			return;

		var entry = PositionPrice;
		if (entry <= 0m)
			return;

		var buffer = TrailingBufferShortPoints * point;
		var newStop = NormalizePrice(_shortLowestPrice + buffer);
		if (newStop <= 0m)
			return;

		if (TrailOnlyAfterProfit && entry - _shortLowestPrice <= buffer)
			return;

		var minImprovement = TrailingStepPoints > 1 ? (TrailingStepPoints - 1) * point : 0m;
		var threshold = newStop + minImprovement;

		if (!_shortStop.HasValue || _shortStop.Value == 0m || _shortStop.Value > threshold)
			_shortStop = newStop;
	}

	private bool CheckType1Signal()
	{
		if (_history.Count < 3)
			return false;

		var lastIndex = _history.Count - 1;
		var prev = _history[lastIndex - 1];
		var prev2 = _history[lastIndex - 2];

		var lowest = FindLowestShift(1, MaxSearchBars);
		if (lowest.HasValue && lowest.Value == 2 && prev2.High > prev.High && prev2.Low < prev.Low)
			return true;

		var highest = FindHighestShift(2, MaxSearchBars);
		if (highest.HasValue && highest.Value == 2 && prev2.High > prev.High && prev2.Low < prev.Low)
			return true;

		return false;
	}

	private bool CheckType2Signal()
	{
		if (_history.Count < 3)
			return false;

		var lastIndex = _history.Count - 1;
		var prev = _history[lastIndex - 1];
		var prev2 = _history[lastIndex - 2];

		var lowest = FindLowestShift(1, MaxSearchBars);
		if (lowest.HasValue && lowest.Value == 1 && prev.High > prev2.High && prev.Low < prev2.Low)
			return true;

		var highest = FindHighestShift(1, MaxSearchBars);
		if (highest.HasValue && highest.Value == 1 && prev2.High <= prev.High && prev2.Low > prev.Low)
			return true;

		return false;
	}

	private int? FindLowestShift(int startShift, int count)
	{
		if (count <= 0)
			return null;

		var total = _history.Count;
		var lastIndex = total - 1;
		var maxShift = Math.Min(lastIndex, startShift + count - 1);

		var bestShift = -1;
		var bestValue = decimal.MaxValue;

		for (var shift = startShift; shift <= maxShift; shift++)
		{
			var index = lastIndex - shift;
			if (index < 0)
				break;

			var low = _history[index].Low;
			if (low < bestValue)
			{
				bestValue = low;
				bestShift = shift;
			}
		}

		return bestShift >= 0 ? bestShift : (int?)null;
	}

	private int? FindHighestShift(int startShift, int count)
	{
		if (count <= 0)
			return null;

		var total = _history.Count;
		var lastIndex = total - 1;
		var maxShift = Math.Min(lastIndex, startShift + count - 1);

		var bestShift = -1;
		var bestValue = decimal.MinValue;

		for (var shift = startShift; shift <= maxShift; shift++)
		{
			var index = lastIndex - shift;
			if (index < 0)
				break;

			var high = _history[index].High;
			if (high > bestValue)
			{
				bestValue = high;
				bestShift = shift;
			}
		}

		return bestShift >= 0 ? bestShift : (int?)null;
	}

	private void PlaceType1Orders(DateTimeOffset currentTime)
	{
		if (_history.Count < 3)
			return;

		var lastIndex = _history.Count - 1;
		var prev = _history[lastIndex - 1];
		var prev2 = _history[lastIndex - 2];

		SubmitStraddle(_type1Straddle, prev, prev2, prev2, prev, currentTime);
	}

	private void PlaceType2Orders(DateTimeOffset currentTime)
	{
		if (_history.Count < 2)
			return;

		var lastIndex = _history.Count - 1;
		var prev = _history[lastIndex - 1];

		SubmitStraddle(_type2Straddle, prev, prev, prev, prev, currentTime);
	}

	private void SubmitStraddle(PendingStraddle straddle, CandleSnapshot entrySource, CandleSnapshot stopSourceLong, CandleSnapshot stopSourceShort, CandleSnapshot targetSource, DateTimeOffset currentTime)
	{
		var point = _pointSize;
		if (point <= 0m)
			return;

		var gap = GapPoints * point;
		var longTake = BuyTakeProfitPoints > 0 ? (gap + BuyTakeProfitPoints * point) : (decimal?)null;
		var shortTake = SellTakeProfitPoints > 0 ? (gap + SellTakeProfitPoints * point) : (decimal?)null;

		var buyPrice = NormalizePrice(entrySource.High + gap);
		var sellPrice = NormalizePrice(entrySource.Low - gap);

		var buyStop = NormalizePrice(stopSourceLong.Low - gap);
		var sellStop = NormalizePrice(stopSourceShort.High + gap);

		var buyTake = longTake.HasValue ? NormalizePrice(targetSource.High + longTake.Value) : (decimal?)null;
		var sellTake = shortTake.HasValue ? NormalizePrice(targetSource.Low - shortTake.Value) : (decimal?)null;

		var expiration = CalculateExpiration(currentTime);

		if (buyPrice > 0m && buyStop > 0m)
		{
			var order = BuyStop(Volume, buyPrice);
			if (order != null)
			{
				straddle.BuyOrder = order;
				straddle.BuyStopLoss = buyStop;
				straddle.BuyTakeProfit = buyTake;
			}
		}

		if (sellPrice > 0m && sellStop > 0m)
		{
			var order = SellStop(Volume, sellPrice);
			if (order != null)
			{
				straddle.SellOrder = order;
				straddle.SellStopLoss = sellStop;
				straddle.SellTakeProfit = sellTake;
			}
		}

		if (straddle.HasOrders)
			straddle.Expiration = expiration;
	}

	private DateTimeOffset? CalculateExpiration(DateTimeOffset currentTime)
	{
		var factor = OrderExpirationFactor;
		if (factor <= 0)
			return null;

		var frame = CandleType.Arg as TimeSpan? ?? TimeSpan.Zero;
		if (frame <= TimeSpan.Zero)
			frame = TimeSpan.FromMinutes(1);

		return currentTime + TimeSpan.FromTicks(frame.Ticks * factor);
	}

	private decimal NormalizePrice(decimal price)
	{
		if (price <= 0m)
			return price;

		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		var steps = Math.Round(price / step.Value, MidpointRounding.AwayFromZero);
		return steps * step.Value;
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(DateTimeOffset closeTime, decimal open, decimal high, decimal low, decimal close)
		{
			CloseTime = closeTime;
			Open = open;
			High = high;
			Low = low;
			Close = close;
		}

		public DateTimeOffset CloseTime { get; }
		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}

	private sealed class PendingStraddle
	{
		public Order BuyOrder;
		public Order SellOrder;
		public decimal? BuyStopLoss;
		public decimal? BuyTakeProfit;
		public decimal? SellStopLoss;
		public decimal? SellTakeProfit;
		public DateTimeOffset? Expiration;

		public bool HasOrders => BuyOrder != null || SellOrder != null;

		public void Reset()
		{
			BuyOrder = null;
			SellOrder = null;
			BuyStopLoss = null;
			BuyTakeProfit = null;
			SellStopLoss = null;
			SellTakeProfit = null;
			Expiration = null;
		}

		public void ClearIfEmpty()
		{
			if (HasOrders)
				return;

			BuyStopLoss = null;
			BuyTakeProfit = null;
			SellStopLoss = null;
			SellTakeProfit = null;
			Expiration = null;
		}
	}
}
