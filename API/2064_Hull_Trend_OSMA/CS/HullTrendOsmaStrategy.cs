using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hull Trend OSMA indicator.
/// Opens long position when the oscillator rises twice in a row and short position when it falls twice in a row.
/// Opposite positions are closed on each new signal.
/// </summary>
public class HullTrendOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _hullPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;
	
	// Previous oscillator values
	private decimal? _prev1;
	private decimal? _prev2;
	
	/// <summary>
	/// Period for Hull Moving Average.
	/// </summary>
	public int HullPeriod
	{
		get => _hullPeriod.Value;
		set => _hullPeriod.Value = value;
	}
	
	/// <summary>
	/// Period for smoothing applied to the oscillator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}
	
	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initialize the Hull Trend OSMA strategy.
	/// </summary>
	public HullTrendOsmaStrategy()
	{
		_hullPeriod = Param(nameof(HullPeriod), 20)
		.SetDisplay("Hull Period", "Period for Hull Moving Average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);
		
		_signalPeriod = Param(nameof(SignalPeriod), 5)
		.SetDisplay("Signal Period", "Period for smoothing applied to the oscillator", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 15, 2);
		
		_takeProfit = Param(nameof(TakeProfit), 2000m)
		.SetDisplay("Take Profit", "Take profit distance in price units", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1000m, 3000m, 500m);
		
		_stopLoss = Param(nameof(StopLoss), 1000m)
		.SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(500m, 2000m, 500m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prev1 = null;
		_prev2 = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var hma = new HullMovingAverage { Length = HullPeriod };
		var signal = new SimpleMovingAverage { Length = SignalPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(hma, signal, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawIndicator(area, signal);
			DrawOwnTrades(area);
		}
		
		StartProtection(
		takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
		stopLoss: new Unit(StopLoss, UnitTypes.Absolute)
		);
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal signalValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
		return;
		
		// Check trading permission
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var osma = hmaValue - signalValue;
		
		if (_prev1 is null || _prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = osma;
			return;
		}
		
		var prev = _prev1.Value;
		var prevPrev = _prev2.Value;
		
		var isRising = prev > prevPrev && osma >= prev;
		var isFalling = prev < prevPrev && osma <= prev;
		
		if (isRising)
		{
			// Close short positions
			if (Position < 0)
			BuyMarket(Math.Abs(Position));
			
			// Open long if flat
			if (Position == 0)
			BuyMarket(Volume);
		}
		else if (isFalling)
		{
			// Close long positions
			if (Position > 0)
			SellMarket(Position);
			
			// Open short if flat
			if (Position == 0)
			SellMarket(Volume);
		}
		
		_prev2 = _prev1;
		_prev1 = osma;
	}
}
