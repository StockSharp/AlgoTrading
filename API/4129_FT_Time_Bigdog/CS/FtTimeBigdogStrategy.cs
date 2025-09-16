using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// London breakout style strategy converted from the MetaTrader expert advisor "FT TIME BIGDOG".
/// </summary>
public class FtTimeBigdogStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<decimal> _rangeLimitPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _orderBufferPoints;
	private readonly StrategyParam<decimal> _pointMultiplier;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _priceStep;
	private DateTime? _currentDate;
	private decimal? _sessionHigh;
	private decimal? _sessionLow;
	private bool _rangeCompleted;
	private bool _ordersPlaced;
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;
	private Sides? _activeSide;
	private decimal? _activeStopPrice;
	private decimal? _activeTakePrice;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	/// <summary>
	/// Initializes a new instance of the <see cref="FtTimeBigdogStrategy"/> class.
	/// </summary>
	public FtTimeBigdogStrategy()
	{
		_startHour = Param(nameof(StartHour), 14)
			.SetDisplay("Start hour", "Hour when the accumulation window begins", "Timing")
			.SetRange(0, 23)
			.SetCanOptimize(true);

		_stopHour = Param(nameof(StopHour), 16)
			.SetDisplay("Stop hour", "Hour when pending orders become eligible", "Timing")
			.SetRange(0, 23)
			.SetCanOptimize(true);

		_rangeLimitPoints = Param(nameof(RangeLimitPoints), 50m)
			.SetDisplay("Max range (points)", "Maximum allowed distance between high and low of the session", "Filters")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetDisplay("Take profit (points)", "Take profit distance measured in broker points", "Risk management")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_orderBufferPoints = Param(nameof(OrderBufferPoints), 20m)
			.SetDisplay("Buffer (points)", "Minimum distance between the current price and the pending order", "Entries")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_pointMultiplier = Param(nameof(PointMultiplier), 1m)
			.SetDisplay("Point multiplier", "Multiplier applied to the instrument point size", "General")
			.SetGreaterThanZero();

		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Order volume used for both stop entries", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Timeframe used to evaluate the breakout window", "General");
	}

	/// <summary>
	/// Hour when the accumulation window begins.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour when pending orders become eligible.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Maximum allowed distance between session high and low measured in broker points.
	/// </summary>
	public decimal RangeLimitPoints
	{
		get => _rangeLimitPoints.Value;
		set => _rangeLimitPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in broker points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum distance required between the market price and each pending order.
	/// </summary>
	public decimal OrderBufferPoints
	{
		get => _orderBufferPoints.Value;
		set => _orderBufferPoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the instrument point size. Use 10 for five digit forex symbols.
	/// </summary>
	public decimal PointMultiplier
	{
		get => _pointMultiplier.Value;
		set => _pointMultiplier.Value = value;
	}

	/// <summary>
	/// Volume used for both buy stop and sell stop orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle series used to measure the breakout window.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_priceStep = 0m;
		_currentDate = null;
		_sessionHigh = null;
		_sessionLow = null;
		_rangeCompleted = false;
		_ordersPlaced = false;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;
		_activeSide = null;
		_activeStopPrice = null;
		_activeTakePrice = null;
		_bestBid = null;
		_bestAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;

		if (_priceStep <= 0m)
			throw new InvalidOperationException("Security price step is not defined.");

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();
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
			_pendingLongStop = null;
			_pendingLongTake = null;

			CancelIfActive(ref _sellStopOrder);
		}
		else if (Position < 0m)
		{
			_activeSide = Sides.Sell;
			_activeStopPrice = _pendingShortStop;
			_activeTakePrice = _pendingShortTake;
			_pendingShortStop = null;
			_pendingShortTake = null;

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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (StartHour > StopHour)
		{
			return;
		}

		var candleTime = candle.OpenTime.UtcDateTime;

		if (_currentDate == null || _currentDate.Value.Date != candleTime.Date)
		{
			ResetDailyState(candleTime.Date);
		}

		UpdateRange(candle);
		ProcessActivePosition(candle);

		if (!_rangeCompleted || _ordersPlaced)
			return;

		if (_sessionHigh is not decimal rangeHigh || _sessionLow is not decimal rangeLow)
			return;

		var rangeSize = rangeHigh - rangeLow;
		var maxRange = ConvertPoints(RangeLimitPoints);

		if (rangeSize <= 0m || rangeSize >= maxRange)
			return;

		var orderDistance = ConvertPoints(OrderBufferPoints);
		var takeDistance = ConvertPoints(TakeProfitPoints);

		var askReference = _bestAsk ?? candle.ClosePrice;
		var bidReference = _bestBid ?? candle.ClosePrice;

		if (_buyStopOrder == null && Position <= 0m && rangeHigh - askReference > orderDistance)
		{
			var buyPrice = NormalizePrice(rangeHigh);
			_pendingLongStop = NormalizePrice(rangeLow);
			_pendingLongTake = NormalizePrice(rangeHigh + takeDistance);
			_buyStopOrder = BuyStop(Volume, buyPrice);
			_ordersPlaced = true;
		}

		if (_sellStopOrder == null && Position >= 0m && bidReference - rangeLow > orderDistance)
		{
			var sellPrice = NormalizePrice(rangeLow);
			_pendingShortStop = NormalizePrice(rangeHigh);
			_pendingShortTake = NormalizePrice(rangeLow - takeDistance);
			_sellStopOrder = SellStop(Volume, sellPrice);
			_ordersPlaced = true;
		}
	}

	private void UpdateRange(ICandleMessage candle)
	{
		var candleTime = candle.OpenTime.UtcDateTime;
		var hour = candleTime.Hour;

		if (hour < StartHour)
			return;

		if (hour > StopHour)
		{
			_rangeCompleted = _sessionHigh.HasValue && _sessionLow.HasValue;
			return;
		}

		_sessionHigh = _sessionHigh.HasValue ? Math.Max(_sessionHigh.Value, candle.HighPrice) : candle.HighPrice;
		_sessionLow = _sessionLow.HasValue ? Math.Min(_sessionLow.Value, candle.LowPrice) : candle.LowPrice;

		if (hour == StopHour)
			_rangeCompleted = true;
	}

	private void ProcessActivePosition(ICandleMessage candle)
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
			}
		}
		else if (_activeSide == Sides.Sell && Position < 0m)
		{
			if (_activeStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(Math.Abs(Position));
				ResetActivePosition();
				return;
			}

			if (_activeTakePrice is decimal shortTake && candle.LowPrice <= shortTake)
			{
				BuyMarket(Math.Abs(Position));
				ResetActivePosition();
			}
		}
	}

	private void ResetDailyState(DateTime date)
	{
		_currentDate = date;
		_sessionHigh = null;
		_sessionLow = null;
		_rangeCompleted = false;
		_ordersPlaced = false;
		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;
		ResetActivePosition();
		CancelIfActive(ref _buyStopOrder);
		CancelIfActive(ref _sellStopOrder);
	}

	private void ResetActivePosition()
	{
		_activeSide = null;
		_activeStopPrice = null;
		_activeTakePrice = null;
	}

	private void CancelIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private decimal ConvertPoints(decimal points)
	{
		return points * _priceStep * PointMultiplier;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		return security != null ? security.ShrinkPrice(price) : price;
	}
}
