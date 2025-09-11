
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Day trading strategy based on MACD, RSI, EMA crossover, Bollinger Bands and ATR stops.
/// </summary>
public class MacdRsiEmaBbAtrDayTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _emaFast;
	private readonly StrategyParam<int> _emaSlow;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;
	
	private MovingAverageConvergenceDivergence _macd;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _emaFastInd;
	private ExponentialMovingAverage _emaSlowInd;
	private AverageTrueRange _atr;
	private BollingerBands _bb;
	
	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int EmaFast { get => _emaFast.Value; set => _emaFast.Value = value; }
	public int EmaSlow { get => _emaSlow.Value; set => _emaSlow.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal TrailAtrMultiplier { get => _trailAtrMultiplier.Value; set => _trailAtrMultiplier.Value = value; }
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public decimal BbMultiplier { get => _bbMultiplier.Value; set => _bbMultiplier.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public MacdRsiEmaBbAtrDayTradingStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
		.SetDisplay("MACD Fast", "MACD fast period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetDisplay("MACD Slow", "MACD slow period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 50, 1);
		
		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetDisplay("MACD Signal", "MACD signal period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "Length of RSI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);
		
		_rsiOverbought = Param(nameof(RsiOverbought), 70)
		.SetDisplay("RSI Overbought", "Overbought level", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(60, 80, 5);
		
		_rsiOversold = Param(nameof(RsiOversold), 30)
		.SetDisplay("RSI Oversold", "Oversold level", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 5);
		
		_emaFast = Param(nameof(EmaFast), 9)
		.SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_emaSlow = Param(nameof(EmaSlow), 21)
		.SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 1);
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetDisplay("ATR Length", "ATR length", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
		.SetDisplay("ATR Multiplier", "Stop-loss ATR multiplier", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
		
		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 1.5m)
		.SetDisplay("Trail ATR Mult", "Trailing ATR multiplier", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);
		
		_bbLength = Param(nameof(BbLength), 20)
		.SetDisplay("BB Length", "Bollinger Bands length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 1);
		
		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
		.SetDisplay("BB Mult", "Bollinger Bands multiplier", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_riskReward = Param(nameof(RiskReward), 2m)
		.SetDisplay("Risk Reward", "Take profit multiplier", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_macd?.Reset();
		_rsi?.Reset();
		_emaFastInd?.Reset();
		_emaSlowInd?.Reset();
		_atr?.Reset();
		_bb?.Reset();
		_prevMacd = 0m;
		_prevSignal = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFast,
			Slow = MacdSlow,
			Signal = MacdSignal
		};
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_emaFastInd = new ExponentialMovingAverage { Length = EmaFast };
		_emaSlowInd = new ExponentialMovingAverage { Length = EmaSlow };
		_atr = new AverageTrueRange { Length = AtrLength };
		_bb = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_macd, _rsi, _emaFastInd, _emaSlowInd, _atr, _bb, ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle,
	decimal macd,
	decimal signal,
	decimal hist,
	decimal rsi,
	decimal emaFast,
	decimal emaSlow,
	decimal atr,
	decimal middle,
	decimal upper,
	decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var bullCross = _prevMacd <= _prevSignal && macd > signal;
		var bearCross = _prevMacd >= _prevSignal && macd < signal;
		var upTrend = emaFast > emaSlow;
		var downTrend = emaFast < emaSlow;
		var bbSqueeze = (upper - lower) / middle < 0.1m;
		
		var longCondition = bullCross && upTrend && rsi > 40m && rsi < RsiOverbought && !bbSqueeze;
		var shortCondition = bearCross && downTrend && rsi < 60m && rsi > RsiOversold && !bbSqueeze;
		
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
			var trail = candle.ClosePrice - atr * TrailAtrMultiplier;
			if (trail > _stopPrice)
			_stopPrice = trail;
			
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			var trail = candle.ClosePrice + atr * TrailAtrMultiplier;
			if (trail < _stopPrice)
			_stopPrice = trail;
			
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			BuyMarket(Math.Abs(Position));
		}
		
		_prevMacd = macd;
		_prevSignal = signal;
	}
}
