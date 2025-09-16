using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedging grid strategy translated from the MQL5 expert "Urdala_Trol".
/// </summary>
public class UrdalaTrolStrategy : Strategy
{
	private const decimal VolumeTolerance = 0.0000001m;

	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _minLotsMultiplier;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _gridStepPips;
	private readonly StrategyParam<decimal> _minNearestPips;

	private readonly List<PositionItem> _longPositions = new();
	private readonly List<PositionItem> _shortPositions = new();
	private readonly Dictionary<Order, PendingOrderInfo> _orders = new();

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;

	private decimal _pipValue;
	private decimal _stopLossOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;
	private decimal _gridStepOffset;
	private decimal _minNearestOffset;

	/// <summary>
	/// Base volume for the initial hedge.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// How many minimum lots are added when scaling into new trades.
	/// </summary>
	public int MinLotsMultiplier
	{
		get => _minLotsMultiplier.Value;
		set => _minLotsMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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
	/// Additional distance in pips that must be covered before the trailing stop moves.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Distance between grid trades in pips.
	/// </summary>
	public decimal GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Minimal distance from an existing position when re-entering after a stop.
	/// </summary>
	public decimal MinNearestPips
	{
		get => _minNearestPips.Value;
		set => _minNearestPips.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public UrdalaTrolStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetDisplay("Base Volume", "Initial order volume", "Trading");

		_minLotsMultiplier = Param(nameof(MinLotsMultiplier), 3)
			.SetGreaterThanZero()
			.SetDisplay("Min Lots Multiplier", "Minimum lot multiplier when scaling", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Price move required to advance the trailing stop", "Risk");

		_gridStepPips = Param(nameof(GridStepPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step (pips)", "Minimal distance between grid entries", "Trading");

		_minNearestPips = Param(nameof(MinNearestPips), 3m)
			.SetDisplay("Min Nearest (pips)", "Minimal distance to existing trades after a stop", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longPositions.Clear();
		_shortPositions.Clear();
		_orders.Clear();

		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;

		_pipValue = 0m;
		_stopLossOffset = 0m;
		_trailingStopOffset = 0m;
		_trailingStepOffset = 0m;
		_gridStepOffset = 0m;
		_minNearestOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_pipValue = CalculatePipValue();
		_stopLossOffset = StopLossPips * _pipValue;
		_trailingStopOffset = TrailingStopPips * _pipValue;
		_trailingStepOffset = TrailingStepPips * _pipValue;
		_gridStepOffset = GridStepPips * _pipValue;
		_minNearestOffset = MinNearestPips * _pipValue;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBestBid = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasBestAsk = true;
			}
		}

		if (_hasBestBid && _hasBestAsk)
			ProcessStrategy();
	}

	private void ProcessStrategy()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateStops();

		if (_orders.Count > 0)
			return;

		var longCount = _longPositions.Count;
		var shortCount = _shortPositions.Count;

		if (longCount + shortCount == 0)
		{
			TryOpenPosition(Sides.Buy, BaseVolume);
			TryOpenPosition(Sides.Sell, BaseVolume);
			return;
		}

		if (longCount == 0 || shortCount == 0)
		{
			var stepVolume = GetMinVolumeStep();
			if (stepVolume <= 0m)
				return;

			if (longCount > 0 && shortCount == 0)
			{
				var (price, volume) = FindLeastProfitable(_longPositions, _bestBid, true);
				if (price.HasValue && volume > 0m && price.Value - _bestAsk >= _gridStepOffset)
				{
					var newVolume = volume + MinLotsMultiplier * stepVolume;
					TryOpenPosition(Sides.Buy, newVolume);
					return;
				}
			}

			if (shortCount > 0 && longCount == 0)
			{
				var (price, volume) = FindLeastProfitable(_shortPositions, _bestAsk, false);
				if (price.HasValue && volume > 0m && _bestBid - price.Value >= _gridStepOffset)
				{
					var newVolume = volume + MinLotsMultiplier * stepVolume;
					TryOpenPosition(Sides.Sell, newVolume);
				}
			}
		}
	}

	private void UpdateStops()
	{
		if (_stopLossOffset <= 0m && _trailingStopOffset <= 0m)
			return;

		foreach (var position in _longPositions.ToArray())
		{
			var currentPrice = _bestBid;
			if (currentPrice <= 0m)
				continue;

			if (_trailingStopOffset > 0m && currentPrice - position.EntryPrice > _trailingStopOffset + _trailingStepOffset)
			{
				var trigger = currentPrice - (_trailingStopOffset + _trailingStepOffset);
				if (!position.StopPrice.HasValue || position.StopPrice.Value < trigger)
					position.StopPrice = currentPrice - _trailingStopOffset;
			}

			if (position.StopPrice.HasValue && currentPrice <= position.StopPrice.Value)
				ClosePosition(position, CloseReason.StopLoss);
		}

		foreach (var position in _shortPositions.ToArray())
		{
			var currentPrice = _bestAsk;
			if (currentPrice <= 0m)
				continue;

			if (_trailingStopOffset > 0m && position.EntryPrice - currentPrice > _trailingStopOffset + _trailingStepOffset)
			{
				var trigger = currentPrice + (_trailingStopOffset + _trailingStepOffset);
				if (!position.StopPrice.HasValue || position.StopPrice.Value > trigger)
					position.StopPrice = currentPrice + _trailingStopOffset;
			}

			if (position.StopPrice.HasValue && currentPrice >= position.StopPrice.Value)
				ClosePosition(position, CloseReason.StopLoss);
		}
	}

	private void TryOpenPosition(Sides side, decimal volume)
	{
		if (Security == null || Portfolio == null)
			return;

		var normalized = NormalizeVolume(volume);
		if (normalized <= 0m)
			return;

		var order = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Volume = normalized,
			Side = side,
			Type = OrderTypes.Market,
			Comment = side == Sides.Buy ? "UrdalaTrol:LongEntry" : "UrdalaTrol:ShortEntry"
		};

		var position = new PositionItem
		{
			Side = side
		};

		_orders[order] = new PendingOrderInfo
		{
			IsEntry = true,
			Position = position,
			RemainingVolume = normalized
		};

		RegisterOrder(order);
	}

	private void ClosePosition(PositionItem position, CloseReason reason)
	{
		if (Security == null || Portfolio == null)
			return;

		if (position.IsClosing)
			return;

		position.IsClosing = true;

		if (position.Side == Sides.Buy)
			_longPositions.Remove(position);
		else
			_shortPositions.Remove(position);

		var exitSide = position.Side == Sides.Buy ? Sides.Sell : Sides.Buy;
		var normalized = NormalizeVolume(position.Volume);
		if (normalized <= 0m)
			return;

		var order = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Volume = normalized,
			Side = exitSide,
			Type = OrderTypes.Market,
			Comment = reason == CloseReason.StopLoss ? "UrdalaTrol:Stop" : "UrdalaTrol:Exit"
		};

		_orders[order] = new PendingOrderInfo
		{
			IsEntry = false,
			Position = position,
			RemainingVolume = normalized,
			CloseReason = reason
		};

		RegisterOrder(order);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null || !_orders.TryGetValue(trade.Order, out var info))
			return;

