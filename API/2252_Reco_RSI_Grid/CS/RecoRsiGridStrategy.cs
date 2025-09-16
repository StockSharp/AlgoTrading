using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based grid strategy with adaptive distance and volume.
/// Opens the first trade when RSI reaches overbought/oversold zones
/// and then adds counter trades as price moves by configurable steps.
/// All positions are closed together on defined profit or loss.
/// </summary>
public class RecoRsiGridStrategy : Strategy
{
    private readonly StrategyParam<int> _rsiPeriod;
    private readonly StrategyParam<decimal> _rsiSellZone;
    private readonly StrategyParam<decimal> _rsiBuyZone;
    private readonly StrategyParam<decimal> _startDistance;
    private readonly StrategyParam<decimal> _distanceMultiplier;
    private readonly StrategyParam<decimal> _maxDistance;
    private readonly StrategyParam<decimal> _minDistance;
    private readonly StrategyParam<int> _maxOrders;
    private readonly StrategyParam<decimal> _lot;
    private readonly StrategyParam<decimal> _lotMultiplier;
    private readonly StrategyParam<decimal> _maxLot;
    private readonly StrategyParam<decimal> _minLot;
    private readonly StrategyParam<bool> _useCloseProfit;
    private readonly StrategyParam<decimal> _profitFirstOrder;
    private readonly StrategyParam<decimal> _profitMultiplier;
    private readonly StrategyParam<bool> _useCloseLose;
    private readonly StrategyParam<decimal> _loseFirstOrder;
    private readonly StrategyParam<decimal> _loseMultiplier;
    private readonly StrategyParam<decimal> _pointMultiplier;
    private readonly StrategyParam<DataType> _candleType;

    private decimal _lastOrderPrice;
    private bool _lastOrderIsBuy;
    private int _ordersTotal;
    private decimal _point;

    /// <summary>
    /// RSI indicator period.
    /// </summary>
    public int RsiPeriod
    {
	get => _rsiPeriod.Value;
	set => _rsiPeriod.Value = value;
    }

    /// <summary>
    /// RSI value that triggers a sell.
    /// </summary>
    public decimal RsiSellZone
    {
	get => _rsiSellZone.Value;
	set => _rsiSellZone.Value = value;
    }

    /// <summary>
    /// RSI value that triggers a buy.
    /// </summary>
    public decimal RsiBuyZone
    {
	get => _rsiBuyZone.Value;
	set => _rsiBuyZone.Value = value;
    }

    /// <summary>
    /// Initial distance from the last order in points.
    /// </summary>
    public decimal StartDistance
    {
	get => _startDistance.Value;
	set => _startDistance.Value = value;
    }

    /// <summary>
    /// Multiplier applied to distance for each new order.
    /// </summary>
    public decimal DistanceMultiplier
    {
	get => _distanceMultiplier.Value;
	set => _distanceMultiplier.Value = value;
    }

    /// <summary>
    /// Maximum allowed distance in points.
    /// </summary>
    public decimal MaxDistance
    {
	get => _maxDistance.Value;
	set => _maxDistance.Value = value;
    }

    /// <summary>
    /// Minimum allowed distance in points.
    /// </summary>
    public decimal MinDistance
    {
	get => _minDistance.Value;
	set => _minDistance.Value = value;
    }

    /// <summary>
    /// Maximum number of simultaneous orders.
    /// </summary>
    public int MaxOrders
    {
	get => _maxOrders.Value;
	set => _maxOrders.Value = value;
    }

    /// <summary>
    /// Base order volume.
    /// </summary>
    public decimal Lot
    {
	get => _lot.Value;
	set => _lot.Value = value;
    }

    /// <summary>
    /// Volume multiplier for each additional order.
    /// </summary>
    public decimal LotMultiplier
    {
	get => _lotMultiplier.Value;
	set => _lotMultiplier.Value = value;
    }

    /// <summary>
    /// Maximum allowed volume per order.
    /// </summary>
    public decimal MaxLot
    {
	get => _maxLot.Value;
	set => _maxLot.Value = value;
    }

    /// <summary>
    /// Minimum allowed volume per order.
    /// </summary>
    public decimal MinLot
    {
	get => _minLot.Value;
	set => _minLot.Value = value;
    }

