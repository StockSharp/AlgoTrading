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
/// Pending stop-order breakout strategy inspired by the TrendMeLeaveMe expert advisor.
/// Uses a regression trend line as the dynamic channel center and offsets upper/lower boundaries.
/// When price trades near the trend line it places stop orders that include static stop-loss and take-profit distances.
/// </summary>
public class TrendMeLeaveMeChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _buyStepUpper;
	private readonly StrategyParam<int> _buyStepLower;
	private readonly StrategyParam<int> _sellStepUpper;
	private readonly StrategyParam<int> _sellStepLower;
	private readonly StrategyParam<int> _buyTakeProfitSteps;
	private readonly StrategyParam<int> _buyStopLossSteps;
	private readonly StrategyParam<int> _sellTakeProfitSteps;
	private readonly StrategyParam<int> _sellStopLossSteps;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _sellVolume;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;
	private decimal _entryPrice;

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public TrendMeLeaveMeChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General");

		_trendLength = Param(nameof(TrendLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("Trend Length", "Number of candles used in the regression trend line", "Trend")
		
		.SetOptimize(50, 200, 25);

		_buyStepUpper = Param(nameof(BuyStepUpper), 10)
		.SetGreaterThanZero()
		.SetDisplay("Buy Upper Offset", "Number of price steps added above the trend line for buy stop", "Buy Orders")
		
		.SetOptimize(5, 30, 5);

		_buyStepLower = Param(nameof(BuyStepLower), 50)
		.SetGreaterThanZero()
		.SetDisplay("Buy Lower Offset", "Number of price steps below the trend line that activates buy orders", "Buy Orders")
		
		.SetOptimize(20, 80, 10);

		_sellStepUpper = Param(nameof(SellStepUpper), 50)
		.SetGreaterThanZero()
		.SetDisplay("Sell Upper Offset", "Number of price steps above the trend line that activates sell orders", "Sell Orders")
		
		.SetOptimize(20, 80, 10);

		_sellStepLower = Param(nameof(SellStepLower), 10)
		.SetGreaterThanZero()
		.SetDisplay("Sell Lower Offset", "Number of price steps below the trend line for sell stop", "Sell Orders")
		
		.SetOptimize(5, 30, 5);

		_buyTakeProfitSteps = Param(nameof(BuyTakeProfitSteps), 50)
		.SetGreaterThanZero()
		.SetDisplay("Buy Take Profit", "Take-profit distance in price steps for long trades", "Risk")
		
		.SetOptimize(20, 100, 10);

		_buyStopLossSteps = Param(nameof(BuyStopLossSteps), 30)
		.SetGreaterThanZero()
		.SetDisplay("Buy Stop Loss", "Stop-loss distance in price steps for long trades", "Risk")
		
		.SetOptimize(10, 60, 10);

		_sellTakeProfitSteps = Param(nameof(SellTakeProfitSteps), 50)
		.SetGreaterThanZero()
		.SetDisplay("Sell Take Profit", "Take-profit distance in price steps for short trades", "Risk")
		
		.SetOptimize(20, 100, 10);

		_sellStopLossSteps = Param(nameof(SellStopLossSteps), 30)
		.SetGreaterThanZero()
		.SetDisplay("Sell Stop Loss", "Stop-loss distance in price steps for short trades", "Risk")
		
		.SetOptimize(10, 60, 10);

		_buyVolume = Param(nameof(BuyVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Buy Volume", "Order volume for buy stop entries", "Buy Orders");

		_sellVolume = Param(nameof(SellVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Sell Volume", "Order volume for sell stop entries", "Sell Orders");
	}

	/// <summary>
	/// Candle aggregation used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the regression trend line indicator.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Price steps added above the trend line for buy stop orders.
	/// </summary>
	public int BuyStepUpper
	{
		get => _buyStepUpper.Value;
		set => _buyStepUpper.Value = value;
	}

	/// <summary>
	/// Price steps subtracted below the trend line that activates buy orders.
	/// </summary>
	public int BuyStepLower
	{
		get => _buyStepLower.Value;
		set => _buyStepLower.Value = value;
	}

	/// <summary>
	/// Price steps added above the trend line that activates sell orders.
	/// </summary>
	public int SellStepUpper
	{
		get => _sellStepUpper.Value;
		set => _sellStepUpper.Value = value;
	}

	/// <summary>
	/// Price steps subtracted below the trend line for sell stop orders.
	/// </summary>
	public int SellStepLower
	{
		get => _sellStepLower.Value;
		set => _sellStepLower.Value = value;
	}

	/// <summary>
	/// Take-profit distance (price steps) for long trades.
	/// </summary>
	public int BuyTakeProfitSteps
	{
		get => _buyTakeProfitSteps.Value;
		set => _buyTakeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance (price steps) for long trades.
	/// </summary>
	public int BuyStopLossSteps
	{
		get => _buyStopLossSteps.Value;
		set => _buyStopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance (price steps) for short trades.
	/// </summary>
	public int SellTakeProfitSteps
	{
		get => _sellTakeProfitSteps.Value;
		set => _sellTakeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance (price steps) for short trades.
	/// </summary>
	public int SellStopLossSteps
	{
		get => _sellStopLossSteps.Value;
		set => _sellStopLossSteps.Value = value;
	}

	/// <summary>
	/// Order volume for buy stop entries.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Order volume for sell stop entries.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var regression = new LinearRegression { Length = TrendLength };
		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(regression, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		CleanupInactiveOrders();

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var close = candle.ClosePrice;
		var middle = trendValue;

		var buyUpper = NormalizePrice(middle + BuyStepUpper * priceStep);
		var buyLower = NormalizePrice(middle - BuyStepLower * priceStep);
		var sellUpper = NormalizePrice(middle + SellStepUpper * priceStep);
		var sellLower = NormalizePrice(middle - SellStepLower * priceStep);

		ManageBuyStop(close, middle, buyLower, buyUpper, priceStep);
		ManageSellStop(close, middle, sellUpper, sellLower, priceStep);
	}

	private void ManageBuyStop(decimal close, decimal middle, decimal lower, decimal upper, decimal priceStep)
	{
		var volume = BuyVolume;
		if (volume <= 0m)
		return;

		if (Position > 0m)
		{
			CancelOrderIfActive(ref _buyStopOrder);
			return;
		}

		var shouldPlace = close <= middle && close >= lower;

		if (!shouldPlace)
		{
			CancelOrderIfActive(ref _buyStopOrder);
			return;
		}

		if (_buyStopOrder is null)
		{
			_buyStopOrder = BuyMarket(volume);
			return;
		}

		if (_buyStopOrder.State != OrderStates.Active)
		{
			_buyStopOrder = BuyMarket(volume);
			return;
		}

		var diff = Math.Abs(_buyStopOrder.Price - upper);
		var minDiff = priceStep / 2m;
		if (minDiff <= 0m)
		minDiff = priceStep;

		if (diff >= minDiff)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = BuyMarket(volume);
		}
	}

	private void ManageSellStop(decimal close, decimal middle, decimal upper, decimal lower, decimal priceStep)
	{
		var volume = SellVolume;
		if (volume <= 0m)
		return;

		if (Position < 0m)
		{
			CancelOrderIfActive(ref _sellStopOrder);
			return;
		}

		var shouldPlace = close >= middle && close <= upper;

		if (!shouldPlace)
		{
			CancelOrderIfActive(ref _sellStopOrder);
			return;
		}

		if (_sellStopOrder is null)
		{
			_sellStopOrder = SellMarket(volume);
			return;
		}

		if (_sellStopOrder.State != OrderStates.Active)
		{
			_sellStopOrder = SellMarket(volume);
			return;
		}

		var diff = Math.Abs(_sellStopOrder.Price - lower);
		var minDiff = priceStep / 2m;
		if (minDiff <= 0m)
		minDiff = priceStep;

		if (diff >= minDiff)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		CleanupInactiveOrders();

		if (Position == 0m)
		{
			CancelProtectionOrders();
			return;
		}

		SetupProtection(Position > 0m);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (Position != 0m && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;

		if (Position == 0m)
			_entryPrice = 0m;
	}

	private void SetupProtection(bool isLong)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		var entryPrice = _entryPrice;
		if (entryPrice <= 0m)
		return;

		CancelProtectionOrders();

		if (isLong)
		{
			var stopLoss = NormalizePrice(entryPrice - BuyStopLossSteps * priceStep);
			var takeProfit = NormalizePrice(entryPrice + BuyTakeProfitSteps * priceStep);

			if (BuyStopLossSteps > 0)
			_stopLossOrder = SellMarket(volume);

			if (BuyTakeProfitSteps > 0)
			_takeProfitOrder = SellMarket(volume);
		}
		else
		{
			var stopLoss = NormalizePrice(entryPrice + SellStopLossSteps * priceStep);
			var takeProfit = NormalizePrice(entryPrice - SellTakeProfitSteps * priceStep);

			if (SellStopLossSteps > 0)
			_stopLossOrder = BuyMarket(volume);

			if (SellTakeProfitSteps > 0)
			_takeProfitOrder = BuyMarket(volume);
		}
	}

	private void CleanupInactiveOrders()
	{
		if (_buyStopOrder != null && _buyStopOrder.State != OrderStates.Active)
		_buyStopOrder = null;

		if (_sellStopOrder != null && _sellStopOrder.State != OrderStates.Active)
		_sellStopOrder = null;

		if (_stopLossOrder != null && _stopLossOrder.State != OrderStates.Active)
		_stopLossOrder = null;

		if (_takeProfitOrder != null && _takeProfitOrder.State != OrderStates.Active)
		_takeProfitOrder = null;
	}

	private void CancelProtectionOrders()
	{
		CancelOrderIfActive(ref _stopLossOrder);
		CancelOrderIfActive(ref _takeProfitOrder);
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order is null)
		return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		return security?.ShrinkPrice(price) ?? price;
	}
}

