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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cycle-based market order grid strategy converted from the MetaTrader expert "CycleMarketOrder_V181".
/// Builds a ladder of price intervals and sends market orders whenever the price crosses an interval.
/// Trailing stops are applied once the profit exceeds the configured break-even distance.
/// </summary>
public class CycleMarketOrderStrategy : Strategy
{
	private readonly StrategyParam<int> _entryDirection;
	private readonly StrategyParam<decimal> _maxPrice;
	private readonly StrategyParam<int> _maxCount;
	private readonly StrategyParam<decimal> _spanPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useWeekendMode;
	private readonly StrategyParam<int> _weekendHour;
	private readonly StrategyParam<int> _weekstartHour;
	private readonly StrategyParam<int> _magicBase;

	private decimal _pipSize;
	private decimal _bestBid;
	private decimal _bestAsk;
	private SlotState[] _slots = Array.Empty<SlotState>();
	private readonly Dictionary<Order, OrderIntent> _orderIntents = new();

	/// <summary>
	/// Entry direction: 1 for buy, -1 for sell, 0 to disable new entries.
	/// </summary>
	public int EntryDirection
	{
		get => _entryDirection.Value;
		set => _entryDirection.Value = value;
	}

	/// <summary>
	/// Reference price used to build the grid of slots.
	/// </summary>
	public decimal MaxPrice
	{
		get => _maxPrice.Value;
		set => _maxPrice.Value = value;
	}

	/// <summary>
	/// Number of slots managed by the grid.
	/// </summary>
	public int MaxCount
	{
		get => _maxCount.Value;
		set => _maxCount.Value = value;
	}

	/// <summary>
	/// Distance between consecutive slots expressed in pips.
	/// </summary>
	public decimal SpanPips
	{
		get => _spanPips.Value;
		set => _spanPips.Value = value;
	}

	/// <summary>
	/// Volume used when opening a slot.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Break-even offset in pips before the trailing stop becomes active.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enable or disable the weekend protection filter.
	/// </summary>
	public bool UseWeekendMode
	{
		get => _useWeekendMode.Value;
		set => _useWeekendMode.Value = value;
	}

	/// <summary>
	/// Hour of Saturday (terminal time) when trading is paused.
	/// </summary>
	public int WeekendHour
	{
		get => _weekendHour.Value;
		set => _weekendHour.Value = value;
	}

	/// <summary>
	/// Hour of Monday (terminal time) when trading resumes.
	/// </summary>
	public int WeekstartHour
	{
		get => _weekstartHour.Value;
		set => _weekstartHour.Value = value;
	}

