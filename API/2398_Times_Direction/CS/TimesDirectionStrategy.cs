using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time based strategy that opens and closes a position at specified moments.
/// Supports optional stop-loss and take-profit levels.
/// </summary>
public class TimesDirectionStrategy : Strategy
{
	private readonly StrategyParam<TradeMode> _trade;
	private readonly StrategyParam<DateTimeOffset> _openTime;
	private readonly StrategyParam<DateTimeOffset> _closeTime;
	private readonly StrategyParam<TimeSpan> _tradeInterval;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	
	/// <summary>
	/// Trading direction.
	/// </summary>
	public enum TradeMode
	{
		/// <summary>Buy position.</summary>
		Buy,
		/// <summary>Sell position.</summary>
		Sell,
	}
	
	/// <summary>
	/// Direction to open position.
	/// </summary>
	public TradeMode Trade
	{
		get => _trade.Value;
		set => _trade.Value = value;
	}
	
	/// <summary>
	/// Time when position is opened.
	/// </summary>
	public DateTimeOffset OpenTime
	{
		get => _openTime.Value;
		set => _openTime.Value = value;
	}
	
	/// <summary>
	/// Time when position is closed.
	/// </summary>
	public DateTimeOffset CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}
	
	/// <summary>
	/// Allowed deviation from open and close time.
	/// </summary>
	public TimeSpan TradeInterval
	{
		get => _tradeInterval.Value;
		set => _tradeInterval.Value = value;
	}
	
	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="TimesDirectionStrategy"/> class.
	/// </summary>
	public TimesDirectionStrategy()
	{
		_trade = Param(nameof(Trade), TradeMode.Sell)
		.SetDisplay("Trade Direction", "Direction to open position", "General");
		
		_openTime = Param(nameof(OpenTime), DateTimeOffset.MinValue)
		.SetDisplay("Open Time", "Time when position is opened", "General");
		
		_closeTime = Param(nameof(CloseTime), DateTimeOffset.MaxValue)
		.SetDisplay("Close Time", "Time when position is closed", "General");
		
		_tradeInterval = Param(nameof(TradeInterval), TimeSpan.FromMinutes(1))
		.SetDisplay("Interval", "Allowed time range around open/close", "General");
		
		_stopLoss = Param(nameof(StopLoss), 1000m)
		.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
		
		_takeProfit = Param(nameof(TakeProfit), 2000m)
		.SetDisplay("Take Profit", "Take profit in price units", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to process", "General");
		
		Volume = 0.1m;
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
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var current = candle.OpenTime;
		
		if (Position == 0)
		{
			var openEnd = OpenTime + TradeInterval;
			if (current >= OpenTime && current < openEnd)
			{
				_entryPrice = candle.ClosePrice;
				if (Trade == TradeMode.Buy)
				BuyMarket(Volume);
				else
				SellMarket(Volume);
			}
		}
		else
		{
			var closeEnd = CloseTime + TradeInterval;
			if (current >= CloseTime && current < closeEnd)
			{
				if (Position > 0)
				SellMarket(Position);
				else
				BuyMarket(-Position);
				
				_entryPrice = 0m;
				return;
			}
			
			if (_entryPrice != 0m)
			{
				if (Position > 0)
				{
					var sl = _entryPrice - StopLoss;
					var tp = _entryPrice + TakeProfit;
					if (candle.LowPrice <= sl || candle.HighPrice >= tp)
					SellMarket(Position);
				}
				else
				{
					var sl = _entryPrice + StopLoss;
					var tp = _entryPrice - TakeProfit;
					if (candle.HighPrice >= sl || candle.LowPrice <= tp)
					BuyMarket(-Position);
				}
			}
		}
	}
}
