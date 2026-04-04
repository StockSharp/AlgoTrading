namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo;
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
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _ordersSpacePips;
	private readonly StrategyParam<int> _pkPips;

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
	private decimal _currentPrice;
	private bool _hasPriceData;

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
	/// Candle type used to drive the grid logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Initializes a new instance of the <see cref="Zs1ForexInstrumentsStrategy"/> class.
	/// </summary>
	public Zs1ForexInstrumentsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle timeframe for price sampling.", "General");

		_ordersSpacePips = Param(nameof(OrdersSpacePips), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Orders Space (pips)", "Distance between successive grid levels.", "Trading")
			.SetOptimize(100m, 2000m, 100m);

		_pkPips = Param(nameof(PkPips), 10)
			.SetNotNegative()
			.SetDisplay("Zone Offset (pips)", "Additional offset applied when checking zone boundaries.", "Trading");
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
		_currentPrice = 0m;
		_hasPriceData = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipValue = CalculatePipValue();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_currentPrice = candle.ClosePrice;
		_hasPriceData = true;

		if (!_hasPriceData || _currentPrice <= 0m)
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

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Trade == null)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;
		var side = trade.Order?.Side;

		if (side == null)
			return;

		// Determine intent based on position context
		if (side == Sides.Buy)
		{
			if (Position < 0 || _isClosingAll)
			{
				// Closing short
				ReduceEntries(_shortEntries, volume);
			}
			else
			{
				// Opening long
				_longEntries.Add(new Entry(price, volume));
				_lastOrderDirection = Sides.Buy;
			}
		}
		else
		{
			if (Position > 0 || _isClosingAll)
			{
				// Closing long
				ReduceEntries(_longEntries, volume);
			}
			else
			{
				// Opening short
				_shortEntries.Add(new Entry(price, volume));
				_lastOrderDirection = Sides.Sell;
			}
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
		BuyMarket();
		SellMarket();

		_firstStage = 1;
		_zone = 0;
		_lastZone = 0;
		_zoneChanged = false;
		_firstPrice = _currentPrice;
		_firstOrderDirection = null;
		_lastOrderDirection = null;
	}

	private void CloseFirstOrders()
	{
		if (_longEntries.Count > 0 && _currentPrice > _longEntries[0].Price)
		{
			// Long is profitable, close it, keep short
			SellMarket();
			_firstStage = 2;
			_firstOrderDirection = Sides.Sell;
			_lastOrderDirection = Sides.Sell;
			return;
		}

		if (_shortEntries.Count > 0 && _currentPrice < _shortEntries[0].Price)
		{
			// Short is profitable, close it, keep long
			BuyMarket();
			_firstStage = 2;
			_firstOrderDirection = Sides.Buy;
			_lastOrderDirection = Sides.Buy;
		}
	}

	private void OpenBuyOrder()
	{
		BuyMarket();
	}

	private void OpenSellOrder()
	{
		SellMarket();
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

		// Close by selling longs and buying back shorts
		if (_longEntries.Any())
		{
			var totalLong = _longEntries.Sum(e => e.Volume);
			if (totalLong > 0m)
				SellMarket(totalLong);
		}

		if (_shortEntries.Any())
		{
			var totalShort = _shortEntries.Sum(e => e.Volume);
			if (totalShort > 0m)
				BuyMarket(totalShort);
		}

		if (!_longEntries.Any() && !_shortEntries.Any())
		{
			ResetState();
			_isClosingAll = false;
		}
	}

	private void ResetState()
	{
		_longEntries.Clear();
		_shortEntries.Clear();
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
		var price = _currentPrice + offset;

		if (price >= _firstPrice + step * (_zone + 1))
		{
			_lastZone = _zone;
			_zone++;
			_zoneChanged = true;
		}
		else if (price <= _firstPrice - step * (1 - _zone))
		{
			_lastZone = _zone;
			_zone--;
			_zoneChanged = true;
		}

		if (_zoneChanged && _zone == _lastZone)
		{
			_zoneChanged = false;
		}
	}

	private int GetOrdersTotal()
	{
		return _longEntries.Count + _shortEntries.Count;
	}

	private decimal CalculateFloatingProfit()
	{
		if (!_hasPriceData)
			return 0m;

		decimal profit = 0m;

		foreach (var entry in _longEntries)
		{
			profit += (_currentPrice - entry.Price) * entry.Volume;
		}

		foreach (var entry in _shortEntries)
		{
			profit += (entry.Price - _currentPrice) * entry.Volume;
		}

		return profit;
	}

	private void ReduceEntries(List<Entry> entries, decimal volume)
	{
		var remaining = volume;

		while (remaining > 0m && entries.Count > 0)
		{
			var current = entries[0];
			if (current.Volume <= remaining + 0.0001m)
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

	private decimal CalculatePipValue()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		return step;
	}
}
