using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy placing layered orders around the current price.
/// Supports breakout and counter-trend modes with automatic take-profit.
/// </summary>
public class AiGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gridSize;
	private readonly StrategyParam<int> _gridSteps;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<bool> _useBreakout;
	private readonly StrategyParam<bool> _useCounter;
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly List<Order> _gridOrders = new();
	private Order _tpOrder = null!;
	
	/// <summary>
	/// Distance between grid orders.
	/// </summary>
	public decimal GridSize { get => _gridSize.Value; set => _gridSize.Value = value; }
	
	/// <summary>
	/// Number of levels on each side of the price.
	/// </summary>
	public int GridSteps { get => _gridSteps.Value; set => _gridSteps.Value = value; }
	
	/// <summary>
	/// Take-profit distance for filled orders.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	
	/// <summary>
	/// Enable long orders.
	/// </summary>
	public bool AllowLong { get => _allowLong.Value; set => _allowLong.Value = value; }
	
	/// <summary>
	/// Enable short orders.
	/// </summary>
	public bool AllowShort { get => _allowShort.Value; set => _allowShort.Value = value; }
	
	/// <summary>
	/// Use breakout stop orders.
	/// </summary>
	public bool UseBreakout { get => _useBreakout.Value; set => _useBreakout.Value = value; }
	
	/// <summary>
	/// Use counter-trend limit orders.
	/// </summary>
	public bool UseCounter { get => _useCounter.Value; set => _useCounter.Value = value; }
	
	/// <summary>
	/// Candle type used for grid recalculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="AiGridStrategy"/>.
	/// </summary>
	public AiGridStrategy()
	{
		_gridSize = Param(nameof(GridSize), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Size", "Distance between orders in price units", "Grid");
		
		_gridSteps = Param(nameof(GridSteps), 10)
			.SetGreaterThanZero()
			.SetDisplay("Grid Steps", "Number of levels above and below price", "Grid");
		
		_takeProfit = Param(nameof(TakeProfit), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Distance to profit target", "Grid");
		
		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Enable long orders", "Trading");
		
		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Enable short orders", "Trading");
		
		_useBreakout = Param(nameof(UseBreakout), true)
			.SetDisplay("Use Breakout", "Place stop orders for breakouts", "Trading");
		
		_useCounter = Param(nameof(UseCounter), true)
			.SetDisplay("Use Counter", "Place limit orders against price", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for grid update", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
			base.OnReseted();
			_gridOrders.Clear();
			_tpOrder = null!;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
			base.OnStarted(time);
		
			var subscription = SubscribeCandles(CandleType);
			subscription.Bind(ProcessCandle).Start();
		
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
		
			foreach (var order in _gridOrders)
			CancelOrder(order);
		
			_gridOrders.Clear();
		
			var price = candle.ClosePrice;
		
			for (var i = 1; i <= GridSteps; i++)
			{
				var offset = GridSize * i;
			
				if (AllowLong)
				{
					if (UseCounter)
					_gridOrders.Add(BuyLimit(Volume, price - offset));
					if (UseBreakout)
					_gridOrders.Add(BuyStop(Volume, price + offset));
				}
			
				if (AllowShort)
				{
					if (UseCounter)
					_gridOrders.Add(SellLimit(Volume, price + offset));
					if (UseBreakout)
					_gridOrders.Add(SellStop(Volume, price - offset));
				}
			}
	}
	
	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
			base.OnNewMyTrade(trade);
		
			var order = trade.Order;
			if (order == null)
			return;
		
			if (order.Direction == Sides.Buy)
			RegisterTakeProfit(true, order.Price);
			else if (order.Direction == Sides.Sell)
			RegisterTakeProfit(false, order.Price);
	}
	
	private void RegisterTakeProfit(bool isLong, decimal entryPrice)
	{
			if (_tpOrder != null && _tpOrder.State == OrderStates.Active)
			CancelOrder(_tpOrder);
		
			var target = isLong
			? entryPrice + TakeProfit
			: entryPrice - TakeProfit;
		
			_tpOrder = isLong
			? SellLimit(Volume, target)
			: BuyLimit(Volume, target);
	}
}
