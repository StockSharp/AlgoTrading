using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-based trader entering at a specific clock time.
/// </summary>
public class TimeTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _tradeMinute;
	private readonly StrategyParam<int> _tradeSecond;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	
	private bool _buyDone;
	private bool _sellDone;
	
	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Trade hour.
	/// </summary>
	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}
	
	/// <summary>
	/// Trade minute.
	/// </summary>
	public int TradeMinute
	{
		get => _tradeMinute.Value;
		set => _tradeMinute.Value = value;
	}
	
	/// <summary>
	/// Trade second.
	/// </summary>
	public int TradeSecond
	{
		get => _tradeSecond.Value;
		set => _tradeSecond.Value = value;
	}
	
	/// <summary>
	/// Allow entering long position.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}
	
	/// <summary>
	/// Allow entering short position.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Candle type to watch for time.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes <see cref="TimeTraderStrategy"/>.
	/// </summary>
	public TimeTraderStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 0.2m)
		.SetNotNegative()
		.SetDisplay("Take Profit (%)", "Take profit percentage", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);
		
		_stopLoss = Param(nameof(StopLoss), 0.2m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (%)", "Stop loss percentage", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);
		
		_tradeHour = Param(nameof(TradeHour), 0)
		.SetRange(0, 23)
		.SetDisplay("Trade Hour", "Hour of execution", "Time");
		
		_tradeMinute = Param(nameof(TradeMinute), 0)
		.SetRange(0, 59)
		.SetDisplay("Trade Minute", "Minute of execution", "Time");
		
		_tradeSecond = Param(nameof(TradeSecond), 0)
		.SetRange(0, 59)
		.SetDisplay("Trade Second", "Second of execution", "Time");
		
		_allowBuy = Param(nameof(AllowBuy), true)
		.SetDisplay("Allow Buy", "Enable long orders", "Trading");
		
		_allowSell = Param(nameof(AllowSell), true)
		.SetDisplay("Allow Sell", "Enable short orders", "Trading");
		
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromSeconds(1).TimeFrame())
		.SetDisplay("Candle Type", "Resolution to check time", "General");
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
		
		_buyDone = false;
		_sellDone = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection(
		takeProfit: new Unit(TakeProfit, UnitTypes.Percent),
		stopLoss: new Unit(StopLoss, UnitTypes.Percent));
		
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
		
		var t = candle.OpenTime;
		
		if (t.Hour != TradeHour || t.Minute != TradeMinute || t.Second > TradeSecond)
		return;
		
		if (AllowBuy && !_buyDone)
		{
			BuyMarket(Volume);
			_buyDone = true;
		}
		
		if (AllowSell && !_sellDone)
		{
			SellMarket(Volume);
			_sellDone = true;
		}
	}
}