	/// <summary>
	/// Base identifier applied to each slot for logging purposes.
	/// </summary>
	public int MagicNumberBase
	{
		get => _magicBase.Value;
		set => _magicBase.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CycleMarketOrderStrategy"/> class.
	/// </summary>
	public CycleMarketOrderStrategy()
	{
		_entryDirection = Param(nameof(EntryDirection), 1)
			.SetDisplay("Entry Direction", "1 = buy, -1 = sell, 0 = pause", "Trading");

		_maxPrice = Param(nameof(MaxPrice), 105m)
			.SetDisplay("Max Price", "Upper reference price for slot calculations", "Grid");

		_maxCount = Param(nameof(MaxCount), 100)
			.SetDisplay("Slot Count", "Number of slots managed by the grid", "Grid")
			.SetGreaterThanZero();

		_spanPips = Param(nameof(SpanPips), 10m)
			.SetDisplay("Span (pips)", "Distance between consecutive slots", "Grid")
			.SetGreaterThanZero();

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetDisplay("Order Volume", "Volume used for each slot order", "Trading")
			.SetGreaterThanZero();

		_breakEvenPips = Param(nameof(BreakEvenPips), 40m)
			.SetDisplay("Break-Even (pips)", "Profit threshold before trailing activates", "Risk")
			.SetNotNegative();

		_trailingStopPips = Param(nameof(TrailingStopPips), 20m)
			.SetDisplay("Trailing Stop (pips)", "Trailing distance applied after break-even", "Risk")
			.SetNotNegative();

		_useWeekendMode = Param(nameof(UseWeekendMode), false)
			.SetDisplay("Weekend Mode", "Disable trading during configured weekend hours", "Timing");

		_weekendHour = Param(nameof(WeekendHour), 4)
			.SetDisplay("Weekend Hour", "Hour on Saturday when trading pauses", "Timing")
			.SetRange(0, 23);

		_weekstartHour = Param(nameof(WeekstartHour), 8)
			.SetDisplay("Weekstart Hour", "Hour on Monday when trading resumes", "Timing")
			.SetRange(0, 23);

		_magicBase = Param(nameof(MagicNumberBase), 20140000)
			.SetDisplay("Magic Base", "Identifier offset used for slot tracking", "Diagnostics");

		Volume = OrderVolume;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = 0m;
		_bestAsk = 0m;
		_pipSize = 0m;
		_orderIntents.Clear();

		foreach (var slot in _slots)
		{
			slot?.Reset();
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_pipSize = CalculatePipSize();
		InitializeSlots();

		// Subscribe to level-1 data to receive fresh bid/ask quotes similar to tick events in MetaTrader.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelAllOrders();
		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		if (!_orderIntents.TryGetValue(trade.Order, out var intent))
			return;

		var slot = _slots[intent.SlotIndex];
		var tradeVolume = trade.Trade.Volume;
		var tradePrice = trade.Trade.Price;

		if (intent.IsExit)
		{
			slot.PendingExitVolume -= tradeVolume;
			if (slot.PendingExitVolume < 0m)
				slot.PendingExitVolume = 0m;

			slot.OpenVolume -= tradeVolume;
			if (slot.OpenVolume <= 0m)
			{
				slot.Reset();
			}
			else
			{
				slot.EntryPriceSum = slot.AverageEntryPrice * slot.OpenVolume;
			}

			if (slot.PendingExitVolume <= 0m)
				_orderIntents.Remove(trade.Order);
		}
		else
		{
			slot.PendingEntryVolume -= tradeVolume;
			if (slot.PendingEntryVolume < 0m)
				slot.PendingEntryVolume = 0m;

			slot.EntryPriceSum += tradePrice * tradeVolume;
			slot.OpenVolume += tradeVolume;
			slot.AverageEntryPrice = slot.OpenVolume > 0m ? slot.EntryPriceSum / slot.OpenVolume : 0m;
			slot.Direction = intent.Direction;
			slot.TrailingStopPrice = null; // Reset trailing when a new entry is filled.

			if (slot.PendingEntryVolume <= 0m)
				_orderIntents.Remove(trade.Order);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		if (!_orderIntents.TryGetValue(order, out var intent))
			return;

		var slot = _slots[intent.SlotIndex];
		if (intent.IsExit)
		{
			slot.PendingExitVolume = 0m;
		}
		else
		{
			slot.PendingEntryVolume = 0m;
			slot.EntryPriceSum = 0m;
			slot.AverageEntryPrice = 0m;
			slot.TrailingStopPrice = null;
		}

		_orderIntents.Remove(order);
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		// Update cached best bid and ask prices from the incoming level-1 snapshot.
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bid)
			_bestBid = bid;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal ask)
			_bestAsk = ask;

		if (_bestBid <= 0m && _bestAsk <= 0m)
			return;

		var time = message.ServerTime != default ? message.ServerTime : CurrentTime;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseWeekendMode && IsWeekendRestricted(time))
			return; // Weekend filter blocks both trailing management and new entries.

		UpdateTrailingStops();

		if (EntryDirection == 0)
			return; // Entries disabled by the user.

		TryEnterPositions();
	}

	private void TryEnterPositions()
	{
		if (_pipSize <= 0m || SpanPips <= 0m || _slots.Length == 0)
			return;

		var step = SpanPips * _pipSize;
		var volume = OrderVolume;
		if (volume <= 0m || step <= 0m)
			return;

		var count = _slots.Length;

		if (EntryDirection > 0)
		{
			if (_bestAsk <= 0m)
				return;

			var ask = _bestAsk;
			for (var i = 0; i < count; i++)
			{
				var slot = _slots[i];
				if (slot.IsBusy)
					continue;

				var startPrice = MaxPrice - step * (count - 1 - i);
				var endPrice = MaxPrice - step * (count - i);

				if (!(startPrice > ask && ask > endPrice))
					continue;

				var order = BuyMarket(volume);
				if (order == null)
					continue;

				slot.PendingEntryVolume += volume;
				slot.Direction = Sides.Buy;
				slot.EntryPriceSum = 0m;
				slot.AverageEntryPrice = 0m;
				slot.TrailingStopPrice = null;

				_orderIntents[order] = new OrderIntent(i, false, Sides.Buy);

				LogInfo($"Slot {slot.MagicNumber} buy triggered at ask {ask:F5} inside range [{endPrice:F5}, {startPrice:F5}].");
			}
		}
		else if (EntryDirection < 0)
		{
			if (_bestBid <= 0m)
				return;

			var bid = _bestBid;
			for (var i = 0; i < count; i++)
			{
				var slot = _slots[i];
				if (slot.IsBusy)
					continue;

				var startPrice = MaxPrice - step * i;
				var endPrice = MaxPrice - step * (i - 1);

				if (!(startPrice < bid && bid < endPrice))
					continue;

				var order = SellMarket(volume);
				if (order == null)
					continue;

				slot.PendingEntryVolume += volume;
				slot.Direction = Sides.Sell;
				slot.EntryPriceSum = 0m;
				slot.AverageEntryPrice = 0m;
				slot.TrailingStopPrice = null;

				_orderIntents[order] = new OrderIntent(i, false, Sides.Sell);

				LogInfo($"Slot {slot.MagicNumber} sell triggered at bid {bid:F5} inside range [{startPrice:F5}, {endPrice:F5}].");
			}
		}
	}

