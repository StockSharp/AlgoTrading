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
/// Conversion of the LBS_V12 MetaTrader expert advisor.
/// Places buy and sell stop orders around the previous 15-minute candle at a configurable hour.
/// </summary>
public class LbsV12Strategy : Strategy
{
	private readonly StrategyParam<int> _triggerHour;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private Subscription _subscription;

	private ICandleMessage _previousCandle;
	private DateTime? _currentDate;
	private DateTime? _lastTriggerDate;
	private bool _ordersPlaced;

	private decimal _priceStep;

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private Order _buyStopOrder;
	private Order _sellStopOrder;

	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;

	private decimal? _pendingLongEntryPrice;
	private decimal? _pendingShortEntryPrice;

	private Sides? _activeSide;
	private decimal? _activeStopPrice;
	private decimal? _activeTakePrice;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;


	/// <summary>
	/// Hour (terminal time) when stop orders should be placed.
	/// </summary>
	public int TriggerHour
	{
		get => _triggerHour.Value;
		set => _triggerHour.Value = value;
	}

	/// <summary>
	/// Take profit distance in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in MetaTrader points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// ATR period used to offset pending orders.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="LbsV12Strategy"/>.
	/// </summary>
	public LbsV12Strategy()
	{

		_triggerHour = Param(nameof(TriggerHour), 9)
			.SetDisplay("Trigger Hour", "Hour when stop orders are sent", "Execution")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 20m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Average True Range length", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.CreateCandleTimeFrame(TimeSpan.FromMinutes(15)))
			.SetDisplay("Signal Candles", "Candle type used for signals", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security ?? throw new InvalidOperationException("Security is not set.");

		_priceStep = security.PriceStep ?? 1m;

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		_subscription = SubscribeCandles(CandleType);
		_subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleTime = candle.OpenTime.UtcDateTime;
		var date = candleTime.Date;

		if (_currentDate == null || _currentDate.Value != date)
		{
			ResetDailyState(date);
		}

		ManageActivePosition(candle);

		var referenceCandle = _previousCandle;
		_previousCandle = candle;

		if (referenceCandle == null)
			return;

		if (candleTime.Hour == TriggerHour && candleTime.Minute == 0 && _lastTriggerDate != date)
		{
			PlaceStopOrders(referenceCandle, atrValue, date);
		}
	}

	private void PlaceStopOrders(ICandleMessage referenceCandle, decimal atrValue, DateTime date)
	{
		if (Volume <= 0m)
			return;

		if (_ordersPlaced || Position != 0m)
			return;

		var step = _priceStep > 0m ? _priceStep : 1m;
		var spread = _bestAsk.HasValue && _bestBid.HasValue ? Math.Max(_bestAsk.Value - _bestBid.Value, 0m) : step;
		var takeDistance = ConvertPoints(TakeProfitPoints);

		var buyPrice = NormalizePrice(referenceCandle.HighPrice + step + spread + atrValue);
		var longStop = NormalizePrice(referenceCandle.LowPrice - step);
		var longTake = takeDistance > 0m ? NormalizePrice(buyPrice + takeDistance) : (decimal?)null;

		var sellPrice = NormalizePrice(referenceCandle.LowPrice - step - spread - atrValue);
		var shortStop = NormalizePrice(referenceCandle.HighPrice + step);
		var shortTake = takeDistance > 0m ? NormalizePrice(sellPrice - takeDistance) : (decimal?)null;

		_buyStopOrder = BuyStop(Volume, buyPrice);
		_pendingLongStop = longStop;
		_pendingLongTake = longTake;
		_pendingLongEntryPrice = buyPrice;

		_sellStopOrder = SellStop(Volume, sellPrice);
		_pendingShortStop = shortStop;
		_pendingShortTake = shortTake;
		_pendingShortEntryPrice = sellPrice;

		_ordersPlaced = true;
		_lastTriggerDate = date;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (_activeSide == null)
			return;

		if (_activeSide == Sides.Buy && Position > 0m)
		{
			if (_activeStopPrice is decimal longStop && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				ResetActivePosition();
				return;
			}

			if (_activeTakePrice is decimal longTake && candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				ResetActivePosition();
				return;
			}

			UpdateTrailingStopForLong(candle);
		}
		else if (_activeSide == Sides.Sell && Position < 0m)
		{
			var positionVolume = Math.Abs(Position);

			if (_activeStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(positionVolume);
				ResetActivePosition();
				return;
			}

			if (_activeTakePrice is decimal shortTake && candle.LowPrice <= shortTake)
			{
				BuyMarket(positionVolume);
				ResetActivePosition();
				return;
			}

			UpdateTrailingStopForShort(candle);
		}
	}

	private void UpdateTrailingStopForLong(ICandleMessage candle)
	{
		var trailingDistance = ConvertPoints(TrailingStopPoints);

		if (trailingDistance <= 0m || _longEntryPrice is not decimal entry)
			return;

		var maxPrice = candle.HighPrice;

		if (maxPrice - entry <= trailingDistance)
			return;

		var newStop = NormalizePrice(maxPrice - trailingDistance);

		if (_activeStopPrice is not decimal currentStop || newStop > currentStop)
			_activeStopPrice = newStop;
	}

	private void UpdateTrailingStopForShort(ICandleMessage candle)
	{
		var trailingDistance = ConvertPoints(TrailingStopPoints);

		if (trailingDistance <= 0m || _shortEntryPrice is not decimal entry)
			return;

		var minPrice = candle.LowPrice;

		if (entry - minPrice <= trailingDistance)
			return;

		var newStop = NormalizePrice(minPrice + trailingDistance);

		if (_activeStopPrice is not decimal currentStop || newStop < currentStop)
			_activeStopPrice = newStop;
	}

	private void ResetDailyState(DateTime date)
	{
		_currentDate = date;
		_ordersPlaced = false;
		_lastTriggerDate = null;

		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;
		_pendingLongEntryPrice = null;
		_pendingShortEntryPrice = null;

		CancelIfActive(ref _buyStopOrder);
		CancelIfActive(ref _sellStopOrder);
	}

	private void ResetActivePosition()
	{
		_activeSide = null;
		_activeStopPrice = null;
		_activeTakePrice = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	private void CancelIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private decimal ConvertPoints(decimal points)
	{
		var step = _priceStep > 0m ? _priceStep : 1m;
		return points * step;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		return security != null ? security.ShrinkPrice(price) : price;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyStopOrder != null && order == _buyStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_buyStopOrder = null;

		if (_sellStopOrder != null && order == _sellStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetActivePosition();
			return;
		}

		if (Position > 0m)
		{
			_activeSide = Sides.Buy;
			_activeStopPrice = _pendingLongStop;
			_activeTakePrice = _pendingLongTake;
			_longEntryPrice = _pendingLongEntryPrice;

			_pendingLongStop = null;
			_pendingLongTake = null;
			_pendingLongEntryPrice = null;

			CancelIfActive(ref _sellStopOrder);
		}
		else if (Position < 0m)
		{
			_activeSide = Sides.Sell;
			_activeStopPrice = _pendingShortStop;
			_activeTakePrice = _pendingShortTake;
			_shortEntryPrice = _pendingShortEntryPrice;

			_pendingShortStop = null;
			_pendingShortTake = null;
			_pendingShortEntryPrice = null;

			CancelIfActive(ref _buyStopOrder);
		}
	}

	/// <inheritdoc />
	protected override void OnLevel1(Security security, Level1ChangeMessage message)
	{
		base.OnLevel1(security, message);

		if (security != Security)
			return;

		if (message.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
			_bestBid = bid;

		if (message.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
			_bestAsk = ask;
	}
}

