namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class TwentyOneHourStrategy : Strategy
{
	// Parameter that controls the order volume in lots.
	private readonly StrategyParam<decimal> _volume;

	// Parameter defining the hour when pending orders are created.
	private readonly StrategyParam<int> _startHour;

	// Parameter defining the hour when positions are closed and pending orders removed.
	private readonly StrategyParam<int> _stopHour;

	// Parameter that stores the distance in points between market price and stop entries.
	private readonly StrategyParam<int> _stepPoints;

	// Parameter that stores the take-profit distance in points.
	private readonly StrategyParam<int> _takeProfitPoints;

	// Parameter that configures the candle type used for time tracking.
	private readonly StrategyParam<DataType> _candleType;

	// Cached best bid price from level1 updates.
	private decimal? _bestBid;

	// Cached best ask price from level1 updates.
	private decimal? _bestAsk;

	// Reference to the currently active buy stop order.
	private Order _buyStopOrder;

	// Reference to the currently active sell stop order.
	private Order _sellStopOrder;

	// Cached price step that converts point distances to absolute prices.
	private decimal _priceStep;

	// Date when pending orders were last placed.
	private DateTime? _lastPlacementDate;

	// Date when the daily cleanup was last executed.
	private DateTime? _lastCleanupDate;

	public TwentyOneHourStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "Trading");

		_startHour = Param(nameof(StartHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour to place pending orders", "Schedule");

		_stopHour = Param(nameof(StopHour), 22)
			.SetRange(0, 23)
			.SetDisplay("Stop Hour", "Hour to close positions", "Schedule");

		_stepPoints = Param(nameof(StepPoints), 15)
			.SetGreaterThanZero()
			.SetDisplay("Entry Offset (points)", "Distance from bid/ask to place stops", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200)
			.SetDisplay("Take Profit (points)", "Target distance from entry in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for time tracking", "General");
	}

	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

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

		_bestBid = null;
		_bestAsk = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_lastPlacementDate = null;
		_lastCleanupDate = null;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;

		if (TakeProfitPoints > 0)
		{
			StartProtection(takeProfit: new Unit(TakeProfitPoints * _priceStep, UnitTypes.Point));
		}

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription.Bind(ProcessCandle).Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var bid = level1.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid != null)
			_bestBid = bid;

		var ask = level1.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask != null)
			_bestAsk = ask;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		EnsurePendingConsistency();

		var candleTime = candle.OpenTime;

		if (candleTime.Hour == StartHour && candleTime.Minute == 0)
			TryPlacePendingOrders(candle);

		if (candleTime.Hour == StopHour && candleTime.Minute == 0)
			HandleStopWindow(candleTime);
	}

	private void TryPlacePendingOrders(ICandleMessage candle)
	{
		var date = candle.OpenTime.Date;

		if (_lastPlacementDate == date)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Volume <= 0m)
			return;

		var ask = _bestAsk ?? Security?.BestAskPrice ?? candle.ClosePrice;
		var bid = _bestBid ?? Security?.BestBidPrice ?? candle.ClosePrice;

		if (ask <= 0m || bid <= 0m)
			return;

		var stepDistance = StepPoints * _priceStep;

		if (stepDistance <= 0m)
			return;

		var buyPrice = RoundPrice(ask + stepDistance);
		var sellPrice = RoundPrice(bid - stepDistance);

		if (buyPrice <= 0m || sellPrice <= 0m)
			return;

		CancelPendingOrders();

		var buyOrder = BuyStop(Volume, buyPrice);
		if (buyOrder != null)
			_buyStopOrder = buyOrder;

		var sellOrder = SellStop(Volume, sellPrice);
		if (sellOrder != null)
			_sellStopOrder = sellOrder;

		if (_buyStopOrder != null || _sellStopOrder != null)
			_lastPlacementDate = date;
	}

	private void HandleStopWindow(DateTimeOffset time)
	{
		var date = time.Date;

		if (_lastCleanupDate == date)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ClosePositions();
		CancelPendingOrders();
		_lastCleanupDate = date;
	}

	private void ClosePositions()
	{
		var position = Position;

		if (position > 0m)
		{
			SellMarket(position);
		}
		else if (position < 0m)
		{
			BuyMarket(Math.Abs(position));
		}
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null)
		{
			if (_buyStopOrder.State.IsActive())
				CancelOrder(_buyStopOrder);

			_buyStopOrder = null;
		}

		if (_sellStopOrder != null)
		{
			if (_sellStopOrder.State.IsActive())
				CancelOrder(_sellStopOrder);

			_sellStopOrder = null;
		}
	}

	private void EnsurePendingConsistency()
	{
		if (_buyStopOrder != null && !_buyStopOrder.State.IsActive())
			_buyStopOrder = null;

		if (_sellStopOrder != null && !_sellStopOrder.State.IsActive())
			_sellStopOrder = null;

		var buyActive = _buyStopOrder != null && _buyStopOrder.State.IsActive();
		var sellActive = _sellStopOrder != null && _sellStopOrder.State.IsActive();

		if (buyActive == sellActive)
			return;

		CancelPendingOrders();
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;

		if (step == null || step.Value <= 0m)
			return price;

		return Math.Round(price / step.Value, 0, MidpointRounding.AwayFromZero) * step.Value;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null || order.Security != Security)
			return;

		if (_buyStopOrder != null && order == _buyStopOrder && !_buyStopOrder.State.IsActive())
			_buyStopOrder = null;

		if (_sellStopOrder != null && order == _sellStopOrder && !_sellStopOrder.State.IsActive())
			_sellStopOrder = null;
	}
}
