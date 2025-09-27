namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Recreates a Bollinger band breakout system with layered pending orders and trailing stops.
/// </summary>
public class BollingerBandPendingStopsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bandPeriod;
	private readonly StrategyParam<decimal> _bandDeviation;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _firstTakeProfit;
	private readonly StrategyParam<decimal> _secondTakeProfit;
	private readonly StrategyParam<decimal> _thirdTakeProfit;
	private readonly StrategyParam<bool> _useBandTrailing;

	private readonly SlotState[] _longSlots = new[]
	{
		new SlotState(true),
		new SlotState(true),
		new SlotState(true)
	};
	private readonly SlotState[] _shortSlots = new[]
	{
		new SlotState(false),
		new SlotState(false),
		new SlotState(false)
	};

	private decimal _priceStep;
	private bool _stepInitialized;

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBandPendingStopsStrategy"/> class.
	/// </summary>
	public BollingerBandPendingStopsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_bandPeriod = Param(nameof(BandPeriod), 144)
		.SetDisplay("Band Period", "Number of candles in the Bollinger bands", "Indicators")
		.SetGreaterThan(0);

		_bandDeviation = Param(nameof(BandDeviation), 4m)
		.SetDisplay("Band Deviation", "Standard deviation multiplier for the bands", "Indicators")
		.SetGreaterThanZero();

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Hour when pending orders become eligible", "Trading")
		.SetOptimize(0, 23, 1);

		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "Hour when new pending orders are blocked", "Trading")
		.SetOptimize(0, 23, 1);

		_firstTakeProfit = Param(nameof(FirstTakeProfit), 21m)
		.SetDisplay("First TP", "Take-profit distance in price steps for slot 1", "Risk")
		.SetGreaterThanZero();

		_secondTakeProfit = Param(nameof(SecondTakeProfit), 34m)
		.SetDisplay("Second TP", "Take-profit distance in price steps for slot 2", "Risk")
		.SetGreaterThanZero();

		_thirdTakeProfit = Param(nameof(ThirdTakeProfit), 55m)
		.SetDisplay("Third TP", "Take-profit distance in price steps for slot 3", "Risk")
		.SetGreaterThanZero();

		_useBandTrailing = Param(nameof(UseBandTrailingStop), true)
		.SetDisplay("Use Band Trailing", "Use the opposite band instead of EMA for trailing", "Risk");
	}

	/// <summary>
	/// Time frame used for Bollinger band calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles used by the Bollinger bands.
	/// </summary>
	public int BandPeriod
	{
		get => _bandPeriod.Value;
		set => _bandPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for the Bollinger bands.
	/// </summary>
	public decimal BandDeviation
	{
		get => _bandDeviation.Value;
		set => _bandDeviation.Value = value;
	}

	/// <summary>
	/// Trading window opening hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading window closing hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Take-profit distance (in price steps) for the first slot.
	/// </summary>
	public decimal FirstTakeProfit
	{
		get => _firstTakeProfit.Value;
		set => _firstTakeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit distance (in price steps) for the second slot.
	/// </summary>
	public decimal SecondTakeProfit
	{
		get => _secondTakeProfit.Value;
		set => _secondTakeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit distance (in price steps) for the third slot.
	/// </summary>
	public decimal ThirdTakeProfit
	{
		get => _thirdTakeProfit.Value;
		set => _thirdTakeProfit.Value = value;
	}

	/// <summary>
	/// Determines whether the trailing stop uses the opposite band (true) or the middle EMA (false).
	/// </summary>
	public bool UseBandTrailingStop
	{
		get => _useBandTrailing.Value;
		set => _useBandTrailing.Value = value;
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

		foreach (var slot in _longSlots)
			slot.Reset();

		foreach (var slot in _shortSlots)
			slot.Reset();

		_priceStep = 0m;
		_stepInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BandPeriod,
			Width = BandDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(bollinger, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		EnsurePriceStep();

		UpdatePendingOrders(upper, lower);
		UpdateTrailingStops(middle, upper, lower, candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hour = candle.OpenTime.Hour;
		if (hour <= StartHour || hour >= EndHour)
			return;

		if (upper <= candle.ClosePrice || lower >= candle.ClosePrice)
			return;

		TryPlaceLongSlots(upper, lower);
		TryPlaceShortSlots(upper, lower);
	}

	private void TryPlaceLongSlots(decimal upper, decimal lower)
	{
		var targets = new[] { FirstTakeProfit, SecondTakeProfit, ThirdTakeProfit };

		for (var i = 0; i < _longSlots.Length; i++)
		{
			var slot = _longSlots[i];
			if (!slot.CanPlaceNewOrder())
				continue;

			var volume = Volume;
			if (volume <= 0m)
				continue;

			var takeSteps = targets[i];
			var takeOffset = takeSteps * _priceStep;
			if (takeOffset <= 0m)
				takeOffset = _priceStep;

			var entryPrice = upper;
			var stopPrice = lower;
			var takePrice = entryPrice + takeOffset;

			slot.ConfigurePending(entryPrice, stopPrice, takePrice, takeSteps);
			slot.EntryOrder = BuyStop(volume, entryPrice);
		}
	}

	private void TryPlaceShortSlots(decimal upper, decimal lower)
	{
		var targets = new[] { FirstTakeProfit, SecondTakeProfit, ThirdTakeProfit };

		for (var i = 0; i < _shortSlots.Length; i++)
		{
			var slot = _shortSlots[i];
			if (!slot.CanPlaceNewOrder())
				continue;

			var volume = Volume;
			if (volume <= 0m)
				continue;

			var takeSteps = targets[i];
			var takeOffset = takeSteps * _priceStep;
			if (takeOffset <= 0m)
				takeOffset = _priceStep;

			var entryPrice = lower;
			var stopPrice = upper;
			var takePrice = entryPrice - takeOffset;

			slot.ConfigurePending(entryPrice, stopPrice, takePrice, takeSteps);
			slot.EntryOrder = SellStop(volume, entryPrice);
		}
	}

	private void UpdatePendingOrders(decimal upper, decimal lower)
	{
		foreach (var slot in _longSlots)
			slot.UpdatePending(this, upper, lower, _priceStep);

		foreach (var slot in _shortSlots)
			slot.UpdatePending(this, lower, upper, _priceStep);
	}

	private void UpdateTrailingStops(decimal middle, decimal upper, decimal lower, decimal closePrice)
	{
		foreach (var slot in _longSlots)
			slot.UpdateTrailing(this, UseBandTrailingStop ? lower : middle, closePrice, _priceStep);

		foreach (var slot in _shortSlots)
			slot.UpdateTrailing(this, UseBandTrailingStop ? upper : middle, closePrice, _priceStep);
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		foreach (var slot in _longSlots)
			slot.HandleOrderChanged(order, this);

		foreach (var slot in _shortSlots)
			slot.HandleOrderChanged(order, this);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		foreach (var slot in _longSlots)
			if (slot.TryHandleTrade(trade, this, _priceStep))
				return;

		foreach (var slot in _shortSlots)
			if (slot.TryHandleTrade(trade, this, _priceStep))
				return;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		foreach (var slot in _longSlots)
			slot.CancelAll(this);

		foreach (var slot in _shortSlots)
			slot.CancelAll(this);

		base.OnStopped();
	}

	private void EnsurePriceStep()
	{
		if (_stepInitialized)
			return;

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 0.0001m;

		_stepInitialized = true;
	}

	private sealed class SlotState
	{
		public SlotState(bool isLong)
		{
			IsLong = isLong;
		}

		public bool IsLong { get; }
		public Order EntryOrder { get; set; }
		public Order StopOrder { get; set; }
		public Order TakeOrder { get; set; }
		public decimal PlannedEntryPrice { get; private set; }
		public decimal PlannedStopPrice { get; private set; }
		public decimal PlannedTakePrice { get; private set; }
		public decimal TakeOffsetSteps { get; private set; }
		public decimal FilledVolume { get; private set; }
		public decimal LastEntryPrice { get; private set; }

		public void ConfigurePending(decimal entryPrice, decimal stopPrice, decimal takePrice, decimal takeOffsetSteps)
		{
			PlannedEntryPrice = entryPrice;
			PlannedStopPrice = stopPrice;
			PlannedTakePrice = takePrice;
			TakeOffsetSteps = takeOffsetSteps;
		}

		public bool CanPlaceNewOrder()
		{
			if (FilledVolume > 0m)
				return false;

			return EntryOrder == null || EntryOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled;
		}

		public void UpdatePending(BollingerBandPendingStopsStrategy strategy, decimal newEntryPrice, decimal newOppositeBand, decimal priceStep)
		{
			if (EntryOrder == null)
				return;

			if (EntryOrder.State != OrderStates.Active)
				return;

			var currentPrice = EntryOrder.Price;
			var shouldUpdate = IsLong ? newEntryPrice < currentPrice : newEntryPrice > currentPrice;
			if (!shouldUpdate)
				return;

			var volume = EntryOrder.Volume ?? strategy.Volume;
			if (volume <= 0m)
				volume = strategy.Volume;

			var takeSteps = TakeOffsetSteps;
			var takePrice = IsLong
			? newEntryPrice + takeSteps * priceStep
			: newEntryPrice - takeSteps * priceStep;

			if (takePrice <= 0m)
				takePrice = newEntryPrice;

			strategy.ReRegisterOrder(EntryOrder, newEntryPrice, volume);
			PlannedEntryPrice = newEntryPrice;
			PlannedStopPrice = newOppositeBand;
			PlannedTakePrice = takePrice;
		}

		public void UpdateTrailing(BollingerBandPendingStopsStrategy strategy, decimal referencePrice, decimal closePrice, decimal priceStep)
		{
			if (FilledVolume <= 0m)
				return;

			if (referencePrice <= 0m)
				return;

			if (IsLong)
			{
				if (closePrice <= LastEntryPrice)
					return;

				if (closePrice <= referencePrice)
					return;

				var newStop = referencePrice;
				if (StopOrder is { State: OrderStates.Active } && StopOrder.Price >= newStop - priceStep / 2m)
					return;

				MoveStop(strategy, newStop);
			}
			else
			{
				if (closePrice >= LastEntryPrice)
					return;

				if (closePrice >= referencePrice)
					return;

				var newStop = referencePrice;
				if (StopOrder is { State: OrderStates.Active } && StopOrder.Price <= newStop + priceStep / 2m)
					return;

				MoveStop(strategy, newStop);
			}
		}

		public void HandleOrderChanged(Order order, BollingerBandPendingStopsStrategy strategy)
		{
			if (order == EntryOrder && order.State is OrderStates.Canceled or OrderStates.Failed)
				EntryOrder = null;

			if (order == StopOrder && order.State is OrderStates.Canceled or OrderStates.Failed or OrderStates.Done)
				StopOrder = null;

			if (order == TakeOrder && order.State is OrderStates.Canceled or OrderStates.Failed or OrderStates.Done)
				TakeOrder = null;
		}

		public bool TryHandleTrade(MyTrade trade, BollingerBandPendingStopsStrategy strategy, decimal priceStep)
		{
			var order = trade.Order;
			if (order == null)
				return false;

			var tradeVolume = trade.Trade.Volume;
			var tradePrice = trade.Trade.Price;

			if (order == EntryOrder)
			{
				FilledVolume += tradeVolume;
				LastEntryPrice = tradePrice;

				if (StopOrder is { State: OrderStates.Active })
					strategy.CancelOrder(StopOrder);
				if (TakeOrder is { State: OrderStates.Active })
					strategy.CancelOrder(TakeOrder);

				if (PlannedStopPrice > 0m)
					StopOrder = IsLong
						? strategy.SellStop(FilledVolume, PlannedStopPrice)
						: strategy.BuyStop(FilledVolume, PlannedStopPrice);

				if (PlannedTakePrice > 0m)
					TakeOrder = IsLong
						? strategy.SellLimit(FilledVolume, PlannedTakePrice)
						: strategy.BuyLimit(FilledVolume, PlannedTakePrice);

				return true;
			}

			if (order != StopOrder && order != TakeOrder)
				return false;

			FilledVolume -= tradeVolume;
			if (FilledVolume <= 0m)
			{
				FilledVolume = 0m;

				if (order == StopOrder && TakeOrder is { State: OrderStates.Active })
					strategy.CancelOrder(TakeOrder);
				else if (order == TakeOrder && StopOrder is { State: OrderStates.Active })
					strategy.CancelOrder(StopOrder);

				EntryOrder = null;
				StopOrder = null;
				TakeOrder = null;
				PlannedEntryPrice = 0m;
				PlannedStopPrice = 0m;
				PlannedTakePrice = 0m;
				TakeOffsetSteps = 0m;
				LastEntryPrice = 0m;
			}

			return true;
		}

		public void CancelAll(BollingerBandPendingStopsStrategy strategy)
		{
			if (EntryOrder is { State: OrderStates.Active })
				strategy.CancelOrder(EntryOrder);
			if (StopOrder is { State: OrderStates.Active })
				strategy.CancelOrder(StopOrder);
			if (TakeOrder is { State: OrderStates.Active })
				strategy.CancelOrder(TakeOrder);

			Reset();
		}

		public void Reset()
		{
			EntryOrder = null;
			StopOrder = null;
			TakeOrder = null;
			PlannedEntryPrice = 0m;
			PlannedStopPrice = 0m;
			PlannedTakePrice = 0m;
			TakeOffsetSteps = 0m;
			FilledVolume = 0m;
			LastEntryPrice = 0m;
		}

		private void MoveStop(BollingerBandPendingStopsStrategy strategy, decimal newStop)
		{
			if (StopOrder is { State: OrderStates.Active })
				strategy.CancelOrder(StopOrder);

			if (FilledVolume <= 0m)
				return;

			StopOrder = IsLong
			? strategy.SellStop(FilledVolume, newStop)
			: strategy.BuyStop(FilledVolume, newStop);
		}
	}
}