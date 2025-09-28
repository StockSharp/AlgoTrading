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
/// Moving Average Envelopes strategy converted from MetaTrader 5 implementation.
/// Places staggered limit orders around the moving average with envelope based targets.
/// </summary>
public class MaEnvelopesStrategy : Strategy
{
	public enum MaMethods
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	public enum AppliedPrices
	{
		Open,
		High,
		Low,
		Close,
		Median,
		Typical,
		Weighted
	}

	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _firstStopTakeProfitPips;
	private readonly StrategyParam<int> _secondStopTakeProfitPips;
	private readonly StrategyParam<int> _thirdStopTakeProfitPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMethods> _maMethod;
	private readonly StrategyParam<AppliedPrices> _appliedPrice;
	private readonly StrategyParam<decimal> _envelopeDeviation;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _maIndicator;
	private readonly Queue<decimal> _maValues = new();
	private decimal? _previousClose;
	private decimal _firstOffset;
	private decimal _secondOffset;
	private decimal _thirdOffset;
	private int _lossStreak;
	private decimal _bestBidPrice;
	private decimal _bestAskPrice;

	private readonly EntrySlot[] _buySlots;
	private readonly EntrySlot[] _sellSlots;
	private readonly Dictionary<Order, EntrySlot> _orderSlots = new();

