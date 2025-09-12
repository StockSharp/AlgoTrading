using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

	/// <summary>
	/// EMA crossover strategy with MACD and RSI filters plus percent-based exits.
	/// </summary>
	public class ManadiBuySellStrategy : Strategy
	{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiUpperLong;
	private readonly StrategyParam<int> _rsiLowerLong;
	private readonly StrategyParam<int> _rsiUpperShort;
	private readonly StrategyParam<int> _rsiLowerShort;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private MovingAverageConvergenceDivergence _macd;
	private RelativeStrengthIndex _rsi;
	
	private decimal _prevEmaFast;
	private decimal _prevEmaSlow;
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
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	
	/// <summary>
	/// RSI upper bound for long entries.
	/// </summary>
	public int RsiUpperLong { get => _rsiUpperLong.Value; set => _rsiUpperLong.Value = value; }
	
	/// <summary>
	/// RSI lower bound for long entries.
	/// </summary>
	public int RsiLowerLong { get => _rsiLowerLong.Value; set => _rsiLowerLong.Value = value; }
	
	/// <summary>
	/// RSI upper bound for short entries.
	/// </summary>
	public int RsiUpperShort { get => _rsiUpperShort.Value; set => _rsiUpperShort.Value = value; }
	
	/// <summary>
	/// RSI lower bound for short entries.
	/// </summary>
	public int RsiLowerShort { get => _rsiLowerShort.Value; set => _rsiLowerShort.Value = value; }
	
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
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	
	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ManadiBuySellStrategy()
	{
	_fastEmaLength = Param(nameof(FastEmaLength), 9)
	.SetDisplay("Fast EMA Length", "Length for fast EMA", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(5, 15, 1);
	
	_slowEmaLength = Param(nameof(SlowEmaLength), 21)
	.SetDisplay("Slow EMA Length", "Length for slow EMA", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(10, 30, 1);
	
	_rsiLength = Param(nameof(RsiLength), 14)
	.SetDisplay("RSI Length", "Length for RSI", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(10, 20, 1);
	
	_rsiUpperLong = Param(nameof(RsiUpperLong), 70)
	.SetDisplay("RSI Upper Long", "Upper bound for long", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(60, 80, 5);
	
	_rsiLowerLong = Param(nameof(RsiLowerLong), 40)
	.SetDisplay("RSI Lower Long", "Lower bound for long", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(30, 50, 5);
	
	_rsiUpperShort = Param(nameof(RsiUpperShort), 60)
	.SetDisplay("RSI Upper Short", "Upper bound for short", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(50, 70, 5);
	
	_rsiLowerShort = Param(nameof(RsiLowerShort), 30)
	.SetDisplay("RSI Lower Short", "Lower bound for short", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(20, 40, 5);
	
	_macdFast = Param(nameof(MacdFast), 12)
	.SetDisplay("MACD Fast", "Fast period", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(8, 16, 1);
	
	_macdSlow = Param(nameof(MacdSlow), 26)
	.SetDisplay("MACD Slow", "Slow period", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(20, 30, 1);
	
	_macdSignal = Param(nameof(MacdSignal), 9)
	.SetDisplay("MACD Signal", "Signal period", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(5, 15, 1);
	
	_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.03m)
	.SetDisplay("Take Profit %", "Take profit percent", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(0.01m, 0.05m, 0.01m);
	
	_stopLossPercent = Param(nameof(StopLossPercent), 0.015m)
	.SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(0.01m, 0.03m, 0.005m);
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
	_emaFast?.Reset();
	_emaSlow?.Reset();
	_macd?.Reset();
	_rsi?.Reset();
	_prevEmaFast = 0m;
	_prevEmaSlow = 0m;
	_stopPrice = 0m;
	_takeProfitPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	_emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
	_emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
	_macd = new MovingAverageConvergenceDivergence
	{
	Fast = MacdFast,
	Slow = MacdSlow,
	Signal = MacdSignal
	};
	_rsi = new RelativeStrengthIndex { Length = RsiLength };
	
	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_emaFast, _emaSlow, _macd, _rsi, ProcessCandle)
	.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle,
	decimal fastEma,
	decimal slowEma,
	decimal macd,
	decimal signal,
	decimal hist,
	decimal rsi)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	var bullCross = _prevEmaFast <= _prevEmaSlow && fastEma > slowEma;
	var bearCross = _prevEmaFast >= _prevEmaSlow && fastEma < slowEma;
	
	var longCondition = bullCross && macd > signal && rsi < RsiUpperLong && rsi > RsiLowerLong;
	var shortCondition = bearCross && macd < signal && rsi > RsiLowerShort && rsi < RsiUpperShort;
	
	if (longCondition && Position <= 0)
	{
	var volume = Volume + Math.Abs(Position);
	BuyMarket(volume);
	var close = candle.ClosePrice;
	_stopPrice = close * (1m - StopLossPercent);
	_takeProfitPrice = close * (1m + TakeProfitPercent);
	}
	else if (shortCondition && Position >= 0)
	{
	var volume = Volume + Math.Abs(Position);
	SellMarket(volume);
	var close = candle.ClosePrice;
	_stopPrice = close * (1m + StopLossPercent);
	_takeProfitPrice = close * (1m - TakeProfitPercent);
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
	
	_prevEmaFast = fastEma;
	_prevEmaSlow = slowEma;
	}
	}
