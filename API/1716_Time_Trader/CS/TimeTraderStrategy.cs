using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens trades at a specified time with fixed stop loss and take profit.
/// </summary>
public class TimeTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _tradeMinute;
	private readonly StrategyParam<int> _tradeSecond;
	private readonly StrategyParam<DataType> _candleType;
	
	private bool _isTraded;
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}
	
	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}
	
	/// <summary>
	/// Hour of the day to trade.
	/// </summary>
	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}
	
	/// <summary>
	/// Minute of the hour to trade.
	/// </summary>
	public int TradeMinute
	{
		get => _tradeMinute.Value;
		set => _tradeMinute.Value = value;
	}
	
	/// <summary>
	/// Second of the minute to trade.
	/// </summary>
	public int TradeSecond
	{
		get => _tradeSecond.Value;
		set => _tradeSecond.Value = value;
	}
	
	/// <summary>
	/// Candle type for time tracking.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="TimeTraderStrategy"/>.
	/// </summary>
	public TimeTraderStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");
		
		_takeProfit = Param(nameof(TakeProfit), 20)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (ticks)", "Take profit in ticks", "Risk");
		
		_stopLoss = Param(nameof(StopLoss), 20)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (ticks)", "Stop loss in ticks", "Risk");
		
		_allowBuy = Param(nameof(AllowBuy), true)
		.SetDisplay("Allow Buy", "Enable long trades", "General");
		
		_allowSell = Param(nameof(AllowSell), true)
		.SetDisplay("Allow Sell", "Enable short trades", "General");
		
		_tradeHour = Param(nameof(TradeHour), 0)
		.SetRange(0, 23)
		.SetDisplay("Trade Hour", "Hour to trade", "General");
		
		_tradeMinute = Param(nameof(TradeMinute), 0)
		.SetRange(0, 59)
		.SetDisplay("Trade Minute", "Minute to trade", "General");
		
		_tradeSecond = Param(nameof(TradeSecond), 0)
		.SetRange(0, 59)
		.SetDisplay("Trade Second", "Second to trade", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromSeconds(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		
		var step = Security.PriceStep ?? 1m;
		StartProtection(
		takeProfit: new Unit(TakeProfit * step, UnitTypes.Point),
		stopLoss: new Unit(StopLoss * step, UnitTypes.Point));
		
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
		
		if (_isTraded)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var time = candle.OpenTime;
		if (time.Hour != TradeHour || time.Minute != TradeMinute || time.Second > TradeSecond)
		return;
		
		if (AllowBuy)
		BuyMarket(Volume);
		
		if (AllowSell)
		SellMarket(Volume);
		
		_isTraded = true;
	}
}
