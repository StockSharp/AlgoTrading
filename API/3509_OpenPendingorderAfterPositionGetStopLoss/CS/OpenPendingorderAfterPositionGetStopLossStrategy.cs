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
/// Places pending stop orders based on the slope of the Stochastic %K line
/// and immediately restores the cancelled direction after a stop-loss exit.
/// </summary>
public class OpenPendingorderAfterPositionGetStopLossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _minStopDistancePoints;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<TimeSpan?> _pendingExpiry;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic = null!;
	private decimal? _lastK;
	private decimal? _lastK1;
	private decimal? _lastK2;
	private decimal _pointValue;
	private decimal _bestBid;
	private decimal _bestAsk;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _longStopLossOrder;
	private Order _longTakeProfitOrder;
	private Order _shortStopLossOrder;
	private Order _shortTakeProfitOrder;

	private decimal? _pendingLongStopPrice;
	private decimal? _pendingLongTakePrice;
	private decimal? _pendingShortStopPrice;
	private decimal? _pendingShortTakePrice;

	private DateTimeOffset? _buyStopExpiration;
	private DateTimeOffset? _sellStopExpiration;

	/// <summary>
	/// Trade volume for every pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in symbol points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in symbol points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum distance (in points) required between current price and a new pending order.
	/// </summary>
	public int MinStopDistancePoints
	{
		get => _minStopDistancePoints.Value;
		set => _minStopDistancePoints.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous positions per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Number of bars used for the %K calculation.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars used for the %D smoothing line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing factor applied to %K.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Expiration interval for pending stop orders.
	/// </summary>
	public TimeSpan? PendingExpiry
	{
		get => _pendingExpiry.Value;
		set => _pendingExpiry.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the Stochastic oscillator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenPendingorderAfterPositionGetStopLossStrategy"/> class.
	/// </summary>
	public OpenPendingorderAfterPositionGetStopLossStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Trade volume for each pending stop order", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Stop-loss distance in instrument points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 900)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Take-profit distance in instrument points", "Risk");

		_minStopDistancePoints = Param(nameof(MinStopDistancePoints), 0)
		.SetNotNegative()
		.SetDisplay("Min Distance (points)", "Minimal offset between price and a pending order", "Trading");

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum simultaneous positions per direction", "Trading");

		_kPeriod = Param(nameof(KPeriod), 22)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Number of bars used for the %K line", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Smoothing period applied to %K", "Indicators");

		_slowing = Param(nameof(Slowing), 2)
		.SetGreaterThanZero()
		.SetDisplay("Slowing", "Additional smoothing factor for %K", "Indicators");

		_pendingExpiry = Param<TimeSpan?>(nameof(PendingExpiry), TimeSpan.FromDays(1))
		.SetDisplay("Pending Expiry", "Lifetime of pending stop orders", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for indicator calculations", "General");
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

		_stochastic = null!;
		_lastK = null;
		_lastK1 = null;
		_lastK2 = null;
		_pointValue = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopLossOrder = null;
		_longTakeProfitOrder = null;
		_shortStopLossOrder = null;
		_shortTakeProfitOrder = null;

		_pendingLongStopPrice = null;
		_pendingLongTakePrice = null;
		_pendingShortStopPrice = null;
		_pendingShortTakePrice = null;

		_buyStopExpiration = null;
		_sellStopExpiration = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pointValue = Security?.PriceStep ?? 0m;
		Volume = OrderVolume;

		_stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			KPeriod = KPeriod,
			DPeriod = DPeriod,
			Smooth = Slowing
		};

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.BindEx(_stochastic, ProcessCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(depth =>
		{
			_bestBid = depth.GetBestBid()?.Price ?? _bestBid;
			_bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
		});

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!stochasticValue.IsFinal)
		return;

		if (_pointValue <= 0m)
		_pointValue = Security?.PriceStep ?? 0m;

		if (_bestBid <= 0m)
		_bestBid = candle.ClosePrice;

		if (_bestAsk <= 0m)
		_bestAsk = candle.ClosePrice;

		// Cancel pending orders whose lifetime expired on the last bar.
		CancelExpiredOrders(candle.CloseTime);

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.K is not decimal currentK)
		return;

		_lastK2 = _lastK1;
		_lastK1 = _lastK;
		_lastK = currentK;

		if (_lastK2 is not decimal kTwoBarsAgo)
		return;

		var longPositions = Position > 0 ? 1 : 0;
		var shortPositions = Position < 0 ? 1 : 0;

		// Falling %K line -> place a short breakout order.
		if (_lastK < kTwoBarsAgo
		&& shortPositions < MaxPositions
		&& !IsOrderActive(_sellStopOrder)
		&& !HasActiveShortProtection())
		{
			PlaceSellStop(candle.CloseTime);
		}

		// Rising %K line -> place a long breakout order.
		if (_lastK > kTwoBarsAgo
		&& longPositions < MaxPositions
		&& !IsOrderActive(_buyStopOrder)
		&& !HasActiveLongProtection())
		{
			PlaceBuyStop(candle.CloseTime);
		}
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
		return;

		// Entry orders finished - prepare protective stop/take orders.
		if (order == _buyStopOrder)
		{
			_buyStopOrder = null;
			RegisterLongProtection(trade.Trade.Volume);
		}
		else if (order == _sellStopOrder)
		{
			_sellStopOrder = null;
			RegisterShortProtection(trade.Trade.Volume);
		}
		// Stop-loss orders closed the position - reinstall the corresponding pending entry.
		else if (order == _longStopLossOrder)
		{
			_longStopLossOrder = null;
			CancelOrderIfActive(ref _longTakeProfitOrder);
			TryReopenLong(trade.Trade.ServerTime);
		}
		else if (order == _shortStopLossOrder)
		{
			_shortStopLossOrder = null;
			CancelOrderIfActive(ref _shortTakeProfitOrder);
			TryReopenShort(trade.Trade.ServerTime);
		}
		// Take-profit orders remove the paired protective stop.
		else if (order == _longTakeProfitOrder)
		{
			_longTakeProfitOrder = null;
			CancelOrderIfActive(ref _longStopLossOrder);
		}
		else if (order == _shortTakeProfitOrder)
		{
			_shortTakeProfitOrder = null;
			CancelOrderIfActive(ref _shortStopLossOrder);
		}
	}

	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			if (order == _buyStopOrder)
			{
				_buyStopOrder = null;
				_buyStopExpiration = null;
			}
			else if (order == _sellStopOrder)
			{
				_sellStopOrder = null;
				_sellStopExpiration = null;
			}
			else if (order == _longStopLossOrder)
			{
				_longStopLossOrder = null;
			}
			else if (order == _longTakeProfitOrder)
			{
				_longTakeProfitOrder = null;
			}
			else if (order == _shortStopLossOrder)
			{
				_shortStopLossOrder = null;
			}
			else if (order == _shortTakeProfitOrder)
			{
				_shortTakeProfitOrder = null;
			}
		}
	}

	protected override void OnStopped()
	{
		base.OnStopped();

		CancelOrderIfActive(ref _buyStopOrder);
		CancelOrderIfActive(ref _sellStopOrder);
		CancelOrderIfActive(ref _longStopLossOrder);
		CancelOrderIfActive(ref _longTakeProfitOrder);
		CancelOrderIfActive(ref _shortStopLossOrder);
		CancelOrderIfActive(ref _shortTakeProfitOrder);
	}

	private void PlaceBuyStop(DateTimeOffset referenceTime)
	{
		var volume = OrderVolume;
		if (volume <= 0m)
		return;

		// Use spread plus minimal distance as an offset from the best ask.
		var stopOffset = GetStopOffset();
		var ask = _bestAsk > 0m ? _bestAsk : (_bestBid > 0m ? _bestBid : 0m);
		if (ask <= 0m || stopOffset <= 0m)
		return;

		var price = NormalizePrice(ask + stopOffset);
		if (price <= 0m)
		return;

		_pendingLongStopPrice = StopLossPoints > 0 ? NormalizePrice(price - _pointValue * StopLossPoints) : null;
		_pendingLongTakePrice = TakeProfitPoints > 0 ? NormalizePrice(price + _pointValue * TakeProfitPoints) : null;

		_buyStopOrder = BuyStop(price: price, volume: volume);
		_buyStopExpiration = PendingExpiry.HasValue ? referenceTime + PendingExpiry.Value : null;
	}

	private void PlaceSellStop(DateTimeOffset referenceTime)
	{
		var volume = OrderVolume;
		if (volume <= 0m)
		return;

		// Use spread plus minimal distance as an offset from the best bid.
		var stopOffset = GetStopOffset();
		var bid = _bestBid > 0m ? _bestBid : (_bestAsk > 0m ? _bestAsk : 0m);
		if (bid <= 0m || stopOffset <= 0m)
		return;

		var price = NormalizePrice(bid - stopOffset);
		if (price <= 0m)
		return;

		_pendingShortStopPrice = StopLossPoints > 0 ? NormalizePrice(price + _pointValue * StopLossPoints) : null;
		_pendingShortTakePrice = TakeProfitPoints > 0 ? NormalizePrice(price - _pointValue * TakeProfitPoints) : null;

		_sellStopOrder = SellStop(price: price, volume: volume);
		_sellStopExpiration = PendingExpiry.HasValue ? referenceTime + PendingExpiry.Value : null;
	}

	private void RegisterLongProtection(decimal volume)
	{
		CancelOrderIfActive(ref _longStopLossOrder);
		CancelOrderIfActive(ref _longTakeProfitOrder);

		if (volume <= 0m)
		volume = Math.Abs(Position);

		if (_pendingLongStopPrice is decimal stopLoss)
		{
			// Place a sell stop to protect the new long position.
			_longStopLossOrder = SellStop(price: stopLoss, volume: volume);
		}

		if (_pendingLongTakePrice is decimal takeProfit)
		{
			// Place a sell limit to take profits on the long position.
			_longTakeProfitOrder = SellLimit(price: takeProfit, volume: volume);
		}

		_pendingLongStopPrice = null;
		_pendingLongTakePrice = null;
	}

	private void RegisterShortProtection(decimal volume)
	{
		CancelOrderIfActive(ref _shortStopLossOrder);
		CancelOrderIfActive(ref _shortTakeProfitOrder);

		if (volume <= 0m)
		volume = Math.Abs(Position);

		if (_pendingShortStopPrice is decimal stopLoss)
		{
			// Place a buy stop to protect the new short position.
			_shortStopLossOrder = BuyStop(price: stopLoss, volume: volume);
		}

		if (_pendingShortTakePrice is decimal takeProfit)
		{
			// Place a buy limit to capture profits on the short position.
			_shortTakeProfitOrder = BuyLimit(price: takeProfit, volume: volume);
		}

		_pendingShortStopPrice = null;
		_pendingShortTakePrice = null;
	}

	private void TryReopenLong(DateTimeOffset time)
	{
		if (Position > 0 || IsOrderActive(_buyStopOrder) || MaxPositions <= 0)
		return;

		PlaceBuyStop(time);
	}

	private void TryReopenShort(DateTimeOffset time)
	{
		if (Position < 0 || IsOrderActive(_sellStopOrder) || MaxPositions <= 0)
		return;

		PlaceSellStop(time);
	}

	private void CancelExpiredOrders(DateTimeOffset now)
	{
		if (_buyStopOrder != null
		&& _buyStopExpiration is DateTimeOffset buyExpiry
		&& now >= buyExpiry)
		{
			CancelOrder(_buyStopOrder);
			_buyStopExpiration = null;
		}

		if (_sellStopOrder != null
		&& _sellStopExpiration is DateTimeOffset sellExpiry
		&& now >= sellExpiry)
		{
			CancelOrder(_sellStopOrder);
			_sellStopExpiration = null;
		}
	}

	private bool HasActiveLongProtection()
	{
		return IsOrderActive(_longStopLossOrder) || IsOrderActive(_longTakeProfitOrder);
	}

	private bool HasActiveShortProtection()
	{
		return IsOrderActive(_shortStopLossOrder) || IsOrderActive(_shortTakeProfitOrder);
	}

	private static bool IsOrderActive(Order order)
	{
		return order != null && order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order == null)
		return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private decimal GetStopOffset()
	{
		var minDistance = _pointValue * MinStopDistancePoints;
		var spreadOffset = (_bestAsk > 0m && _bestBid > 0m) ? Math.Abs(_bestAsk - _bestBid) : 0m;

		var offset = spreadOffset + minDistance;
		if (offset <= 0m)
		offset = _pointValue > 0m ? _pointValue : 0m;

		return offset;
	}

	private decimal NormalizePrice(decimal price)
	{
		var priceStep = Security?.PriceStep;
		if (priceStep == null || priceStep == 0m)
		return price;

		var steps = Math.Round(price / priceStep.Value, MidpointRounding.AwayFromZero);
		return steps * priceStep.Value;
	}
}