    /// <summary>
    /// Enable closing all positions on profit target.
    /// </summary>
    public bool UseCloseProfit
    {
	get => _useCloseProfit.Value;
	set => _useCloseProfit.Value = value;
    }

    /// <summary>
    /// Profit target for the first order.
    /// </summary>
    public decimal ProfitFirstOrder
    {
	get => _profitFirstOrder.Value;
	set => _profitFirstOrder.Value = value;
    }

    /// <summary>
    /// Multiplier applied to profit target per order.
    /// </summary>
    public decimal ProfitMultiplier
    {
	get => _profitMultiplier.Value;
	set => _profitMultiplier.Value = value;
    }

    /// <summary>
    /// Enable closing all positions on loss.
    /// </summary>
    public bool UseCloseLose
    {
	get => _useCloseLose.Value;
	set => _useCloseLose.Value = value;
    }

    /// <summary>
    /// Loss threshold for the first order.
    /// </summary>
    public decimal LoseFirstOrder
    {
	get => _loseFirstOrder.Value;
	set => _loseFirstOrder.Value = value;
    }

    /// <summary>
    /// Multiplier applied to loss threshold per order.
    /// </summary>
    public decimal LoseMultiplier
    {
	get => _loseMultiplier.Value;
	set => _loseMultiplier.Value = value;
    }

    /// <summary>
    /// Multiplier for price step to convert points.
    /// </summary>
    public decimal PointMultiplier
    {
	get => _pointMultiplier.Value;
	set => _pointMultiplier.Value = value;
    }

    /// <summary>
    /// Type of candles used by the strategy.
    /// </summary>
    public DataType CandleType
    {
	get => _candleType.Value;
	set => _candleType.Value = value;
    }

