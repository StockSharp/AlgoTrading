namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI, MA, MACD and Stochastic based long-only bot.
/// </summary>
public class UltimateTradingBotStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevRsi;
	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevK;
	private decimal _prevD;
	private bool _isFirst;
	
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}
	
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}
	
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}
	
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}
	
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}
	
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}
	
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}
	
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public UltimateTradingBotStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "Period of RSI", "General");
		
		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
		.SetDisplay("RSI Overbought", "Overbought level", "General");
		
		_rsiOversold = Param(nameof(RsiOversold), 30m)
		.SetDisplay("RSI Oversold", "Oversold level", "General");
		
		_maLength = Param(nameof(MaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "Moving average period", "General");
		
		_stochLength = Param(nameof(StochLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Length", "Stochastic lookback", "General");
		
		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period", "General");
		
		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period", "General");
		
		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0m;
		_prevMacd = 0m;
		_prevSignal = 0m;
		_prevK = 0m;
		_prevD = 0m;
		_isFirst = true;
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ma = new SMA { Length = MaLength };
		var stoch = new StochasticOscillator
		{
			Length = StochLength,
			K = { Length = 3 },
			D = { Length = 3 }
		};
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortLength = MacdFast,
			LongLength = MacdSlow,
			SignalLength = MacdSignal
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(rsi, ma, macd, stoch, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, ma);
			DrawIndicator(area, macd);
			DrawIndicator(area, stoch);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue maValue, IIndicatorValue macdValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var rsi = rsiValue.ToDecimal();
		var ma = maValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd;
		var signalLine = macdTyped.Signal;
		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal k || stochTyped.D is not decimal d)
		return;
		
		if (_isFirst)
		{
			_prevRsi = rsi;
			_prevMacd = macdLine;
			_prevSignal = signalLine;
			_prevK = k;
			_prevD = d;
			_isFirst = false;
			return;
		}
		
		var rsiCrossUp = _prevRsi <= RsiOversold && rsi > RsiOversold;
		var rsiCrossDown = _prevRsi >= RsiOverbought && rsi < RsiOverbought;
		var macdCrossUp = _prevMacd <= _prevSignal && macdLine > signalLine;
		var macdCrossDown = _prevMacd >= _prevSignal && macdLine < signalLine;
		var stochCrossUp = _prevK <= _prevD && k > d;
		var stochCrossDown = _prevK >= _prevD && k < d;
		
		var longCondition = rsiCrossUp && candle.ClosePrice > ma && macdCrossUp && stochCrossUp;
		var shortCondition = rsiCrossDown && candle.ClosePrice < ma && macdCrossDown && stochCrossDown;
		
		if (longCondition && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && Position > 0)
		SellMarket(Position);
		
		_prevRsi = rsi;
		_prevMacd = macdLine;
		_prevSignal = signalLine;
		_prevK = k;
		_prevD = d;
	}
}
