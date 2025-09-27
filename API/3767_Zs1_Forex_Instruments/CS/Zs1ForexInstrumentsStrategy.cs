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
/// Hedged grid strategy converted from the MetaTrader expert "Zs1_www_forex-instruments_info".
/// The strategy opens an initial buy/sell pair, tracks price zones relative to the starting level
/// and adds or closes positions according to the original tunnel logic.
/// </summary>
public class Zs1ForexInstrumentsStrategy : Strategy
{

	private static readonly decimal[] TunnelMultipliers =
	{
		1m, 3m, 6m, 12m, 24m, 48m, 96m, 192m, 384m, 768m, 1536m, 3072m,
	};

	private readonly StrategyParam<decimal> _ordersSpacePips;
	private readonly StrategyParam<int> _pkPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _volumeTolerance;

	private readonly Dictionary<Order, OrderIntent> _orderIntents = new();
	private readonly List<Entry> _longEntries = new();
	private readonly List<Entry> _shortEntries = new();

	private decimal _pipValue;
	private decimal _firstPrice;
	private int _zone;
	private int _lastZone;
	private bool _zoneChanged;
	private int _firstStage;
	private Sides? _firstOrderDirection;
	private Sides? _lastOrderDirection;
	private bool _isClosingAll;
	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;

	private enum OrderIntent
	{
		OpenLong,
		OpenShort,
		CloseLong,
		CloseShort,
	}

	private sealed class Entry
	{
		public Entry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; set; }

