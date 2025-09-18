using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range based grid strategy converted from the MetaTrader expert "RangeEA".
/// It builds a limit order grid across the weekly trading range and manages profits automatically.
/// </summary>
public class RangeEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _weeklyCandleType;
	private readonly StrategyParam<int> _startTradeHour;
	private readonly StrategyParam<int> _endTradeHour;
	private readonly StrategyParam<bool> _closeAllAtEndTrade;
	private readonly StrategyParam<int> _maxOpenOrders;
	private readonly StrategyParam<int> _numberOfOrders;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _resetOrdersDaily;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<decimal> _targetPercentage;

	private DateTime? _lastResetDay;
	private decimal? _previousWeeklyHigh;
	private decimal? _previousWeeklyLow;
	private decimal? _currentWeeklyHigh;
	private decimal? _currentWeeklyLow;
	private decimal _rangeHigh;
	private decimal _rangeLow;
	private decimal _tradingRange;
	private decimal? _initialEquity;

	private readonly List<decimal> _executedPrices = new();

	/// <summary>
	/// Candle type used for regular strategy processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the higher timeframe trading range.
	/// </summary>
	public DataType WeeklyCandleType
	{
		get => _weeklyCandleType.Value;
		set => _weeklyCandleType.Value = value;
	}

	/// <summary>
	/// Hour of day when new orders may start being placed.
	/// </summary>
	public int StartTradeHour
	{
		get => _startTradeHour.Value;
		set => _startTradeHour.Value = value;
	}

	/// <summary>
	/// Hour of day when trading stops and optional liquidation occurs.
	/// </summary>
	public int EndTradeHour
	{
		get => _endTradeHour.Value;
		set => _endTradeHour.Value = value;
	}

	/// <summary>
	/// When enabled all positions and orders are closed after the trading window ends.
	/// </summary>
	public bool CloseAllAtEndTrade
	{
		get => _closeAllAtEndTrade.Value;
		set => _closeAllAtEndTrade.Value = value;
	}

	/// <summary>
	/// Maximum allowed number of simultaneously open orders and positions.
	/// </summary>
	public int MaxOpenOrders
	{
		get => _maxOpenOrders.Value;
		set => _maxOpenOrders.Value = value;
	}

	/// <summary>
	/// Number of pending limit orders distributed inside the weekly range.
	/// </summary>
	public int NumberOfOrders
	{
		get => _numberOfOrders.Value;
		set => _numberOfOrders.Value = value;
	}

	/// <summary>
	/// Trading volume used for each limit order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Defines whether the pending grid is cleared at the beginning of each day.
	/// </summary>
	public bool ResetOrdersDaily
	{
		get => _resetOrdersDaily.Value;
		set => _resetOrdersDaily.Value = value;
	}

	/// <summary>
	/// Minimum stop-loss distance measured in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Minimum take-profit distance measured in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Multiplier used to enlarge the calculated stop-loss distance.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier used to enlarge the calculated take-profit distance.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Percentage of equity growth that triggers full liquidation.
	/// </summary>
	public decimal TargetPercentage
	{
		get => _targetPercentage.Value;
		set => _targetPercentage.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RangeEaStrategy"/>.
	/// </summary>
	public RangeEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Trading candle", "Primary candle type for grid management", "General");

		_weeklyCandleType = Param(nameof(WeeklyCandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("Weekly candle", "Higher timeframe candle used to derive the range", "General");

		_startTradeHour = Param(nameof(StartTradeHour), 0)
			.SetDisplay("Start hour", "Hour when the strategy can begin placing orders", "Schedule")
			.SetCanOptimize(true);

		_endTradeHour = Param(nameof(EndTradeHour), 24)
			.SetDisplay("End hour", "Hour after which new trades are blocked", "Schedule")
			.SetCanOptimize(true);

		_closeAllAtEndTrade = Param(nameof(CloseAllAtEndTrade), true)
			.SetDisplay("Liquidate at end", "Close orders and positions outside the trading window", "Schedule");

		_maxOpenOrders = Param(nameof(MaxOpenOrders), 5)
			.SetDisplay("Max open orders", "Maximum amount of simultaneous positions and pending orders", "Risk")
			.SetCanOptimize(true);

		_numberOfOrders = Param(nameof(NumberOfOrders), 10)
			.SetDisplay("Grid orders", "Number of pending limit orders inside the range", "Trading")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetDisplay("Order volume", "Volume used for each limit order", "Trading")
			.SetGreaterThanZero();

		_resetOrdersDaily = Param(nameof(ResetOrdersDaily), true)
			.SetDisplay("Reset daily", "Rebuild the pending grid every new trading day", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 60m)
			.SetDisplay("Stop-loss points", "Minimum stop-loss distance in points", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 60m)
			.SetDisplay("Take-profit points", "Minimum take-profit distance in points", "Risk")
			.SetCanOptimize(true);

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 3m)
			.SetDisplay("Stop multiplier", "Multiplier applied to the dynamic stop-loss distance", "Risk")
			.SetCanOptimize(true);

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 1m)
			.SetDisplay("Take multiplier", "Multiplier applied to the dynamic take-profit distance", "Risk")
			.SetCanOptimize(true);

		_targetPercentage = Param(nameof(TargetPercentage), 8m)
			.SetDisplay("Target percentage", "Equity gain percentage that forces liquidation", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_initialEquity = Portfolio?.CurrentValue;

		var tradeSubscription = SubscribeCandles(CandleType);
		tradeSubscription
			.Bind(ProcessTradingCandle)
			.Start();

		var weeklySubscription = SubscribeCandles(WeeklyCandleType);
		weeklySubscription
			.Bind(ProcessWeeklyCandle)
			.Start();
	}

	private void ProcessWeeklyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previousWeeklyHigh = _currentWeeklyHigh;
		_previousWeeklyLow = _currentWeeklyLow;
		_currentWeeklyHigh = candle.HighPrice;
		_currentWeeklyLow = candle.LowPrice;

		if (_previousWeeklyHigh.HasValue && _previousWeeklyLow.HasValue &&
			_currentWeeklyHigh.HasValue && _currentWeeklyLow.HasValue)
		{
			var high = Math.Max(_previousWeeklyHigh.Value, _currentWeeklyHigh.Value);
			var low = Math.Min(_previousWeeklyLow.Value, _currentWeeklyLow.Value);

			_rangeHigh = high;
			_rangeLow = low;
			_tradingRange = Math.Max(0m, high - low);
		}
	}

	private void ProcessTradingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var barTime = candle.CloseTime;

		HandleDailyReset(barTime);

		if (!IsWithinTradingWindow(barTime))
		{
			if (CloseAllAtEndTrade)
			{
				CloseAllPositionsAndOrders();
			}

			return;
		}

		if (_tradingRange <= 0m || NumberOfOrders <= 0)
			return;

		if (GetPendingLimitOrderCount() == 0)
		{
			PlaceGridOrders(candle);
		}

		EnsureReplacementOrder(candle.ClosePrice);
		CheckTargetGain();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Trade is null)
			return;

		var order = trade.Order;
		if (order is null)
			return;

		if (order.Type != OrderTypes.Limit)
			return;

		if (order.Balance > 0m)
			return;

		_executedPrices.Add(trade.Trade.Price);
	}

	private void PlaceGridOrders(ICandleMessage candle)
	{
		var distance = NumberOfOrders > 0 ? _tradingRange / NumberOfOrders : 0m;
		if (distance <= 0m)
			return;

		var currentPrice = candle.ClosePrice;

		for (var index = 0; index < NumberOfOrders; index++)
		{
			if (MaxOpenOrders > 0 && GetOpenOrdersCount() >= MaxOpenOrders)
				return;

			var price = NormalizePrice(_rangeLow + distance * index);
			if (price <= 0m)
				continue;

			if (price > currentPrice)
			{
				TryPlaceSellLimit(price, currentPrice);
			}
			else
			{
				TryPlaceBuyLimit(price, currentPrice);
			}
		}
	}

	private void EnsureReplacementOrder(decimal currentPrice)
	{
		if (_executedPrices.Count <= 1)
			return;

		if (MaxOpenOrders > 0 && GetOpenOrdersCount() >= MaxOpenOrders)
			return;

		if (GetPendingLimitOrderCount() != NumberOfOrders - 2)
			return;

		var penultimatePrice = _executedPrices[_executedPrices.Count - 2];

		if (penultimatePrice > currentPrice)
		{
			TryPlaceSellLimit(penultimatePrice, currentPrice);
		}
		else
		{
			TryPlaceBuyLimit(penultimatePrice, currentPrice);
		}
	}

	private void TryPlaceSellLimit(decimal price, decimal referencePrice)
	{
		if (OrderVolume <= 0m)
			return;

		var difference = price - referencePrice;
		if (difference <= 0m)
			difference = PointsToPrice(StopLossPoints);

		var stopLossDistance = Math.Max(difference * StopLossMultiplier, PointsToPrice(StopLossPoints));
		var takeProfitDistance = Math.Max(difference * TakeProfitMultiplier, PointsToPrice(TakeProfitPoints));

		var stopLossPrice = NormalizePrice(price + stopLossDistance);
		var takeProfitPrice = NormalizePrice(price - takeProfitDistance);

		if (stopLossPrice <= price || takeProfitPrice >= price)
			return;

		SellLimit(OrderVolume, price, stopLoss: stopLossPrice, takeProfit: takeProfitPrice);
	}

	private void TryPlaceBuyLimit(decimal price, decimal referencePrice)
	{
		if (OrderVolume <= 0m)
			return;

		var difference = referencePrice - price;
		if (difference <= 0m)
			difference = PointsToPrice(StopLossPoints);

		var stopLossDistance = Math.Max(difference * StopLossMultiplier, PointsToPrice(StopLossPoints));
		var takeProfitDistance = Math.Max(difference * TakeProfitMultiplier, PointsToPrice(TakeProfitPoints));

		var stopLossPrice = NormalizePrice(price - stopLossDistance);
		var takeProfitPrice = NormalizePrice(price + takeProfitDistance);

		if (stopLossPrice >= price || takeProfitPrice <= price)
			return;

		BuyLimit(OrderVolume, price, stopLoss: stopLossPrice, takeProfit: takeProfitPrice);
	}

	private void HandleDailyReset(DateTimeOffset time)
	{
		var currentDay = time.Date;
		if (_lastResetDay.HasValue && _lastResetDay.Value == currentDay)
			return;

		_lastResetDay = currentDay;
		_executedPrices.Clear();

		if (ResetOrdersDaily)
		{
			CancelPendingOrders();
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= StartTradeHour && hour < EndTradeHour;
	}

	private void CloseAllPositionsAndOrders()
	{
		CancelPendingOrders();

		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private void CancelPendingOrders()
	{
		var pendingOrders = new List<Order>();
		foreach (var order in ActiveOrders)
		{
			if (order.Type == OrderTypes.Limit)
			{
				pendingOrders.Add(order);
			}
		}

		foreach (var order in pendingOrders)
		{
			CancelOrder(order);
		}
	}

	private void CheckTargetGain()
	{
		if (TargetPercentage <= 0m)
			return;

		var portfolio = Portfolio;
		if (portfolio is null)
			return;

		var currentValue = portfolio.CurrentValue;
		if (!_initialEquity.HasValue)
		{
			_initialEquity = currentValue;
			return;
		}

		var initial = _initialEquity.Value;
		if (initial <= 0m)
			return;

		var gain = currentValue - initial;
		var gainPercent = gain / initial * 100m;

		if (gainPercent >= TargetPercentage)
		{
			CloseAllPositionsAndOrders();
		}
	}

	private int GetOpenOrdersCount()
	{
		var count = 0;
		foreach (var order in ActiveOrders)
		{
			if (order.State == OrderStates.Pending || order.State == OrderStates.Active)
			{
				count++;
			}
		}

		if (Position != 0m)
		{
			count++;
		}

		return count;
	}

	private int GetPendingLimitOrderCount()
	{
		var count = 0;
		foreach (var order in ActiveOrders)
		{
			if (order.Type == OrderTypes.Limit &&
				(order.State == OrderStates.Pending || order.State == OrderStates.Active))
			{
				count++;
			}
		}

		return count;
	}

	private decimal PointsToPrice(decimal points)
	{
		var step = Security?.PriceStep;
		if (step.HasValue && step.Value > 0m)
			return points * step.Value;

		return points;
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}
}
