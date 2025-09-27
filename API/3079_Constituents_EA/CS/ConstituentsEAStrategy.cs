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
/// Ports the Constituents MetaTrader strategy to the StockSharp high level API.
/// The strategy places a pair of pending orders around the recent high/low range at a scheduled hour.
/// </summary>
public class ConstituentsEaStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _searchDepth;
	private readonly StrategyParam<OrderMode> _orderMode;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _pointValue;
	private readonly StrategyParam<decimal> _minOrderDistancePips;
	private readonly StrategyParam<decimal> _minStopDistancePips;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private Order _buyOrder;
	private Order _sellOrder;
	private decimal _pointValueAbsolute;
	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Type of pending order to place when the entry conditions are met.
	/// </summary>
	public enum OrderMode
	{
		/// <summary>
		/// Place limit orders at the identified support and resistance levels.
		/// </summary>
		Limit,

		/// <summary>
		/// Place stop orders at the identified breakout levels.
		/// </summary>
		Stop
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ConstituentsEaStrategy"/> class.
	/// </summary>
	public ConstituentsEaStrategy()
	{
		_startHour = Param(nameof(StartHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour (0-23) when pending orders are submitted", "General");

		_searchDepth = Param(nameof(SearchDepth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Search Depth", "Number of completed candles used to find extremes", "Setup")
			.SetCanOptimize(true);

		_orderMode = Param(nameof(PendingOrderMode), OrderMode.Limit)
			.SetDisplay("Order Type", "Pending order style (limit or stop)", "Setup");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk");

		_pointValue = Param(nameof(PointValue), 0.0001m)
			.SetNotNegative()
			.SetDisplay("Point Value", "Price value of one pip (0 = auto from security)", "Risk");

		_minOrderDistancePips = Param(nameof(MinOrderDistancePips), 0m)
			.SetNotNegative()
			.SetDisplay("Min Order Distance", "Minimum distance (in pips) between price and pending orders", "Risk");

		_minStopDistancePips = Param(nameof(MinStopDistancePips), 0m)
			.SetNotNegative()
			.SetDisplay("Min Stop Distance", "Minimal distance (in pips) required for stop/take placement", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe used to evaluate highs/lows", "General");
	}

	/// <summary>
	/// Hour (0-23) when the strategy submits fresh pending orders.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Number of completed candles used to determine the recent range.
	/// </summary>
	public int SearchDepth
	{
		get => _searchDepth.Value;
		set => _searchDepth.Value = value;
	}

	/// <summary>
	/// Pending order style (limit or stop).
	/// </summary>
	public OrderMode PendingOrderMode
	{
		get => _orderMode.Value;
		set => _orderMode.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Pip size expressed in price units. Set to zero to auto detect from the security.
	/// </summary>
	public decimal PointValue
	{
		get => _pointValue.Value;
		set => _pointValue.Value = value;
	}

	/// <summary>
	/// Minimum distance between current prices and the pending order entry price (in pips).
	/// </summary>
	public decimal MinOrderDistancePips
	{
		get => _minOrderDistancePips.Value;
		set => _minOrderDistancePips.Value = value;
	}

	/// <summary>
	/// Minimum distance required for stop loss or take profit values (in pips).
	/// </summary>
	public decimal MinStopDistancePips
	{
		get => _minStopDistancePips.Value;
		set => _minStopDistancePips.Value = value;
	}

	/// <summary>
	/// Working candle type.
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

		_highest = null!;
		_lowest = null!;
		_buyOrder = null;
		_sellOrder = null;
		_pointValueAbsolute = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValueAbsolute = ResolvePointValue();

		_highest = new Highest { Length = SearchDepth };
		_lowest = new Lowest { Length = SearchDepth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();

		var takeDistance = TakeProfitPips * _pointValueAbsolute;
		var stopDistance = StopLossPips * _pointValueAbsolute;
		Unit takeProfitUnit = takeDistance > 0m ? new Unit(takeDistance, UnitTypes.Absolute) : null;
		Unit stopLossUnit = stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : null;
		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_highest.Length != SearchDepth)
			_highest.Length = SearchDepth;

		if (_lowest.Length != SearchDepth)
			_lowest.Length = SearchDepth;

		var highValue = _highest.Process(candle.HighPrice).ToDecimal();
		var lowValue = _lowest.Process(candle.LowPrice).ToDecimal();

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (HasActivePendingOrders())
			return;

		var nextOpenTime = GetNextOpenTime(candle);
		if (nextOpenTime == null || nextOpenTime.Value.Hour != StartHour)
			return;

		_pointValueAbsolute = ResolvePointValue();

		var stopDistance = StopLossPips * _pointValueAbsolute;
		var takeDistance = TakeProfitPips * _pointValueAbsolute;
		var minOrderDistance = MinOrderDistancePips * _pointValueAbsolute;
		var minStopDistance = MinStopDistancePips * _pointValueAbsolute;

		var stopValid = StopLossPips <= 0m || stopDistance >= minStopDistance;
		var takeValid = TakeProfitPips <= 0m || takeDistance >= minStopDistance;

		if (!stopValid || !takeValid)
		{
			LogWarning("Stop or take profit distance is below the configured minimum. Pending orders skipped.");
			return;
		}

		var referenceBid = _bestBid > 0m ? _bestBid : candle.ClosePrice;
		var referenceAsk = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;

		if (PendingOrderMode == OrderMode.Limit)
		{
			TryPlaceBuyLimit(lowValue, referenceBid, minOrderDistance);
			TryPlaceSellLimit(highValue, referenceAsk, minOrderDistance);
		}
		else
		{
			TryPlaceBuyStop(highValue, referenceAsk, minOrderDistance);
			TryPlaceSellStop(lowValue, referenceBid, minOrderDistance);
		}
	}

	private void ProcessOrderBook(QuoteChangeMessage depth)
	{
		var bestBid = depth.GetBestBid();
		if (bestBid != null)
			_bestBid = bestBid.Price;

		var bestAsk = depth.GetBestAsk();
		if (bestAsk != null)
			_bestAsk = bestAsk.Price;
	}

	private DateTimeOffset? GetNextOpenTime(ICandleMessage candle)
	{
		if (candle.CloseTime != default)
			return candle.CloseTime;

		if (CandleType.Arg is TimeSpan span)
			return candle.OpenTime + span;

		return null;
	}

	private void TryPlaceBuyLimit(decimal price, decimal referenceBid, decimal minDistance)
	{
		if (referenceBid <= 0m)
			return;

		if (referenceBid - price < minDistance)
			return;

		var order = BuyLimit(price);
		if (order != null)
			_buyOrder = order;
	}

	private void TryPlaceSellLimit(decimal price, decimal referenceAsk, decimal minDistance)
	{
		if (referenceAsk <= 0m)
			return;

		if (price - referenceAsk < minDistance)
			return;

		var order = SellLimit(price);
		if (order != null)
			_sellOrder = order;
	}

	private void TryPlaceBuyStop(decimal price, decimal referenceAsk, decimal minDistance)
	{
		if (referenceAsk <= 0m)
			return;

		if (price - referenceAsk < minDistance)
			return;

		var order = BuyStop(price);
		if (order != null)
			_buyOrder = order;
	}

	private void TryPlaceSellStop(decimal price, decimal referenceBid, decimal minDistance)
	{
		if (referenceBid <= 0m)
			return;

		if (referenceBid - price < minDistance)
			return;

		var order = SellStop(price);
		if (order != null)
			_sellOrder = order;
	}

	private bool HasActivePendingOrders()
	{
		if (_buyOrder != null && !_buyOrder.State.IsActive())
			_buyOrder = null;

		if (_sellOrder != null && !_sellOrder.State.IsActive())
			_sellOrder = null;

		if (_buyOrder != null && _buyOrder.State.IsActive())
			return true;

		if (_sellOrder != null && _sellOrder.State.IsActive())
			return true;

		foreach (var order in Orders)
		{
			if ((order.Type == OrderTypes.Limit || order.Type == OrderTypes.Stop) && order.State.IsActive())
				return true;
		}

		return false;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade?.Order;
		if (order == null)
			return;

		if (_buyOrder != null && order == _buyOrder)
		{
			_buyOrder = null;
			if (_sellOrder != null && _sellOrder.State.IsActive())
			{
				CancelOrder(_sellOrder);
				_sellOrder = null;
			}
		}
		else if (_sellOrder != null && order == _sellOrder)
		{
			_sellOrder = null;
			if (_buyOrder != null && _buyOrder.State.IsActive())
			{
				CancelOrder(_buyOrder);
				_buyOrder = null;
			}
		}
	}

	private decimal ResolvePointValue()
	{
		var custom = PointValue;
		if (custom > 0m)
			return custom;

		var security = Security;
		if (security != null)
		{
			var step = security.PriceStep;
			if (step != null && step.Value > 0m)
				return step.Value;

			var minStep = security.MinStep;
			if (minStep != null && minStep.Value > 0m)
				return minStep.Value;
		}

		return 0.0001m;
	}
}