		public decimal Volume { get; set; }
	}

	/// <summary>
	/// Distance in pips between consecutive price zones.
	/// </summary>
	public decimal OrdersSpacePips
	{
		get => _ordersSpacePips.Value;
		set => _ordersSpacePips.Value = value;
	}

	/// <summary>
	/// Additional pip offset used when detecting new zones.
	/// </summary>
	public int PkPips
	{
		get => _pkPips.Value;
		set => _pkPips.Value = value;
	}

	/// <summary>
	/// Base volume for the initial hedge trade.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}
	/// <summary>
	/// Allowed difference used when comparing position volumes.
	/// </summary>
	public decimal VolumeTolerance
	{
		get => _volumeTolerance.Value;
		set => _volumeTolerance.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Zs1ForexInstrumentsStrategy"/> class.
	/// </summary>
	public Zs1ForexInstrumentsStrategy()
	{
		_ordersSpacePips = Param(nameof(OrdersSpacePips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Orders Space (pips)", "Distance between successive grid levels.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_pkPips = Param(nameof(PkPips), 10)
			.SetNotNegative()
			.SetDisplay("Zone Offset (pips)", "Additional offset applied when checking zone boundaries.", "Trading");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Base volume for the hedge orders.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 1m, 0.01m);
		_volumeTolerance = Param(nameof(VolumeTolerance), 0.0000001m)
			.SetDisplay("Volume tolerance", "Allowed difference when comparing cumulative volumes.", "Trading")
			.SetNotNegative();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_orderIntents.Clear();
		_longEntries.Clear();
		_shortEntries.Clear();
		_pipValue = 0m;
		_firstPrice = 0m;
		_zone = 0;
		_lastZone = 0;
		_zoneChanged = false;
		_firstStage = 0;
		_firstOrderDirection = null;
		_lastOrderDirection = null;
		_isClosingAll = false;
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipValue = CalculatePipValue();

		StartProtection();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order is not { } order || !_orderIntents.TryGetValue(order, out var intent))
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		switch (intent)
		{
			case OrderIntent.OpenLong:
				_longEntries.Add(new Entry(price, volume));
				_lastOrderDirection = Sides.Buy;
				break;

			case OrderIntent.OpenShort:
				_shortEntries.Add(new Entry(price, volume));
				_lastOrderDirection = Sides.Sell;
				break;

			case OrderIntent.CloseLong:
				ReduceEntries(_longEntries, volume);
				break;

			case OrderIntent.CloseShort:
				ReduceEntries(_shortEntries, volume);
				break;
		}

		if (order.Balance <= 0m || IsOrderCompleted(order))
		{
			_orderIntents.Remove(order);
		}

		if (_firstStage == 1 && _firstPrice == 0m && _longEntries.Count > 0 && _shortEntries.Count > 0)
		{
			var longPrice = _longEntries[0].Price;
			var shortPrice = _shortEntries[0].Price;
			_firstPrice = (longPrice + shortPrice) / 2m;
		}

		if (!_longEntries.Any() && !_shortEntries.Any() && _isClosingAll)
		{
			ResetState();
			_isClosingAll = false;
		}
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

		if (!_hasBestBid || !_hasBestAsk)
			return;

		if (_isClosingAll)
			return;

		var ordersTotal = GetOrdersTotal();

		if (_firstStage != 0)
			CheckZone();

		if (_firstStage == 0 && ordersTotal == 0)
		{
			OpenFirst();
			ordersTotal = GetOrdersTotal();
		}

		if (_zoneChanged)
		{
			ProcessZoneChange();
		}

		if (ordersTotal >= 3 && CalculateFloatingProfit() >= 0m)
		{
			CloseAllOrders();
		}
	}

	private void ProcessZoneChange()
	{
		switch (_firstStage)
		{
			case 1:
				ZoneF1();
				break;
			case 2 when _firstOrderDirection == Sides.Buy:
				switch (_zone)
				{
					case -2:
						ZoneMinusTwo();
						break;
					case -1:
						ZoneMinusOne();
						break;
					case 0:
						ZoneZero();
						break;
					case 1:
					case 2:
						ZonePlusOne();
						break;
				}
				break;
			case 2 when _firstOrderDirection == Sides.Sell:
				switch (_zone)
				{
					case 2:
						ZoneMinusTwo();
						break;
					case 1:
						ZoneMinusOne();
						break;
					case 0:
						ZoneZero();
						break;
					case -1:
					case -2:
						ZonePlusOne();
						break;
				}
				break;
		}
	}

	private void ZoneF1()
	{
		_zoneChanged = false;
		CloseFirstOrders();
	}

	private void ZoneMinusTwo()
	{
		_zoneChanged = false;

		if (CalculateFloatingProfit() > 0m)
		{
			CloseAllOrders();
		}
		else
		{
			OpenAnother();
		}
	}

	private void ZoneMinusOne()
	{
		_zoneChanged = false;

		if (_firstOrderDirection == null)
			return;

		if (_firstOrderDirection == Sides.Buy)
		{
			OpenSellOrder();
		}
		else
		{
			OpenBuyOrder();
		}
	}

	private void ZoneZero()
	{
		_zoneChanged = false;

		if (_firstOrderDirection == null)
			return;

		if (_firstOrderDirection == Sides.Buy)
		{
			OpenBuyOrder();
		}
		else
		{
			OpenSellOrder();
		}
	}

	private void ZonePlusOne()
	{
		_zoneChanged = false;

		if (CalculateFloatingProfit() > 0m)
		{
			CloseAllOrders();
		}
		else
		{
			OpenAnother();
		}
	}

	private void OpenFirst()
	{
		var volume = InitialVolume;
		ValidateVolume(volume);

		var buyOrder = BuyMarket(volume);
		if (buyOrder != null)
		{
			_orderIntents[buyOrder] = OrderIntent.OpenLong;
		}

		var sellOrder = SellMarket(volume);
		if (sellOrder != null)
		{
			_orderIntents[sellOrder] = OrderIntent.OpenShort;
		}

		_firstStage = 1;
		_zone = 0;
		_lastZone = 0;
		_zoneChanged = false;
		_firstPrice = GetMidPrice();
		_firstOrderDirection = null;
		_lastOrderDirection = null;
	}

	private void CloseFirstOrders()
	{
		if (_longEntries.Count > 0 && _bestBid > _longEntries[0].Price)
		{
			CloseEntry(Sides.Buy, _longEntries[0].Volume);
			_firstStage = 2;
			_firstOrderDirection = Sides.Sell;
			_lastOrderDirection = Sides.Sell;
			return;
		}

		if (_shortEntries.Count > 0 && _bestAsk < _shortEntries[0].Price)
		{
			CloseEntry(Sides.Sell, _shortEntries[0].Volume);
			_firstStage = 2;
			_firstOrderDirection = Sides.Buy;
			_lastOrderDirection = Sides.Buy;
		}
	}

	private void OpenBuyOrder()
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		ValidateVolume(volume);

		var order = BuyMarket(volume);
		if (order != null)
		{
			_orderIntents[order] = OrderIntent.OpenLong;
		}
	}

	private void OpenSellOrder()
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		ValidateVolume(volume);

		var order = SellMarket(volume);
		if (order != null)
		{
			_orderIntents[order] = OrderIntent.OpenShort;
		}
	}

	private void OpenAnother()
	{
		if (_lastOrderDirection == Sides.Buy)
		{
			OpenSellOrder();
		}
		else if (_lastOrderDirection == Sides.Sell)
		{
			OpenBuyOrder();
		}
		else if (_firstOrderDirection == Sides.Buy)
		{
			OpenSellOrder();
		}
		else if (_firstOrderDirection == Sides.Sell)
		{
			OpenBuyOrder();
		}
	}

	private void CloseAllOrders()
	{
		if (_isClosingAll)
			return;

		_zoneChanged = false;
		_isClosingAll = true;

		CloseSide(Sides.Buy);
		CloseSide(Sides.Sell);

		if (!_longEntries.Any() && !_shortEntries.Any())
		{
			ResetState();
			_isClosingAll = false;
		}
	}

	private void CloseSide(Sides side)
	{
		var entries = side == Sides.Buy ? _longEntries : _shortEntries;
		if (!entries.Any())
			return;

		var totalVolume = entries.Sum(e => e.Volume);
		if (totalVolume <= 0m)
			return;

		Order order;

		if (side == Sides.Buy)
		{
			order = SellMarket(totalVolume);
			if (order != null)
				_orderIntents[order] = OrderIntent.CloseLong;
		}
		else
		{
			order = BuyMarket(totalVolume);
			if (order != null)
				_orderIntents[order] = OrderIntent.CloseShort;
		}
	}

	private void CloseEntry(Sides side, decimal volume)
	{
		if (volume <= 0m)
			return;

	Order order;

		if (side == Sides.Buy)
		{
			order = SellMarket(volume);
			if (order != null)
				_orderIntents[order] = OrderIntent.CloseLong;
		}
		else
		{
			order = BuyMarket(volume);
			if (order != null)
				_orderIntents[order] = OrderIntent.CloseShort;
		}
	}

	private void ResetState()
	{
		_zone = 0;
		_lastZone = 0;
		_zoneChanged = false;
		_firstStage = 0;
		_firstOrderDirection = null;
		_lastOrderDirection = null;
		_firstPrice = 0m;
	}

	private void CheckZone()
	{
		var step = OrdersSpacePips * _pipValue;
		if (step <= 0m || _firstPrice <= 0m)
			return;

		var offset = PkPips * _pipValue;

		if (_lastOrderDirection == Sides.Sell)
		{
			var bid = _bestBid + offset;
			if (bid >= _firstPrice + step * (_zone + 1))
			{
				_lastZone = _zone;
				_zone++;
				_zoneChanged = true;
			}
			else if (bid <= _firstPrice + step * (_zone - 1))
			{
				_lastZone = _zone;
				_zone--;
				_zoneChanged = true;
			}
		}
		else if (_lastOrderDirection == Sides.Buy)
		{
			var ask = _bestAsk - offset;
			if (ask >= _firstPrice + step * (_zone + 1))
			{
				_lastZone = _zone;
				_zone++;
				_zoneChanged = true;
			}
			else if (ask <= _firstPrice + step * (_zone - 1))
			{
				_lastZone = _zone;
				_zone--;
				_zoneChanged = true;
			}
		}
		else
		{
			var price = GetMidPrice();
			if (price >= _firstPrice + step * (_zone + 1))
			{
				_lastZone = _zone;
				_zone++;
				_zoneChanged = true;
			}
			else if (price <= _firstPrice + step * (_zone - 1))
			{
				_lastZone = _zone;
				_zone--;
				_zoneChanged = true;
			}
		}

		if (_zoneChanged && _zone == _lastZone)
		{
			_zoneChanged = false;
		}
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = InitialVolume;
		var ordersTotal = GetOrdersTotal();
		var multiplier = 1m;

		if ((_zone == 0 || _zone == -1) && ordersTotal >= 1 && _firstOrderDirection == Sides.Buy)
		{
			multiplier = GetTunnelMultiplier(ordersTotal);
		}
		else if ((_zone == 0 || _zone == 1) && ordersTotal >= 1 && _firstOrderDirection == Sides.Sell)
		{
			multiplier = GetTunnelMultiplier(ordersTotal);
		}

		return baseVolume * multiplier;
	}

	private static decimal GetTunnelMultiplier(int ordersTotal)
	{
		if (ordersTotal < 0)
			return 1m;

		if (ordersTotal >= TunnelMultipliers.Length)
			return TunnelMultipliers[^1];

		return TunnelMultipliers[ordersTotal];
	}

	private int GetOrdersTotal()
	{
		return _longEntries.Count + _shortEntries.Count;
	}

	private decimal CalculateFloatingProfit()
	{
		if (!_hasBestBid || !_hasBestAsk)
			return 0m;

		decimal profit = 0m;

		foreach (var entry in _longEntries)
		{
			profit += (_bestBid - entry.Price) * entry.Volume;
		}

		foreach (var entry in _shortEntries)
		{
			profit += (entry.Price - _bestAsk) * entry.Volume;
		}

		return profit;
	}

	private void ReduceEntries(List<Entry> entries, decimal volume)
	{
		var remaining = volume;

		while (remaining > 0m && entries.Count > 0)
		{
			var current = entries[0];
			if (current.Volume <= remaining + VolumeTolerance)
			{
				remaining -= current.Volume;
				entries.RemoveAt(0);
			}
			else
			{
				current.Volume -= remaining;
				remaining = 0m;
			}
		}
	}

	private decimal GetMidPrice()
	{
		return (_bestBid + _bestAsk) / 2m;
	}

	private void ValidateVolume(decimal volume)
	{
		if (volume <= 0m || Security == null)
			return;

		if (Security.MinVolume is { } minVolume && volume < minVolume - VolumeTolerance)
			throw new InvalidOperationException($"Volume {volume} is less than the minimal allowed {minVolume}.");

		if (Security.MaxVolume is { } maxVolume && volume > maxVolume + VolumeTolerance)
			throw new InvalidOperationException($"Volume {volume} is greater than the maximal allowed {maxVolume}.");

		if (Security.VolumeStep is { } step && step > 0m)
		{
			var steps = Math.Round(volume / step);
			var normalized = steps * step;
			if (Math.Abs(normalized - volume) > VolumeTolerance)
				throw new InvalidOperationException($"Volume {volume} is not a multiple of the minimal step {step}.");
		}
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

	private static bool IsOrderCompleted(Order order)
	{
		return order.State == OrderStates.Done
			|| order.State == OrderStates.Failed
			|| order.State == OrderStates.Cancelled;
	}
}

