using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that places stop orders on rounded price levels guided by a Kaufman adaptive moving average.
/// </summary>
public class BhsSystemStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossBuyPoints;
	private readonly StrategyParam<int> _stopLossSellPoints;
	private readonly StrategyParam<int> _trailingStopBuyPoints;
	private readonly StrategyParam<int> _trailingStopSellPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<int> _roundStepPoints;
	private readonly StrategyParam<decimal> _expirationHours;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _amaFastPeriod;
	private readonly StrategyParam<int> _amaSlowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousAma;
	private bool _hasPreviousAma;
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private Order? _protectionOrder;
	private DateTimeOffset? _buyOrderTime;
	private DateTimeOffset? _sellOrderTime;
	private decimal? _lastTrailingStopPrice;

	/// <summary>
	/// Trade volume used for both entry and protective orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades expressed in points.
	/// </summary>
	public int StopLossBuyPoints
	{
		get => _stopLossBuyPoints.Value;
		set => _stopLossBuyPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades expressed in points.
	/// </summary>
	public int StopLossSellPoints
	{
		get => _stopLossSellPoints.Value;
		set => _stopLossSellPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points for long positions.
	/// </summary>
	public int TrailingStopBuyPoints
	{
		get => _trailingStopBuyPoints.Value;
		set => _trailingStopBuyPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points for short positions.
	/// </summary>
	public int TrailingStopSellPoints
	{
		get => _trailingStopSellPoints.Value;
		set => _trailingStopSellPoints.Value = value;
	}

	/// <summary>
	/// Minimum step in points between trailing stop updates.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Step used to build rounded trigger prices in points.
	/// </summary>
	public int RoundStepPoints
	{
		get => _roundStepPoints.Value;
		set => _roundStepPoints.Value = value;
	}

	/// <summary>
	/// Lifetime of pending entry orders in hours.
	/// </summary>
	public decimal ExpirationHours
	{
		get => _expirationHours.Value;
		set => _expirationHours.Value = value;
	}

	/// <summary>
	/// Main period of the adaptive moving average.
	/// </summary>
	public int AmaLength
	{
		get => _amaLength.Value;
		set => _amaLength.Value = value;
	}

	/// <summary>
	/// Fast smoothing constant of the adaptive moving average.
	/// </summary>
	public int AmaFastPeriod
	{
		get => _amaFastPeriod.Value;
		set => _amaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothing constant of the adaptive moving average.
	/// </summary>
	public int AmaSlowPeriod
	{
		get => _amaSlowPeriod.Value;
		set => _amaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BhsSystemStrategy"/> class.
	/// </summary>
	public BhsSystemStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Lot size used for entry orders", "Trading");

		_stopLossBuyPoints = Param(nameof(StopLossBuyPoints), 300)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Buy (points)", "Distance in points for long stop loss", "Risk");

		_stopLossSellPoints = Param(nameof(StopLossSellPoints), 300)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Sell (points)", "Distance in points for short stop loss", "Risk");

		_trailingStopBuyPoints = Param(nameof(TrailingStopBuyPoints), 100)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop Buy (points)", "Trailing distance in points for long positions", "Risk");

		_trailingStopSellPoints = Param(nameof(TrailingStopSellPoints), 100)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop Sell (points)", "Trailing distance in points for short positions", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (points)", "Minimum step in points between trailing updates", "Risk");

		_roundStepPoints = Param(nameof(RoundStepPoints), 500)
			.SetGreaterThanZero()
			.SetDisplay("Round Step (points)", "Number of points used to build round price levels", "Execution");

		_expirationHours = Param(nameof(ExpirationHours), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Order Expiration (hours)", "Lifetime of pending entry orders in hours", "Execution");

		_amaLength = Param(nameof(AmaLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("AMA Length", "Adaptive moving average period", "Indicators");

		_amaFastPeriod = Param(nameof(AmaFastPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("AMA Fast Period", "Fast smoothing constant for AMA", "Indicators");

		_amaSlowPeriod = Param(nameof(AmaSlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("AMA Slow Period", "Slow smoothing constant for AMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for analysis", "General");
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

		_previousAma = 0m;
		_hasPreviousAma = false;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_protectionOrder = null;
		_buyOrderTime = null;
		_sellOrderTime = null;
		_lastTrailingStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure the adaptive moving average with user parameters.
		var ama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaLength,
			FastSCPeriod = AmaFastPeriod,
			SlowSCPeriod = AmaSlowPeriod
		};

		// Subscribe to candle data and bind indicator updates.
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ama, ProcessCandle)
			.Start();

		// Draw price, indicator and trades if a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ama);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal amaValue)
	{
		// Work only with finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Store the first AMA value for future comparisons.
		if (!_hasPreviousAma)
		{
			_previousAma = amaValue;
			_hasPreviousAma = true;
			return;
		}

		// Skip trading logic until the strategy is synchronized with the market.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousAma = amaValue;
			return;
		}

		// Remove stale references and expire old orders before making decisions.
		CleanupInactiveOrders();
		CancelExpiredOrders();

		var price = candle.ClosePrice;
		var (_, priceCeil, priceFloor) = CalculateRoundLevels(price);

		var hasPendingOrders = IsOrderActive(_buyStopOrder) || IsOrderActive(_sellStopOrder);
		var hasPosition = Position != 0;

		// Place a new pending order only when there are no open trades or working orders.
		if (!hasPosition && !hasPendingOrders)
		{
			if (price > _previousAma)
			{
				PlaceBuyStop(priceCeil);
			}
			else if (price < _previousAma)
			{
				PlaceSellStop(priceFloor);
			}
		}

		// Update trailing protection for active positions.
		UpdateTrailing(candle);

		_previousAma = amaValue;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			// Reset protection when the position is flat.
			CancelOrderIfActive(ref _protectionOrder);
			_lastTrailingStopPrice = null;
			return;
		}

		// A non-zero delta with a zero previous position means a brand-new trade.
		if (Position > 0 && Position - delta == 0)
		{
			CancelOrderIfActive(ref _buyStopOrder, ref _buyOrderTime);
			CancelOrderIfActive(ref _sellStopOrder, ref _sellOrderTime);
			CreateInitialStop(true);
		}
		else if (Position < 0 && Position - delta == 0)
		{
			CancelOrderIfActive(ref _buyStopOrder, ref _buyOrderTime);
			CancelOrderIfActive(ref _sellStopOrder, ref _sellOrderTime);
			CreateInitialStop(false);
		}
	}

	private (decimal rounded, decimal ceil, decimal floor) CalculateRoundLevels(decimal price)
	{
		var point = Security?.PriceStep ?? 0m;
		var stepPoints = RoundStepPoints;

		// If step settings are not available, return the original price.
		if (point <= 0m || stepPoints <= 0)
			return (price, price, price);

		var step = stepPoints * point;
		if (step <= 0m)
			return (price, price, price);

		var ratio = price / step;
		var roundedIndex = decimal.Round(ratio, 0, MidpointRounding.AwayFromZero);
		var priceRound = roundedIndex * step;

		var ceilIndex = decimal.Ceiling((priceRound + step / 2m) / step);
		var floorIndex = decimal.Floor((priceRound - step / 2m) / step);

		var priceCeil = ceilIndex * step;
		var priceFloor = floorIndex * step;

		return (priceRound, priceCeil, priceFloor);
	}

	private void PlaceBuyStop(decimal price)
	{
		if (OrderVolume <= 0m)
			return;

		// Ensure only one buy stop order exists at a time.
		CancelOrderIfActive(ref _buyStopOrder, ref _buyOrderTime);

		_buyStopOrder = BuyStop(OrderVolume, price);
		_buyOrderTime = CurrentTime;
	}

	private void PlaceSellStop(decimal price)
	{
		if (OrderVolume <= 0m)
			return;

		// Ensure only one sell stop order exists at a time.
		CancelOrderIfActive(ref _sellStopOrder, ref _sellOrderTime);

		_sellStopOrder = SellStop(OrderVolume, price);
		_sellOrderTime = CurrentTime;
	}

	private void CreateInitialStop(bool isLong)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return;

		var distancePoints = isLong ? StopLossBuyPoints : StopLossSellPoints;
		if (distancePoints <= 0)
		{
			CancelOrderIfActive(ref _protectionOrder);
			_lastTrailingStopPrice = null;
			return;
		}

		var distance = distancePoints * step;
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var stopPrice = isLong
			? PositionPrice - distance
			: PositionPrice + distance;

		CancelOrderIfActive(ref _protectionOrder);

		_protectionOrder = isLong
			? SellStop(volume, stopPrice)
			: BuyStop(volume, stopPrice);

		_lastTrailingStopPrice = stopPrice;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (Position > 0)
		{
			ApplyTrailing(candle.ClosePrice, true);
		}
		else if (Position < 0)
		{
			ApplyTrailing(candle.ClosePrice, false);
		}
	}

	private void ApplyTrailing(decimal currentPrice, bool isLong)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return;

		var trailingPoints = isLong ? TrailingStopBuyPoints : TrailingStopSellPoints;
		if (trailingPoints <= 0)
			return;

		var trailingDistance = trailingPoints * step;
		var trailingStep = TrailingStepPoints * step;
		var entryPrice = PositionPrice;

		if (entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (isLong)
		{
			var profit = currentPrice - entryPrice;
			if (profit <= trailingDistance + trailingStep)
				return;

			var newStop = currentPrice - trailingDistance;

			if (_lastTrailingStopPrice is decimal lastStop && newStop <= lastStop + trailingStep)
				return;

			CancelOrderIfActive(ref _protectionOrder);

			_protectionOrder = SellStop(volume, newStop);
			_lastTrailingStopPrice = newStop;
		}
		else
		{
			var profit = entryPrice - currentPrice;
			if (profit <= trailingDistance + trailingStep)
				return;

			var newStop = currentPrice + trailingDistance;

			if (_lastTrailingStopPrice is decimal lastStop && newStop >= lastStop - trailingStep)
				return;

			CancelOrderIfActive(ref _protectionOrder);

			_protectionOrder = BuyStop(volume, newStop);
			_lastTrailingStopPrice = newStop;
		}
	}

	private void CancelExpiredOrders()
	{
		if (ExpirationHours <= 0m)
			return;

		var expiration = TimeSpan.FromHours((double)ExpirationHours);
		var now = CurrentTime;

		if (_buyOrderTime != null && now - _buyOrderTime >= expiration)
			CancelOrderIfActive(ref _buyStopOrder, ref _buyOrderTime);

		if (_sellOrderTime != null && now - _sellOrderTime >= expiration)
			CancelOrderIfActive(ref _sellStopOrder, ref _sellOrderTime);
	}

	private void CleanupInactiveOrders()
	{
		if (_buyStopOrder != null && !IsOrderActive(_buyStopOrder))
		{
			_buyStopOrder = null;
			_buyOrderTime = null;
		}

		if (_sellStopOrder != null && !IsOrderActive(_sellStopOrder))
		{
			_sellStopOrder = null;
			_sellOrderTime = null;
		}

		if (_protectionOrder != null && !IsOrderActive(_protectionOrder))
		{
			_protectionOrder = null;
			_lastTrailingStopPrice = null;
		}
	}

	private void CancelOrderIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (IsOrderActive(order))
			CancelOrder(order);

		order = null;
	}

	private void CancelOrderIfActive(ref Order? order, ref DateTimeOffset? time)
	{
		if (order == null)
			return;

		if (IsOrderActive(order))
			CancelOrder(order);

		order = null;
		time = null;
	}

	private static bool IsOrderActive(Order? order)
	{
		return order != null && (order.State == OrderStates.Active || order.State == OrderStates.Pending);
	}
}
