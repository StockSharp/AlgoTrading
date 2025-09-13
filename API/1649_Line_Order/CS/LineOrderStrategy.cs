using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Executes a market order when price crosses a predefined horizontal line.
/// Manages optional stop-loss, take-profit and trailing stop.
/// </summary>
public class LineOrderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _linePrice;
	private readonly StrategyParam<bool> _isBuy;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private decimal _maxPrice;
	private decimal _minPrice;
	private bool _positionOpened;
	
	/// <summary>
	/// Price level for the pending order.
	/// </summary>
	public decimal LinePrice
	{
		get => _linePrice.Value;
		set => _linePrice.Value = value;
	}
	
	/// <summary>
	/// Buy when price crosses the line if true, otherwise sell.
	/// </summary>
	public bool IsBuy
	{
		get => _isBuy.Value;
		set => _isBuy.Value = value;
	}
	
	/// <summary>
	/// Stop-loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Type of candles used for monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes <see cref="LineOrderStrategy"/>.
	/// </summary>
	public LineOrderStrategy()
	{
		_linePrice = Param(nameof(LinePrice), 0m)
		.SetDisplay("Line Price", "Price level for pending order", "General")
		.SetGreaterThanZero();
		
		_isBuy = Param(nameof(IsBuy), true)
		.SetDisplay("Buy Direction", "Buy when price crosses the line if true, otherwise sell", "General");
		
		_stopLoss = Param(nameof(StopLoss), 0m)
		.SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk")
		.SetGreaterOrEqualZero();
		
		_takeProfit = Param(nameof(TakeProfit), 0m)
		.SetDisplay("Take Profit", "Take profit distance in price units", "Risk")
		.SetGreaterOrEqualZero();
		
		_trailingStop = Param(nameof(TrailingStop), 0m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in price units", "Risk")
		.SetGreaterOrEqualZero();
		
		_volume = Param(nameof(Volume), 1m)
		.SetDisplay("Volume", "Order volume", "General")
		.SetGreaterThanZero();
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for monitoring", "General");
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
		_entryPrice = 0m;
		_maxPrice = 0m;
		_minPrice = 0m;
		_positionOpened = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (Position == 0)
		{
			if (IsBuy && candle.ClosePrice >= LinePrice)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_maxPrice = _entryPrice;
				_positionOpened = true;
			}
			else if (!IsBuy && candle.ClosePrice <= LinePrice)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_minPrice = _entryPrice;
				_positionOpened = true;
			}
		}
		else if (_positionOpened)
		{
			if (Position > 0)
			{
				if (candle.HighPrice > _maxPrice)
				_maxPrice = candle.HighPrice;
				
				if (StopLoss > 0 && candle.LowPrice <= _entryPrice - StopLoss)
				{
					SellMarket(Math.Abs(Position));
					ResetState();
					return;
				}
				
				if (TakeProfit > 0 && candle.HighPrice >= _entryPrice + TakeProfit)
				{
					SellMarket(Math.Abs(Position));
					ResetState();
					return;
				}
				
				if (TrailingStop > 0 && candle.LowPrice <= _maxPrice - TrailingStop)
				{
					SellMarket(Math.Abs(Position));
					ResetState();
					return;
				}
			}
			else if (Position < 0)
			{
				if (_minPrice == 0m || candle.LowPrice < _minPrice)
				_minPrice = candle.LowPrice;
				
				if (StopLoss > 0 && candle.HighPrice >= _entryPrice + StopLoss)
				{
					BuyMarket(Math.Abs(Position));
					ResetState();
					return;
				}
				
				if (TakeProfit > 0 && candle.LowPrice <= _entryPrice - TakeProfit)
				{
					BuyMarket(Math.Abs(Position));
					ResetState();
					return;
				}
				
				if (TrailingStop > 0 && candle.HighPrice >= _minPrice + TrailingStop)
				{
					BuyMarket(Math.Abs(Position));
					ResetState();
					return;
				}
			}
		}
	}
	
	private void ResetState()
	{
		_entryPrice = 0m;
		_maxPrice = 0m;
		_minPrice = 0m;
		_positionOpened = false;
	}
}
