namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid averaging strategy converted from the MetaTrader 5 expert advisor "VR Smart Grid Lite".
/// </summary>
public class VrSmartGridLiteStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<CloseMode> _closeMode;
	private readonly StrategyParam<decimal> _orderStepPips;
	private readonly StrategyParam<decimal> _minimalProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _buyEntries = new();
	private readonly List<PositionEntry> _sellEntries = new();

	private decimal _pipSize;

	/// <summary>
	/// Defines how the strategy exits grid positions.
	/// </summary>
	public enum CloseMode
	{
		/// <summary>
		/// Close the most extreme pair using the weighted average price.
		/// </summary>
		Average,

		/// <summary>
		/// Close the oldest layer and partially reduce the newest layer.
		/// </summary>
		PartialClose,
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VrSmartGridLiteStrategy"/> class.
	/// </summary>
	public VrSmartGridLiteStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Target distance for single position exits", "Trading")
			.SetCanOptimize(true);

		_startVolume = Param(nameof(StartVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Initial trade volume for the first grid order", "Trading")
			.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 2.56m)
			.SetDisplay("Max Volume", "Maximum allowed trade volume per order", "Risk")
			.SetCanOptimize(true);

		_closeMode = Param(nameof(Mode), CloseMode.Average)
			.SetDisplay("Close Mode", "Averaging or partial close logic for managing the grid", "Risk Management");

		_orderStepPips = Param(nameof(OrderStepPips), 390m)
			.SetGreaterThanZero()
			.SetDisplay("Order Step (pips)", "Minimum distance before adding a new grid order", "Trading")
			.SetCanOptimize(true);

		_minimalProfitPips = Param(nameof(MinimalProfitPips), 70m)
			.SetGreaterThanZero()
			.SetDisplay("Minimal Profit (pips)", "Profit buffer added to the averaging exit price", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candle type for decision making", "General");
	}

	/// <summary>
	/// Take profit in pips applied when only one order is open.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initial order volume used for the first grid entry.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Upper limit for the volume of any grid order.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Exit logic applied when multiple grid orders are active.
	/// </summary>
	public CloseMode Mode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	/// <summary>
	/// Distance in pips before opening the next averaging order.
	/// </summary>
	public decimal OrderStepPips
	{
		get => _orderStepPips.Value;
		set => _orderStepPips.Value = value;
	}

	/// <summary>
	/// Additional profit buffer measured in pips for closing the grid.
	/// </summary>
	public decimal MinimalProfitPips
	{
		get => _minimalProfitPips.Value;
		set => _minimalProfitPips.Value = value;
	}

	/// <summary>
	/// Candle data type used for calculations.
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
		_buyEntries.Clear();
		_sellEntries.Clear();
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null || trade.Order.Security != Security)
		return;

		var volume = trade.Trade.Volume;

		if (trade.Order.Side == Sides.Buy)
		{
			var remainder = ReduceEntries(_sellEntries, volume);
			if (remainder > 0m)
			{
				// Remaining volume belongs to a new long position.
				_buyEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var remainder = ReduceEntries(_buyEntries, volume);
			if (remainder > 0m)
			{
				// Remaining volume becomes a new short position.
				_sellEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		ManageExits(candle);
		TryOpenPositions(candle);
	}

	private void ManageExits(ICandleMessage candle)
	{
		switch (Mode)
		{
			case CloseMode.Average:
				HandleAverageMode(candle);
				break;
			case CloseMode.PartialClose:
				HandlePartialCloseMode(candle);
				break;
		}
	}

	private void HandleAverageMode(ICandleMessage candle)
	{
		var profitOffset = MinimalProfitPips * _pipSize;
		var takeOffset = TakeProfitPips * _pipSize;

		if (_buyEntries.Count == 1)
		{
			var entry = _buyEntries[0];
			var target = entry.Price + takeOffset;
			if (takeOffset > 0m && candle.HighPrice >= target)
			{
				RequestClose(_buyEntries, entry, entry.Volume, true);
			}
		}
		else if (_buyEntries.Count >= 2)
		{
			var highest = GetHighestPriceEntry(_buyEntries);
			var lowest = GetLowestPriceEntry(_buyEntries);
			if (highest != null && lowest != null)
			{
				var totalVolume = highest.Volume + lowest.Volume;
				if (totalVolume > 0m)
				{
					var weighted = (highest.Price * highest.Volume + lowest.Price * lowest.Volume) / totalVolume;
					var target = weighted + profitOffset;
					if (candle.HighPrice >= target)
					{
						RequestClose(_buyEntries, highest, highest.Volume, true);
						RequestClose(_buyEntries, lowest, lowest.Volume, true);
					}
				}
			}
		}

		if (_sellEntries.Count == 1)
		{
			var entry = _sellEntries[0];
			var target = entry.Price - takeOffset;
			if (takeOffset > 0m && candle.LowPrice <= target)
			{
				RequestClose(_sellEntries, entry, entry.Volume, false);
			}
		}
		else if (_sellEntries.Count >= 2)
		{
			var highest = GetHighestPriceEntry(_sellEntries);
			var lowest = GetLowestPriceEntry(_sellEntries);
			if (highest != null && lowest != null)
			{
				var totalVolume = highest.Volume + lowest.Volume;
				if (totalVolume > 0m)
				{
					var weighted = (highest.Price * highest.Volume + lowest.Price * lowest.Volume) / totalVolume;
					var target = weighted - profitOffset;
					if (candle.LowPrice <= target)
					{
						RequestClose(_sellEntries, highest, highest.Volume, false);
						RequestClose(_sellEntries, lowest, lowest.Volume, false);
					}
				}
			}
		}
	}

	private void HandlePartialCloseMode(ICandleMessage candle)
	{
		var profitOffset = MinimalProfitPips * _pipSize;
		var takeOffset = TakeProfitPips * _pipSize;

		if (_buyEntries.Count == 1)
		{
			var entry = _buyEntries[0];
			var target = entry.Price + takeOffset;
			if (takeOffset > 0m && candle.HighPrice >= target)
			{
				RequestClose(_buyEntries, entry, entry.Volume, true);
			}
		}
		else if (_buyEntries.Count >= 2)
		{
			var highest = GetHighestPriceEntry(_buyEntries);
			var lowest = GetLowestPriceEntry(_buyEntries);
			if (highest != null && lowest != null)
			{
				var numerator = highest.Price * StartVolume + lowest.Price * lowest.Volume;
				var denominator = StartVolume + lowest.Volume;
				if (denominator > 0m)
				{
					var target = numerator / denominator + profitOffset;
					if (candle.HighPrice >= target)
					{
						var volumeToClose = Math.Min(StartVolume, highest.Volume);
						RequestClose(_buyEntries, highest, volumeToClose, true);
						RequestClose(_buyEntries, lowest, lowest.Volume, true);
					}
				}
			}
		}

		if (_sellEntries.Count == 1)
		{
			var entry = _sellEntries[0];
			var target = entry.Price - takeOffset;
			if (takeOffset > 0m && candle.LowPrice <= target)
			{
				RequestClose(_sellEntries, entry, entry.Volume, false);
			}
		}
		else if (_sellEntries.Count >= 2)
		{
			var highest = GetHighestPriceEntry(_sellEntries);
			var lowest = GetLowestPriceEntry(_sellEntries);
			if (highest != null && lowest != null)
			{
				var numerator = highest.Price * StartVolume + lowest.Price * lowest.Volume;
				var denominator = StartVolume + lowest.Volume;
				if (denominator > 0m)
				{
					var target = numerator / denominator - profitOffset;
					if (candle.LowPrice <= target)
					{
						var volumeToClose = Math.Min(StartVolume, highest.Volume);
						RequestClose(_sellEntries, highest, volumeToClose, false);
						RequestClose(_sellEntries, lowest, lowest.Volume, false);
					}
				}
			}
		}
	}

	private void TryOpenPositions(ICandleMessage candle)
	{
		var stepDistance = OrderStepPips * _pipSize;
		if (stepDistance <= 0m)
		return;

		var price = candle.ClosePrice;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			if (_buyEntries.Count == 0)
			{
				OpenPosition(Sides.Buy, StartVolume);
			}
			else
			{
				var lowest = GetLowestPriceEntry(_buyEntries);
				if (lowest != null && lowest.Price - price >= stepDistance && !_buyEntries.Any(e => e.IsClosing))
				{
					var nextVolume = lowest.Volume * 2m;
					if (MaxVolume > 0m && nextVolume > MaxVolume)
						nextVolume = MaxVolume;
					OpenPosition(Sides.Buy, nextVolume);
				}
			}
		}

		if (candle.ClosePrice < candle.OpenPrice)
		{
			if (_sellEntries.Count == 0)
			{
				OpenPosition(Sides.Sell, StartVolume);
			}
			else
			{
				var highest = GetHighestPriceEntry(_sellEntries);
				if (highest != null && price - highest.Price >= stepDistance && !_sellEntries.Any(e => e.IsClosing))
				{
					var nextVolume = highest.Volume * 2m;
					if (MaxVolume > 0m && nextVolume > MaxVolume)
						nextVolume = MaxVolume;
					OpenPosition(Sides.Sell, nextVolume);
				}
			}
		}
	}

	private void OpenPosition(Sides side, decimal requestedVolume)
	{
		var volume = AdjustVolume(requestedVolume);
		if (volume <= 0m)
		return;

		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else if (side == Sides.Sell)
		{
			SellMarket(volume);
		}
	}

	private void RequestClose(List<PositionEntry> entries, PositionEntry entry, decimal volume, bool closingLong)
	{
		var targetVolume = AdjustVolume(volume);
		if (entry == null || targetVolume <= 0m || entry.IsClosing)
		return;

		MoveEntryToFront(entries, entry);
		entry.IsClosing = true;

		if (closingLong)
		{
			SellMarket(targetVolume);
		}
		else
		{
			BuyMarket(targetVolume);
		}
	}

	private static void MoveEntryToFront(List<PositionEntry> entries, PositionEntry entry)
	{
		var index = entries.IndexOf(entry);
		if (index <= 0)
		return;

		entries.RemoveAt(index);
		entries.Insert(0, entry);
	}

	private static PositionEntry GetLowestPriceEntry(List<PositionEntry> entries)
	{
		return entries.Count == 0 ? null : entries.MinBy(e => e.Price);
	}

	private static PositionEntry GetHighestPriceEntry(List<PositionEntry> entries)
	{
		return entries.Count == 0 ? null : entries.MaxBy(e => e.Price);
	}

	private decimal ReduceEntries(List<PositionEntry> entries, decimal volume)
	{
		var remaining = volume;

		while (remaining > 0m && entries.Count > 0)
		{
			var entry = entries[0];
			var used = Math.Min(entry.Volume, remaining);
			entry.Volume -= used;
			remaining -= used;

			if (entry.Volume <= 0m)
			{
				entries.RemoveAt(0);
			}
			else
			{
				entry.IsClosing = false;
			}
		}

		return remaining;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.StepVolume ?? 0m;
		if (step > 0m)
		{
			var steps = decimal.Floor(volume / step);
			volume = steps * step;
		}

		var min = security.MinVolume ?? 0m;
		if (min > 0m && volume < min)
		return 0m;

		var max = security.MaxVolume ?? 0m;
		if (max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 0.0001m;

		var digits = 0;
		var value = step;

		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
		return step * 10m;

		return step;
	}

	private sealed class PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; set; }
		public decimal Volume { get; set; }
		public bool IsClosing { get; set; }
	}
}
