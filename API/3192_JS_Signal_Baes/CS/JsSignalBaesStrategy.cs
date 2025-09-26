using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Param;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the "JS Signal Baes" expert advisor.
/// Combines six timeframes and five indicators to align trend direction.
/// Opens a netted position when all timeframes agree and can invert signals through a parameter.
/// </summary>
public class JsSignalBaesStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<MovingAverageKind> _maMethod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSmoothing;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _timeFrame1;
	private readonly StrategyParam<DataType> _timeFrame2;
	private readonly StrategyParam<DataType> _timeFrame3;
	private readonly StrategyParam<DataType> _timeFrame4;
	private readonly StrategyParam<DataType> _timeFrame5;
	private readonly StrategyParam<DataType> _timeFrame6;

	private TimeframeState[] _states = Array.Empty<TimeframeState>();
	private TimeframeState _primaryState;

	/// <summary>
	/// Period for the Commodity Channel Index.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

/// <summary>
/// Fast moving average length.
/// </summary>
public int FastMaPeriod
{
	get => _fastMaPeriod.Value;
	set => _fastMaPeriod.Value = value;
}

/// <summary>
/// Slow moving average length.
/// </summary>
public int SlowMaPeriod
{
	get => _slowMaPeriod.Value;
	set => _slowMaPeriod.Value = value;
}

/// <summary>
/// Smoothing method used by moving averages.
/// </summary>
public MovingAverageKind MaMethod
{
	get => _maMethod.Value;
	set => _maMethod.Value = value;
}

/// <summary>
/// Fast period of MACD.
/// </summary>
public int MacdFastPeriod
{
	get => _macdFastPeriod.Value;
	set => _macdFastPeriod.Value = value;
}

/// <summary>
/// Slow period of MACD.
/// </summary>
public int MacdSlowPeriod
{
	get => _macdSlowPeriod.Value;
	set => _macdSlowPeriod.Value = value;
}

/// <summary>
/// Signal period of MACD.
/// </summary>
public int MacdSignalPeriod
{
	get => _macdSignalPeriod.Value;
	set => _macdSignalPeriod.Value = value;
}

/// <summary>
/// K period of the stochastic oscillator.
/// </summary>
public int StochasticKPeriod
{
	get => _stochasticKPeriod.Value;
	set => _stochasticKPeriod.Value = value;
}

/// <summary>
/// D period of the stochastic oscillator.
/// </summary>
public int StochasticDPeriod
{
	get => _stochasticDPeriod.Value;
	set => _stochasticDPeriod.Value = value;
}

/// <summary>
/// Smoothing factor of the stochastic oscillator.
/// </summary>
public int StochasticSmoothing
{
	get => _stochasticSmoothing.Value;
	set => _stochasticSmoothing.Value = value;
}

/// <summary>
/// Period for RSI.
/// </summary>
public int RsiPeriod
{
	get => _rsiPeriod.Value;
	set => _rsiPeriod.Value = value;
}

/// <summary>
/// Inverts long and short signals.
/// </summary>
public bool ReverseSignals
{
	get => _reverseSignals.Value;
	set => _reverseSignals.Value = value;
}

/// <summary>
/// Primary timeframe (typically the chart timeframe).
/// </summary>
public DataType TimeFrame1
{
	get => _timeFrame1.Value;
	set => _timeFrame1.Value = value;
}

/// <summary>
/// Second confirmation timeframe.
/// </summary>
public DataType TimeFrame2
{
	get => _timeFrame2.Value;
	set => _timeFrame2.Value = value;
}

/// <summary>
/// Third confirmation timeframe.
/// </summary>
public DataType TimeFrame3
{
	get => _timeFrame3.Value;
	set => _timeFrame3.Value = value;
}

/// <summary>
/// Fourth confirmation timeframe.
/// </summary>
public DataType TimeFrame4
{
	get => _timeFrame4.Value;
	set => _timeFrame4.Value = value;
}

/// <summary>
/// Fifth confirmation timeframe.
/// </summary>
public DataType TimeFrame5
{
	get => _timeFrame5.Value;
	set => _timeFrame5.Value = value;
}

/// <summary>
/// Sixth confirmation timeframe.
/// </summary>
public DataType TimeFrame6
{
	get => _timeFrame6.Value;
	set => _timeFrame6.Value = value;
}

