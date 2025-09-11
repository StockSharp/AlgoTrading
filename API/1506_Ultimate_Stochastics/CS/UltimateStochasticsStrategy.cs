using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic oscillator based strategy supporting long and short trades with optional opposite exits and fixed percent targets.
/// </summary>
public class UltimateStochasticsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastKLength;
	private readonly StrategyParam<int> _slowKLength;
	private readonly StrategyParam<int> _slowDLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<bool> _allowLongs;
	private readonly StrategyParam<bool> _allowShorts;
	private readonly StrategyParam<bool> _useOpposite;
	private readonly StrategyParam<bool> _useTradeManagement;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevK;
	private decimal _prevD;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;
	
	/// <summary>
	/// Fast %K length.
	/// </summary>
public int FastKLength { get => _fastKLength.Value; set => _fastKLength.Value = value; }

/// <summary>
/// Slow %K length.
/// </summary>
public int SlowKLength { get => _slowKLength.Value; set => _slowKLength.Value = value; }

/// <summary>
/// Slow %D length.
/// </summary>
public int SlowDLength { get => _slowDLength.Value; set => _slowDLength.Value = value; }

/// <summary>
/// Overbought threshold.
/// </summary>
public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

/// <summary>
/// Oversold threshold.
/// </summary>
public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

/// <summary>
/// Allow long trades.
/// </summary>
public bool AllowLongs { get => _allowLongs.Value; set => _allowLongs.Value = value; }

/// <summary>
/// Allow short trades.
/// </summary>
public bool AllowShorts { get => _allowShorts.Value; set => _allowShorts.Value = value; }

/// <summary>
/// Exit on opposite signal.
/// </summary>
public bool UseOpposite { get => _useOpposite.Value; set => _useOpposite.Value = value; }

/// <summary>
/// Use trade management (take profit/stop loss).
/// </summary>
public bool UseTradeManagement { get => _useTradeManagement.Value; set => _useTradeManagement.Value = value; }

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

public UltimateStochasticsStrategy()
{
	_fastKLength = Param(nameof(FastKLength), 9).SetGreaterThanZero().SetDisplay("Fast K", "Fast %K length", "Indicators").SetCanOptimize(true).SetOptimize(5, 15, 2);
	_slowKLength = Param(nameof(SlowKLength), 18).SetGreaterThanZero().SetDisplay("Slow K", "Slow %K smoothing", "Indicators").SetCanOptimize(true).SetOptimize(10, 25, 2);
	_slowDLength = Param(nameof(SlowDLength), 4).SetGreaterThanZero().SetDisplay("Slow D", "%D length", "Indicators").SetCanOptimize(true).SetOptimize(2, 8, 1);
	_overbought = Param(nameof(Overbought), 60m).SetRange(0m, 100m).SetDisplay("Overbought", "Overbought level", "Levels");
	_oversold = Param(nameof(Oversold), 90m).SetRange(0m, 100m).SetDisplay("Oversold", "Oversold level", "Levels");
	_allowLongs = Param(nameof(AllowLongs), true).SetDisplay("Allow Longs", "Enable long trades", "Trading");
	_allowShorts = Param(nameof(AllowShorts), true).SetDisplay("Allow Shorts", "Enable short trades", "Trading");
	_useOpposite = Param(nameof(UseOpposite), true).SetDisplay("Use Opposite", "Close on opposite signal", "Risk");
	_useTradeManagement = Param(nameof(UseTradeManagement), true).SetDisplay("Use Trade Mgmt", "Use TP/SL", "Risk");
	_takeProfitPercent = Param(nameof(TakeProfitPercent), 14m).SetGreaterThanZero().SetDisplay("TP %", "Take profit percent", "Risk");
	_stopLossPercent = Param(nameof(StopLossPercent), 8m).SetGreaterThanZero().SetDisplay("SL %", "Stop loss percent", "Risk");
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, CandleType)];
}

protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);
	
	var stoch = new StochasticOscillator
	{
		Length = FastKLength,
		K = { Length = SlowKLength },
	D = { Length = SlowDLength }
	};
	
	var subscription = SubscribeCandles(CandleType);
	subscription.BindEx(stoch, ProcessCandle).Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		var stochArea = CreateChartArea();
		DrawIndicator(stochArea, stoch);
		DrawOwnTrades(area);
	}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochVal)
{
	if (candle.State != CandleStates.Finished)
	return;
	
	var typed = (StochasticOscillatorValue)stochVal;
	var k = typed.K;
	var d = typed.D;
	
	bool longSignal = _prevK <= _prevD && k > d && k < Oversold;
	bool shortSignal = _prevK >= _prevD && k < d && k > Overbought;
	
	if (longSignal && AllowLongs && Position <= 0)
	{
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		if (UseTradeManagement)
		{
			_longStop = candle.ClosePrice * (1 - StopLossPercent / 100m);
			_longTake = candle.ClosePrice * (1 + TakeProfitPercent / 100m);
		}
	}
	else if (shortSignal && AllowShorts && Position >= 0)
	{
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		if (UseTradeManagement)
		{
			_shortStop = candle.ClosePrice * (1 + StopLossPercent / 100m);
			_shortTake = candle.ClosePrice * (1 - TakeProfitPercent / 100m);
		}
	}
	else
	{
		if (Position > 0)
		{
			if (UseOpposite && shortSignal)
			SellMarket(Math.Abs(Position));
			else if (UseTradeManagement && (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake))
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (UseOpposite && longSignal)
			BuyMarket(Math.Abs(Position));
			else if (UseTradeManagement && (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake))
			BuyMarket(Math.Abs(Position));
		}
	}
	
	_prevK = k;
	_prevD = d;
}
}