		var tradeVolume = trade.Trade.Volume;
		var tradePrice = trade.Trade.Price;

		info.RemainingVolume -= tradeVolume;
		info.FilledVolume += tradeVolume;
		info.WeightedPrice += tradePrice * tradeVolume;

		if (info.RemainingVolume > VolumeTolerance)
			return;

		_orders.Remove(trade.Order);

		var averagePrice = info.WeightedPrice / info.FilledVolume;
		var position = info.Position;

		if (info.IsEntry)
		{
			position.Volume = info.FilledVolume;
			position.EntryPrice = averagePrice;
			position.StopPrice = _stopLossOffset > 0m
				? position.Side == Sides.Buy
					? position.EntryPrice - _stopLossOffset
					: position.EntryPrice + _stopLossOffset
				: null;
			position.IsClosing = false;

			if (position.Side == Sides.Buy)
				_longPositions.Add(position);
			else
				_shortPositions.Add(position);
		}
		else
		{
			var profit = position.Side == Sides.Buy
				? (averagePrice - position.EntryPrice) * info.FilledVolume
				: (position.EntryPrice - averagePrice) * info.FilledVolume;

			position.Volume = info.FilledVolume;
			HandlePositionClosed(position, profit, info.CloseReason, averagePrice);
		}
	}

	private void HandlePositionClosed(PositionItem position, decimal profit, CloseReason reason, decimal exitPrice)
	{
		if (reason != CloseReason.StopLoss)
			return;

		var stepVolume = GetMinVolumeStep();
		if (stepVolume <= 0m)
			return;

		var additional = MinLotsMultiplier * stepVolume;
		var newVolume = position.Volume + additional;
		if (newVolume <= 0m)
			return;

		if (profit < 0m)
		{
			if (!IsNearestPosition(position.Side, exitPrice))
				TryOpenPosition(position.Side, newVolume);
		}
		else
		{
			var opposite = position.Side == Sides.Buy ? Sides.Sell : Sides.Buy;
			TryOpenPosition(opposite, newVolume);
		}
	}

	private (decimal? price, decimal volume) FindLeastProfitable(IReadOnlyList<PositionItem> positions, decimal currentPrice, bool isLong)
	{
		decimal? price = null;
		decimal volume = 0m;

		foreach (var position in positions)
		{
			var profit = isLong
				? (currentPrice - position.EntryPrice) * position.Volume
				: (position.EntryPrice - currentPrice) * position.Volume;

			if (profit >= 0m)
				continue;

			if (price == null)
			{
				price = position.EntryPrice;
				volume = position.Volume;
			}
			else if (isLong ? position.EntryPrice < price.Value : position.EntryPrice > price.Value)
			{
				price = position.EntryPrice;
				volume = position.Volume;
			}
		}

		return (price, volume);
	}

	private bool IsNearestPosition(Sides side, decimal referencePrice)
	{
		if (_minNearestOffset <= 0m)
			return false;

		var positions = side == Sides.Buy ? _longPositions : _shortPositions;
		var currentPrice = side == Sides.Buy ? _bestBid : _bestAsk;

		foreach (var position in positions)
		{
			if (Math.Abs(currentPrice - referencePrice) < _minNearestOffset)
				return true;
		}

		return false;
	}

	private decimal GetMinVolumeStep()
	{
		if (Security?.VolumeStep is { } step && step > 0m)
			return step;

		if (Security?.MinVolume is { } minVolume && minVolume > 0m)
			return minVolume;

		return 1m;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		if (Security?.VolumeStep is { } step && step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (Security?.MinVolume is { } minVolume && minVolume > 0m && volume < minVolume)
			return 0m;

		if (Security?.MaxVolume is { } maxVolume && maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private decimal CalculatePipValue()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var scaled = step;
		var digits = 0;
		while (scaled < 1m && digits < 10)
		{
			scaled *= 10m;
			digits++;
		}

		var adjust = (digits == 3 || digits == 5) ? 10m : 1m;
		return step * adjust;
	}

	private sealed class PositionItem
	{
		public Sides Side { get; set; }
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal? StopPrice { get; set; }
		public bool IsClosing { get; set; }
	}

	private sealed class PendingOrderInfo
	{
		public bool IsEntry { get; set; }
		public PositionItem Position { get; set; } = null!;
		public decimal RemainingVolume { get; set; }
		public decimal FilledVolume { get; set; }
		public decimal WeightedPrice { get; set; }
		public CloseReason CloseReason { get; set; }
	}

	private enum CloseReason
	{
		StopLoss
	}
}