/// <summary>
/// Initializes strategy parameters with defaults taken from the original expert advisor.
/// </summary>
public JsSignalBaesStrategy()
{
	_cciPeriod = Param(nameof(CciPeriod), 13)
	.SetGreaterThanZero()
	.SetDisplay("CCI Period", "Length for Commodity Channel Index", "Indicators")
	.SetCanOptimize(true);

	_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
	.SetGreaterThanZero()
	.SetDisplay("Fast MA", "Fast moving average length", "Moving Averages")
	.SetCanOptimize(true);

	_slowMaPeriod = Param(nameof(SlowMaPeriod), 9)
	.SetGreaterThanZero()
	.SetDisplay("Slow MA", "Slow moving average length", "Moving Averages")
	.SetCanOptimize(true);

	_maMethod = Param(nameof(MaMethod), MovingAverageKind.LinearWeighted)
	.SetDisplay("MA Method", "Smoothing method for the two averages", "Moving Averages");

	_macdFastPeriod = Param(nameof(MacdFastPeriod), 8)
	.SetGreaterThanZero()
	.SetDisplay("MACD Fast", "Fast EMA length", "MACD")
	.SetCanOptimize(true);

	_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 17)
	.SetGreaterThanZero()
	.SetDisplay("MACD Slow", "Slow EMA length", "MACD")
	.SetCanOptimize(true);

	_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
	.SetGreaterThanZero()
	.SetDisplay("MACD Signal", "Signal moving average length", "MACD")
	.SetCanOptimize(true);

	_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
	.SetGreaterThanZero()
	.SetDisplay("Stochastic K", "Main period for the stochastic", "Oscillators")
	.SetCanOptimize(true);

	_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
	.SetGreaterThanZero()
	.SetDisplay("Stochastic D", "Signal period for the stochastic", "Oscillators")
	.SetCanOptimize(true);

	_stochasticSmoothing = Param(nameof(StochasticSmoothing), 3)
	.SetGreaterThanZero()
	.SetDisplay("Stochastic Smoothing", "Smoothing factor for the stochastic", "Oscillators")
	.SetCanOptimize(true);

	_rsiPeriod = Param(nameof(RsiPeriod), 9)
	.SetGreaterThanZero()
	.SetDisplay("RSI Period", "Length for RSI", "Indicators")
	.SetCanOptimize(true);

	_reverseSignals = Param(nameof(ReverseSignals), false)
	.SetDisplay("Reverse", "Invert the long and short signals", "General");

	_timeFrame1 = Param(nameof(TimeFrame1), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("TimeFrame1", "Primary timeframe", "Timeframes");
	_timeFrame2 = Param(nameof(TimeFrame2), TimeSpan.FromMinutes(5).TimeFrame())
	.SetDisplay("TimeFrame2", "Second timeframe", "Timeframes");
	_timeFrame3 = Param(nameof(TimeFrame3), TimeSpan.FromMinutes(15).TimeFrame())
	.SetDisplay("TimeFrame3", "Third timeframe", "Timeframes");
	_timeFrame4 = Param(nameof(TimeFrame4), TimeSpan.FromMinutes(30).TimeFrame())
	.SetDisplay("TimeFrame4", "Fourth timeframe", "Timeframes");
	_timeFrame5 = Param(nameof(TimeFrame5), TimeSpan.FromHours(1).TimeFrame())
	.SetDisplay("TimeFrame5", "Fifth timeframe", "Timeframes");
	_timeFrame6 = Param(nameof(TimeFrame6), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("TimeFrame6", "Sixth timeframe", "Timeframes");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return new[]
	{
		(Security, TimeFrame1),
		(Security, TimeFrame2),
		(Security, TimeFrame3),
		(Security, TimeFrame4),
		(Security, TimeFrame5),
		(Security, TimeFrame6)
	};
}

/// <inheritdoc />
protected override void OnReseted()
{
	base.OnReseted();

	_states = Array.Empty<TimeframeState>();
	_primaryState = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	_states = CreateStates();
	_primaryState = _states.FirstOrDefault();

	var area = CreateChartArea();

	foreach (var state in _states)
	{
		var subscription = SubscribeCandles(state.TimeFrame);
		subscription
		.BindEx(state.FastMa, state.SlowMa, state.Macd, state.Rsi, state.Cci, state.Stochastic,
		(candle, fast, slow, macdValue, rsiValue, cciValue, stochasticValue) =>
		ProcessTimeframe(state, candle, fast, slow, macdValue, rsiValue, cciValue, stochasticValue))
		.Start();

		if (area != null && state == _primaryState)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, state.FastMa);
			DrawIndicator(area, state.SlowMa);
			DrawIndicator(area, state.Macd);
			DrawIndicator(area, state.Rsi);
			DrawIndicator(area, state.Cci);
			DrawIndicator(area, state.Stochastic);
			DrawOwnTrades(area);
		}
}

StartProtection();
}

private TimeframeState[] CreateStates()
{
	return new[]
	{
		CreateState(TimeFrame1),
		CreateState(TimeFrame2),
		CreateState(TimeFrame3),
		CreateState(TimeFrame4),
		CreateState(TimeFrame5),
		CreateState(TimeFrame6)
	};
}