	/// <summary>
	/// Maximum risk per trade as a fraction of portfolio value.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Decrease factor applied after consecutive losing trades.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Take-profit and stop-loss distance for the first entry (in pips).
	/// </summary>
	public int FirstStopTakeProfitPips
	{
		get => _firstStopTakeProfitPips.Value;
		set => _firstStopTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Take-profit and stop-loss distance for the second entry (in pips).
	/// </summary>
	public int SecondStopTakeProfitPips
	{
		get => _secondStopTakeProfitPips.Value;
		set => _secondStopTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Take-profit and stop-loss distance for the third entry (in pips).
	/// </summary>
	public int ThirdStopTakeProfitPips
	{
		get => _thirdStopTakeProfitPips.Value;
		set => _thirdStopTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Hour of the day (terminal time) when order placement becomes active.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour of the day (terminal time) when pending orders are cancelled.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Shift applied to the moving average (number of bars back).
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MaMethods MaMethodType
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source used for calculations.
	/// </summary>
	public AppliedPrices AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Envelope deviation in percent.
	/// </summary>
	public decimal EnvelopeDeviation
	{
		get => _envelopeDeviation.Value;
		set => _envelopeDeviation.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaEnvelopesStrategy"/> class.
	/// </summary>
	public MaEnvelopesStrategy()
	{
		_maximumRisk = Param(nameof(MaximumRisk), 0.02m)
			.SetDisplay("Maximum Risk", "Maximum risk per trade as share of equity", "Risk")
			.SetNotNegative();

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
			.SetDisplay("Decrease Factor", "Loss streak volume reduction factor", "Risk")
			.SetNotNegative();

		_firstStopTakeProfitPips = Param(nameof(FirstStopTakeProfitPips), 8)
			.SetDisplay("First SL/TP", "Distance from envelope for the first order (pips)", "Targets")
			.SetGreaterThanZero();

		_secondStopTakeProfitPips = Param(nameof(SecondStopTakeProfitPips), 13)
			.SetDisplay("Second SL/TP", "Distance from envelope for the second order (pips)", "Targets")
			.SetGreaterThanZero();

		_thirdStopTakeProfitPips = Param(nameof(ThirdStopTakeProfitPips), 21)
			.SetDisplay("Third SL/TP", "Distance from envelope for the third order (pips)", "Targets")
			.SetGreaterThanZero();

		_startHour = Param(nameof(StartHour), 20)
			.SetDisplay("Start Hour", "Hour when the strategy may place orders", "Timing");

		_endHour = Param(nameof(EndHour), 22)
			.SetDisplay("End Hour", "Hour when pending orders are cancelled", "Timing");

		_maPeriod = Param(nameof(MaPeriod), 109)
			.SetDisplay("MA Period", "Moving average period", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 0)
			.SetDisplay("MA Shift", "Bars to shift the moving average", "Indicators")
			.SetNotNegative();

		_maMethod = Param(nameof(MaMethodType), MaMethods.Exponential)
			.SetDisplay("MA Method", "Moving average calculation method", "Indicators");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrices.Close)
			.SetDisplay("Applied Price", "Price source for indicator", "Indicators");

		_envelopeDeviation = Param(nameof(EnvelopeDeviation), 0.05m)
			.SetDisplay("Envelope Deviation", "Envelope width in percent", "Indicators")
			.SetNotNegative()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

		_buySlots = new[] { new EntrySlot(), new EntrySlot(), new EntrySlot() };
		_sellSlots = new[] { new EntrySlot(), new EntrySlot(), new EntrySlot() };
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
		ResetInternalState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetInternalState();
		_maIndicator = CreateMovingAverage(MaMethodType, MaPeriod);
		UpdateOffsets();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeOrderBook()
			.Bind(depth =>
			{
				var bestBid = depth.GetBestBid();
				if (bestBid != null)
					_bestBidPrice = bestBid.Price;

				var bestAsk = depth.GetBestAsk();
				if (bestAsk != null)
					_bestAskPrice = bestAsk.Price;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelAllOrders();
		ResetInternalState();
		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		if (!_orderSlots.TryGetValue(trade.Order, out var slot))
			return;

		var tradeVolume = trade.Trade.Volume;
		var tradePrice = trade.Trade.Price;

		if (slot.EntryOrder != null && trade.Order == slot.EntryOrder)
		{
			slot.RemainingEntryVolume -= tradeVolume;
			slot.FilledVolume += tradeVolume;
			slot.EntryPriceSum += tradePrice * tradeVolume;
			slot.AverageEntryPrice = slot.FilledVolume > 0m ? slot.EntryPriceSum / slot.FilledVolume : 0m;

			if (slot.RemainingEntryVolume <= 0m)
			{
				_orderSlots.Remove(trade.Order);
				slot.EntryOrder = null;
				slot.PositionVolume = slot.FilledVolume;
				RegisterProtectiveOrders(slot);
			}

			return;
		}

		if (slot.StopOrder != null && trade.Order == slot.StopOrder)
		{
			HandleExitTrade(slot, tradePrice, tradeVolume, true);
			return;
		}

		if (slot.TakeProfitOrder != null && trade.Order == slot.TakeProfitOrder)
		{
			HandleExitTrade(slot, tradePrice, tradeVolume, false);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_maIndicator == null)
			return;

		var price = GetAppliedPrice(candle, AppliedPrice);
		var maValue = _maIndicator.Process(price, candle.OpenTime, true);

		if (!_maIndicator.IsFormed)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var currentMa = maValue.ToDecimal();
		AddMaValue(currentMa);

		var shiftedMa = GetShiftedMaValue();
		if (shiftedMa == null)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var upper = shiftedMa.Value * (1m + EnvelopeDeviation / 100m);
		var lower = shiftedMa.Value * (1m - EnvelopeDeviation / 100m);

		var prevClose = _previousClose;
		_previousClose = candle.ClosePrice;
		if (prevClose == null)
			return;

		var hour = candle.CloseTime.Hour;
		var askPrice = GetCurrentAsk(candle);
		var bidPrice = GetCurrentBid(candle);
		var minDistance = GetMinimalDistance(askPrice, bidPrice);

		if (hour >= StartHour && hour < EndHour)
		{
			if (prevClose.Value > shiftedMa.Value && prevClose.Value < upper && askPrice > shiftedMa.Value)
			{
				if (TryPlaceBuyOrders(shiftedMa.Value, lower, upper, askPrice, bidPrice, minDistance))
					return;
			}

			if (prevClose.Value < shiftedMa.Value && prevClose.Value > lower && bidPrice < shiftedMa.Value)
			{
				if (TryPlaceSellOrders(shiftedMa.Value, upper, lower, askPrice, bidPrice, minDistance))
					return;
			}
		}
		else if (hour >= EndHour)
		{
			CancelAllPendingEntries();
		}
	}

	private bool TryPlaceBuyOrders(decimal maValue, decimal lower, decimal upper, decimal askPrice, decimal bidPrice, decimal minDistance)
	{
		if (TryPlaceOrder(_buySlots[0], Sides.Buy, maValue, lower, upper + _firstOffset, askPrice, bidPrice, minDistance))
			return true;

		if (TryPlaceOrder(_buySlots[1], Sides.Buy, maValue, lower, upper + _secondOffset, askPrice, bidPrice, minDistance))
			return true;

		if (TryPlaceOrder(_buySlots[2], Sides.Buy, maValue, lower, upper + _thirdOffset, askPrice, bidPrice, minDistance))
			return true;

		return false;
	}

	private bool TryPlaceSellOrders(decimal maValue, decimal upper, decimal lower, decimal askPrice, decimal bidPrice, decimal minDistance)
	{
		if (TryPlaceOrder(_sellSlots[0], Sides.Sell, maValue, upper, lower - _firstOffset, askPrice, bidPrice, minDistance))
			return true;

		if (TryPlaceOrder(_sellSlots[1], Sides.Sell, maValue, upper, lower - _secondOffset, askPrice, bidPrice, minDistance))
			return true;

		if (TryPlaceOrder(_sellSlots[2], Sides.Sell, maValue, upper, lower - _thirdOffset, askPrice, bidPrice, minDistance))
			return true;

		return false;
	}

	private bool TryPlaceOrder(EntrySlot slot, Sides direction, decimal entryPrice, decimal stopPrice, decimal takeProfitPrice, decimal askPrice, decimal bidPrice, decimal minDistance)
	{
		if (slot.EntryOrder != null || slot.PositionVolume > 0m || slot.RemainingEntryVolume > 0m)
			return false;

		if (direction == Sides.Buy)
		{
			if (askPrice <= 0m)
				return false;

			if (askPrice - entryPrice < minDistance)
				return false;
		}
		else
		{
			if (bidPrice <= 0m)
				return false;

			if (entryPrice - bidPrice < minDistance)
				return false;
		}

		var volume = CalculateOrderVolume(entryPrice, stopPrice);
		if (volume <= 0m)
			return false;

		entryPrice = NormalizePrice(entryPrice);
		stopPrice = NormalizePrice(stopPrice);
		takeProfitPrice = NormalizePrice(takeProfitPrice);

		if (stopPrice <= 0m || takeProfitPrice <= 0m)
			return false;

		Order order = direction == Sides.Buy
			? BuyLimit(volume, entryPrice)
			: SellLimit(volume, entryPrice);

		if (order == null)
			return false;

		slot.EntryOrder = order;
		slot.Direction = direction;
		slot.Volume = volume;
		slot.RemainingEntryVolume = volume;
		slot.FilledVolume = 0m;
		slot.PositionVolume = 0m;
		slot.EntryPriceSum = 0m;
		slot.AverageEntryPrice = 0m;
		slot.StopOrder = null;
		slot.TakeProfitOrder = null;
		slot.StopPrice = stopPrice;
		slot.TakeProfitPrice = takeProfitPrice;
		slot.ExitRemainingVolume = 0m;

		_orderSlots[order] = slot;
		return true;
	}

	private void HandleExitTrade(EntrySlot slot, decimal tradePrice, decimal tradeVolume, bool isStop)
	{
		if (isStop)
		{
			if (slot.StopOrder != null)
				_orderSlots.Remove(slot.StopOrder);
			if (slot.TakeProfitOrder != null)
			{
				if (slot.TakeProfitOrder.State == OrderStates.Active)
					CancelOrder(slot.TakeProfitOrder);
				_orderSlots.Remove(slot.TakeProfitOrder);
				slot.TakeProfitOrder = null;
			}
			slot.StopOrder = null;
		}
		else
		{
			if (slot.TakeProfitOrder != null)
				_orderSlots.Remove(slot.TakeProfitOrder);
			if (slot.StopOrder != null)
			{
				if (slot.StopOrder.State == OrderStates.Active)
					CancelOrder(slot.StopOrder);
				_orderSlots.Remove(slot.StopOrder);
				slot.StopOrder = null;
			}
			slot.TakeProfitOrder = null;
		}

		slot.PositionVolume = Math.Max(slot.PositionVolume - tradeVolume, 0m);
		ProcessExitResult(slot, tradePrice, tradeVolume);

		if (slot.PositionVolume <= 0m)
		{
			slot.Reset();
			return;
		}

		RegisterProtectiveOrders(slot);
	}

	private void RegisterProtectiveOrders(EntrySlot slot)
	{
		if (slot.PositionVolume <= 0m)
			return;

		if (slot.StopOrder != null && slot.StopOrder.State == OrderStates.Active)
			CancelOrder(slot.StopOrder);
		if (slot.TakeProfitOrder != null && slot.TakeProfitOrder.State == OrderStates.Active)
			CancelOrder(slot.TakeProfitOrder);

		if (slot.StopOrder != null)
			_orderSlots.Remove(slot.StopOrder);
		if (slot.TakeProfitOrder != null)
			_orderSlots.Remove(slot.TakeProfitOrder);

		var stopPrice = NormalizePrice(slot.StopPrice);
		var takeProfitPrice = NormalizePrice(slot.TakeProfitPrice);

		Order stopOrder = slot.Direction == Sides.Buy
			? SellStop(slot.PositionVolume, stopPrice)
			: BuyStop(slot.PositionVolume, stopPrice);

		if (stopOrder != null)
		{
			slot.StopOrder = stopOrder;
			_orderSlots[stopOrder] = slot;
		}

		Order takeProfitOrder = slot.Direction == Sides.Buy
			? SellLimit(slot.PositionVolume, takeProfitPrice)
			: BuyLimit(slot.PositionVolume, takeProfitPrice);

		if (takeProfitOrder != null)
		{
			slot.TakeProfitOrder = takeProfitOrder;
			_orderSlots[takeProfitOrder] = slot;
		}

		slot.ExitRemainingVolume = slot.PositionVolume;
	}

	private void ProcessExitResult(EntrySlot slot, decimal exitPrice, decimal volume)
	{
		if (slot.AverageEntryPrice == 0m || volume <= 0m)
			return;

		var directionFactor = slot.Direction == Sides.Buy ? 1m : -1m;
		var pnl = (exitPrice - slot.AverageEntryPrice) * volume * directionFactor;

		if (pnl > 0m)
		{
			_lossStreak = 0;
		}
		else if (pnl < 0m)
		{
			_lossStreak++;
		}
	}

	private decimal CalculateOrderVolume(decimal entryPrice, decimal stopPrice)
	{
		var volume = Volume;
		var accountValue = Portfolio?.CurrentValue ?? 0m;

		if (MaximumRisk > 0m && accountValue > 0m)
		{
			var riskAmount = accountValue * MaximumRisk;
			var stopDistance = Math.Abs(entryPrice - stopPrice);
			if (stopDistance > 0m)
				volume = riskAmount / stopDistance;
		}

		if (DecreaseFactor > 0m && _lossStreak > 1)
		{
			var decrease = volume * _lossStreak / DecreaseFactor;
			volume -= decrease;
		}

		if (volume <= 0m)
			return 0m;

		volume = Math.Round(volume, 2, MidpointRounding.AwayFromZero);

		var volumeStep = Security?.VolumeStep ?? 0m;
		if (volumeStep > 0m)
		{
			var steps = Math.Floor(volume / volumeStep);
			if (steps < 1m)
				steps = 1m;
			volume = steps * volumeStep;
		}

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security?.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private decimal GetCurrentAsk(ICandleMessage candle)
	{
		return _bestAskPrice > 0m ? _bestAskPrice : candle.ClosePrice;
	}

	private decimal GetCurrentBid(ICandleMessage candle)
	{
		return _bestBidPrice > 0m ? _bestBidPrice : candle.ClosePrice;
	}

	private decimal GetMinimalDistance(decimal askPrice, decimal bidPrice)
	{
		var step = Security?.PriceStep ?? 0m;
		var distance = step > 0m ? step : 0m;

		if (distance <= 0m)
		{
			var spread = askPrice > 0m && bidPrice > 0m ? askPrice - bidPrice : 0m;
			distance = spread;
		}

		return distance > 0m ? distance * 3m : 0m;
	}

	private void AddMaValue(decimal value)
	{
		_maValues.Enqueue(value);
		var maxCount = Math.Max(1, MaShift + 1);
		while (_maValues.Count > maxCount)
			_maValues.Dequeue();
	}

	private decimal? GetShiftedMaValue()
	{
		var shift = MaShift;
		if (_maValues.Count <= shift)
			return null;

		var targetIndex = _maValues.Count - 1 - shift;
		var index = 0;
		foreach (var value in _maValues)
		{
			if (index == targetIndex)
				return value;
			index++;
		}

		return null;
	}

	private void UpdateOffsets()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var decimals = Security?.Decimals ?? 0;
		var adjust = (decimals == 3 || decimals == 5) ? 10m : 1m;
		var pip = step * adjust;

		_firstOffset = FirstStopTakeProfitPips * pip;
		_secondOffset = SecondStopTakeProfitPips * pip;
		_thirdOffset = ThirdStopTakeProfitPips * pip;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		var steps = Math.Round(price / step, 0, MidpointRounding.AwayFromZero);
		return steps * step;
	}

	private void CancelAllPendingEntries()
	{
		CancelPendingEntries(_buySlots);
		CancelPendingEntries(_sellSlots);
	}

	private void CancelPendingEntries(EntrySlot[] slots)
	{
		foreach (var slot in slots)
		{
			if (slot.EntryOrder == null)
				continue;

			if (slot.EntryOrder.State == OrderStates.Active)
				CancelOrder(slot.EntryOrder);

			_orderSlots.Remove(slot.EntryOrder);
			slot.EntryOrder = null;
			slot.RemainingEntryVolume = 0m;
			slot.Volume = 0m;
			slot.FilledVolume = 0m;
			slot.PositionVolume = 0m;
			slot.EntryPriceSum = 0m;
			slot.AverageEntryPrice = 0m;
			slot.StopPrice = 0m;
			slot.TakeProfitPrice = 0m;
			slot.ExitRemainingVolume = 0m;
			slot.StopOrder = null;
			slot.TakeProfitOrder = null;
		}
	}

	private void CancelAllOrders()
	{
		CancelOrdersForSlots(_buySlots);
		CancelOrdersForSlots(_sellSlots);
	}

	private void CancelOrdersForSlots(EntrySlot[] slots)
	{
		foreach (var slot in slots)
		{
			if (slot.EntryOrder != null)
			{
				if (slot.EntryOrder.State == OrderStates.Active)
					CancelOrder(slot.EntryOrder);
				_orderSlots.Remove(slot.EntryOrder);
				slot.EntryOrder = null;
			}

			if (slot.StopOrder != null)
			{
				if (slot.StopOrder.State == OrderStates.Active)
					CancelOrder(slot.StopOrder);
				_orderSlots.Remove(slot.StopOrder);
				slot.StopOrder = null;
			}

			if (slot.TakeProfitOrder != null)
			{
				if (slot.TakeProfitOrder.State == OrderStates.Active)
					CancelOrder(slot.TakeProfitOrder);
				_orderSlots.Remove(slot.TakeProfitOrder);
				slot.TakeProfitOrder = null;
			}

			slot.Reset();
		}
	}

	private void ResetInternalState()
	{
		_maValues.Clear();
		_previousClose = null;
		_lossStreak = 0;
		_bestBidPrice = 0m;
		_bestAskPrice = 0m;
		_orderSlots.Clear();

		foreach (var slot in _buySlots)
			slot.Reset();
		foreach (var slot in _sellSlots)
			slot.Reset();
	}

	private static IIndicator CreateMovingAverage(MaMethods method, int period)
	{
		return method switch
		{
			MaMethods.Simple => new SimpleMovingAverage { Length = period },
			MaMethods.Exponential => new ExponentialMovingAverage { Length = period },
			MaMethods.Smoothed => new SmoothedMovingAverage { Length = period },
			MaMethods.LinearWeighted => new WeightedMovingAverage { Length = period },
			_ => new ExponentialMovingAverage { Length = period }
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrices type)
	{
		return type switch
		{
			AppliedPrices.Open => candle.OpenPrice,
			AppliedPrices.High => candle.HighPrice,
			AppliedPrices.Low => candle.LowPrice,
			AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrices.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private sealed class EntrySlot
	{
		public Order EntryOrder;
		public Order StopOrder;
		public Order TakeProfitOrder;
		public decimal Volume;
		public decimal RemainingEntryVolume;
		public decimal FilledVolume;
		public decimal PositionVolume;
		public decimal EntryPriceSum;
		public decimal AverageEntryPrice;
		public decimal StopPrice;
		public decimal TakeProfitPrice;
		public decimal ExitRemainingVolume;
		public Sides Direction = Sides.Buy;

		public void Reset()
		{
			EntryOrder = null;
			StopOrder = null;
			TakeProfitOrder = null;
			Volume = 0m;
			RemainingEntryVolume = 0m;
			FilledVolume = 0m;
			PositionVolume = 0m;
			EntryPriceSum = 0m;
			AverageEntryPrice = 0m;
			StopPrice = 0m;
			TakeProfitPrice = 0m;
			ExitRemainingVolume = 0m;
			Direction = Sides.Buy;
		}
	}
}

