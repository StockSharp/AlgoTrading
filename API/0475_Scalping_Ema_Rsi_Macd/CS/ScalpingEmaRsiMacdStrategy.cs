using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 30-minute scalping strategy based on EMA crossover with RSI, MACD and volume filter.
/// </summary>
public class ScalpingEmaRsiMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _trendEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private ExponentialMovingAverage _trendEma;
	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergence _macd;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _volumeSma;
	
	private decimal _prevFastEma;
	private decimal _prevSlowEma;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	
	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	
	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	
	/// <summary>
	/// Trend EMA length.
	/// </summary>
	public int TrendEmaLength { get => _trendEmaLength.Value; set => _trendEmaLength.Value = value; }
	
	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	
	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	
	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	
	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	
	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	
	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	
	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	
	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	
	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	
	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }
	
	/// <summary>
	/// Volume threshold multiplier.
	/// </summary>
	public decimal VolumeThreshold { get => _volumeThreshold.Value; set => _volumeThreshold.Value = value; }
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ScalpingEmaRsiMacdStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 12)
		.SetDisplay("Fast EMA Length", "Length for fast EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(8, 20, 1);
		
		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
		.SetDisplay("Slow EMA Length", "Length for slow EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 1);
		
		_trendEmaLength = Param(nameof(TrendEmaLength), 55)
		.SetDisplay("Trend EMA Length", "Length for trend EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(40, 80, 5);
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "Length for RSI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 1);
		
		_rsiOverbought = Param(nameof(RsiOverbought), 65)
		.SetDisplay("RSI Overbought", "Upper RSI bound", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(60, 80, 5);
		
		_rsiOversold = Param(nameof(RsiOversold), 35)
		.SetDisplay("RSI Oversold", "Lower RSI bound", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 5);
		
		_macdFast = Param(nameof(MacdFast), 12)
		.SetDisplay("MACD Fast", "Fast period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(8, 16, 1);
		
		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetDisplay("MACD Slow", "Slow period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 1);
		
		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetDisplay("MACD Signal", "Signal period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetDisplay("ATR Length", "Length for ATR", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 1);
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
		.SetDisplay("ATR Multiplier", "Multiplier for stop-loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_riskReward = Param(nameof(RiskReward), 2m)
		.SetDisplay("Risk Reward", "Take profit multiplier", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
		.SetDisplay("Volume MA Length", "Length for volume average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 5);
		
		_volumeThreshold = Param(nameof(VolumeThreshold), 1.3m)
		.SetDisplay("Volume Threshold", "Volume multiplier", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1m, 2m, 0.1m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_fastEma?.Reset();
		_slowEma?.Reset();
		_trendEma?.Reset();
		_rsi?.Reset();
		_macd?.Reset();
		_atr?.Reset();
		_volumeSma?.Reset();
		_prevFastEma = 0;
		_prevSlowEma = 0;
		_stopPrice = 0;
		_takeProfitPrice = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_trendEma = new ExponentialMovingAverage { Length = TrendEmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFast,
			Slow = MacdSlow,
			Signal = MacdSignal
		};
		_atr = new AverageTrueRange { Length = AtrLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeMaLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastEma, _slowEma, _trendEma, _rsi, _macd, _atr, ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle,
	decimal fastEma,
	decimal slowEma,
	decimal trendEma,
	decimal rsi,
	decimal macd,
	decimal signal,
	decimal hist,
	decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var volMa = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var highVol = candle.TotalVolume > volMa * VolumeThreshold;
		
		var upTrend = candle.ClosePrice > trendEma && fastEma > slowEma;
		var downTrend = candle.ClosePrice < trendEma && fastEma < slowEma;
		var bullCross = _prevFastEma <= _prevSlowEma && fastEma > slowEma;
		var bearCross = _prevFastEma >= _prevSlowEma && fastEma < slowEma;
		
		var longCondition = bullCross && upTrend && rsi > 40m && rsi < RsiOverbought && macd > signal && highVol;
		var shortCondition = bearCross && downTrend && rsi < 60m && rsi > RsiOversold && macd < signal && highVol;
		
		if (longCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_stopPrice = candle.ClosePrice - atr * AtrMultiplier;
			_takeProfitPrice = candle.ClosePrice + (candle.ClosePrice - _stopPrice) * RiskReward;
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_stopPrice = candle.ClosePrice + atr * AtrMultiplier;
			_takeProfitPrice = candle.ClosePrice - (_stopPrice - candle.ClosePrice) * RiskReward;
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			BuyMarket(Math.Abs(Position));
		}
		
		_prevFastEma = fastEma;
		_prevSlowEma = slowEma;
	}
}