private TimeframeState CreateState(DataType dataType)
{
	var fastMa = CreateMovingAverage(MaMethod, FastMaPeriod);
	var slowMa = CreateMovingAverage(MaMethod, SlowMaPeriod);
	var macd = new MovingAverageConvergenceDivergenceSignal
	{
		Macd =
		{
			ShortMa = { Length = MacdFastPeriod },
			LongMa = { Length = MacdSlowPeriod }
		},
	SignalMa = { Length = MacdSignalPeriod }
};
var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
var cci = new CommodityChannelIndex { Length = CciPeriod };
var stochastic = new StochasticOscillator
{
	KPeriod = StochasticKPeriod,
	DPeriod = StochasticDPeriod,
	Smooth = StochasticSmoothing
};

return new TimeframeState(dataType, fastMa, slowMa, macd, rsi, cci, stochastic);
}

private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageKind type, int length)
{
	return type switch
	{
		MovingAverageKind.Simple => new SimpleMovingAverage { Length = length },
		MovingAverageKind.Exponential => new ExponentialMovingAverage { Length = length },
		MovingAverageKind.Smoothed => new SmoothedMovingAverage { Length = length },
		MovingAverageKind.LinearWeighted => new WeightedMovingAverage { Length = length },
		_ => new SimpleMovingAverage { Length = length }
	};
}

private void ProcessTimeframe(TimeframeState state, ICandleMessage candle, decimal fastMaValue, decimal slowMaValue,
IIndicatorValue macdValue, decimal rsiValue, decimal cciValue, IIndicatorValue stochasticValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (!macdValue.IsFinal || !stochasticValue.IsFinal)
	return;

	if (!state.FastMa.IsFormed || !state.SlowMa.IsFormed || !state.Macd.IsFormed || !state.Rsi.IsFormed ||
	!state.Cci.IsFormed || !state.Stochastic.IsFormed)
	return;

	if (state.LastTime == candle.OpenTime)
	return;

	var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	if (macd.Macd is not decimal macdLine || macd.Signal is not decimal macdSignal)
	return;

	var stochastic = (StochasticOscillatorValue)stochasticValue;
	if (stochastic.K is not decimal stochMain)
	return;

	state.Update(candle.OpenTime, fastMaValue, slowMaValue, macdLine, macdSignal, rsiValue, cciValue, stochMain);

	if (state == _primaryState)
	EvaluateSignals();
}

private void EvaluateSignals()
{
	if (_primaryState == null)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (_states.Any(s => !s.HasValues))
	return;

	var bullish = _states.All(s => s.IsBullish);
	var bearish = _states.All(s => s.IsBearish);

	if (bullish == bearish)
	return;

	var longSignal = ReverseSignals ? bearish : bullish;
	var shortSignal = ReverseSignals ? bullish : bearish;

	if (longSignal && Position == 0)
	BuyMarket();
	else if (shortSignal && Position == 0)
	SellMarket();
}

/// <summary>
/// Moving average types supported by the original expert advisor.
/// </summary>
public enum MovingAverageKind
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}

private sealed class TimeframeState
{
	public TimeframeState(DataType timeFrame, LengthIndicator<decimal> fastMa, LengthIndicator<decimal> slowMa,
	MovingAverageConvergenceDivergenceSignal macd, RelativeStrengthIndex rsi, CommodityChannelIndex cci,
	StochasticOscillator stochastic)
	{
		TimeFrame = timeFrame;
		FastMa = fastMa;
		SlowMa = slowMa;
		Macd = macd;
		Rsi = rsi;
		Cci = cci;
		Stochastic = stochastic;
	}

public DataType TimeFrame { get; }
public LengthIndicator<decimal> FastMa { get; }
public LengthIndicator<decimal> SlowMa { get; }
public MovingAverageConvergenceDivergenceSignal Macd { get; }
public RelativeStrengthIndex Rsi { get; }
public CommodityChannelIndex Cci { get; }
public StochasticOscillator Stochastic { get; }
public bool HasValues { get; private set; }
public DateTimeOffset LastTime { get; private set; }
public decimal FastMaValue { get; private set; }
public decimal SlowMaValue { get; private set; }
public decimal MacdLine { get; private set; }
public decimal MacdSignal { get; private set; }
public decimal RsiValue { get; private set; }
public decimal CciValue { get; private set; }
public decimal StochasticMain { get; private set; }

public bool IsBullish => HasValues && FastMaValue > SlowMaValue && RsiValue > 50m && CciValue > 0m && StochasticMain > 40m && MacdLine > MacdSignal;

public bool IsBearish => HasValues && FastMaValue < SlowMaValue && RsiValue < 50m && CciValue < 0m && StochasticMain < 60m && MacdLine < MacdSignal;

public void Update(DateTimeOffset time, decimal fastMaValue, decimal slowMaValue, decimal macdLine, decimal macdSignal, decimal rsiValue, decimal cciValue, decimal stochasticMain)
{
	LastTime = time;
	FastMaValue = fastMaValue;
	SlowMaValue = slowMaValue;
	MacdLine = macdLine;
	MacdSignal = macdSignal;
	RsiValue = rsiValue;
	CciValue = cciValue;
	StochasticMain = stochasticMain;
	HasValues = true;
}
}
}