    /// <summary>
    /// Initialize parameters.
    /// </summary>
    public RecoRsiGridStrategy()
    {
	_rsiPeriod =
	    Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI indicator period", "Signal")
		.SetCanOptimize(true);

	_rsiSellZone =
	    Param(nameof(RsiSellZone), 70m)
		.SetDisplay("RSI Sell Zone", "RSI level to sell", "Signal");

	_rsiBuyZone =
	    Param(nameof(RsiBuyZone), 30m)
		.SetDisplay("RSI Buy Zone", "RSI level to buy", "Signal");

	_startDistance =
	    Param(nameof(StartDistance), 20m)
		.SetDisplay("Start Distance", "Initial distance in points",
			    "Distance");

	_distanceMultiplier =
	    Param(nameof(DistanceMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Distance Multiplier", "Distance multiplier",
			    "Distance");

	_maxDistance =
	    Param(nameof(MaxDistance), 0m)
		.SetDisplay("Max Distance", "Maximum distance", "Distance");

	_minDistance =
	    Param(nameof(MinDistance), 0m)
		.SetDisplay("Min Distance", "Minimum distance", "Distance");

	_maxOrders = Param(nameof(MaxOrders), 0)
			 .SetDisplay("Max Orders", "Maximum number of orders",
				     "General");

	_lot = Param(nameof(Lot), 1m)
		   .SetGreaterThanZero()
		   .SetDisplay("Lot", "Base order volume", "Lot");

	_lotMultiplier =
	    Param(nameof(LotMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Multiplier", "Volume multiplier", "Lot");

	_maxLot = Param(nameof(MaxLot), 0m)
		      .SetDisplay("Max Lot", "Maximum order volume", "Lot");

	_minLot = Param(nameof(MinLot), 0m)
		      .SetDisplay("Min Lot", "Minimum order volume", "Lot");

	_useCloseProfit = Param(nameof(UseCloseProfit), true)
			      .SetDisplay("Use Close Profit",
					  "Enable profit based exit", "Exit");

	_profitFirstOrder =
	    Param(nameof(ProfitFirstOrder), 2m)
		.SetDisplay("Profit First Order",
			    "Profit target for first order", "Exit");

	_profitMultiplier =
	    Param(nameof(ProfitMultiplier), 0.7m)
		.SetDisplay("Profit Multiplier", "Profit multiplier", "Exit");

	_useCloseLose =
	    Param(nameof(UseCloseLose), false)
		.SetDisplay("Use Close Lose", "Enable loss based exit", "Exit");

	_loseFirstOrder =
	    Param(nameof(LoseFirstOrder), 6m)
		.SetDisplay("Lose First Order",
			    "Loss threshold for first order", "Exit");

	_loseMultiplier =
	    Param(nameof(LoseMultiplier), 1.1m)
		.SetDisplay("Lose Multiplier", "Loss multiplier", "Exit");

	_pointMultiplier = Param(nameof(PointMultiplier), 10m)
			       .SetGreaterThanZero()
			       .SetDisplay("Point Multiplier",
					   "Price step multiplier", "General");

	_candleType =
	    Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
    }

    /// <inheritdoc />
    public override IEnumerable<(Security sec, DataType dt)>
    GetWorkingSecurities()
    {
	return [(Security, CandleType)];
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
	base.OnStarted(time);

	_point = (Security.PriceStep ?? 1m) * PointMultiplier;

	var rsi = new RSI { Length = RsiPeriod };

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(rsi, OnProcess).Start();

	var area = CreateChartArea();
	if (area != null)
	{
	    DrawCandles(area, subscription);
	    DrawIndicator(area, rsi);
	    DrawOwnTrades(area);
	}
    }

    private void OnProcess(ICandleMessage candle, decimal rsiValue)
    {
	if (candle.State != CandleStates.Finished)
	    return;

	if (!IsFormedAndOnlineAndAllowTrading())
	    return;

	if (_ordersTotal > 0)
	{
	    var openProfit = Position * (candle.ClosePrice - PositionPrice);
	    var totalProfit = PnL + openProfit;

	    var profitTarget =
		ProfitFirstOrder *
		(decimal)Math.Pow((double)ProfitMultiplier, _ordersTotal - 1);
	    var lossTarget =
		LoseFirstOrder *
		(decimal)Math.Pow((double)LoseMultiplier, _ordersTotal - 1);

	    if (UseCloseProfit && totalProfit >= profitTarget)
	    {
		CloseAll();
		return;
	    }

	    if (UseCloseLose && totalProfit <= -lossTarget)
	    {
		CloseAll();
		return;
	    }
	}

	var signal = GetSignal(candle.ClosePrice, rsiValue);

	if (signal != 0)
	    OpenTrade(signal, candle.ClosePrice);
    }

    private int GetSignal(decimal price, decimal rsiValue)
    {
	if (_ordersTotal == 0)
	{
	    if (rsiValue >= RsiSellZone)
		return -1;
	    if (rsiValue <= RsiBuyZone)
		return 1;
	    return 0;
	}

	if (MaxOrders > 0 && _ordersTotal >= MaxOrders)
	    return 0;

	var dist =
	    StartDistance * _point *
	    (decimal)Math.Pow((double)DistanceMultiplier, _ordersTotal - 1);
	if (MaxDistance > 0 && dist > MaxDistance * _point)
	    dist = MaxDistance * _point;
	if (MinDistance > 0 && dist < MinDistance * _point)
	    dist = MinDistance * _point;

	if (_lastOrderIsBuy)
	{
	    if (price <= _lastOrderPrice - dist)
		return -1;
	}
	else
	{
	    if (price >= _lastOrderPrice + dist)
		return 1;
	}

	return 0;
    }

    private void OpenTrade(int direction, decimal price)
    {
	var volume =
	    Lot * (decimal)Math.Pow((double)LotMultiplier, _ordersTotal);
	if (MaxLot > 0 && volume > MaxLot)
	    volume = MaxLot;
	if (MinLot > 0 && volume < MinLot)
	    volume = MinLot;

	if (direction > 0)
	{
	    BuyMarket(volume);
	    _lastOrderIsBuy = true;
	}
	else
	{
	    SellMarket(volume);
	    _lastOrderIsBuy = false;
	}

	_lastOrderPrice = price;
	_ordersTotal++;
    }

    private void CloseAll()
    {
	if (Position > 0)
	    SellMarket(Position);
	else if (Position < 0)
	    BuyMarket(-Position);

	_ordersTotal = 0;
	_lastOrderPrice = 0m;
    }
}
