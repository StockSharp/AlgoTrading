namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class QuantumReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLen;
	private readonly StrategyParam<decimal> _bollingerMult;
	private readonly StrategyParam<int> _rsiLen;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _smoothLen;
	
	private SimpleMovingAverage _sma;
	private decimal? _entry;
	
	public QuantumReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type", "General");
		_bollingerLen = Param(nameof(BollingerLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("BB Length", "Bollinger period", "Indicators");
		_bollingerMult = Param(nameof(BollingerMultiplier), 2.2m)
		.SetGreaterThanZero()
		.SetDisplay("BB Mult", "Deviation multiplier", "Indicators");
		_rsiLen = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI period", "Indicators");
		_rsiOversold = Param(nameof(RsiOversold), 45m)
		.SetNotNegative()
		.SetDisplay("RSI Oversold", "Oversold level", "Indicators");
		_smoothLen = Param(nameof(RsiSmoothLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("RSI Smooth", "RSI smoothing", "Indicators");
	}
	
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BollingerLength { get => _bollingerLen.Value; set => _bollingerLen.Value = value; }
	public decimal BollingerMultiplier { get => _bollingerMult.Value; set => _bollingerMult.Value = value; }
	public int RsiLength { get => _rsiLen.Value; set => _rsiLen.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int RsiSmoothLength { get => _smoothLen.Value; set => _smoothLen.Value = value; }
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_sma = new SimpleMovingAverage { Length = RsiSmoothLength };
		var bb = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(bb, rsi, Process).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, bb);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
	
	private void Process(ICandleMessage candle, decimal basis, decimal upper, decimal lower, decimal rsi)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
		return;
		var rsiSmooth = _sma.Process(rsi, candle.ServerTime, true).ToDecimal();
		if ((candle.ClosePrice <= lower || rsiSmooth < RsiOversold) && Position <= 0)
		{
			BuyMarket();
			_entry = candle.ClosePrice;
		}
		else if (Position > 0 && _entry is decimal e && candle.ClosePrice > e)
		{
			SellMarket();
			_entry = null;
		}
	}
}
