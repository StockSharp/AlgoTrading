using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// WAMI Cloud X2 strategy combining higher timeframe trend filtering with lower timeframe entry timing.
/// </summary>
public class WamiCloudX2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<int> _trendPeriod1;
	private readonly StrategyParam<MovingAverageMethod> _trendMethod1;
	private readonly StrategyParam<int> _trendPeriod2;
	private readonly StrategyParam<MovingAverageMethod> _trendMethod2;
	private readonly StrategyParam<int> _trendPeriod3;
	private readonly StrategyParam<MovingAverageMethod> _trendMethod3;
	private readonly StrategyParam<int> _trendSignalPeriod;
	private readonly StrategyParam<MovingAverageMethod> _trendSignalMethod;

	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<int> _signalPeriod1;
	private readonly StrategyParam<MovingAverageMethod> _signalMethod1;
	private readonly StrategyParam<int> _signalPeriod2;
	private readonly StrategyParam<MovingAverageMethod> _signalMethod2;
	private readonly StrategyParam<int> _signalPeriod3;
	private readonly StrategyParam<MovingAverageMethod> _signalMethod3;
	private readonly StrategyParam<int> _signalSignalPeriod;
	private readonly StrategyParam<MovingAverageMethod> _signalSignalMethod;

	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _closeLongOnTrendFlip;
	private readonly StrategyParam<bool> _closeShortOnTrendFlip;
	private readonly StrategyParam<bool> _closeLongOnSignal;
	private readonly StrategyParam<bool> _closeShortOnSignal;
	private readonly StrategyParam<decimal> _tradeVolume;

	private readonly List<WamiSample> _signalHistory = new();
	private int _trendDirection;

	private record struct WamiSample(decimal Main, decimal Signal);

	/// <summary>
	/// Higher timeframe candle type used for the trend filter.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// First smoothing period for the trend WAMI calculation.
	/// </summary>
	public int TrendPeriod1
	{
		get => _trendPeriod1.Value;
		set => _trendPeriod1.Value = value;
	}

	/// <summary>
	/// Method used for the first trend moving average.
	/// </summary>
	public MovingAverageMethod TrendMethod1
	{
		get => _trendMethod1.Value;
		set => _trendMethod1.Value = value;
	}

	/// <summary>
	/// Second smoothing period for the trend WAMI calculation.
	/// </summary>
	public int TrendPeriod2
	{
		get => _trendPeriod2.Value;
		set => _trendPeriod2.Value = value;
	}

	/// <summary>
	/// Method used for the second trend moving average.
	/// </summary>
	public MovingAverageMethod TrendMethod2
	{
		get => _trendMethod2.Value;
		set => _trendMethod2.Value = value;
	}

	/// <summary>
	/// Third smoothing period for the trend WAMI calculation.
	/// </summary>
	public int TrendPeriod3
	{
		get => _trendPeriod3.Value;
		set => _trendPeriod3.Value = value;
	}

	/// <summary>
	/// Method used for the third trend moving average.
	/// </summary>
	public MovingAverageMethod TrendMethod3
	{
		get => _trendMethod3.Value;
		set => _trendMethod3.Value = value;
	}

	/// <summary>
	/// Period of the signal line inside the trend WAMI chain.
	/// </summary>
	public int TrendSignalPeriod
	{
		get => _trendSignalPeriod.Value;
		set => _trendSignalPeriod.Value = value;
	}

	/// <summary>
	/// Method of the signal line inside the trend WAMI chain.
	/// </summary>
	public MovingAverageMethod TrendSignalMethod
	{
		get => _trendSignalMethod.Value;
		set => _trendSignalMethod.Value = value;
	}

	/// <summary>
	/// Lower timeframe candle type used to search for entries.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// First smoothing period for the signal WAMI chain.
	/// </summary>
	public int SignalPeriod1
	{
		get => _signalPeriod1.Value;
		set => _signalPeriod1.Value = value;
	}

	/// <summary>
	/// Method of the first signal moving average.
	/// </summary>
	public MovingAverageMethod SignalMethod1
	{
		get => _signalMethod1.Value;
		set => _signalMethod1.Value = value;
	}

	/// <summary>
	/// Second smoothing period for the signal WAMI chain.
	/// </summary>
	public int SignalPeriod2
	{
		get => _signalPeriod2.Value;
		set => _signalPeriod2.Value = value;
	}

	/// <summary>
	/// Method of the second signal moving average.
	/// </summary>
	public MovingAverageMethod SignalMethod2
	{
		get => _signalMethod2.Value;
		set => _signalMethod2.Value = value;
	}

	/// <summary>
	/// Third smoothing period for the signal WAMI chain.
	/// </summary>
	public int SignalPeriod3
	{
		get => _signalPeriod3.Value;
		set => _signalPeriod3.Value = value;
	}

	/// <summary>
	/// Method of the third signal moving average.
	/// </summary>
	public MovingAverageMethod SignalMethod3
	{
		get => _signalMethod3.Value;
		set => _signalMethod3.Value = value;
	}

	/// <summary>
	/// Period of the signal line used on the entry timeframe.
	/// </summary>
	public int SignalSignalPeriod
	{
		get => _signalSignalPeriod.Value;
		set => _signalSignalPeriod.Value = value;
	}

	/// <summary>
	/// Method of the signal line on the entry timeframe.
	/// </summary>
	public MovingAverageMethod SignalSignalMethod
	{
		get => _signalSignalMethod.Value;
		set => _signalSignalMethod.Value = value;
	}

	/// <summary>
	/// Number of closed candles back to evaluate for the cross conditions.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Enables buying when the entry conditions are met.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables selling when the entry conditions are met.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Forces long exits when the trend timeframe flips bearish.
	/// </summary>
	public bool CloseLongOnTrendFlip
	{
		get => _closeLongOnTrendFlip.Value;
		set => _closeLongOnTrendFlip.Value = value;
	}

	/// <summary>
	/// Forces short exits when the trend timeframe flips bullish.
	/// </summary>
	public bool CloseShortOnTrendFlip
	{
		get => _closeShortOnTrendFlip.Value;
		set => _closeShortOnTrendFlip.Value = value;
	}

	/// <summary>
	/// Closes long positions when the signal WAMI keeps pointing down.
	/// </summary>
	public bool CloseLongOnSignal
	{
		get => _closeLongOnSignal.Value;
		set => _closeLongOnSignal.Value = value;
	}

	/// <summary>
	/// Closes short positions when the signal WAMI keeps pointing up.
	/// </summary>
	public bool CloseShortOnSignal
	{
		get => _closeShortOnSignal.Value;
		set => _closeShortOnSignal.Value = value;
	}

	/// <summary>
	/// Volume used for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="WamiCloudX2Strategy"/>.
	/// </summary>
	public WamiCloudX2Strategy()
	{
		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Trend Candle", "Higher timeframe used for the trend filter", "Trend");

		_trendPeriod1 = Param(nameof(TrendPeriod1), 4)
		.SetGreaterThanZero()
		.SetDisplay("Trend MA 1", "First smoothing period for trend WAMI", "Trend");

		_trendMethod1 = Param(nameof(TrendMethod1), MovingAverageMethod.Sma)
		.SetDisplay("Trend MA 1 Method", "Method for the first trend MA", "Trend");

		_trendPeriod2 = Param(nameof(TrendPeriod2), 13)
		.SetGreaterThanZero()
		.SetDisplay("Trend MA 2", "Second smoothing period for trend WAMI", "Trend");

		_trendMethod2 = Param(nameof(TrendMethod2), MovingAverageMethod.Sma)
		.SetDisplay("Trend MA 2 Method", "Method for the second trend MA", "Trend");

		_trendPeriod3 = Param(nameof(TrendPeriod3), 13)
		.SetGreaterThanZero()
		.SetDisplay("Trend MA 3", "Third smoothing period for trend WAMI", "Trend");

		_trendMethod3 = Param(nameof(TrendMethod3), MovingAverageMethod.Sma)
		.SetDisplay("Trend MA 3 Method", "Method for the third trend MA", "Trend");

		_trendSignalPeriod = Param(nameof(TrendSignalPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Trend Signal", "Signal period on the trend timeframe", "Trend");

		_trendSignalMethod = Param(nameof(TrendSignalMethod), MovingAverageMethod.Sma)
		.SetDisplay("Trend Signal Method", "Signal method on the trend timeframe", "Trend");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Signal Candle", "Lower timeframe used for entries", "Signal");

		_signalPeriod1 = Param(nameof(SignalPeriod1), 4)
		.SetGreaterThanZero()
		.SetDisplay("Signal MA 1", "First smoothing period for entry WAMI", "Signal");

		_signalMethod1 = Param(nameof(SignalMethod1), MovingAverageMethod.Sma)
		.SetDisplay("Signal MA 1 Method", "Method for the first signal MA", "Signal");

		_signalPeriod2 = Param(nameof(SignalPeriod2), 13)
		.SetGreaterThanZero()
		.SetDisplay("Signal MA 2", "Second smoothing period for entry WAMI", "Signal");

		_signalMethod2 = Param(nameof(SignalMethod2), MovingAverageMethod.Sma)
		.SetDisplay("Signal MA 2 Method", "Method for the second signal MA", "Signal");

		_signalPeriod3 = Param(nameof(SignalPeriod3), 13)
		.SetGreaterThanZero()
		.SetDisplay("Signal MA 3", "Third smoothing period for entry WAMI", "Signal");

		_signalMethod3 = Param(nameof(SignalMethod3), MovingAverageMethod.Sma)
		.SetDisplay("Signal MA 3 Method", "Method for the third signal MA", "Signal");

		_signalSignalPeriod = Param(nameof(SignalSignalPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Signal Line", "Signal period on the entry timeframe", "Signal");

		_signalSignalMethod = Param(nameof(SignalSignalMethod), MovingAverageMethod.Sma)
		.SetDisplay("Signal Line Method", "Signal method on the entry timeframe", "Signal");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Bar", "Closed bars back used for WAMI cross", "Signal");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
		.SetDisplay("Enable Buy", "Allow long entries", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
		.SetDisplay("Enable Sell", "Allow short entries", "Trading");

		_closeLongOnTrendFlip = Param(nameof(CloseLongOnTrendFlip), true)
		.SetDisplay("Close Long on Trend", "Close longs when trend turns bearish", "Exits");

		_closeShortOnTrendFlip = Param(nameof(CloseShortOnTrendFlip), true)
		.SetDisplay("Close Short on Trend", "Close shorts when trend turns bullish", "Exits");

		_closeLongOnSignal = Param(nameof(CloseLongOnSignal), true)
		.SetDisplay("Close Long on Signal", "Close longs if signal WAMI stays bearish", "Exits");

		_closeShortOnSignal = Param(nameof(CloseShortOnSignal), true)
		.SetDisplay("Close Short on Signal", "Close shorts if signal WAMI stays bullish", "Exits");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume for new entries", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, TrendCandleType),
			(Security, SignalCandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_signalHistory.Clear();
		_trendDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var trendIndicator = CreateWami(
		TrendPeriod1,
		TrendMethod1,
		TrendPeriod2,
		TrendMethod2,
		TrendPeriod3,
		TrendMethod3,
		TrendSignalPeriod,
		TrendSignalMethod);

		var signalIndicator = CreateWami(
		SignalPeriod1,
		SignalMethod1,
		SignalPeriod2,
		SignalMethod2,
		SignalPeriod3,
		SignalMethod3,
		SignalSignalPeriod,
		SignalSignalMethod);

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription
		.BindEx(trendIndicator, ProcessTrendCandle)
		.Start();

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription
		.BindEx(signalIndicator, ProcessSignalCandle)
		.Start();

		var signalArea = CreateChartArea();
		if (signalArea != null)
		{
			DrawCandles(signalArea, signalSubscription);
			DrawIndicator(signalArea, signalIndicator);
			DrawOwnTrades(signalArea);
		}

		var trendArea = CreateChartArea();
		if (trendArea != null)
		{
			DrawCandles(trendArea, trendSubscription);
			DrawIndicator(trendArea, trendIndicator);
		}
	}

	private static WamiIndicator CreateWami(
	int period1,
	MovingAverageMethod method1,
	int period2,
	MovingAverageMethod method2,
	int period3,
	MovingAverageMethod method3,
	int signalPeriod,
	MovingAverageMethod signalMethod)
	{
		return new WamiIndicator
		{
			Length1 = period1,
			Method1 = method1,
			Length2 = period2,
			Method2 = method2,
			Length3 = period3,
			Method3 = method3,
			SignalLength = signalPeriod,
			SignalMethod = signalMethod
		};
	}

	private void ProcessTrendCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (indicatorValue is not WamiValue wami || !wami.IsFormed)
		return;

		var main = wami.Main;
		var signal = wami.Signal;

		if (main > signal)
		{
			_trendDirection = 1;
		}
		else if (main < signal)
		{
			_trendDirection = -1;
		}
		else
		{
			_trendDirection = 0;
		}
	}

	private void ProcessSignalCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (indicatorValue is not WamiValue wami || !wami.IsFormed)
		return;

		var currentSample = new WamiSample(wami.Main, wami.Signal);
		_signalHistory.Add(currentSample);

		var signalBarShift = Math.Max(1, SignalBar);
		var maxHistory = signalBarShift + 3;
		if (_signalHistory.Count > maxHistory)
		{
			_signalHistory.RemoveAt(0);
		}

		if (_signalHistory.Count < signalBarShift + 1)
		return;

		var total = _signalHistory.Count;
		var current = _signalHistory[total - signalBarShift];
		var previous = _signalHistory[total - signalBarShift - 1];

		var closeLong = false;
		var closeShort = false;
		var openLong = false;
		var openShort = false;

		if (CloseLongOnSignal && previous.Main < previous.Signal)
		{
			closeLong = true;
		}

		if (CloseShortOnSignal && previous.Main > previous.Signal)
		{
			closeShort = true;
		}

		if (_trendDirection < 0)
		{
			if (CloseLongOnTrendFlip)
			{
				closeLong = true;
			}

			if (EnableSellEntries && current.Main >= current.Signal && previous.Main < previous.Signal)
			{
				openShort = true;
			}
		}
		else if (_trendDirection > 0)
		{
			if (CloseShortOnTrendFlip)
			{
				closeShort = true;
			}

			if (EnableBuyEntries && current.Main <= current.Signal && previous.Main > previous.Signal)
			{
				openLong = true;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (closeLong && Position > 0)
		{
			SellMarket(Position);
		}

		if (closeShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (openLong)
		{
			OpenLong();
		}

		if (openShort)
		{
			OpenShort();
		}
	}

	private void OpenLong()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position >= 0)
		{
			if (Position > 0)
			return;

			BuyMarket(TradeVolume);
		}
		else
		{
			BuyMarket(Math.Abs(Position) + TradeVolume);
		}
	}

	private void OpenShort()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position <= 0)
		{
			if (Position < 0)
			return;

			SellMarket(TradeVolume);
		}
		else
		{
			SellMarket(Position + TradeVolume);
		}
	}
}

/// <summary>
/// WAMI indicator output containing both main and signal lines.
/// </summary>
public class WamiValue : ComplexIndicatorValue
{
	public WamiValue(IIndicator indicator, IIndicatorValue input, decimal main, decimal signal, bool isFormed)
	: base(indicator, input, (nameof(Main), main), (nameof(Signal), signal))
	{
		IsFormed = isFormed;
	}

	/// <summary>
	/// Main WAMI value.
	/// </summary>
	public decimal Main => (decimal)GetValue(nameof(Main));

	/// <summary>
	/// Signal WAMI value.
	/// </summary>
	public decimal Signal => (decimal)GetValue(nameof(Signal));

	/// <summary>
	/// Indicates whether all inner averages are formed.
	/// </summary>
	public bool IsFormed { get; }
}

/// <summary>
/// WAMI indicator implementation built from chained moving averages of price momentum.
/// </summary>
public class WamiIndicator : BaseIndicator<WamiValue>
{
	private decimal? _previousClose;
	private IIndicator _ma1;
	private IIndicator _ma2;
	private IIndicator _ma3;
	private IIndicator _signalMa;

	/// <summary>
	/// Period of the first moving average in the chain.
	/// </summary>
	public int Length1 { get; set; } = 4;

	/// <summary>
	/// Method of the first moving average in the chain.
	/// </summary>
	public MovingAverageMethod Method1 { get; set; } = MovingAverageMethod.Sma;

	/// <summary>
	/// Period of the second moving average in the chain.
	/// </summary>
	public int Length2 { get; set; } = 13;

	/// <summary>
	/// Method of the second moving average in the chain.
	/// </summary>
	public MovingAverageMethod Method2 { get; set; } = MovingAverageMethod.Sma;

	/// <summary>
	/// Period of the third moving average in the chain.
	/// </summary>
	public int Length3 { get; set; } = 13;

	/// <summary>
	/// Method of the third moving average in the chain.
	/// </summary>
	public MovingAverageMethod Method3 { get; set; } = MovingAverageMethod.Sma;

	/// <summary>
	/// Period of the final signal moving average.
	/// </summary>
	public int SignalLength { get; set; } = 4;

	/// <summary>
	/// Method of the final signal moving average.
	/// </summary>
	public MovingAverageMethod SignalMethod { get; set; } = MovingAverageMethod.Sma;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle)
		return new WamiValue(this, input, default, default, false);

		if (candle.State != CandleStates.Finished)
		return new WamiValue(this, input, default, default, false);

		var close = candle.ClosePrice;

		if (_previousClose is null)
		{
			_previousClose = close;
			return new WamiValue(this, input, default, default, false);
		}

		var momentum = close - _previousClose.Value;
		_previousClose = close;

		_ma1 ??= CreateAverage(Method1, Length1);
		var ma1Value = _ma1.Process(new DecimalIndicatorValue(_ma1, momentum, input.Time));
		if (!_ma1.IsFormed)
		return new WamiValue(this, input, default, default, false);

		var ma1 = ma1Value.ToDecimal();

		_ma2 ??= CreateAverage(Method2, Length2);
		var ma2Value = _ma2.Process(new DecimalIndicatorValue(_ma2, ma1, input.Time));
		if (!_ma2.IsFormed)
		return new WamiValue(this, input, default, default, false);

		var ma2 = ma2Value.ToDecimal();

		_ma3 ??= CreateAverage(Method3, Length3);
		var ma3Value = _ma3.Process(new DecimalIndicatorValue(_ma3, ma2, input.Time));
		if (!_ma3.IsFormed)
		return new WamiValue(this, input, default, default, false);

		var ma3 = ma3Value.ToDecimal();

		_signalMa ??= CreateAverage(SignalMethod, SignalLength);
		var signalValue = _signalMa.Process(new DecimalIndicatorValue(_signalMa, ma3, input.Time));
		if (!_signalMa.IsFormed)
		return new WamiValue(this, input, default, default, false);

		var signal = signalValue.ToDecimal();

		return new WamiValue(this, input, ma3, signal, true);
	}

	private static IIndicator CreateAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Ema => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smma => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Lwma => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}
}

/// <summary>
/// Moving average methods supported by the WAMI implementation.
/// </summary>
public enum MovingAverageMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Sma,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Ema,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smma,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma
}
