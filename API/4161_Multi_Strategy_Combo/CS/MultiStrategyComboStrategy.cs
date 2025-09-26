using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-indicator combo strategy converted from the MQL4 Multi-Strategy iFSF EA.
/// Combines MA, RSI, MACD, Stochastic, SAR, trend, and Bollinger filters.
/// </summary>
public class MultiStrategyComboStrategy : Strategy
{
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<int> _comboFactor;

	private readonly StrategyParam<bool> _useMa;
	private readonly StrategyParam<bool> _useLastMaSignal;
	private readonly StrategyParam<int> _maMode;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _midMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<MaMethod> _fastMaMethod;
	private readonly StrategyParam<MaMethod> _midMaMethod;
	private readonly StrategyParam<MaMethod> _slowMaMethod;
	private readonly StrategyParam<DataType> _maCandleType;

	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<bool> _useLastRsiSignal;
	private readonly StrategyParam<int> _rsiMode;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<decimal> _rsiBuyZone;
	private readonly StrategyParam<decimal> _rsiSellZone;
	private readonly StrategyParam<DataType> _rsiCandleType;

	private readonly StrategyParam<bool> _useMacd;
	private readonly StrategyParam<bool> _useLastMacdSignal;
	private readonly StrategyParam<int> _macdMode;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<DataType> _macdCandleType;

	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<bool> _useLastStochasticSignal;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticKLength;
	private readonly StrategyParam<int> _stochasticDLength;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<bool> _stochasticUseThresholds;
	private readonly StrategyParam<int> _stochasticUpper;
	private readonly StrategyParam<int> _stochasticLower;
	private readonly StrategyParam<DataType> _stochasticCandleType;

	private readonly StrategyParam<bool> _useSar;
	private readonly StrategyParam<bool> _useLastSarSignal;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _sarCandleType;

	private readonly StrategyParam<bool> _useTrendDetection;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<DataType> _trendCandleType;

	private readonly StrategyParam<bool> _useBollingerFilter;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviationMedium;
	private readonly StrategyParam<decimal> _bollingerDeviationWide;
	private readonly StrategyParam<int> _rangeParameter;
	private readonly StrategyParam<DataType> _bollingerCandleType;

	private readonly StrategyParam<bool> _useNoiseFilter;
	private readonly StrategyParam<int> _noiseAtrLength;
	private readonly StrategyParam<decimal> _noiseThreshold;
	private readonly StrategyParam<DataType> _noiseCandleType;

	private readonly StrategyParam<bool> _useAutoClose;
	private readonly StrategyParam<bool> _allowOppositeAfterClose;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<bool> _useTrailingStop;

	private LengthIndicator<decimal>? _fastMa;
	private LengthIndicator<decimal>? _midMa;
	private LengthIndicator<decimal>? _slowMa;
	private RelativeStrengthIndex? _rsi;
	private MovingAverageConvergenceDivergence? _macd;
	private StochasticOscillator? _stochastic;
	private ParabolicSar? _sar;
	private AverageDirectionalIndex? _adx;
	private BollingerBands? _bollingerMedium;
	private BollingerBands? _bollingerWide;
	private AverageTrueRange? _noiseAtr;

	private decimal? _fastMaPrev;
	private decimal? _fastMaCurrent;
	private decimal? _midMaPrev;
	private decimal? _midMaCurrent;
	private decimal? _slowMaPrev;
	private decimal? _slowMaCurrent;

	private decimal? _rsiPrev;
	private decimal? _rsiCurrent;

	private decimal? _macdMainPrev;
	private decimal? _macdMain;
	private decimal? _macdSignalPrev;
	private decimal? _macdSignal;

	private decimal? _stochasticMain;
	private decimal? _stochasticSignal;

	private decimal? _sarValue;
	private decimal? _sarClose;

	private decimal? _adxValue;
	private decimal? _adxPlus;
	private decimal? _adxMinus;

	private decimal? _bollingerMediumUpper;
	private decimal? _bollingerMediumLower;
	private decimal? _bollingerWideUpper;
	private decimal? _bollingerWideLower;
	private ICandleMessage _lastBollingerCandle;

	private decimal? _noiseAtrValue;

	private readonly Queue<decimal> _rsiHistory = new();

	private SignalDirection _lastMaSignal;
	private SignalDirection _lastRsiSignal;
	private SignalDirection _lastMacdSignal;
	private SignalDirection _lastStochasticSignal;
	private SignalDirection _lastSarSignal;

	private DateTimeOffset _lastTradeBarTime;

