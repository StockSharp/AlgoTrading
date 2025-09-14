using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MBKAsctrend3 based strategy.
/// </summary>
public class Mbkasctrend3Strategy : Strategy
{
	private readonly StrategyParam<int> _wpr1;
	private readonly StrategyParam<int> _wpr2;
	private readonly StrategyParam<int> _wpr3;
	private readonly StrategyParam<int> _swing;
	private readonly StrategyParam<int> _avgSwing;
	private readonly StrategyParam<decimal> _w1;
	private readonly StrategyParam<decimal> _w2;
	private readonly StrategyParam<decimal> _w3;
	private readonly StrategyParam<decimal> _sl;
	private readonly StrategyParam<decimal> _tp;
	private readonly StrategyParam<DataType> _candle;
	private int _prevTrend;
	
	public int WprLength1 { get => _wpr1.Value; set => _wpr1.Value = value; }
	public int WprLength2 { get => _wpr2.Value; set => _wpr2.Value = value; }
	public int WprLength3 { get => _wpr3.Value; set => _wpr3.Value = value; }
	public int Swing { get => _swing.Value; set => _swing.Value = value; }
	public int AverageSwing { get => _avgSwing.Value; set => _avgSwing.Value = value; }
	public decimal Weight1 { get => _w1.Value; set => _w1.Value = value; }
	public decimal Weight2 { get => _w2.Value; set => _w2.Value = value; }
	public decimal Weight3 { get => _w3.Value; set => _w3.Value = value; }
	public decimal StopLoss { get => _sl.Value; set => _sl.Value = value; }
	public decimal TakeProfit { get => _tp.Value; set => _tp.Value = value; }
	public DataType CandleType { get => _candle.Value; set => _candle.Value = value; }
	
	public Mbkasctrend3Strategy()
	{
		_wpr1 = Param(nameof(WprLength1),9).SetGreaterThanZero().SetCanOptimize().SetDisplay("WPR Length 1","Period for the first WPR","Indicator");
		_wpr2 = Param(nameof(WprLength2),33).SetGreaterThanZero().SetCanOptimize().SetDisplay("WPR Length 2","Period for the second WPR","Indicator");
		_wpr3 = Param(nameof(WprLength3),77).SetGreaterThanZero().SetCanOptimize().SetDisplay("WPR Length 3","Period for the third WPR","Indicator");
		_swing = Param(nameof(Swing),3).SetDisplay("Swing","Swing adjustment","Indicator");
		_avgSwing = Param(nameof(AverageSwing),-5).SetDisplay("Average Swing","Average swing adjustment","Indicator");
		_w1 = Param(nameof(Weight1),1m).SetDisplay("Weight 1","Weight for WPR1","Indicator");
		_w2 = Param(nameof(Weight2),3m).SetDisplay("Weight 2","Weight for WPR2","Indicator");
		_w3 = Param(nameof(Weight3),1m).SetDisplay("Weight 3","Weight for WPR3","Indicator");
		_sl = Param(nameof(StopLoss),1000m).SetGreaterThanZero().SetDisplay("Stop Loss","Stop loss in points","Protection");
		_tp = Param(nameof(TakeProfit),2000m).SetGreaterThanZero().SetDisplay("Take Profit","Take profit in points","Protection");
		_candle = Param(nameof(CandleType),TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type","Time frame for calculations","General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		var w1 = new WilliamsR { Length = WprLength1 };
		var w2 = new WilliamsR { Length = WprLength2 };
		var w3 = new WilliamsR { Length = WprLength3 };
		SubscribeCandles(CandleType).Bind(w1, w2, w3, ProcessCandle).Start();
		StartProtection(stopLoss: new Unit(StopLoss, UnitTypes.Absolute), takeProfit: new Unit(TakeProfit, UnitTypes.Absolute));
		base.OnStarted(time);
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal v1, decimal v2, decimal v3)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
		return;
		
		var r1 = 100m + v1;
		var r2 = 100m + v2;
		var r3 = 100m + v3;
		var sum = Weight1 + Weight2 + Weight3;
		var avg = (Weight1 * r1 + Weight2 * r2 + Weight3 * r3) / sum;
		var upLevel = 67m + Swing;
		var dnLevel = 33m - Swing;
		var up1 = 50m - AverageSwing;
		var dn1 = 50m + AverageSwing;
		var trend = 0;
		if (avg > upLevel && r3 >= up1) trend = 1;
		else if (avg < dnLevel && r3 <= dn1) trend = -1;
		
		if (_prevTrend <= 0 && trend > 0 && Position <= 0)
		BuyMarket();
		else if (_prevTrend >= 0 && trend < 0 && Position >= 0)
		SellMarket();
		
		if (trend != 0)
		_prevTrend = trend;
	}
}
