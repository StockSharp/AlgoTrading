using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class XauusdTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _emaShort;
	private readonly StrategyParam<int> _emaLong;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<decimal> _tpRiskRatio;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _stopPrice;
	private decimal _takePrice;
	public int EmaShort { get => _emaShort.Value; set => _emaShort.Value = value; }
	public int EmaLong { get => _emaLong.Value; set => _emaLong.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
	public decimal TpRiskRatio { get => _tpRiskRatio.Value; set => _tpRiskRatio.Value = value; }
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public XauusdTrendStrategy()
	{
		_emaShort = Param(nameof(EmaShort), 50).SetDisplay("EMA Short").SetCanOptimize(true);
		_emaLong = Param(nameof(EmaLong), 200).SetDisplay("EMA Long").SetCanOptimize(true);
		_rsiLength = Param(nameof(RsiLength), 14).SetDisplay("RSI Length").SetCanOptimize(true);
		_rsiOverbought = Param(nameof(RsiOverbought), 70m).SetDisplay("RSI Overbought").SetCanOptimize(true);
		_rsiOversold = Param(nameof(RsiOversold), 30m).SetDisplay("RSI Oversold").SetCanOptimize(true);
		_bollingerLength = Param(nameof(BollingerLength), 20).SetDisplay("BB Length").SetCanOptimize(true);
		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m).SetDisplay("BB Mult").SetCanOptimize(true);
		_tpRiskRatio = Param(nameof(TpRiskRatio), 2m).SetDisplay("TP/SL Ratio").SetCanOptimize(true);
		_riskPercent = Param(nameof(RiskPercent), 1m).SetDisplay("Risk %").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type");
	}
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0;
		_takePrice = 0;
	}
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();
		
		var emaFast = new ExponentialMovingAverage { Length = EmaShort };
		var emaSlow = new ExponentialMovingAverage { Length = EmaLong };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var bb = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		
		var sub = SubscribeCandles(CandleType);
		sub.Bind(emaFast, emaSlow, rsi, bb, Process).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}
	private void Process(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal rsiValue, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var longCond = emaFast > emaSlow && rsiValue < RsiOversold && candle.ClosePrice > upper;
		var shortCond = emaFast < emaSlow && rsiValue > RsiOverbought && candle.ClosePrice < lower;
		
		if (longCond && Position <= 0)
		{
			var vol = Volume + Math.Abs(Position);
			BuyMarket(vol);
			
			var pv = Portfolio.CurrentValue ?? 0m;
			var risk = pv * (RiskPercent / 100m);
			var sl = risk / candle.ClosePrice;
			var tp = sl * TpRiskRatio;
			_stopPrice = candle.ClosePrice - sl;
			_takePrice = candle.ClosePrice + tp;
		}
		else if (shortCond && Position >= 0)
		{
			var vol = Volume + Math.Abs(Position);
			SellMarket(vol);
			
			var pv = Portfolio.CurrentValue ?? 0m;
			var risk = pv * (RiskPercent / 100m);
			var sl = risk / candle.ClosePrice;
			var tp = sl * TpRiskRatio;
			_stopPrice = candle.ClosePrice + sl;
			_takePrice = candle.ClosePrice - tp;
		}
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.ClosePrice <= _stopPrice)
			SellMarket(Math.Abs(Position));
			else if (candle.HighPrice >= _takePrice || candle.ClosePrice >= _takePrice)
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.ClosePrice >= _stopPrice)
			BuyMarket(Math.Abs(Position));
			else if (candle.LowPrice <= _takePrice || candle.ClosePrice <= _takePrice)
			BuyMarket(Math.Abs(Position));
		}
	}
}