	private void UpdateTrailingStops()
	{
		if (TrailingStopPips <= 0m || _pipSize <= 0m)
			return;

		var breakEvenDistance = BreakEvenPips * _pipSize;
		var trailingDistance = TrailingStopPips * _pipSize;

		for (var i = 0; i < _slots.Length; i++)
		{
			var slot = _slots[i];
			if (slot.OpenVolume <= 0m)
				continue;

			if (slot.Direction == Sides.Buy)
			{
				if (_bestBid <= 0m)
					continue;

				var triggerPrice = slot.AverageEntryPrice + breakEvenDistance;
				var newStop = _bestBid - trailingDistance;

				if (_bestBid >= triggerPrice && (!slot.TrailingStopPrice.HasValue || newStop > slot.TrailingStopPrice.Value))
				{
					slot.TrailingStopPrice = newStop;
				}

				if (slot.TrailingStopPrice.HasValue && _bestBid <= slot.TrailingStopPrice.Value)
				{
					CloseSlot(i, slot);
				}
			}
			else
			{
				if (_bestAsk <= 0m)
					continue;

				var triggerPrice = slot.AverageEntryPrice - breakEvenDistance;
				var newStop = _bestAsk + trailingDistance;

				if (_bestAsk <= triggerPrice && (!slot.TrailingStopPrice.HasValue || newStop < slot.TrailingStopPrice.Value))
				{
					slot.TrailingStopPrice = newStop;
				}

				if (slot.TrailingStopPrice.HasValue && _bestAsk >= slot.TrailingStopPrice.Value)
				{
					CloseSlot(i, slot);
				}
			}
		}
	}

	private void CloseSlot(int index, SlotState slot)
	{
		if (slot.PendingExitVolume > 0m || slot.OpenVolume <= 0m)
			return;

		Order order;
		if (slot.Direction == Sides.Buy)
		{
			order = SellMarket(slot.OpenVolume);
		}
		else
		{
			order = BuyMarket(slot.OpenVolume);
		}

		if (order == null)
			return;

		slot.PendingExitVolume += slot.OpenVolume;
		_orderIntents[order] = new OrderIntent(index, true, slot.Direction);

		LogInfo($"Slot {slot.MagicNumber} closing at trailing stop {slot.TrailingStopPrice ?? "n/a"}.");
	}

	private void InitializeSlots()
	{
		var count = MaxCount;
		if (count <= 0)
		{
			_slots = Array.Empty<SlotState>();
			return;
		}

		_slots = new SlotState[count];
		for (var i = 0; i < count; i++)
		{
			_slots[i] = new SlotState(MagicNumberBase + i);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private bool IsWeekendRestricted(DateTimeOffset time)
	{
		var local = time.LocalDateTime;
		return local.DayOfWeek switch
		{
			DayOfWeek.Saturday when local.Hour >= WeekendHour => true,
			DayOfWeek.Sunday => true,
			DayOfWeek.Monday when local.Hour < WeekstartHour => true,
			_ => false
		};
	}

	private readonly struct OrderIntent
	{
		public OrderIntent(int slotIndex, bool isExit, Sides direction)
		{
			SlotIndex = slotIndex;
			IsExit = isExit;
			Direction = direction;
		}

		public int SlotIndex { get; }
		public bool IsExit { get; }
		public Sides Direction { get; }
	}

	private sealed class SlotState
	{
		public SlotState(int magicNumber)
		{
			MagicNumber = magicNumber;
			Reset();
		}

		public int MagicNumber { get; }
		public decimal OpenVolume { get; set; }
		public decimal PendingEntryVolume { get; set; }
		public decimal PendingExitVolume { get; set; }
		public decimal EntryPriceSum { get; set; }
		public decimal AverageEntryPrice { get; set; }
		public decimal? TrailingStopPrice { get; set; }
		public Sides Direction { get; set; } = Sides.Buy;

		public bool IsBusy => PendingEntryVolume > 0m || PendingExitVolume > 0m || OpenVolume > 0m;

		public void Reset()
		{
			OpenVolume = 0m;
			PendingEntryVolume = 0m;
			PendingExitVolume = 0m;
			EntryPriceSum = 0m;
			AverageEntryPrice = 0m;
			TrailingStopPrice = null;
			Direction = Sides.Buy;
		}
	}
}

