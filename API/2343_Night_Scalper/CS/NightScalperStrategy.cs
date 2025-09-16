using System;
using System.Collections.Generic;


using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Night scalping strategy using Bollinger Bands.
/// </summary>
public class NightScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _rangeThreshold;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _range;
	private decimal _sl;
	private decimal _tp;
	
	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}
	
	/// <summary>
	/// Maximum band width in points to allow trades.
	/// </summary>
	public decimal RangeThreshold
	{
		get => _rangeThreshold.Value;
		set => _rangeThreshold.Value = value;
	}
	
	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Hour to start trading.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public NightScalperStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 40)
		.SetDisplay("BB Period", "Bollinger period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 5);
		
		_bollingerDeviation = Param(nameof(BollingerDeviation), 1m)
		.SetDisplay("BB Deviation", "Bollinger deviation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.1m);
		
		_rangeThreshold = Param(nameof(RangeThreshold), 450m)
		.SetDisplay("Range Threshold", "Maximum band width", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 1000m, 50m);
		
		_stopLoss = Param(nameof(StopLoss), 370)
		.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100, 1000, 50);
		
		_takeProfit = Param(nameof(TakeProfit), 20)
		.SetDisplay("Take Profit", "Take profit in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);
		
		_startHour = Param(nameof(StartHour), 19)
		.SetDisplay("Start Hour", "Hour to start trading", "General")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_range = _sl = _tp = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var tick = Security?.PriceStep ?? 1m;
		_range = RangeThreshold * tick;
		_sl = StopLoss * tick;
		_tp = TakeProfit * tick;
		
		StartProtection(new Unit(_tp), new Unit(_sl));
		
		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(bb, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var bb = (BollingerBandsValue)bbValue;
		
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
		return;
		
		var width = upper - lower;
		var hour = candle.CloseTime.Hour;
		
		if (Position != 0 && hour < StartHour)
		{
			ClosePosition();
			return;
		}
		
		if (Position == 0 && hour >= StartHour && width < _range)
		{
			if (candle.ClosePrice < lower)
			BuyMarket();
			else if (candle.ClosePrice > upper)
			SellMarket();
		}
	}
}
