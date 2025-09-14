using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Automatically attaches stop loss and take profit to existing positions and trails the stop.
/// </summary>
public class AutoTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<bool> _fridayTrade;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<bool> _autoTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStopStep;
	private readonly StrategyParam<bool> _automaticTakeProfit;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _automaticStopLoss;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;
	
	private Order _stopOrder;
	private Order _takeProfitOrder;
	private decimal _trailDistance;
	private decimal _trailStart;
	
	public AutoTrailingStopStrategy()
	{
		_fridayTrade = Param(nameof(FridayTrade), true)
		.SetDisplay("Friday Trade", "Allow trailing on Friday.", "General");
		
		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing Stop", "Enable trailing stop.", "Protection");
		
		_autoTrailingStop = Param(nameof(AutoTrailingStop), true)
		.SetDisplay("Auto Trailing Stop", "Use default trailing stop value.", "Protection");
		
		_trailingStop = Param(nameof(TrailingStop), 6m)
		.SetDisplay("Trailing Stop", "Trailing stop distance.", "Protection")
		.SetCanOptimize();
		
		_trailingStopStep = Param(nameof(TrailingStopStep), 1m)
		.SetDisplay("Trailing Stop Step", "Step to move trailing stop.", "Protection");
		
		_automaticTakeProfit = Param(nameof(AutomaticTakeProfit), true)
		.SetDisplay("Automatic Take Profit", "Place automatic take profit.", "Protection");
		
		_takeProfit = Param(nameof(TakeProfit), 35m)
		.SetDisplay("Take Profit", "Take profit distance.", "Protection")
		.SetCanOptimize();
		
		_automaticStopLoss = Param(nameof(AutomaticStopLoss), true)
		.SetDisplay("Automatic Stop Loss", "Place automatic stop loss.", "Protection");
		
		_stopLoss = Param(nameof(StopLoss), 114m)
		.SetDisplay("Stop Loss", "Stop loss distance.", "Protection")
		.SetCanOptimize();
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type for price updates.", "General");
	}
	
	public bool FridayTrade { get => _fridayTrade.Value; set => _fridayTrade.Value = value; }
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }
	public bool AutoTrailingStop { get => _autoTrailingStop.Value; set => _autoTrailingStop.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal TrailingStopStep { get => _trailingStopStep.Value; set => _trailingStopStep.Value = value; }
	public bool AutomaticTakeProfit { get => _automaticTakeProfit.Value; set => _automaticTakeProfit.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public bool AutomaticStopLoss { get => _automaticStopLoss.Value; set => _automaticStopLoss.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_stopOrder = null;
		_takeProfitOrder = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_trailDistance = AutoTrailingStop ? 6m : TrailingStop;
		_trailStart = _trailDistance / 2m;
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only finished candles to avoid premature decisions.
		if (candle.State != CandleStates.Finished)
		return;
		
		// Optional skip of trailing logic on Fridays.
		if (!FridayTrade && candle.OpenTime.DayOfWeek == DayOfWeek.Friday)
		return;
		
		var price = candle.ClosePrice;
		
		if (Position > 0)
		{
			// Manage protective orders for long position.
			EnsureProtection(true, price);
			
			// Update trailing stop when price moves enough in favor.
			if (UseTrailingStop && price - PositionPrice >= _trailStart)
			{
				var newStop = price - _trailDistance;
				if (_stopOrder == null || newStop - _stopOrder.Price >= TrailingStopStep)
				MoveStop(true, newStop);
			}
		}
		else if (Position < 0)
		{
			// Manage protective orders for short position.
			EnsureProtection(false, price);
			
			// Update trailing stop for short position.
			if (UseTrailingStop && PositionPrice - price >= _trailStart)
			{
				var newStop = price + _trailDistance;
				if (_stopOrder == null || _stopOrder.Price - newStop >= TrailingStopStep)
				MoveStop(false, newStop);
			}
		}
	}
	
	private void EnsureProtection(bool isLong, decimal price)
	{
		// Place initial stop-loss if it is missing.
		if (_stopOrder == null && AutomaticStopLoss)
		{
			var slPrice = isLong ? price - StopLoss : price + StopLoss;
			_stopOrder = isLong
			? SellStop(Math.Abs(Position), slPrice)
			: BuyStop(Math.Abs(Position), slPrice);
		}
		
		// Place initial take-profit if it is missing.
		if (_takeProfitOrder == null && AutomaticTakeProfit)
		{
			var tpPrice = isLong ? price + TakeProfit : price - TakeProfit;
			_takeProfitOrder = isLong
			? SellLimit(Math.Abs(Position), tpPrice)
			: BuyLimit(Math.Abs(Position), tpPrice);
		}
	}
	
	private void MoveStop(bool isLong, decimal price)
	{
		// Cancel previous stop order before placing a new one.
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		CancelOrder(_stopOrder);
		
		// Register updated stop order in the required direction.
		_stopOrder = isLong
		? SellStop(Math.Abs(Position), price)
		: BuyStop(Math.Abs(Position), price);
	}
	
	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);
		
		if (Position == 0)
		{
			if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
			if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);
			
			_stopOrder = null;
			_takeProfitOrder = null;
		}
	}
}
