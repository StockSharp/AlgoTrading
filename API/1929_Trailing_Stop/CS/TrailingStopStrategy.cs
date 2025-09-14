using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that applies basic trailing stop logic.
/// </summary>
public class TrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailing;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private decimal _stopPrice;
	
	/// <summary>
	/// Profit target distance from entry price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Stop loss distance from entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Trailing stop distance.
	/// </summary>
	public decimal Trailing
	{
		get => _trailing.Value;
		set => _trailing.Value = value;
	}
	
	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStopStrategy"/> class.
	/// </summary>
	public TrailingStopStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 100m)
		.SetDisplay("Take Profit", "Profit distance in price units", "Risk")
		.SetCanOptimize(true);
		
		_stopLoss = Param(nameof(StopLoss), 20m)
		.SetDisplay("Stop Loss", "Loss distance in price units", "Risk")
		.SetCanOptimize(true);
		
		_trailing = Param(nameof(Trailing), 3m)
		.SetDisplay("Trailing", "Trailing stop distance", "Risk")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for price updates", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
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
		
		var price = candle.ClosePrice;
		
		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || _entryPrice - price > StopLoss)
			{
				SellMarket(Position);
				return;
			}
			
			if (Trailing > 0m)
			{
				var newStop = price - Trailing;
				if (_stopPrice < newStop)
				_stopPrice = newStop;
				
				if (price <= _stopPrice)
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price - _entryPrice > StopLoss)
			{
				BuyMarket(-Position);
				return;
			}
			
			if (Trailing > 0m)
			{
				var newStop = price + Trailing;
				if (_stopPrice == 0m || _stopPrice > newStop)
				_stopPrice = newStop;
				
				if (price >= _stopPrice)
				BuyMarket(-Position);
			}
		}
	}
	
	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);
		
		if (Position != 0m && _entryPrice == 0m)
		{
			_entryPrice = trade.Trade.Price;
			_stopPrice = 0m;
		}
		else if (Position == 0m)
		{
			_entryPrice = 0m;
			_stopPrice = 0m;
		}
	}
}

