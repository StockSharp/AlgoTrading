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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending stop breakout strategy converted from the original MetaTrader "Champion" expert advisor.
/// Places three stop orders after an RSI signal, manages dynamic position sizing, and trails stop-loss orders.
/// </summary>
public class ChampionStrategy : Strategy
{
	private readonly StrategyParam<int> _pendingOrderCount;

	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLevel;
	private readonly StrategyParam<decimal> _balancePerLot;
	private readonly StrategyParam<decimal> _minOrderDistancePoints;
	private readonly StrategyParam<decimal> _repriceDistancePoints;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _previousRsiValue;

	private readonly List<Order> _buyStopOrders = new();
	private readonly List<Order> _sellStopOrders = new();

	private Order _stopOrder;
	private Order _takeProfitOrder;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _stopLevelDistance;

	private decimal _pointValue;

	private static readonly Level1Fields? StopLevelField = TryGetField("StopLevel")
	?? TryGetField("MinStopPrice")
	?? TryGetField("StopPrice")
	?? TryGetField("StopDistance");

	/// <summary>
	/// Initializes a new instance of the <see cref="ChampionStrategy"/> class.
	/// </summary>
	public ChampionStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150)
		.SetDisplay("Take Profit (points)", "MetaTrader take-profit distance expressed in points.", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 50)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (points)", "MetaTrader stop-loss distance expressed in points.", "Risk")
		.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of the RSI oscillator.", "Indicators")
		.SetCanOptimize(true);

		_rsiLevel = Param(nameof(RsiLevel), 30m)
		.SetDisplay("RSI Level", "Oversold threshold mirrored for overbought detection (100 - level).", "Indicators")
		.SetCanOptimize(true);

		_balancePerLot = Param(nameof(BalancePerLot), 2000m)
		.SetGreaterThanZero()
		.SetDisplay("Balance per Lot", "Account currency amount required to trade one standard lot.", "Position Sizing");

		_minOrderDistancePoints = Param(nameof(MinOrderDistancePoints), 0m)
		.SetDisplay("Minimum Distance (points)", "Fallback distance for stop orders when the trading server does not expose stop levels.", "Orders");

		_repriceDistancePoints = Param(nameof(RepriceDistancePoints), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Reprice Distance (points)", "Distance that triggers stop order repricing when the spread widens.", "Orders");

		_pendingOrderCount = Param(nameof(PendingOrderCount), 3)
		.SetGreaterThanZero()
		.SetDisplay("Pending Orders", "Number of stop entries placed after each signal.", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time-frame used to calculate the RSI signal.", "Data");
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Length of the RSI oscillator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set
		{
			_rsiPeriod.Value = value;

			if (_rsi != null)
			_rsi.Length = value;
		}
	}

	/// <summary>
	/// Oversold threshold mirrored to detect overbought conditions (100 - level).
	/// </summary>
	public decimal RsiLevel
	{
		get => _rsiLevel.Value;
		set => _rsiLevel.Value = value;
	}

	/// <summary>
	/// Account currency amount associated with a single lot when calculating dynamic volume.
	/// </summary>
	public decimal BalancePerLot
	{
		get => _balancePerLot.Value;
		set => _balancePerLot.Value = value;
	}

	/// <summary>
	/// Fallback distance (in MetaTrader points) applied when the trading venue does not provide a stop level.
	/// </summary>
	public decimal MinOrderDistancePoints
	{
		get => _minOrderDistancePoints.Value;
		set => _minOrderDistancePoints.Value = value;
	}

	/// <summary>
	/// Distance (in MetaTrader points) that triggers stop order repricing.
	/// </summary>
	public decimal RepriceDistancePoints
	{
		get => _repriceDistancePoints.Value;
		set => _repriceDistancePoints.Value = value;
	}

	/// <summary>
	/// Candle type used for RSI calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of stop entries placed after each RSI signal.
	/// </summary>
	public int PendingOrderCount
	{
		get => _pendingOrderCount.Value;
		set => _pendingOrderCount.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;

		if (security != null)
		{
			yield return (security, CandleType);
			yield return (security, DataType.Level1);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyStopOrders.Clear();
		_sellStopOrders.Clear();
		_stopOrder = null;
		_takeProfitOrder = null;
		_previousRsiValue = null;
		_bestBid = null;
		_bestAsk = null;
		_stopLevelDistance = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security ?? throw new InvalidOperationException("Security is not set.");

		_pointValue = security.PriceStep ?? 0.0001m;
		if (_pointValue <= 0m)
		_pointValue = 0.0001m;

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription.Bind(_rsi, ProcessCandle).Start();

		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_rsi.IsFormed)
		{
			_previousRsiValue = rsiValue;
			return;
		}

		if (_previousRsiValue is null)
		{
			_previousRsiValue = rsiValue;
			return;
		}

		CleanupInactiveOrders();

		TryPlacePendingOrders(_previousRsiValue.Value);

		_previousRsiValue = rsiValue;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
		_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
		_bestAsk = ask;

		if (StopLevelField is Level1Fields field && message.Changes.TryGetValue(field, out var stopRaw))
		{
			var converted = ToDecimal(stopRaw);
			if (converted is decimal distance && distance > 0m)
			{
				var stopDistance = distance;
				if (_pointValue > 0m && stopDistance >= 1m)
				stopDistance *= _pointValue;

				_stopLevelDistance = stopDistance;
			}
		}

		CleanupInactiveOrders();
		RepricePendingOrders();
		UpdateTrailingStops();
	}

	private void TryPlacePendingOrders(decimal previousRsi)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m || HasActivePendingOrders())
		return;

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
		return;

		var totalVolume = CalculateBaseVolume();
		if (totalVolume <= 0m)
		return;

		var perOrderVolume = RoundVolume(totalVolume / PendingOrderCount);
		if (perOrderVolume <= 0m)
		return;

		var entryDistance = GetEffectiveStopDistance();

		if (previousRsi < RsiLevel)
		{
			PlaceBuyStops(ask, perOrderVolume, entryDistance);
		}
		else if (previousRsi > 100m - RsiLevel)
		{
			PlaceSellStops(bid, perOrderVolume, entryDistance);
		}
	}

	private void PlaceBuyStops(decimal ask, decimal volume, decimal distance)
	{
		var offset = distance > 0m ? distance : _pointValue;
		var price = NormalizePrice(ask + offset);

		for (var i = 0; i < PendingOrderCount; i++)
		{
			var order = BuyStop(volume, price);
			if (order != null)
			_buyStopOrders.Add(order);
		}
	}

	private void PlaceSellStops(decimal bid, decimal volume, decimal distance)
	{
		var offset = distance > 0m ? distance : _pointValue;
		var price = NormalizePrice(bid - offset);

		for (var i = 0; i < PendingOrderCount; i++)
		{
			var order = SellStop(volume, price);
			if (order != null)
			_sellStopOrders.Add(order);
		}
	}

	private void RepricePendingOrders()
	{
		var repriceDistance = GetRepriceDistance();
		if (repriceDistance <= 0m)
		return;

		var minDistance = GetEffectiveStopDistance();
		var desiredDistance = Math.Max(repriceDistance, minDistance);
		var tolerance = _pointValue > 0m ? _pointValue / 2m : 0m;

		if (_bestAsk is decimal ask)
		{
			for (var i = 0; i < _buyStopOrders.Count; i++)
			{
				var order = _buyStopOrders[i];
				if (!IsOrderActive(order))
				continue;

				var difference = order.Price - ask;
				if (difference > repriceDistance + tolerance)
				{
					var balance = order.Balance ?? order.Volume ?? 0m;
					if (balance <= 0m)
					{
						CancelOrder(order);
						continue;
					}

					var newPrice = NormalizePrice(ask + desiredDistance);
					CancelOrder(order);

					var newOrder = BuyStop(balance, newPrice);
					if (newOrder != null)
					_buyStopOrders[i] = newOrder;
				}
			}
		}

		if (_bestBid is decimal bid)
		{
			for (var i = 0; i < _sellStopOrders.Count; i++)
			{
				var order = _sellStopOrders[i];
				if (!IsOrderActive(order))
				continue;

				var difference = bid - order.Price;
				if (difference > repriceDistance + tolerance)
				{
					var balance = order.Balance ?? order.Volume ?? 0m;
					if (balance <= 0m)
					{
						CancelOrder(order);
						continue;
					}

					var newPrice = NormalizePrice(bid - desiredDistance);
					CancelOrder(order);

					var newOrder = SellStop(balance, newPrice);
					if (newOrder != null)
					_sellStopOrders[i] = newOrder;
				}
			}
		}
	}

	private void UpdateTrailingStops()
	{
		if (Position == 0m)
		return;

		var stopDistance = GetStopLossDistance();
		if (stopDistance <= 0m)
		return;

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
		return;

		var entryPrice = PositionPrice;
		var spread = Math.Max(0m, ask - bid);
		var stopLevel = GetEffectiveStopDistance();

		if (Position > 0m)
		{
			var threshold = bid - stopLevel - spread;
			if (threshold <= entryPrice)
			return;

			var candidate = Math.Max(entryPrice + spread, bid - stopDistance);
			candidate = NormalizePrice(candidate);

			if (_stopOrder == null || _stopOrder.Price + (_pointValue > 0m ? _pointValue / 2m : 0m) < candidate)
			UpdateStopOrder(candidate, true);
		}
		else if (Position < 0m)
		{
			var threshold = entryPrice - stopLevel - spread;
			if (threshold <= ask)
			return;

			var candidate = Math.Min(entryPrice - spread, ask + stopDistance);
			candidate = NormalizePrice(candidate);

			if (_stopOrder == null || _stopOrder.Price - (_pointValue > 0m ? _pointValue / 2m : 0m) > candidate)
			UpdateStopOrder(candidate, false);
		}
	}

	private void UpdateStopOrder(decimal stopPrice, bool isLong)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		CancelProtectiveOrder(ref _stopOrder);
		_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
	}

	private void UpdateTakeProfitOrder(decimal takePrice, bool isLong)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		CancelProtectiveOrder(ref _takeProfitOrder);
		_takeProfitOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
	}

	private void EnsureProtectiveOrders()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var entryPrice = PositionPrice;
		var stopDistance = GetStopLossDistance();
		var takeDistance = GetTakeProfitDistance();
		var isLong = Position > 0m;

		if (stopDistance > 0m)
		{
			var stopPrice = isLong ? NormalizePrice(entryPrice - stopDistance) : NormalizePrice(entryPrice + stopDistance);
			UpdateStopOrder(stopPrice, isLong);
		}
		else
		{
			CancelProtectiveOrder(ref _stopOrder);
		}

		if (takeDistance > 0m)
		{
			var takePrice = isLong ? NormalizePrice(entryPrice + takeDistance) : NormalizePrice(entryPrice - takeDistance);
			UpdateTakeProfitOrder(takePrice, isLong);
		}
		else
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
		}
	}

	private void CancelProtectiveOrder(ref Order order)
	{
		if (order == null)
		return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private void CleanupInactiveOrders()
	{
		for (var i = _buyStopOrders.Count - 1; i >= 0; i--)
		{
			var order = _buyStopOrders[i];
			if (order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
			_buyStopOrders.RemoveAt(i);
		}

		for (var i = _sellStopOrders.Count - 1; i >= 0; i--)
		{
			var order = _sellStopOrders[i];
			if (order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
			_sellStopOrders.RemoveAt(i);
		}
	}

	private decimal CalculateBaseVolume()
	{
		var freeMargin = Portfolio?.CurrentValue ?? 0m;
		var reference = freeMargin > 0m && BalancePerLot > 0m ? freeMargin / BalancePerLot : Volume;

		if (reference <= 0m)
		reference = Volume;

		if (reference <= 0m)
		return 0m;

		if (reference < 0.1m)
		reference = 0.1m;
		else if (reference > 15m)
		reference = 15m;

		reference = Math.Round(reference, 2, MidpointRounding.AwayFromZero);

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(reference / step, MidpointRounding.AwayFromZero));
			reference = steps * step;
		}

		return Math.Round(reference, 2, MidpointRounding.AwayFromZero);
	}

	private decimal RoundVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		return Math.Round(volume, 2, MidpointRounding.AwayFromZero);
	}

	private decimal GetStopLossDistance()
	{
		return StopLossPoints > 0 ? StopLossPoints * _pointValue : 0m;
	}

	private decimal GetTakeProfitDistance()
	{
		return TakeProfitPoints > 0 ? TakeProfitPoints * _pointValue : 0m;
	}

	private decimal GetEffectiveStopDistance()
	{
		var fallback = MinOrderDistancePoints * _pointValue;
		if (_stopLevelDistance is decimal level && level > 0m)
		fallback = Math.Max(fallback, level);

		return fallback;
	}

	private decimal GetRepriceDistance()
	{
		return RepriceDistancePoints * _pointValue;
	}

	private bool HasActivePendingOrders()
	{
		foreach (var order in _buyStopOrders)
		{
			if (IsOrderActive(order))
			return true;
		}

		foreach (var order in _sellStopOrders)
		{
			if (IsOrderActive(order))
			return true;
		}

		return false;
	}

	private static bool IsOrderActive(Order order)
	{
		return order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private static Level1Fields? TryGetField(string name)
	{
		return Enum.TryParse(name, out Level1Fields field) ? field : null;
	}

	private static decimal? ToDecimal(object value)
	{
		return value switch
		{
			decimal dec => dec,
			double dbl => (decimal)dbl,
			float fl => (decimal)fl,
			long l => l,
			int i => i,
			short s => s,
			byte b => b,
			null => null,
			IConvertible convertible => Convert.ToDecimal(convertible, System.Globalization.CultureInfo.InvariantCulture),
			_ => null
		};
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null || order.Security != Security)
		return;

		_buyStopOrders.Remove(order);
		_sellStopOrders.Remove(order);

		if (Position != 0m)
		EnsureProtectiveOrders();
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == null || order.Security != Security)
		return;

		if (order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			_buyStopOrders.Remove(order);
			_sellStopOrders.Remove(order);

			if (_stopOrder == order)
			_stopOrder = null;

			if (_takeProfitOrder == order)
			_takeProfitOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
		}
	}
}
