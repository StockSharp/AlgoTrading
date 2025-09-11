using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive SuperTrend strategy using volatility clustering.
/// </summary>
public class MKCustomeAdaptiveSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _trainingPeriod;
	private readonly StrategyParam<decimal> _highVolPercent;
	private readonly StrategyParam<decimal> _midVolPercent;
	private readonly StrategyParam<decimal> _lowVolPercent;
	private readonly StrategyParam<decimal> _exitPercent;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private AverageTrueRange _atr;
	private Highest _atrHigh;
	private Lowest _atrLow;
	
	private decimal _prevLowerBand;
	private decimal _prevUpperBand;
	private decimal _prevSuperTrend;
	private int _prevDirection;
	private decimal _entryPrice;
	
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public int TrainingPeriod { get => _trainingPeriod.Value; set => _trainingPeriod.Value = value; }
	public decimal HighVolPercent { get => _highVolPercent.Value; set => _highVolPercent.Value = value; }
	public decimal MidVolPercent { get => _midVolPercent.Value; set => _midVolPercent.Value = value; }
	public decimal LowVolPercent { get => _lowVolPercent.Value; set => _lowVolPercent.Value = value; }
	public decimal ExitPercent { get => _exitPercent.Value; set => _exitPercent.Value = value; }
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public MKCustomeAdaptiveSuperTrendStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 10)
		.SetDisplay("ATR Length", "ATR period", "SuperTrend");
		
		_factor = Param(nameof(Factor), 3m)
		.SetDisplay("Factor", "SuperTrend factor", "SuperTrend");
		
		_trainingPeriod = Param(nameof(TrainingPeriod), 100)
		.SetDisplay("Training Period", "Range for volatility", "Volatility");
		
		_highVolPercent = Param(nameof(HighVolPercent), 0.75m)
		.SetDisplay("High Vol Percent", "Percentile for high volatility", "Volatility");
		
		_midVolPercent = Param(nameof(MidVolPercent), 0.5m)
		.SetDisplay("Mid Vol Percent", "Percentile for medium volatility", "Volatility");
		
		_lowVolPercent = Param(nameof(LowVolPercent), 0.25m)
		.SetDisplay("Low Vol Percent", "Percentile for low volatility", "Volatility");
		
		_exitPercent = Param(nameof(ExitPercent), 1.5m)
		.SetDisplay("Take Profit %", "Percent take profit", "Risk");
		
		_stopPercent = Param(nameof(StopPercent), 1m)
		.SetDisplay("Stop Loss %", "Percent stop loss", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_atr = new AverageTrueRange { Length = AtrLength };
		_atrHigh = new Highest { Length = TrainingPeriod };
		_atrLow = new Lowest { Length = TrainingPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, ProcessCandle).Start();
		
		StartProtection(
		new Unit(ExitPercent, UnitTypes.Percent),
		new Unit(StopPercent, UnitTypes.Percent));
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var atrHigh = _atrHigh.Process(atr, candle.OpenTime, true).ToDecimal();
		var atrLow = _atrLow.Process(atr, candle.OpenTime, true).ToDecimal();
		
		var highVol = atrLow + (atrHigh - atrLow) * HighVolPercent;
		var midVol = atrLow + (atrHigh - atrLow) * MidVolPercent;
		var lowVol = atrLow + (atrHigh - atrLow) * LowVolPercent;
		
		var distHigh = Math.Abs(atr - highVol);
		var distMid = Math.Abs(atr - midVol);
		var distLow = Math.Abs(atr - lowVol);
		
		var assigned = distHigh < distMid
		? (distHigh < distLow ? highVol : lowVol)
		: (distMid < distLow ? midVol : lowVol);
		
		var (st, dir) = CalcSuperTrend(candle, assigned);
		
		if (_prevDirection <= 0 && dir > 0 && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (_prevDirection >= 0 && dir < 0 && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		
		if (Position > 0)
		{
			var take = _entryPrice * (1m + ExitPercent / 100m);
			var stop = _entryPrice * (1m - StopPercent / 100m);
			
			if (candle.ClosePrice >= take || candle.ClosePrice <= stop || candle.ClosePrice < st)
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			var take = _entryPrice * (1m - ExitPercent / 100m);
			var stop = _entryPrice * (1m + StopPercent / 100m);
			
			if (candle.ClosePrice <= take || candle.ClosePrice >= stop || candle.ClosePrice > st)
			BuyMarket(Math.Abs(Position));
		}
		
		_prevDirection = dir;
	}
	
	private (decimal st, int dir) CalcSuperTrend(ICandleMessage candle, decimal atrVal)
	{
		var src = (candle.HighPrice + candle.LowPrice) / 2m;
		var upperBand = src + Factor * atrVal;
		var lowerBand = src - Factor * atrVal;
		
		if (_prevLowerBand != default && candle.ClosePrice <= _prevLowerBand)
		lowerBand = _prevLowerBand;
		
		if (_prevUpperBand != default && candle.ClosePrice >= _prevUpperBand)
		upperBand = _prevUpperBand;
		
		var dir = _prevSuperTrend == _prevUpperBand
		? (candle.ClosePrice > upperBand ? 1 : -1)
		: (candle.ClosePrice < lowerBand ? -1 : 1);
		
		var st = dir == 1 ? lowerBand : upperBand;
		
		_prevLowerBand = lowerBand;
		_prevUpperBand = upperBand;
		_prevSuperTrend = st;
		
		return (st, dir);
	}
}
