using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MQL Interceptor strategy using the StockSharp high level API.
/// The strategy aligns multi-timeframe EMA "fans", Stochastic oscillators and several
/// price action filters (flat breakout, hammer detection, divergence and fan convergence).
/// </summary>
public class InterceptorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<decimal> _flatnessCoefficient;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _takeProfitAfterBreakeven;
	private readonly StrategyParam<int> _stopLossAfterBreakeven;
	private readonly StrategyParam<int> _maxFanDistanceM5;
	private readonly StrategyParam<int> _maxFanDistanceM15;
	private readonly StrategyParam<int> _maxFanDistanceH1;
	private readonly StrategyParam<int> _stochasticKPeriodM5;
	private readonly StrategyParam<int> _stochasticUpperM5;
	private readonly StrategyParam<int> _stochasticLowerM5;
	private readonly StrategyParam<int> _stochasticKPeriodM15;
	private readonly StrategyParam<int> _stochasticUpperM15;
	private readonly StrategyParam<int> _stochasticLowerM15;
	private readonly StrategyParam<int> _minBodyPoints;
	private readonly StrategyParam<int> _minFlatBars;
	private readonly StrategyParam<int> _maxFlatPoints;
	private readonly StrategyParam<int> _minDivergenceBars;
	private readonly StrategyParam<int> _hammerLongShadowPercent;
	private readonly StrategyParam<int> _hammerShortShadowPercent;
	private readonly StrategyParam<int> _hammerMinSizePoints;
	private readonly StrategyParam<int> _hammerLookbackBars;
	private readonly StrategyParam<int> _hammerRangeBars;
	private readonly StrategyParam<int> _maxFanWidthAtNarrowest;
	private readonly StrategyParam<int> _fanConvergedBars;
	private readonly StrategyParam<int> _rangeBreakLookback;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<int> _trailingDistancePoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage[] _m5Emas;
	private ExponentialMovingAverage[] _m15Emas;
	private ExponentialMovingAverage[] _h1Emas;
	private StochasticOscillator _stochasticM5;
	private StochasticOscillator _stochasticM15;

	private readonly RollingWindow<CandleSnapshot> _m5History = new(600);
	private readonly RollingWindow<decimal> _m5FanWidthHistory = new(600);
	private readonly RollingWindow<int> _m5OrientationHistory = new(600);
	private readonly RollingWindow<decimal> _stochKHistory = new(600);
	private readonly RollingWindow<decimal> _stochDHistory = new(600);
	private readonly RollingWindow<CandleSnapshot> _m15History = new(200);
	private readonly RollingWindow<decimal> _m15StochKHistory = new(200);
	private readonly RollingWindow<decimal> _m15StochDHistory = new(200);

	private decimal[] _lastM5EmaValues;
	private decimal[] _lastM15EmaValues;
	private decimal[] _lastH1EmaValues;
	private DateTimeOffset? _lastM5EmaTime;
	private DateTimeOffset? _lastM5StochTime;
	private DateTimeOffset? _lastProcessedM5Time;

	private int _veerM15;
	private decimal _distM15;
	private int _probM15;
	private int _veerH1;
	private decimal _distH1;
	private int _stochCrossM15;

	private decimal _priceStep;
	private decimal? _entryPrice;
	private int _positionDirection;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;
	private bool _breakEvenActivated;

	private static readonly int[] EmaLengths = { 34, 55, 89, 144, 233 };

	/// <summary>
	/// Initializes a new instance of the <see cref="InterceptorStrategy"/> class.
	/// </summary>
	public InterceptorStrategy()
	{
		_volumeParam = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_flatnessCoefficient = Param(nameof(FlatnessCoefficient), 0.35m)
			.SetGreaterThanZero()
			.SetDisplay("Flatness Coef", "Flat width multiplier", "Pattern detection");

		_stopLossPoints = Param(nameof(StopLossPoints), 500)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
			.SetDisplay("Take Profit", "Take profit in points (0 disables)", "Risk");

		_takeProfitAfterBreakeven = Param(nameof(TakeProfitAfterBreakeven), 0)
			.SetDisplay("BE Profit", "Required profit before breakeven", "Risk");

		_stopLossAfterBreakeven = Param(nameof(StopLossAfterBreakeven), 450)
			.SetDisplay("BE Stop", "Distance of breakeven stop", "Risk");

		_maxFanDistanceM5 = Param(nameof(MaxFanDistanceM5), 250)
			.SetGreaterThanZero()
			.SetDisplay("Max Fan Dist M5", "Maximum EMA spread on M5", "Trend");

		_maxFanDistanceM15 = Param(nameof(MaxFanDistanceM15), 200)
			.SetGreaterThanZero()
			.SetDisplay("Max Fan Dist M15", "Maximum EMA spread on M15", "Trend");

		_maxFanDistanceH1 = Param(nameof(MaxFanDistanceH1), 600)
			.SetGreaterThanZero()
			.SetDisplay("Max Fan Dist H1", "Maximum EMA spread on H1", "Trend");

		_stochasticKPeriodM5 = Param(nameof(StochasticKPeriodM5), 24)
			.SetGreaterThanZero()
			.SetDisplay("Stoch K M5", "Stochastic %K period M5", "Oscillators");

		_stochasticUpperM5 = Param(nameof(StochasticUpperM5), 85)
			.SetDisplay("Stoch Upper M5", "Overbought level M5", "Oscillators");

		_stochasticLowerM5 = Param(nameof(StochasticLowerM5), 15)
			.SetDisplay("Stoch Lower M5", "Oversold level M5", "Oscillators");

		_stochasticKPeriodM15 = Param(nameof(StochasticKPeriodM15), 24)
			.SetGreaterThanZero()
			.SetDisplay("Stoch K M15", "Stochastic %K period M15", "Oscillators");

		_stochasticUpperM15 = Param(nameof(StochasticUpperM15), 85)
			.SetDisplay("Stoch Upper M15", "Overbought level M15", "Oscillators");

		_stochasticLowerM15 = Param(nameof(StochasticLowerM15), 15)
			.SetDisplay("Stoch Lower M15", "Oversold level M15", "Oscillators");

		_minBodyPoints = Param(nameof(MinBodyPoints), 150)
			.SetDisplay("Min Body", "Minimal body size", "Pattern detection");

		_minFlatBars = Param(nameof(MinFlatBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Min Flat Bars", "Minimal bars in flat", "Pattern detection");

		_maxFlatPoints = Param(nameof(MaxFlatPoints), 150)
			.SetDisplay("Max Flat Width", "Maximum flat size", "Pattern detection");

		_minDivergenceBars = Param(nameof(MinDivergenceBars), 75)
			.SetDisplay("Min Divergence Bars", "Minimal bars between pivots", "Pattern detection");

		_hammerLongShadowPercent = Param(nameof(HammerLongShadowPercent), 80)
			.SetDisplay("Hammer Long %", "Minimal long shadow percent", "Pattern detection");

		_hammerShortShadowPercent = Param(nameof(HammerShortShadowPercent), 10)
			.SetDisplay("Hammer Short %", "Maximum short shadow percent", "Pattern detection");

		_hammerMinSizePoints = Param(nameof(HammerMinSizePoints), 11)
			.SetDisplay("Hammer Size", "Minimal hammer size", "Pattern detection");

		_hammerLookbackBars = Param(nameof(HammerLookbackBars), 8)
			.SetDisplay("Hammer Lookback", "Bars to search for hammer", "Pattern detection");

		_hammerRangeBars = Param(nameof(HammerRangeBars), 15)
			.SetDisplay("Hammer Range", "Bars for high/low validation", "Pattern detection");

		_maxFanWidthAtNarrowest = Param(nameof(MaxFanWidthAtNarrowest), 30)
			.SetDisplay("Horn Width", "Max fan width when converged", "Trend");

		_fanConvergedBars = Param(nameof(FanConvergedBars), 5)
			.SetDisplay("Horn Bars", "Bars to consider horn active", "Trend");

		_rangeBreakLookback = Param(nameof(RangeBreakLookback), 10)
			.SetDisplay("Range Break Bars", "Lookback for range breakout", "Pattern detection");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 500)
			.SetDisplay("Trail Step", "Minimal trailing step", "Risk");

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 3000)
			.SetDisplay("Trail Distance", "Trailing distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("M5 Candles", "Primary candle type", "General");
	}

	/// <summary>
	/// Flat range sensitivity coefficient used in flat breakout detection.
	/// </summary>
	public decimal FlatnessCoefficient
	{
		get => _flatnessCoefficient.Value;
		set => _flatnessCoefficient.Value = value;
	}

	/// <summary>
	/// Stop-loss size expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit size expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Profit in points required before the breakeven stop is activated.
	/// </summary>
	public int TakeProfitAfterBreakeven
	{
		get => _takeProfitAfterBreakeven.Value;
		set => _takeProfitAfterBreakeven.Value = value;
	}

	/// <summary>
	/// Distance of the breakeven stop in points once enabled.
	/// </summary>
	public int StopLossAfterBreakeven
	{
		get => _stopLossAfterBreakeven.Value;
		set => _stopLossAfterBreakeven.Value = value;
	}

	/// <summary>
	/// Maximum EMA fan distance allowed on the M5 timeframe.
	/// </summary>
	public int MaxFanDistanceM5
	{
		get => _maxFanDistanceM5.Value;
		set => _maxFanDistanceM5.Value = value;
	}

	/// <summary>
	/// Maximum EMA fan distance allowed on the M15 timeframe.
	/// </summary>
	public int MaxFanDistanceM15
	{
		get => _maxFanDistanceM15.Value;
		set => _maxFanDistanceM15.Value = value;
	}

	/// <summary>
	/// Maximum EMA fan distance allowed on the H1 timeframe.
	/// </summary>
	public int MaxFanDistanceH1
	{
		get => _maxFanDistanceH1.Value;
		set => _maxFanDistanceH1.Value = value;
	}

	/// <summary>
	/// %K period for the M5 Stochastic oscillator.
	/// </summary>
	public int StochasticKPeriodM5
	{
		get => _stochasticKPeriodM5.Value;
		set => _stochasticKPeriodM5.Value = value;
	}

	/// <summary>
	/// Upper threshold for the M5 Stochastic oscillator.
	/// </summary>
	public int StochasticUpperM5
	{
		get => _stochasticUpperM5.Value;
		set => _stochasticUpperM5.Value = value;
	}

	/// <summary>
	/// Lower threshold for the M5 Stochastic oscillator.
	/// </summary>
	public int StochasticLowerM5
	{
		get => _stochasticLowerM5.Value;
		set => _stochasticLowerM5.Value = value;
	}

	/// <summary>
	/// %K period for the M15 Stochastic oscillator.
	/// </summary>
	public int StochasticKPeriodM15
	{
		get => _stochasticKPeriodM15.Value;
		set => _stochasticKPeriodM15.Value = value;
	}

	/// <summary>
	/// Upper threshold for the M15 Stochastic oscillator.
	/// </summary>
	public int StochasticUpperM15
	{
		get => _stochasticUpperM15.Value;
		set => _stochasticUpperM15.Value = value;
	}

	/// <summary>
	/// Lower threshold for the M15 Stochastic oscillator.
	/// </summary>
	public int StochasticLowerM15
	{
		get => _stochasticLowerM15.Value;
		set => _stochasticLowerM15.Value = value;
	}

	/// <summary>
	/// Minimal candle body size (in points) required for momentum confirmation.
	/// </summary>
	public int MinBodyPoints
	{
		get => _minBodyPoints.Value;
		set => _minBodyPoints.Value = value;
	}

	/// <summary>
	/// Minimal number of bars required to define a flat range.
	/// </summary>
	public int MinFlatBars
	{
		get => _minFlatBars.Value;
		set => _minFlatBars.Value = value;
	}

	/// <summary>
	/// Maximum width of the flat range in points.
	/// </summary>
	public int MaxFlatPoints
	{
		get => _maxFlatPoints.Value;
		set => _maxFlatPoints.Value = value;
	}

	/// <summary>
	/// Minimal separation between divergence pivots in bars.
	/// </summary>
	public int MinDivergenceBars
	{
		get => _minDivergenceBars.Value;
		set => _minDivergenceBars.Value = value;
	}

	/// <summary>
	/// Minimal long shadow percentage for hammer detection.
	/// </summary>
	public int HammerLongShadowPercent
	{
		get => _hammerLongShadowPercent.Value;
		set => _hammerLongShadowPercent.Value = value;
	}

	/// <summary>
	/// Maximum short shadow percentage for hammer detection.
	/// </summary>
	public int HammerShortShadowPercent
	{
		get => _hammerShortShadowPercent.Value;
		set => _hammerShortShadowPercent.Value = value;
	}

	/// <summary>
	/// Minimal hammer size in points.
	/// </summary>
	public int HammerMinSizePoints
	{
		get => _hammerMinSizePoints.Value;
		set => _hammerMinSizePoints.Value = value;
	}

	/// <summary>
	/// Maximum bars back to search for hammer patterns.
	/// </summary>
	public int HammerLookbackBars
	{
		get => _hammerLookbackBars.Value;
		set => _hammerLookbackBars.Value = value;
	}

	/// <summary>
	/// Bars used to validate hammer extremes.
	/// </summary>
	public int HammerRangeBars
	{
		get => _hammerRangeBars.Value;
		set => _hammerRangeBars.Value = value;
	}

	/// <summary>
	/// Maximum EMA fan width considered a "horn".
	/// </summary>
	public int MaxFanWidthAtNarrowest
	{
		get => _maxFanWidthAtNarrowest.Value;
		set => _maxFanWidthAtNarrowest.Value = value;
	}

	/// <summary>
	/// Maximum bars since the fan converged to keep the horn signal valid.
	/// </summary>
	public int FanConvergedBars
	{
		get => _fanConvergedBars.Value;
		set => _fanConvergedBars.Value = value;
	}

	/// <summary>
	/// Lookback window for range breakout confirmation.
	/// </summary>
	public int RangeBreakLookback
	{
		get => _rangeBreakLookback.Value;
		set => _rangeBreakLookback.Value = value;
	}

	/// <summary>
	/// Minimal trailing step when adjusting stops.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance in points from the latest close.
	/// </summary>
	public int TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	/// <summary>
	/// Primary candle source (default M5).
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromMinutes(15).TimeFrame()), (Security, TimeSpan.FromHours(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_m5History.Clear();
		_m5FanWidthHistory.Clear();
		_m5OrientationHistory.Clear();
		_stochKHistory.Clear();
		_stochDHistory.Clear();
		_m15History.Clear();
		_m15StochKHistory.Clear();
		_m15StochDHistory.Clear();

		_lastM5EmaValues = null;
		_lastM15EmaValues = null;
		_lastH1EmaValues = null;
		_lastM5EmaTime = null;
		_lastM5StochTime = null;
		_lastProcessedM5Time = null;

		_veerM15 = 0;
		_distM15 = 0;
		_probM15 = 0;
		_veerH1 = 0;
		_distH1 = 0;
		_stochCrossM15 = 0;

		_entryPrice = null;
		_positionDirection = 0;
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_breakEvenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0.0001m;
		Volume = _volumeParam.Value;

		_m5Emas = EmaLengths.Select(length => new ExponentialMovingAverage { Length = length }).ToArray();
		_m15Emas = EmaLengths.Select(length => new ExponentialMovingAverage { Length = length }).ToArray();
		_h1Emas = EmaLengths.Select(length => new ExponentialMovingAverage { Length = length }).ToArray();
		_stochasticM5 = new StochasticOscillator
		{
			Length = StochasticKPeriodM5,
			K = { Length = 3 },
			D = { Length = 4 },
		};
		_stochasticM15 = new StochasticOscillator
		{
			Length = StochasticKPeriodM15,
			K = { Length = 3 },
			D = { Length = 4 },
		};

		var m5Subscription = SubscribeCandles(CandleType);
		m5Subscription
			.Bind(_m5Emas[0], _m5Emas[1], _m5Emas[2], _m5Emas[3], _m5Emas[4], ProcessM5Emas)
			.BindEx(_stochasticM5, ProcessM5Stochastic)
			.Start();

		var m15Subscription = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		m15Subscription
			.Bind(_m15Emas[0], _m15Emas[1], _m15Emas[2], _m15Emas[3], _m15Emas[4], ProcessM15Emas)
			.BindEx(_stochasticM15, ProcessM15Stochastic)
			.Start();

		SubscribeCandles(TimeSpan.FromHours(1).TimeFrame())
			.Bind(_h1Emas[0], _h1Emas[1], _h1Emas[2], _h1Emas[3], _h1Emas[4], ProcessH1Emas)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, m5Subscription);
			foreach (var ema in _m5Emas)
				DrawIndicator(area, ema);
			DrawIndicator(area, _stochasticM5);
			DrawOwnTrades(area);
		}

		ResetRisk();
	}

	private void ProcessM5Emas(ICandleMessage candle, decimal ema34, decimal ema55, decimal ema89, decimal ema144, decimal ema233)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastM5EmaValues = new[] { ema34, ema55, ema89, ema144, ema233 };
		_lastM5EmaTime = candle.OpenTime;

		TryProcessM5(candle);
	}

	private void ProcessM5Stochastic(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)value;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		_lastM5StochTime = candle.OpenTime;
		_stochKHistory.Add(k);
		_stochDHistory.Add(d);

		TryProcessM5(candle);
	}

	private void ProcessM15Emas(ICandleMessage candle, decimal ema34, decimal ema55, decimal ema89, decimal ema144, decimal ema233)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastM15EmaValues = new[] { ema34, ema55, ema89, ema144, ema233 };
		_veerM15 = CalculateFanOrientation(_lastM15EmaValues, allowEqualityNearFast: true);
		_distM15 = _lastM15EmaValues.Max() - _lastM15EmaValues.Min();
		_probM15 = GetBreakoutProbability(new CandleSnapshot(candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice), _lastM15EmaValues);

		var snapshot = new CandleSnapshot(candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		_m15History.Add(snapshot);
	}

	private void ProcessM15Stochastic(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)value;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		_m15StochKHistory.Add(k);
		_m15StochDHistory.Add(d);
		_stochCrossM15 = EvaluateM15StochasticCross();
	}

	private void ProcessH1Emas(ICandleMessage candle, decimal ema34, decimal ema55, decimal ema89, decimal ema144, decimal ema233)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastH1EmaValues = new[] { ema34, ema55, ema89, ema144, ema233 };
		_veerH1 = CalculateFanOrientation(_lastH1EmaValues, allowEqualityNearFast: false);
		_distH1 = _lastH1EmaValues.Max() - _lastH1EmaValues.Min();
	}

	private void TryProcessM5(ICandleMessage candle)
	{
		if (_lastM5EmaValues == null || _lastM5EmaTime != candle.OpenTime)
			return;

		if (_lastM5StochTime != candle.OpenTime)
			return;

		if (_lastM15EmaValues == null || _lastH1EmaValues == null)
			return;

		if (_lastProcessedM5Time == candle.OpenTime)
			return;

		_lastProcessedM5Time = candle.OpenTime;
		ProcessCompletedM5Candle(candle);
	}

	private void ProcessCompletedM5Candle(ICandleMessage candle)
	{
		var snapshot = new CandleSnapshot(candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		_m5History.Add(snapshot);

		var analysis = AnalyzeM5Fan(snapshot, _lastM5EmaValues!);
		var veerM5 = analysis.orientation;
		var distM5 = analysis.distance;
		var probM5 = analysis.breakout;

		_m5FanWidthHistory.Add(distM5);
		_m5OrientationHistory.Add(veerM5);

		var stochCrossM5 = GetStochasticCrossM5();
		var flatBreak = CheckFlatBreakout(snapshot);
		var rangeBreak = CheckRangeBreak(snapshot);
		var divergenceBull = DetectBullishDivergence();
		var divergenceBear = DetectBearishDivergence();
		var hornBullAge = GetHornAge(true);
		var hornBearAge = GetHornAge(false);
		var hammerSignal = DetectHammer();

		UpdateRiskManagement(snapshot);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = _priceStep;
		var buySignals = new List<string>();
		var sellSignals = new List<string>();

		if (distM5 < MaxFanDistanceM5 * step && veerM5 > 0 && _veerM15 > 0 && _veerH1 > 0 && stochCrossM5 > 0 && HasStrongBody(snapshot, bullish: true))
			buySignals.Add("fan+stoch");

		if (distM5 < MaxFanDistanceM5 * step && veerM5 < 0 && _veerM15 < 0 && _veerH1 < 0 && stochCrossM5 < 0 && HasStrongBody(snapshot, bullish: false))
			sellSignals.Add("fan+stoch");

		if (probM5 > 0 && veerM5 > 0 && _veerM15 > 0 && _veerH1 > 0 && IsEqual(snapshot.Open, snapshot.Low))
			buySignals.Add("m5 breakout");

		if (probM5 < 0 && veerM5 < 0 && _veerM15 < 0 && _veerH1 < 0 && IsEqual(snapshot.Open, snapshot.High))
			sellSignals.Add("m5 breakout");

		if (flatBreak > 0 && veerM5 > 0 && _veerM15 > 0 && _veerH1 > 0)
			buySignals.Add("flat breakout");

		if (flatBreak < 0 && veerM5 < 0 && _veerM15 < 0 && _veerH1 < 0)
			sellSignals.Add("flat breakout");

		if (_probM15 > 0 && probM5 > 0 && _veerM15 > 0 && _veerH1 > 0 && _distM15 < MaxFanDistanceM15 * step)
			buySignals.Add("multi TF breakout");

		if (_probM15 < 0 && probM5 < 0 && _veerM15 < 0 && _veerH1 < 0 && _distM15 < MaxFanDistanceM15 * step)
			sellSignals.Add("multi TF breakout");

		if (divergenceBull > 0 && veerM5 > 0 && _veerM15 > 0 && _veerH1 > 0)
			buySignals.Add("stoch divergence");

		if (divergenceBear > 0 && veerM5 < 0 && _veerM15 < 0 && _veerH1 < 0)
			sellSignals.Add("stoch divergence");

		if (hammerSignal > 0 && veerM5 > 0 && _veerM15 > 0 && _veerH1 > 0)
			buySignals.Add("hammer");

		if (hammerSignal < 0 && veerM5 < 0 && _veerM15 < 0 && _veerH1 < 0)
			sellSignals.Add("hammer");

		if (_stochCrossM15 > 0 && veerM5 > 0 && _veerM15 > 0 && _veerH1 > 0)
			buySignals.Add("stoch M15");

		if (_stochCrossM15 < 0 && veerM5 < 0 && _veerM15 < 0 && _veerH1 < 0)
			sellSignals.Add("stoch M15");

		if (rangeBreak > 0 && hornBullAge > 0 && hornBullAge < FanConvergedBars && _veerM15 > 0 && _veerH1 > 0)
			buySignals.Add("horn breakout");

		if (rangeBreak < 0 && hornBearAge > 0 && hornBearAge < FanConvergedBars && _veerM15 < 0 && _veerH1 < 0)
			sellSignals.Add("horn breakout");

		if (buySignals.Count > 0 && sellSignals.Count == 0)
		{
			var shortVolume = Position < 0 ? Math.Abs(Position) : 0m;
			if (shortVolume > 0)
			{
				BuyMarket(shortVolume);
				ResetRisk();
			}

			if (Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				SetEntry(snapshot, true);
				LogInfo($"Long entry ({string.Join(", ", buySignals)}) at {snapshot.Close} on {snapshot.Time:O}.");
			}
		}
		else if (sellSignals.Count > 0 && buySignals.Count == 0)
		{
			var longVolume = Position > 0 ? Position : 0m;
			if (longVolume > 0)
			{
				SellMarket(longVolume);
				ResetRisk();
			}

			if (Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				SetEntry(snapshot, false);
				LogInfo($"Short entry ({string.Join(", ", sellSignals)}) at {snapshot.Close} on {snapshot.Time:O}.");
			}
		}
		else if (buySignals.Count > 0 && sellSignals.Count > 0)
		{
			LogInfo($"Both long ({string.Join(", ", buySignals)}) and short ({string.Join(", ", sellSignals)}) conditions detected. Skipping trade.");
		}
	}

	private (int orientation, decimal distance, int breakout) AnalyzeM5Fan(CandleSnapshot candle, decimal[] emaValues)
	{
		var orientation = CalculateFanOrientation(emaValues, allowEqualityNearFast: true);
		var distance = emaValues.Max() - emaValues.Min();

		var top = emaValues.Max();
		var bottom = emaValues.Min();
		var breakout = 0;

		if (candle.Open < bottom && candle.Close > top)
			breakout = 1;
		else if (candle.Open > top && candle.Close < bottom)
			breakout = -1;

		return (orientation, distance, breakout);
	}

	private int GetBreakoutProbability(CandleSnapshot candle, decimal[] emaValues)
	{
		if (emaValues.Length < 3)
			return 0;

		var ma0 = emaValues[0];
		var ma1 = emaValues[1];
		var ma2 = emaValues[2];

		if (candle.Open < ma0 && candle.Open < ma1 && candle.Open < ma2 && candle.Close > ma0 && candle.Close > ma1 && candle.Close > ma2)
			return 1;

		if (candle.Open > ma0 && candle.Open > ma1 && candle.Open > ma2 && candle.Close < ma0 && candle.Close < ma1 && candle.Close < ma2)
			return -1;

		return 0;
	}

	private static int CalculateFanOrientation(IReadOnlyList<decimal> values, bool allowEqualityNearFast)
	{
		if (values.Count < 5)
			return 0;

		var ascend = allowEqualityNearFast
			? values[4] < values[3] && values[3] < values[2] && values[2] <= values[1] && values[1] <= values[0]
			: values[4] < values[3] && values[3] < values[2] && values[2] < values[1] && values[1] < values[0];

		var descend = allowEqualityNearFast
			? values[4] > values[3] && values[3] > values[2] && values[2] >= values[1] && values[1] >= values[0]
			: values[4] > values[3] && values[3] > values[2] && values[2] > values[1] && values[1] > values[0];

		if (ascend)
			return 1;

		if (descend)
			return -1;

		return 0;
	}

	private int GetStochasticCrossM5()
	{
		if (_stochKHistory.Count < 2 || _stochDHistory.Count < 2)
			return 0;

		var kPrev = _stochKHistory.Values[^2];
		var kCurr = _stochKHistory.Values[^1];
		var dPrev = _stochDHistory.Values[^2];
		var dCurr = _stochDHistory.Values[^1];

		if (kPrev < dPrev && kCurr > dCurr && kCurr <= StochasticLowerM5)
			return 1;

		if (kPrev > dPrev && kCurr < dCurr && kCurr >= StochasticUpperM5)
			return -1;

		return 0;
	}

	private int EvaluateM15StochasticCross()
	{
		if (_m15StochKHistory.Count < 2 || _m15StochDHistory.Count < 2 || _m15History.Count == 0)
			return 0;

		var kPrev = _m15StochKHistory.Values[^2];
		var kCurr = _m15StochKHistory.Values[^1];
		var dPrev = _m15StochDHistory.Values[^2];
		var dCurr = _m15StochDHistory.Values[^1];
		var candle = _m15History.Values[^1];
		var kPrev2 = _m15StochKHistory.Count >= 3 ? _m15StochKHistory.Values[^3] : kPrev;

		var crossUp = kPrev < dPrev && kCurr > dCurr && candle.Close > candle.Open && (kPrev2 < StochasticLowerM15 || kPrev < StochasticLowerM15 || kCurr < StochasticLowerM15);
		var crossDown = kPrev > dPrev && kCurr < dCurr && candle.Close < candle.Open && (kPrev2 > StochasticUpperM15 || kPrev > StochasticUpperM15 || kCurr > StochasticUpperM15);

		if (crossUp)
			return 1;

		if (crossDown)
			return -1;

		return 0;
	}

	private int CheckFlatBreakout(CandleSnapshot current)
	{
		if (_m5History.Count < MinFlatBars + 1)
			return 0;

		var step = _priceStep;
		var maxWidth = MaxFlatPoints * step;
	var dynamicCoef = FlatnessCoefficient * step;
		var total = _m5History.Count;
		var available = Math.Min(total - 1, 500);

		decimal rangeHigh = decimal.MinValue;
		decimal rangeLow = decimal.MaxValue;
		var barsInRange = 0;

		for (var i = 2; i <= available; i++)
		{
			var index = total - 1 - i;
			if (index < 0)
				break;

			var bar = _m5History.Values[index];
			if (bar.High > rangeHigh)
				rangeHigh = bar.High;
			if (bar.Low < rangeLow)
				rangeLow = bar.Low;

			barsInRange++;

			if (barsInRange < MinFlatBars)
				continue;

			var allowedWidth = Math.Min(maxWidth, barsInRange * dynamicCoef);
			var width = rangeHigh - rangeLow;

			if (width > allowedWidth)
				continue;

			if (current.Open < rangeHigh && current.Close > rangeHigh)
				return 1;

			if (current.Open > rangeLow && current.Close < rangeLow)
				return -1;

			if (_m5History.Count >= 3)
			{
				var prev1 = _m5History.Values[^2];
				var prev2 = _m5History.Values[^3];

				var bullishSequence = prev2.Open < rangeHigh && prev1.Close > rangeHigh && prev2.Close > prev2.Open && prev1.Close > prev1.Open && current.Close > current.Open;
				if (bullishSequence)
					return 1;

				var bearishSequence = prev2.Open > rangeLow && prev1.Close < rangeLow && prev2.Close < prev2.Open && prev1.Close < prev1.Open && current.Close < current.Open;
				if (bearishSequence)
					return -1;
			}
		}

		return 0;
	}

	private int CheckRangeBreak(CandleSnapshot current)
	{
		var lookback = Math.Max(1, RangeBreakLookback);
		if (_m5History.Count <= lookback)
			return 0;

		var start = _m5History.Count - 1 - lookback;
		if (start < 0)
			start = 0;

		var end = _m5History.Count - 1;
		decimal highest = decimal.MinValue;
		decimal lowest = decimal.MaxValue;

		for (var i = start; i < end; i++)
		{
			var bar = _m5History.Values[i];
			if (bar.High > highest)
				highest = bar.High;
			if (bar.Low < lowest)
				lowest = bar.Low;
		}

		if (current.Open < highest && current.Close > highest)
			return 1;

		if (current.Open > lowest && current.Close < lowest)
			return -1;

		return 0;
	}

	private int GetHornAge(bool bullish)
	{
		if (_m5FanWidthHistory.Count == 0 || _m5OrientationHistory.Count == 0)
			return 0;

		var target = bullish ? 1 : -1;
		var limit = MaxFanWidthAtNarrowest * _priceStep;
		var widths = _m5FanWidthHistory.Values;
		var orientations = _m5OrientationHistory.Values;
		var count = Math.Min(widths.Count, orientations.Count);
		var age = 0;

		for (var i = count - 1; i >= 0; i--)
		{
			if (orientations[i] != target)
				break;

			if (widths[i] <= limit)
				age++;
			else
				break;

			if (age >= FanConvergedBars)
				return age;
		}

		return age;
	}

	private int DetectBullishDivergence()
	{
		if (_m5History.Count < MinDivergenceBars + 5 || _stochKHistory.Count < _m5History.Count)
			return 0;

		var lows = _m5History.Values.Select(c => c.Low).ToList();
		var pivots = FindLocalExtrema(lows, findLow: true);
		if (pivots.Count < 2)
			return 0;

		var idx2 = pivots[^1];
		var idx1 = pivots[^2];

		if (idx2 - idx1 < MinDivergenceBars)
			return 0;

		var stoch1 = _stochKHistory.Values[idx1];
		var stoch2 = _stochKHistory.Values[idx2];
		var price1 = lows[idx1];
		var price2 = lows[idx2];

		if (price2 > price1 && stoch2 < stoch1)
			return idx2 - idx1;

		return 0;
	}

	private int DetectBearishDivergence()
	{
		if (_m5History.Count < MinDivergenceBars + 5 || _stochKHistory.Count < _m5History.Count)
			return 0;

		var highs = _m5History.Values.Select(c => c.High).ToList();
		var pivots = FindLocalExtrema(highs, findLow: false);
		if (pivots.Count < 2)
			return 0;

		var idx2 = pivots[^1];
		var idx1 = pivots[^2];

		if (idx2 - idx1 < MinDivergenceBars)
			return 0;

		var stoch1 = _stochKHistory.Values[idx1];
		var stoch2 = _stochKHistory.Values[idx2];
		var price1 = highs[idx1];
		var price2 = highs[idx2];

		if (price2 < price1 && stoch2 > stoch1)
			return idx2 - idx1;

		return 0;
	}

	private static List<int> FindLocalExtrema(IReadOnlyList<decimal> values, bool findLow, int radius = 2)
	{
		var result = new List<int>();

		for (var i = radius; i < values.Count - radius; i++)
		{
			var candidate = values[i];
			var isPivot = true;

			for (var j = 1; j <= radius; j++)
			{
				if (findLow)
				{
					if (values[i - j] <= candidate || values[i + j] <= candidate)
					{
						isPivot = false;
						break;
					}
				}
				else
				{
					if (values[i - j] >= candidate || values[i + j] >= candidate)
					{
						isPivot = false;
						break;
					}
				}
			}

			if (isPivot)
				result.Add(i);
		}

		return result;
	}

	private int DetectHammer()
	{
		if (_m5History.Count <= 1)
			return 0;

		var step = _priceStep;
		var minSize = HammerMinSizePoints * step;
		var longRatio = HammerLongShadowPercent / 100m;
		var shortRatio = HammerShortShadowPercent / 100m;
		var lookback = Math.Min(HammerLookbackBars, _m5History.Count - 1);
		var rangeBars = Math.Min(HammerRangeBars, _m5History.Count);
		var values = _m5History.Values;

		for (var offset = 1; offset <= lookback; offset++)
		{
			var index = values.Count - 1 - offset;
			if (index < 0)
				break;

			var bar = values[index];
			var range = bar.High - bar.Low;
			if (range < minSize)
				continue;

			var upperShadow = bar.High - Math.Max(bar.Open, bar.Close);
			var lowerShadow = Math.Min(bar.Open, bar.Close) - bar.Low;

			if (lowerShadow >= range * longRatio && upperShadow <= range * shortRatio && bar.Close > bar.Open)
			{
				if (IsExtreme(index, rangeBars, bullish: true))
					return offset;
			}

			if (upperShadow >= range * longRatio && lowerShadow <= range * shortRatio && bar.Close < bar.Open)
			{
				if (IsExtreme(index, rangeBars, bullish: false))
					return -offset;
			}
		}

		return 0;
	}

	private bool IsExtreme(int index, int rangeBars, bool bullish)
	{
		var start = Math.Max(0, _m5History.Count - rangeBars);
		var end = _m5History.Count;

		if (bullish)
		{
			var low = _m5History.Values[index].Low;
			for (var i = start; i < end; i++)
			{
				if (_m5History.Values[i].Low < low)
					return false;
			}
			return true;
		}

		var high = _m5History.Values[index].High;
		for (var i = start; i < end; i++)
		{
			if (_m5History.Values[i].High > high)
				return false;
		}

		return true;
	}

	private bool HasStrongBody(CandleSnapshot candle, bool bullish)
	{
		var minBody = MinBodyPoints * _priceStep;

		if (bullish)
			return candle.Close > candle.Open && candle.Close - candle.Open >= minBody;

		return candle.Open > candle.Close && candle.Open - candle.Close >= minBody;
	}

	private bool IsEqual(decimal left, decimal right)
	{
		var tolerance = _priceStep / 2m;
		return Math.Abs(left - right) <= tolerance;
	}

	private void UpdateRiskManagement(CandleSnapshot candle)
	{
		var step = _priceStep;

		if (Position > 0)
		{
			if (_entryPrice == null || _positionDirection <= 0)
				SetEntry(candle, true);

			if (!_breakEvenActivated && StopLossAfterBreakeven > 0 && TakeProfitAfterBreakeven > 0 && _entryPrice != null)
			{
				if (candle.Close - _entryPrice >= TakeProfitAfterBreakeven * step)
				{
					var newStop = candle.Close - StopLossAfterBreakeven * step;
					if (_longStop == null || newStop > _longStop)
					{
						_longStop = newStop;
						_breakEvenActivated = true;
					}
				}
			}

			if (TrailingDistancePoints > 0)
			{
				var newStop = candle.Close - TrailingDistancePoints * step;
				if (_longStop == null || newStop > _longStop + TrailingStepPoints * step)
					_longStop = newStop;
			}

			if (_longTarget != null && candle.High >= _longTarget)
			{
				SellMarket(Position);
				LogInfo($"Long take profit hit at {_longTarget:F4} on {candle.Time:O}.");
				ResetRisk();
				return;
			}

			if (_longStop != null && candle.Low <= _longStop)
			{
				SellMarket(Position);
				LogInfo($"Long stop hit at {_longStop:F4} on {candle.Time:O}.");
				ResetRisk();
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice == null || _positionDirection >= 0)
				SetEntry(candle, false);

			if (!_breakEvenActivated && StopLossAfterBreakeven > 0 && TakeProfitAfterBreakeven > 0 && _entryPrice != null)
			{
				if (_entryPrice - candle.Close >= TakeProfitAfterBreakeven * step)
				{
					var newStop = candle.Close + StopLossAfterBreakeven * step;
					if (_shortStop == null || newStop < _shortStop)
					{
						_shortStop = newStop;
						_breakEvenActivated = true;
					}
				}
			}

			if (TrailingDistancePoints > 0)
			{
				var newStop = candle.Close + TrailingDistancePoints * step;
				if (_shortStop == null || newStop < _shortStop - TrailingStepPoints * step)
					_shortStop = newStop;
			}

			if (_shortTarget != null && candle.Low <= _shortTarget)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short take profit hit at {_shortTarget:F4} on {candle.Time:O}.");
				ResetRisk();
				return;
			}

			if (_shortStop != null && candle.High >= _shortStop)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short stop hit at {_shortStop:F4} on {candle.Time:O}.");
				ResetRisk();
				return;
			}
		}
		else
		{
			ResetRisk();
		}
	}

	private void SetEntry(CandleSnapshot candle, bool isLong)
	{
		_entryPrice = candle.Close;
		_positionDirection = isLong ? 1 : -1;
		_breakEvenActivated = false;

		if (isLong)
		{
			_longStop = StopLossPoints > 0 ? candle.Close - StopLossPoints * _priceStep : null;
			_longTarget = TakeProfitPoints > 0 ? candle.Close + TakeProfitPoints * _priceStep : null;
			_shortStop = null;
			_shortTarget = null;
		}
		else
		{
			_shortStop = StopLossPoints > 0 ? candle.Close + StopLossPoints * _priceStep : null;
			_shortTarget = TakeProfitPoints > 0 ? candle.Close - TakeProfitPoints * _priceStep : null;
			_longStop = null;
			_longTarget = null;
		}
	}

	private void ResetRisk()
	{
		_entryPrice = null;
		_positionDirection = 0;
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_breakEvenActivated = false;
	}

	private sealed class RollingWindow<T>
	{
		private readonly int _capacity;
		private readonly List<T> _values;

		public RollingWindow(int capacity)
		{
			_capacity = capacity;
			_values = new List<T>(capacity);
		}

		public int Count => _values.Count;

		public IReadOnlyList<T> Values => _values;

		public void Add(T value)
		{
			if (_values.Count == _capacity)
				_values.RemoveAt(0);

			_values.Add(value);
		}

		public void Clear()
		{
			_values.Clear();
		}
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(DateTimeOffset time, decimal open, decimal high, decimal low, decimal close)
		{
			Time = time;
			Open = open;
			High = high;
			Low = low;
			Close = close;
		}

		public DateTimeOffset Time { get; }
		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}
}
