using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that manages an existing position using a trailing stop in points.
/// The stop level is moved toward the current price by a fixed offset when profit grows.
/// </summary>
public class UniversalTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _startProfit;
	private readonly StrategyParam<DataType> _candleType;
	
	private Order _stopOrder;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _point;
	private int _lastPosition;
	
	/// <summary>
	/// Offset from current price to stop in points.
	/// </summary>
	public decimal Delta { get => _delta.Value; set => _delta.Value = value; }
	
	/// <summary>
	/// Step for moving stop in points.
	/// </summary>
	public decimal Step { get => _step.Value; set => _step.Value = value; }
	
	/// <summary>
	/// Minimal profit in points to start trailing.
	/// </summary>
	public decimal StartProfit { get => _startProfit.Value; set => _startProfit.Value = value; }
	
	/// <summary>
	/// Candle type used for trailing updates.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of <see cref="UniversalTrailingStopStrategy"/>.
	/// </summary>
	public UniversalTrailingStopStrategy()
	{
		_delta = Param(nameof(Delta), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Offset", "Distance from price to stop in points", "Trailing");
		
		_step = Param(nameof(Step), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Step", "Minimal step to move stop in points", "Trailing");
		
		_startProfit = Param(nameof(StartProfit), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Start Profit", "Required profit in points to start trailing", "Trailing");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for trailing calculations", "General");
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
		_stopOrder = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_lastPosition = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_point = Security?.PriceStep ?? 1m;
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (Position > 0)
		{
			if (_lastPosition <= 0)
			{
				_entryPrice = candle.ClosePrice;
				CancelStop();
			}
			
			var profit = candle.ClosePrice - _entryPrice;
			if (profit >= StartProfit * _point)
			{
				var newStop = candle.ClosePrice - Delta * _point;
				if (_stopOrder == null || newStop - _stopPrice >= Step * _point)
				MoveStop(Sides.Sell, newStop);
			}
		}
		else if (Position < 0)
		{
			if (_lastPosition >= 0)
			{
				_entryPrice = candle.ClosePrice;
				CancelStop();
			}
			
			var profit = _entryPrice - candle.ClosePrice;
			if (profit >= StartProfit * _point)
			{
				var newStop = candle.ClosePrice + Delta * _point;
				if (_stopOrder == null || _stopPrice - newStop >= Step * _point)
				MoveStop(Sides.Buy, newStop);
			}
		}
		else
		{
			_entryPrice = 0m;
			CancelStop();
		}
		
		_lastPosition = Position;
	}
	
	private void MoveStop(Sides side, decimal price)
	{
		CancelStop();
		
		var volume = Math.Abs(Position);
		_stopOrder = side == Sides.Sell
		? SellStop(volume, price)
		: BuyStop(volume, price);
		
		_stopPrice = price;
	}
	
	private void CancelStop()
	{
		if (_stopOrder != null)
		{
			CancelOrder(_stopOrder);
			_stopOrder = null;
		}
	}
}