	/// <summary>
	/// Initializes a new instance of <see cref="MultiStrategyComboStrategy"/>.
	/// </summary>
	public MultiStrategyComboStrategy()
	{
		_primaryCandleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Primary Candle Type", "Main candle series for entries", "General");

		_comboFactor = Param(nameof(ComboFactor), 1)
		.SetDisplay("Combo Factor", "Combination logic between trend and filters", "General")
		.SetRange(1, 3);

		_useMa = Param(nameof(UseMa), true)
		.SetDisplay("Use MA", "Enable moving average signals", "Moving Average");
		_useLastMaSignal = Param(nameof(UseLastMaSignal), true)
		.SetDisplay("Remember Last MA Signal", "Keep previous MA signal until it flips", "Moving Average");
		_maMode = Param(nameof(MaMode), 5)
		.SetDisplay("MA Mode", "Select MA crossover mode (1-5)", "Moving Average")
		.SetRange(1, 5);
		_fastMaLength = Param(nameof(FastMaLength), 5)
		.SetDisplay("Fast MA Length", "Period of the fast moving average", "Moving Average")
		.SetGreaterThanZero();
		_midMaLength = Param(nameof(MidMaLength), 13)
		.SetDisplay("Mid MA Length", "Period of the mid moving average", "Moving Average")
		.SetGreaterThanZero();
		_slowMaLength = Param(nameof(SlowMaLength), 38)
		.SetDisplay("Slow MA Length", "Period of the slow moving average", "Moving Average")
		.SetGreaterThanZero();
		_fastMaMethod = Param(nameof(FastMaMethod), MaMethod.Exponential)
		.SetDisplay("Fast MA Method", "Type of the fast moving average", "Moving Average");
		_midMaMethod = Param(nameof(MidMaMethod), MaMethod.Exponential)
		.SetDisplay("Mid MA Method", "Type of the mid moving average", "Moving Average");
		_slowMaMethod = Param(nameof(SlowMaMethod), MaMethod.Exponential)
		.SetDisplay("Slow MA Method", "Type of the slow moving average", "Moving Average");
		_maCandleType = Param(nameof(MaCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("MA Candle Type", "Candle type for moving averages", "Moving Average");

		_useRsi = Param(nameof(UseRsi), true)
		.SetDisplay("Use RSI", "Enable RSI signals", "RSI");
		_useLastRsiSignal = Param(nameof(UseLastRsiSignal), true)
		.SetDisplay("Remember Last RSI Signal", "Keep previous RSI signal until new one", "RSI");
		_rsiMode = Param(nameof(RsiMode), 1)
		.SetDisplay("RSI Mode", "RSI logic (1-4)", "RSI")
		.SetRange(1, 4);
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "RSI calculation length", "RSI")
		.SetGreaterThanZero();
		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 12m)
		.SetDisplay("RSI Buy Level", "Over sold threshold", "RSI");
		_rsiSellLevel = Param(nameof(RsiSellLevel), 88m)
		.SetDisplay("RSI Sell Level", "Over bought threshold", "RSI");
		_rsiBuyZone = Param(nameof(RsiBuyZone), 55m)
		.SetDisplay("RSI Buy Zone", "Zone lower bound for mode 4", "RSI");
		_rsiSellZone = Param(nameof(RsiSellZone), 45m)
		.SetDisplay("RSI Sell Zone", "Zone upper bound for mode 4", "RSI");
		_rsiCandleType = Param(nameof(RsiCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("RSI Candle Type", "Candle type for RSI", "RSI");

		_useMacd = Param(nameof(UseMacd), true)
		.SetDisplay("Use MACD", "Enable MACD signals", "MACD");
		_useLastMacdSignal = Param(nameof(UseLastMacdSignal), true)
		.SetDisplay("Remember Last MACD Signal", "Keep previous MACD signal", "MACD");
		_macdMode = Param(nameof(MacdMode), 2)
		.SetDisplay("MACD Mode", "MACD logic (1-4)", "MACD")
		.SetRange(1, 4);
		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetDisplay("MACD Fast Length", "MACD fast EMA length", "MACD")
		.SetGreaterThanZero();
		_macdSlowLength = Param(nameof(MacdSlowLength), 24)
		.SetDisplay("MACD Slow Length", "MACD slow EMA length", "MACD")
		.SetGreaterThanZero();
		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetDisplay("MACD Signal Length", "MACD signal EMA length", "MACD")
		.SetGreaterThanZero();
		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("MACD Candle Type", "Candle type for MACD", "MACD");

		_useStochastic = Param(nameof(UseStochastic), true)
		.SetDisplay("Use Stochastic", "Enable stochastic oscillator", "Stochastic");
		_useLastStochasticSignal = Param(nameof(UseLastStochasticSignal), true)
		.SetDisplay("Remember Last Stochastic Signal", "Keep previous stochastic direction", "Stochastic");
		_stochasticLength = Param(nameof(StochasticLength), 5)
		.SetDisplay("%K Length", "Stochastic %K length", "Stochastic")
		.SetGreaterThanZero();
		_stochasticKLength = Param(nameof(StochasticKLength), 5)
		.SetDisplay("%K Smoothing", "Fast smoothing for %K", "Stochastic")
		.SetGreaterThanZero();
		_stochasticDLength = Param(nameof(StochasticDLength), 3)
		.SetDisplay("%D Length", "Stochastic %D length", "Stochastic")
		.SetGreaterThanZero();
		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetDisplay("Slowing", "Stochastic slowing", "Stochastic")
		.SetGreaterThanZero();
		_stochasticUseThresholds = Param(nameof(StochasticUseThresholds), false)
		.SetDisplay("Use Thresholds", "Require %K to move beyond high/low levels", "Stochastic");
		_stochasticUpper = Param(nameof(StochasticUpper), 80)
		.SetDisplay("Stochastic Upper", "Upper threshold", "Stochastic");
		_stochasticLower = Param(nameof(StochasticLower), 20)
		.SetDisplay("Stochastic Lower", "Lower threshold", "Stochastic");
		_stochasticCandleType = Param(nameof(StochasticCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Stochastic Candle Type", "Candle type for stochastic", "Stochastic");

		_useSar = Param(nameof(UseSar), false)
		.SetDisplay("Use SAR", "Enable Parabolic SAR filter", "SAR");
		_useLastSarSignal = Param(nameof(UseLastSarSignal), true)
		.SetDisplay("Remember Last SAR Signal", "Keep previous SAR direction", "SAR");
		_sarStep = Param(nameof(SarStep), 0.02m)
		.SetDisplay("SAR Step", "Acceleration factor step", "SAR");
		_sarMax = Param(nameof(SarMax), 0.2m)
		.SetDisplay("SAR Max", "Maximum acceleration factor", "SAR");
		_sarCandleType = Param(nameof(SarCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("SAR Candle Type", "Candle type for SAR", "SAR");

		_useTrendDetection = Param(nameof(UseTrendDetection), true)
		.SetDisplay("Use ADX Trend Filter", "Enable ADX based trend detection", "Trend");
		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetDisplay("ADX Period", "ADX calculation length", "Trend")
		.SetGreaterThanZero();
		_adxLevel = Param(nameof(AdxLevel), 20m)
		.SetDisplay("ADX Threshold", "Trend strength threshold", "Trend")
		.SetNotNegative();
		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromMinutes(60).TimeFrame())
		.SetDisplay("Trend Candle Type", "Candle type for ADX trend", "Trend");

		_useBollingerFilter = Param(nameof(UseBollingerFilter), true)
		.SetDisplay("Use Bollinger Filter", "Enable Bollinger based ranging entries", "Bollinger");
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetDisplay("Bollinger Period", "Moving average period", "Bollinger")
		.SetGreaterThanZero();
		_bollingerDeviationMedium = Param(nameof(BollingerDeviationMedium), 2m)
		.SetDisplay("Medium Deviation", "Deviation for confirmation band", "Bollinger")
		.SetGreaterThanZero();
		_bollingerDeviationWide = Param(nameof(BollingerDeviationWide), 3m)
		.SetDisplay("Wide Deviation", "Deviation for extreme band", "Bollinger")
		.SetGreaterThanZero();
		_rangeParameter = Param(nameof(RangeParameter), 6)
		.SetDisplay("RSI Range Lookback", "RSI samples to confirm Bollinger bounce", "Bollinger")
		.SetNotNegative();
		_bollingerCandleType = Param(nameof(BollingerCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Bollinger Candle Type", "Candle type for Bollinger bands", "Bollinger");

		_useNoiseFilter = Param(nameof(UseNoiseFilter), true)
		.SetDisplay("Use Noise Filter", "Skip entries when volatility is too low", "Noise");
		_noiseAtrLength = Param(nameof(NoiseAtrLength), 14)
		.SetDisplay("Noise ATR Length", "ATR length for noise filter", "Noise")
		.SetGreaterThanZero();
		_noiseThreshold = Param(nameof(NoiseThreshold), 0.0005m)
		.SetDisplay("Noise Threshold", "ATR threshold below which trading stops", "Noise")
		.SetNotNegative();
		_noiseCandleType = Param(nameof(NoiseCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Noise Candle Type", "Candle type for ATR noise filter", "Noise");

		_useAutoClose = Param(nameof(UseAutoClose), true)
		.SetDisplay("Auto Close", "Close positions on opposite consensus", "Risk");
		_allowOppositeAfterClose = Param(nameof(AllowOppositeAfterClose), false)
		.SetDisplay("Allow Immediate Opposite", "Allow opposite entry on the same bar", "Risk");
		_stopLossOffset = Param(nameof(StopLossOffset), 0m)
		.SetDisplay("Stop Loss Offset", "Absolute price offset for stop loss", "Risk")
		.SetNotNegative();
		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0m)
		.SetDisplay("Take Profit Offset", "Absolute price offset for take profit", "Risk")
		.SetNotNegative();
		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing Stop", "Enable trailing stop on protection", "Risk");
	}

	/// <summary>
	/// Primary candle type used for entries.
	/// </summary>
	public DataType CandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Combination factor matching the original EA logic.
	/// </summary>
	public int ComboFactor
	{
		get => _comboFactor.Value;
		set => _comboFactor.Value = value;
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

		_fastMa = null;
		_midMa = null;
		_slowMa = null;
		_rsi = null;
		_macd = null;
		_stochastic = null;
		_sar = null;
		_adx = null;
		_bollingerMedium = null;
		_bollingerWide = null;
		_noiseAtr = null;

		_fastMaPrev = null;
		_fastMaCurrent = null;
		_midMaPrev = null;
		_midMaCurrent = null;
		_slowMaPrev = null;
		_slowMaCurrent = null;

		_rsiPrev = null;
		_rsiCurrent = null;

		_macdMainPrev = null;
		_macdMain = null;
		_macdSignalPrev = null;
		_macdSignal = null;

		_stochasticMain = null;
		_stochasticSignal = null;

		_sarValue = null;
		_sarClose = null;

		_adxValue = null;
		_adxPlus = null;
		_adxMinus = null;

		_bollingerMediumUpper = null;
		_bollingerMediumLower = null;
		_bollingerWideUpper = null;
		_bollingerWideLower = null;
		_lastBollingerCandle = null;

		_noiseAtrValue = null;

		_rsiHistory.Clear();

		_lastMaSignal = SignalDirection.None;
		_lastRsiSignal = SignalDirection.None;
		_lastMacdSignal = SignalDirection.None;
		_lastStochasticSignal = SignalDirection.None;
		_lastSarSignal = SignalDirection.None;

		_lastTradeBarTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribePrimary();

		if (UseMa)
		{
			_fastMa = CreateMovingAverage(FastMaMethod, FastMaLength);
			_midMa = CreateMovingAverage(MidMaMethod, MidMaLength);
			_slowMa = CreateMovingAverage(SlowMaMethod, SlowMaLength);

			var maSubscription = SubscribeCandles(MaCandleType);
			maSubscription
			.Bind(_fastMa, _midMa, _slowMa, ProcessMaCandle)
			.Start();

			TryDrawIndicators(maSubscription, _fastMa, _midMa, _slowMa);
		}

		if (UseRsi)
		{
			_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
			var rsiSubscription = SubscribeCandles(RsiCandleType);
			rsiSubscription
			.Bind(_rsi, ProcessRsiCandle)
			.Start();

			TryDrawIndicators(rsiSubscription, _rsi);
		}

		if (UseMacd)
		{
			_macd = new MovingAverageConvergenceDivergence
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
				SignalMa = { Length = MacdSignalLength }
			};

			var macdSubscription = SubscribeCandles(MacdCandleType);
			macdSubscription
			.BindEx(_macd, ProcessMacdCandle)
			.Start();

			TryDrawIndicators(macdSubscription, _macd);
		}

		if (UseStochastic)
		{
			_stochastic = new StochasticOscillator
			{
				Length = StochasticLength,
				K = { Length = StochasticKLength },
				D = { Length = StochasticDLength },
				Smooth = { Length = StochasticSlowing }
			};

			var stochSubscription = SubscribeCandles(StochasticCandleType);
			stochSubscription
			.BindEx(_stochastic, ProcessStochasticCandle)
			.Start();

			TryDrawIndicators(stochSubscription, _stochastic);
		}

		if (UseSar)
		{
			_sar = new ParabolicSar
			{
				Acceleration = SarStep,
				AccelerationMax = SarMax
			};

			var sarSubscription = SubscribeCandles(SarCandleType);
			sarSubscription
			.Bind(_sar, ProcessSarCandle)
			.Start();

			TryDrawIndicators(sarSubscription, _sar);
		}

		if (UseTrendDetection)
		{
			_adx = new AverageDirectionalIndex { Length = AdxPeriod };
			var trendSubscription = SubscribeCandles(TrendCandleType);
			trendSubscription
			.BindEx(_adx, ProcessAdxCandle)
			.Start();

			TryDrawIndicators(trendSubscription, _adx);
		}

		if (UseBollingerFilter)
		{
			_bollingerMedium = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviationMedium
			};
			_bollingerWide = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviationWide
			};

			var bollSubscription = SubscribeCandles(BollingerCandleType);
			bollSubscription
			.Bind(_bollingerMedium, _bollingerWide, ProcessBollingerCandle)
			.Start();

			TryDrawIndicators(bollSubscription, _bollingerMedium, _bollingerWide);
		}

		if (UseNoiseFilter)
		{
			_noiseAtr = new AverageTrueRange { Length = NoiseAtrLength };
			var noiseSubscription = SubscribeCandles(NoiseCandleType);
			noiseSubscription
			.Bind(_noiseAtr, ProcessNoiseCandle)
			.Start();
		}

		StartProtection(
		takeProfit: TakeProfitOffset > 0 ? new Unit(TakeProfitOffset, UnitTypes.Absolute) : null,
		stopLoss: StopLossOffset > 0 ? new Unit(StopLossOffset, UnitTypes.Absolute) : null,
		isStopTrailing: UseTrailingStop
		);
	}

	private void SubscribePrimary()
	{
		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription
		.Bind(ProcessPrimaryCandle)
		.Start();

		TryDrawIndicators(primarySubscription);
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		// Skip unfinished bars because the EA works on closed candles.
		if (candle.State != CandleStates.Finished)
		return;

		// Respect trading state and ensure the feed is online.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Wait until every enabled indicator is formed before trading.
		if (!AreIndicatorsReady())
		return;

		// Block entries when the volatility filter reports noisy conditions.
		if (UseNoiseFilter && _noiseAtrValue is decimal atr && atr < NoiseThreshold)
		{
			LogInfo($"Noise filter active (ATR {atr:0.#####} < threshold {NoiseThreshold:0.#####}).");
			return;
		}

		// Combine the individual indicator signals and filters.
		var consensus = EvaluateConsensus();
		var trend = EvaluateTrend();
		var rangeSignal = EvaluateBollingerSignal();

		var entrySignal = CombineSignals(consensus, trend, rangeSignal);

		// Optionally close the position if the consensus flips to the opposite side.
		if (UseAutoClose)
		{
			HandleAutoExit(consensus, candle);
		}

		if (entrySignal == SignalDirection.Buy)
		{
			TryEnterLong(candle);
		}
		else if (entrySignal == SignalDirection.Sell)
		{
			TryEnterShort(candle);
		}
	}

	private void ProcessMaCandle(ICandleMessage candle, decimal fast, decimal mid, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_fastMaPrev = _fastMaCurrent;
		_fastMaCurrent = fast;
		_midMaPrev = _midMaCurrent;
		_midMaCurrent = mid;
		_slowMaPrev = _slowMaCurrent;
		_slowMaCurrent = slow;
	}

	private void ProcessRsiCandle(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiPrev = _rsiCurrent;
		_rsiCurrent = value;

		UpdateRsiHistory(value);
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (value is not MovingAverageConvergenceDivergenceSignalValue macdValue)
		return;

		if (macdValue.Macd is not decimal macdLine || macdValue.Signal is not decimal macdSignal)
		return;

		_macdMainPrev = _macdMain;
		_macdMain = macdLine;
		_macdSignalPrev = _macdSignal;
		_macdSignal = macdSignal;
	}

	private void ProcessStochasticCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (value is not StochasticOscillatorValue stochValue)
		return;

		if (stochValue.K is not decimal main || stochValue.D is not decimal signal)
		return;

		_stochasticMain = main;
		_stochasticSignal = signal;
	}

	private void ProcessSarCandle(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_sarValue = sar;
		_sarClose = candle.ClosePrice;
	}

	private void ProcessAdxCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (value is not AverageDirectionalIndexValue adxValue)
		return;

		if (adxValue.MovingAverage is not decimal adx || adxValue.PlusDi is not decimal plus || adxValue.MinusDi is not decimal minus)
		return;

		_adxValue = adx;
		_adxPlus = plus;
		_adxMinus = minus;
	}

	private void ProcessBollingerCandle(ICandleMessage candle, decimal mediumMiddle, decimal mediumUpper, decimal mediumLower, decimal wideMiddle, decimal wideUpper, decimal wideLower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_bollingerMediumUpper = mediumUpper;
		_bollingerMediumLower = mediumLower;
		_bollingerWideUpper = wideUpper;
		_bollingerWideLower = wideLower;
		_lastBollingerCandle = candle;
	}

	private void ProcessNoiseCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_noiseAtrValue = atr;
	}

	private bool AreIndicatorsReady()
	{
		// Ensure every enabled indicator is ready before generating signals.
		if (UseMa && (!(_fastMa?.IsFormed ?? false) || !(_midMa?.IsFormed ?? false) || !(_slowMa?.IsFormed ?? false)))
		return false;

		if (UseRsi && !(_rsi?.IsFormed ?? false))
		return false;

		if (UseMacd && !(_macd?.IsFormed ?? false))
		return false;

		if (UseStochastic && !(_stochastic?.IsFormed ?? false))
		return false;

		if (UseSar && !(_sar?.IsFormed ?? false))
		return false;

		if (UseTrendDetection && !(_adx?.IsFormed ?? false))
		return false;

		if (UseBollingerFilter && (!(_bollingerMedium?.IsFormed ?? false) || !(_bollingerWide?.IsFormed ?? false)))
		return false;

		if (UseNoiseFilter && !(_noiseAtr?.IsFormed ?? false))
		return false;

		return true;
	}

	private SignalDirection EvaluateConsensus()
	{
		var selected = 0;
		var up = 0;
		var down = 0;

		if (UseRsi)
		{
			selected++;
			var signal = GetRsiSignal();
			if (signal == SignalDirection.Buy)
			up++;
			else if (signal == SignalDirection.Sell)
			down++;
		}

		if (UseStochastic)
		{
			selected++;
			var signal = GetStochasticSignal();
			if (signal == SignalDirection.Buy)
			up++;
			else if (signal == SignalDirection.Sell)
			down++;
		}

		if (UseSar)
		{
			selected++;
			var signal = GetSarSignal();
			if (signal == SignalDirection.Buy)
			up++;
			else if (signal == SignalDirection.Sell)
			down++;
		}

		if (UseMa)
		{
			selected++;
			var signal = GetMaSignal();
			if (signal == SignalDirection.Buy)
			up++;
			else if (signal == SignalDirection.Sell)
			down++;
		}

		if (UseMacd)
		{
			selected++;
			var signal = GetMacdSignal();
			if (signal == SignalDirection.Buy)
			up++;
			else if (signal == SignalDirection.Sell)
			down++;
		}

		if (selected == 0)
		return SignalDirection.None;

		if (up == selected)
		return SignalDirection.Buy;

		if (down == selected)
		return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private TrendState EvaluateTrend()
	{
		if (!UseTrendDetection)
		return TrendState.None;

		if (_adxValue is not decimal adx || _adxPlus is not decimal plus || _adxMinus is not decimal minus)
		return TrendState.None;

		if (adx < AdxLevel)
		{
			return plus >= minus ? TrendState.RangeUp : TrendState.RangeDown;
		}

		return plus >= minus ? TrendState.Up : TrendState.Down;
	}

	private SignalDirection EvaluateBollingerSignal()
	{
		if (!UseBollingerFilter)
		return SignalDirection.None;

		if (_bollingerMediumLower is null || _bollingerMediumUpper is null || _bollingerWideLower is null || _bollingerWideUpper is null)
		return SignalDirection.None;

		if (_lastBollingerCandle is null)
		return SignalDirection.None;

		var candle = _lastBollingerCandle;

		var buyTouch = candle.LowPrice <= _bollingerWideLower.Value && candle.ClosePrice > _bollingerMediumLower.Value;
		var sellTouch = candle.HighPrice >= _bollingerWideUpper.Value && candle.ClosePrice < _bollingerMediumUpper.Value;

		var hasOversoldRsi = RangeParameter <= 0 || HasRsiBelow(RsiBuyLevel);
		var hasOverboughtRsi = RangeParameter <= 0 || HasRsiAbove(RsiSellLevel);

		if (buyTouch && hasOversoldRsi)
		return SignalDirection.Buy;

		if (sellTouch && hasOverboughtRsi)
		return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection CombineSignals(SignalDirection consensus, TrendState trend, SignalDirection rangeSignal)
	{
		return ComboFactor switch
		{
			1 => CombineFactorOne(consensus, trend, rangeSignal),
			2 => CombineFactorTwo(consensus, trend, rangeSignal),
			_ => CombineFactorThree(consensus, trend, rangeSignal)
		};
	}

	private SignalDirection CombineFactorOne(SignalDirection consensus, TrendState trend, SignalDirection rangeSignal)
	{
		if (UseTrendDetection || UseBollingerFilter)
		{
			if (trend is TrendState.RangeDown or TrendState.RangeUp && UseBollingerFilter)
			{
				if (trend == TrendState.RangeUp && rangeSignal == SignalDirection.Buy)
				return SignalDirection.Buy;
				if (trend == TrendState.RangeDown && rangeSignal == SignalDirection.Sell)
				return SignalDirection.Sell;
				return SignalDirection.None;
			}

			if (trend is TrendState.Up or TrendState.Down)
			{
				if (trend == TrendState.Up && consensus == SignalDirection.Buy)
				return SignalDirection.Buy;
				if (trend == TrendState.Down && consensus == SignalDirection.Sell)
				return SignalDirection.Sell;
				return SignalDirection.None;
			}
		}

		if (UseBollingerFilter)
		{
			if (rangeSignal != SignalDirection.None)
			return rangeSignal;
		}

		return consensus;
	}

	private SignalDirection CombineFactorTwo(SignalDirection consensus, TrendState trend, SignalDirection rangeSignal)
	{
		if (UseTrendDetection && !UseBollingerFilter)
		{
			if (trend is TrendState.Up or TrendState.RangeUp)
			return consensus == SignalDirection.Buy ? SignalDirection.Buy : SignalDirection.None;
			if (trend is TrendState.Down or TrendState.RangeDown)
			return consensus == SignalDirection.Sell ? SignalDirection.Sell : SignalDirection.None;
		}

		if (UseTrendDetection && UseBollingerFilter)
		{
			if (trend == TrendState.Up)
			{
				if (consensus == SignalDirection.Buy && rangeSignal != SignalDirection.Sell)
				return SignalDirection.Buy;
				return SignalDirection.None;
			}
			if (trend == TrendState.Down)
			{
				if (consensus == SignalDirection.Sell && rangeSignal != SignalDirection.Buy)
				return SignalDirection.Sell;
				return SignalDirection.None;
			}
			if (trend == TrendState.RangeUp)
			{
				if (consensus == SignalDirection.Buy && rangeSignal == SignalDirection.Buy)
				return SignalDirection.Buy;
				return SignalDirection.None;
			}
			if (trend == TrendState.RangeDown)
			{
				if (consensus == SignalDirection.Sell && rangeSignal == SignalDirection.Sell)
				return SignalDirection.Sell;
				return SignalDirection.None;
			}
		}

		if (!UseTrendDetection && UseBollingerFilter)
		{
			if (rangeSignal == SignalDirection.Buy && consensus == SignalDirection.Buy)
			return SignalDirection.Buy;
			if (rangeSignal == SignalDirection.Sell && consensus == SignalDirection.Sell)
			return SignalDirection.Sell;
			return SignalDirection.None;
		}

		return consensus;
	}

	private SignalDirection CombineFactorThree(SignalDirection consensus, TrendState trend, SignalDirection rangeSignal)
	{
		if (UseTrendDetection && UseBollingerFilter)
		{
			if (trend == TrendState.Up && consensus == SignalDirection.Buy && (rangeSignal == SignalDirection.Buy || rangeSignal == SignalDirection.None))
			return SignalDirection.Buy;
			if (trend == TrendState.Down && consensus == SignalDirection.Sell && (rangeSignal == SignalDirection.Sell || rangeSignal == SignalDirection.None))
			return SignalDirection.Sell;
			if (trend == TrendState.RangeUp && consensus == SignalDirection.Buy && rangeSignal == SignalDirection.Buy)
			return SignalDirection.Buy;
			if (trend == TrendState.RangeDown && consensus == SignalDirection.Sell && rangeSignal == SignalDirection.Sell)
			return SignalDirection.Sell;
			return SignalDirection.None;
		}

		if (UseTrendDetection)
		{
			if (trend == TrendState.Up && consensus == SignalDirection.Buy)
			return SignalDirection.Buy;
			if (trend == TrendState.Down && consensus == SignalDirection.Sell)
			return SignalDirection.Sell;
			return SignalDirection.None;
		}

		if (UseBollingerFilter)
		{
			return rangeSignal;
		}

		return consensus;
	}

	private void HandleAutoExit(SignalDirection consensus, ICandleMessage candle)
	{
		// Mirror the original auto-close logic by reacting immediately to opposite consensus.
		if (Position > 0 && consensus == SignalDirection.Sell)
		{
			SellMarket(Math.Abs(Position));
			_lastTradeBarTime = candle.OpenTime;
			LogInfo("Closed long position on opposite consensus.");
		}
		else if (Position < 0 && consensus == SignalDirection.Buy)
		{
			BuyMarket(Math.Abs(Position));
			_lastTradeBarTime = candle.OpenTime;
			LogInfo("Closed short position on opposite consensus.");
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		// Avoid duplicate entries within the same bar and respect the current position.
		if (Position > 0)
		return;

		if (!AllowOppositeAfterClose && _lastTradeBarTime == candle.OpenTime)
		return;

		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		_lastTradeBarTime = candle.OpenTime;
		LogInfo("Entered long position.");
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		// Avoid duplicate entries within the same bar and respect the current position.
		if (Position < 0)
		return;

		if (!AllowOppositeAfterClose && _lastTradeBarTime == candle.OpenTime)
		return;

		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		_lastTradeBarTime = candle.OpenTime;
		LogInfo("Entered short position.");
	}

	private void UpdateRsiHistory(decimal value)
	{
		// Store recent RSI values for the Bollinger range confirmation.
		if (RangeParameter <= 0)
		return;

		_rsiHistory.Enqueue(value);
		while (_rsiHistory.Count > RangeParameter)
		_rsiHistory.Dequeue();
	}

	private bool HasRsiBelow(decimal threshold)
	{
		if (RangeParameter <= 0)
		return true;

		if (_rsiHistory.Count < RangeParameter)
		return false;

		foreach (var value in _rsiHistory)
		{
			if (value <= threshold)
			return true;
		}

		return false;
	}

	private bool HasRsiAbove(decimal threshold)
	{
		if (RangeParameter <= 0)
		return true;

		if (_rsiHistory.Count < RangeParameter)
		return false;

		foreach (var value in _rsiHistory)
		{
			if (value >= threshold)
			return true;
		}

		return false;
	}

	private SignalDirection GetMaSignal()
	{
		if (_fastMaCurrent is null || _fastMaPrev is null || _midMaCurrent is null || _midMaPrev is null || _slowMaCurrent is null || _slowMaPrev is null)
		return UseLastMaSignal ? _lastMaSignal : SignalDirection.None;

		SignalDirection signal = MaMode switch
		{
			1 => DetectCross(_fastMaPrev.Value, _fastMaCurrent.Value, _midMaPrev.Value, _midMaCurrent.Value),
			2 => DetectCross(_midMaPrev.Value, _midMaCurrent.Value, _slowMaPrev.Value, _slowMaCurrent.Value),
			3 => CombineSignals(
			DetectCross(_midMaPrev.Value, _midMaCurrent.Value, _slowMaPrev.Value, _slowMaCurrent.Value),
			DetectCross(_fastMaPrev.Value, _fastMaCurrent.Value, _midMaPrev.Value, _midMaCurrent.Value)
			),
			4 => DetectCross(_fastMaPrev.Value, _fastMaCurrent.Value, _slowMaPrev.Value, _slowMaCurrent.Value),
			5 => CombineSignals(
			CombineSignals(
			DetectCross(_midMaPrev.Value, _midMaCurrent.Value, _slowMaPrev.Value, _slowMaCurrent.Value),
			DetectCross(_fastMaPrev.Value, _fastMaCurrent.Value, _midMaPrev.Value, _midMaCurrent.Value)
			),
			DetectCross(_fastMaPrev.Value, _fastMaCurrent.Value, _slowMaPrev.Value, _slowMaCurrent.Value)
			),
			_ => SignalDirection.None
		};

		if (UseLastMaSignal)
		{
			if (signal != SignalDirection.None && signal != _lastMaSignal)
			_lastMaSignal = signal;
			return _lastMaSignal;
		}

		return signal;
	}

	private SignalDirection CombineSignals(SignalDirection first, SignalDirection second)
	{
		if (first == SignalDirection.Buy || second == SignalDirection.Buy)
		{
			if (first == SignalDirection.Buy && second != SignalDirection.Sell)
			return SignalDirection.Buy;
			if (second == SignalDirection.Buy && first != SignalDirection.Sell)
			return SignalDirection.Buy;
		}

		if (first == SignalDirection.Sell || second == SignalDirection.Sell)
		{
			if (first == SignalDirection.Sell && second != SignalDirection.Buy)
			return SignalDirection.Sell;
			if (second == SignalDirection.Sell && first != SignalDirection.Buy)
			return SignalDirection.Sell;
		}

		return SignalDirection.None;
	}

	private static SignalDirection DetectCross(decimal prevA, decimal currentA, decimal prevB, decimal currentB)
	{
		var prevDiff = prevA - prevB;
		var currentDiff = currentA - currentB;

		if (prevDiff <= 0m && currentDiff > 0m)
		return SignalDirection.Buy;

		if (prevDiff >= 0m && currentDiff < 0m)
		return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection GetRsiSignal()
	{
		if (_rsiCurrent is null || _rsiPrev is null)
		return UseLastRsiSignal ? _lastRsiSignal : SignalDirection.None;

		var current = _rsiCurrent.Value;
		var previous = _rsiPrev.Value;

		SignalDirection signal = RsiMode switch
		{
			1 => current < RsiBuyLevel ? SignalDirection.Buy : current > RsiSellLevel ? SignalDirection.Sell : SignalDirection.None,
			2 => current > previous ? SignalDirection.Buy : current < previous ? SignalDirection.Sell : SignalDirection.None,
			3 => CombineSignals(
			current < RsiBuyLevel ? SignalDirection.Buy : current > RsiSellLevel ? SignalDirection.Sell : SignalDirection.None,
			current > previous ? SignalDirection.Buy : current < previous ? SignalDirection.Sell : SignalDirection.None
			),
			4 =>
			current > previous && current >= RsiBuyZone && current <= RsiSellLevel
			? SignalDirection.Buy
			: current < previous && current <= RsiSellZone && current >= RsiBuyLevel
			? SignalDirection.Sell
			: SignalDirection.None,
			_ => SignalDirection.None
		};

		if (UseLastRsiSignal)
		{
			if (signal != SignalDirection.None && signal != _lastRsiSignal)
			_lastRsiSignal = signal;
			return _lastRsiSignal;
		}

		return signal;
	}

	private SignalDirection GetMacdSignal()
	{
		if (_macdMain is null || _macdMainPrev is null || _macdSignal is null || _macdSignalPrev is null)
		return UseLastMacdSignal ? _lastMacdSignal : SignalDirection.None;

		var macd = _macdMain.Value;
		var macdPrev = _macdMainPrev.Value;
		var signal = _macdSignal.Value;
		var signalPrev = _macdSignalPrev.Value;

		SignalDirection result = MacdMode switch
		{
			1 =>
			macd > macdPrev && signal > signalPrev && macd > signal
			? SignalDirection.Buy
			: macd < macdPrev && signal < signalPrev && macd < signal
			? SignalDirection.Sell
			: SignalDirection.None,
			2 =>
			macd > signal && macdPrev <= signalPrev && macd < 0m && macdPrev < 0m
			? SignalDirection.Buy
			: macd < signal && macdPrev >= signalPrev && macd > 0m && macdPrev > 0m
			? SignalDirection.Sell
			: SignalDirection.None,
			3 =>
			(macd > signal && macdPrev <= signalPrev && macd < 0m && macdPrev < 0m) ||
			(macd > macdPrev && signal > signalPrev && macd > signal)
			? SignalDirection.Buy
			: (macd < signal && macdPrev >= signalPrev && macd > 0m && macdPrev > 0m) ||
			(macd < macdPrev && signal < signalPrev && macd < signal)
			? SignalDirection.Sell
			: SignalDirection.None,
			4 =>
			signal > 0m && signalPrev < 0m
			? SignalDirection.Buy
			: signal < 0m && signalPrev > 0m
			? SignalDirection.Sell
			: SignalDirection.None,
			_ => SignalDirection.None
		};

		if (UseLastMacdSignal)
		{
			if (result != SignalDirection.None && result != _lastMacdSignal)
			_lastMacdSignal = result;
			return _lastMacdSignal;
		}

		return result;
	}

	private SignalDirection GetStochasticSignal()
	{
		if (_stochasticMain is null || _stochasticSignal is null)
		return UseLastStochasticSignal ? _lastStochasticSignal : SignalDirection.None;

		var main = _stochasticMain.Value;
		var signal = _stochasticSignal.Value;

		SignalDirection result;

		if (StochasticUseThresholds)
		{
			if (main > signal && main > StochasticUpper)
			result = SignalDirection.Buy;
			else if (main < signal && main < StochasticLower)
			result = SignalDirection.Sell;
			else
			result = SignalDirection.None;
		}
		else
		{
			if (main > signal)
			result = SignalDirection.Buy;
			else if (main < signal)
			result = SignalDirection.Sell;
			else
			result = SignalDirection.None;
		}

		if (UseLastStochasticSignal)
		{
			if (result != SignalDirection.None && result != _lastStochasticSignal)
			_lastStochasticSignal = result;
			return _lastStochasticSignal;
		}

		return result;
	}

	private SignalDirection GetSarSignal()
	{
		if (_sarValue is null || _sarClose is null)
		return UseLastSarSignal ? _lastSarSignal : SignalDirection.None;

		var result = _sarValue.Value < _sarClose.Value ? SignalDirection.Buy : SignalDirection.Sell;

		if (UseLastSarSignal)
		{
			if (result != SignalDirection.None && result != _lastSarSignal)
			_lastSarSignal = result;
			return _lastSarSignal;
		}

		return result;
	}

	private LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int length)
	{
		// Map the original MA method integers to StockSharp indicators.
		return method switch
		{
			MaMethod.Simple => new SMA { Length = length },
			MaMethod.Exponential => new EMA { Length = length },
			MaMethod.Smoothed => new SMMA { Length = length },
			MaMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SMA { Length = length }
		};
	}

	private void TryDrawIndicators(CandleSeries subscription, params IIndicator[] indicators)
	{
		var area = CreateChartArea();
		if (area == null)
		return;

		DrawCandles(area, subscription);

		foreach (var indicator in indicators)
		{
			DrawIndicator(area, indicator);
		}

		DrawOwnTrades(area);
	}

	private bool UseMa => _useMa.Value;
	private bool UseLastMaSignal => _useLastMaSignal.Value;
	private int MaMode => _maMode.Value;
	private int FastMaLength => _fastMaLength.Value;
	private int MidMaLength => _midMaLength.Value;
	private int SlowMaLength => _slowMaLength.Value;
	private MaMethod FastMaMethod => _fastMaMethod.Value;
	private MaMethod MidMaMethod => _midMaMethod.Value;
	private MaMethod SlowMaMethod => _slowMaMethod.Value;
	private DataType MaCandleType => _maCandleType.Value;

	private bool UseRsi => _useRsi.Value;
	private bool UseLastRsiSignal => _useLastRsiSignal.Value;
	private int RsiMode => _rsiMode.Value;
	private int RsiPeriod => _rsiPeriod.Value;
	private decimal RsiBuyLevel => _rsiBuyLevel.Value;
	private decimal RsiSellLevel => _rsiSellLevel.Value;
	private decimal RsiBuyZone => _rsiBuyZone.Value;
	private decimal RsiSellZone => _rsiSellZone.Value;
	private DataType RsiCandleType => _rsiCandleType.Value;

	private bool UseMacd => _useMacd.Value;
	private bool UseLastMacdSignal => _useLastMacdSignal.Value;
	private int MacdMode => _macdMode.Value;
	private int MacdFastLength => _macdFastLength.Value;
	private int MacdSlowLength => _macdSlowLength.Value;
	private int MacdSignalLength => _macdSignalLength.Value;
	private DataType MacdCandleType => _macdCandleType.Value;

	private bool UseStochastic => _useStochastic.Value;
	private bool UseLastStochasticSignal => _useLastStochasticSignal.Value;
	private int StochasticLength => _stochasticLength.Value;
	private int StochasticKLength => _stochasticKLength.Value;
	private int StochasticDLength => _stochasticDLength.Value;
	private int StochasticSlowing => _stochasticSlowing.Value;
	private bool StochasticUseThresholds => _stochasticUseThresholds.Value;
	private int StochasticUpper => _stochasticUpper.Value;
	private int StochasticLower => _stochasticLower.Value;
	private DataType StochasticCandleType => _stochasticCandleType.Value;

	private bool UseSar => _useSar.Value;
	private bool UseLastSarSignal => _useLastSarSignal.Value;
	private decimal SarStep => _sarStep.Value;
	private decimal SarMax => _sarMax.Value;
	private DataType SarCandleType => _sarCandleType.Value;

	private bool UseTrendDetection => _useTrendDetection.Value;
	private int AdxPeriod => _adxPeriod.Value;
	private decimal AdxLevel => _adxLevel.Value;
	private DataType TrendCandleType => _trendCandleType.Value;

	private bool UseBollingerFilter => _useBollingerFilter.Value;
	private int BollingerPeriod => _bollingerPeriod.Value;
	private decimal BollingerDeviationMedium => _bollingerDeviationMedium.Value;
	private decimal BollingerDeviationWide => _bollingerDeviationWide.Value;
	private int RangeParameter => _rangeParameter.Value;
	private DataType BollingerCandleType => _bollingerCandleType.Value;

	private bool UseNoiseFilter => _useNoiseFilter.Value;
	private int NoiseAtrLength => _noiseAtrLength.Value;
	private decimal NoiseThreshold => _noiseThreshold.Value;
	private DataType NoiseCandleType => _noiseCandleType.Value;

	private bool UseAutoClose => _useAutoClose.Value;
	private bool AllowOppositeAfterClose => _allowOppositeAfterClose.Value;
	private decimal StopLossOffset => _stopLossOffset.Value;
	private decimal TakeProfitOffset => _takeProfitOffset.Value;
	private bool UseTrailingStop => _useTrailingStop.Value;

	private enum SignalDirection
	{
		None,
		Buy,
		Sell
	}

	private enum TrendState
	{
		None,
		Up,
		Down,
		RangeUp,
		RangeDown
	}

	/// <summary>
	/// Moving average method.
	/// </summary>
	public enum MaMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}
}
