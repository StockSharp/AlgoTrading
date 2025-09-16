using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "MacdPatternTraderAll" MetaTrader 5 expert advisor.
/// The strategy combines six MACD based entry patterns, adaptive stop/target placement and staged exits.
/// </summary>
public class MacdPatternTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<bool> _enableSlowMartingale;

	private readonly StrategyParam<bool> _pattern1Enabled;
	private readonly StrategyParam<int> _pattern1StopLossBars;
	private readonly StrategyParam<int> _pattern1TakeProfitBars;
	private readonly StrategyParam<int> _pattern1OffsetPoints;
	private readonly StrategyParam<int> _pattern1FastPeriod;
	private readonly StrategyParam<int> _pattern1SlowPeriod;
	private readonly StrategyParam<decimal> _pattern1MaxThreshold;
	private readonly StrategyParam<decimal> _pattern1MinThreshold;

	private readonly StrategyParam<bool> _pattern2Enabled;
	private readonly StrategyParam<int> _pattern2StopLossBars;
	private readonly StrategyParam<int> _pattern2TakeProfitBars;
	private readonly StrategyParam<int> _pattern2OffsetPoints;
	private readonly StrategyParam<int> _pattern2FastPeriod;
	private readonly StrategyParam<int> _pattern2SlowPeriod;
	private readonly StrategyParam<decimal> _pattern2MaxThreshold;
	private readonly StrategyParam<decimal> _pattern2MinThreshold;

	private readonly StrategyParam<bool> _pattern3Enabled;
	private readonly StrategyParam<int> _pattern3StopLossBars;
	private readonly StrategyParam<int> _pattern3TakeProfitBars;
	private readonly StrategyParam<int> _pattern3OffsetPoints;
	private readonly StrategyParam<int> _pattern3FastPeriod;
	private readonly StrategyParam<int> _pattern3SlowPeriod;
	private readonly StrategyParam<decimal> _pattern3InnerMaxThreshold;
	private readonly StrategyParam<decimal> _pattern3OuterMaxThreshold;
	private readonly StrategyParam<decimal> _pattern3InnerMinThreshold;
	private readonly StrategyParam<decimal> _pattern3OuterMinThreshold;

	private readonly StrategyParam<bool> _pattern4Enabled;
	private readonly StrategyParam<int> _pattern4StopLossBars;
	private readonly StrategyParam<int> _pattern4TakeProfitBars;
	private readonly StrategyParam<int> _pattern4OffsetPoints;
	private readonly StrategyParam<int> _pattern4FastPeriod;
	private readonly StrategyParam<int> _pattern4SlowPeriod;
	private readonly StrategyParam<decimal> _pattern4MaxThreshold;
	private readonly StrategyParam<decimal> _pattern4MinThreshold;

	private readonly StrategyParam<bool> _pattern5Enabled;
	private readonly StrategyParam<int> _pattern5StopLossBars;
	private readonly StrategyParam<int> _pattern5TakeProfitBars;
	private readonly StrategyParam<int> _pattern5OffsetPoints;
	private readonly StrategyParam<int> _pattern5FastPeriod;
	private readonly StrategyParam<int> _pattern5SlowPeriod;
	private readonly StrategyParam<decimal> _pattern5UpperReset;
	private readonly StrategyParam<decimal> _pattern5UpperTrigger;
	private readonly StrategyParam<decimal> _pattern5UpperExitReset;
	private readonly StrategyParam<decimal> _pattern5LowerReset;
	private readonly StrategyParam<decimal> _pattern5LowerTrigger;
	private readonly StrategyParam<decimal> _pattern5LowerExitReset;

	private readonly StrategyParam<bool> _pattern6Enabled;
	private readonly StrategyParam<int> _pattern6StopLossBars;
	private readonly StrategyParam<int> _pattern6TakeProfitBars;
	private readonly StrategyParam<int> _pattern6OffsetPoints;
	private readonly StrategyParam<int> _pattern6FastPeriod;
	private readonly StrategyParam<int> _pattern6SlowPeriod;
	private readonly StrategyParam<decimal> _pattern6MaxThreshold;
	private readonly StrategyParam<decimal> _pattern6MinThreshold;
	private readonly StrategyParam<int> _pattern6CountBars;
	private readonly StrategyParam<int> _pattern6MaxBars;
	private readonly StrategyParam<int> _pattern6MinBars;

	private readonly StrategyParam<int> _ema1Period;
	private readonly StrategyParam<int> _ema2Period;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _ema3Period;

	private MovingAverageConvergenceDivergence _macd1 = null!;
	private MovingAverageConvergenceDivergence _macd2 = null!;
	private MovingAverageConvergenceDivergence _macd3 = null!;
	private MovingAverageConvergenceDivergence _macd4 = null!;
	private MovingAverageConvergenceDivergence _macd5 = null!;
	private MovingAverageConvergenceDivergence _macd6 = null!;

	private ExponentialMovingAverage _ema1 = null!;
	private ExponentialMovingAverage _ema2 = null!;
	private SimpleMovingAverage _sma = null!;
	private ExponentialMovingAverage _ema3 = null!;

	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();
	private readonly List<decimal> _closeHistory = new();
	private int _historyLimit;

	private decimal? _macd1Prev1;
	private decimal? _macd1Prev2;
	private decimal? _macd1Prev3;
	private decimal? _macd2Prev1;
	private decimal? _macd2Prev2;
	private decimal? _macd2Prev3;
	private decimal? _macd3Prev1;
	private decimal? _macd3Prev2;
	private decimal? _macd3Prev3;
	private decimal? _macd4Prev1;
	private decimal? _macd4Prev2;
	private decimal? _macd4Prev3;
	private decimal? _macd5Prev1;
	private decimal? _macd5Prev2;
	private decimal? _macd5Prev3;
	private decimal? _macd6Prev1;
	private decimal? _macd6Prev2;
	private decimal? _macd6Prev3;

	private decimal? _ema1Prev;
	private decimal? _ema2Prev;
	private decimal? _smaPrev;
	private decimal? _ema3Prev;

	private bool _pattern1MaxArmed;
	private bool _pattern1MinArmed;
	private bool _pattern1SellReady;
	private bool _pattern1BuyReady;

	private bool _pattern2AboveZero;
	private bool _pattern2BelowZero;
	private bool _pattern2SellReady;
	private bool _pattern2BuyReady;
	private decimal _pattern2SellMagnitude;
	private decimal _pattern2BuyMagnitude;

	private bool _pattern3MomentumUp;
	private bool _pattern3FirstPeakArmed;
	private bool _pattern3SecondPeakArmed;
	private bool _pattern3SellReady;
	private decimal _pattern3Peak1;
	private decimal _pattern3Peak2;
	private decimal _pattern3Peak3;

	private bool _pattern3MomentumDown;
	private bool _pattern3FirstTroughArmed;
	private bool _pattern3SecondTroughArmed;
	private bool _pattern3BuyReady;
	private decimal _pattern3Trough1;
	private decimal _pattern3Trough2;
	private decimal _pattern3Trough3;

	private bool _pattern4SellSetup;
	private bool _pattern4SellReady;
	private decimal _pattern4SellPeak;
	private bool _pattern4BuySetup;
	private bool _pattern4BuyReady;
	private decimal _pattern4BuyTrough;

	private bool _pattern5SellSetup;
	private bool _pattern5SellTrigger;
	private bool _pattern5SellReady;
	private bool _pattern5BuySetup;
	private bool _pattern5BuyTrigger;
	private bool _pattern5BuyReady;

	private int _pattern6SellCount;
	private int _pattern6BuyCount;
	private bool _pattern6SellLocked;
	private bool _pattern6BuyLocked;
	private bool _pattern6SellReady;
	private bool _pattern6BuyReady;

	private decimal _baseVolume;
	private decimal _currentVolume;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;
	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal _longEntryVolume;
	private decimal _shortEntryVolume;
	private int _longPartialCount;
	private int _shortPartialCount;

	private decimal _prevPosition;

	public MacdPatternTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe", "General");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
		.SetDisplay("Time Filter", "Limit trading hours", "General");

		_startTime = Param(nameof(TradingStart), new TimeSpan(7, 0, 0))
		.SetDisplay("Start", "Session start", "General");

		_stopTime = Param(nameof(TradingStop), new TimeSpan(17, 0, 0))
		.SetDisplay("Stop", "Session end", "General");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Base trade volume", "Money Management");

		_minVolume = Param(nameof(MinimumVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Min Volume", "Minimal tradable volume", "Money Management");

		_enableSlowMartingale = Param(nameof(EnableSlowMartingale), true)
		.SetDisplay("Slow Martingale", "Double volume after losing trade", "Money Management");

		_pattern1Enabled = Param(nameof(Pattern1Enabled), true)
		.SetDisplay("Enable", "Enable pattern 1", "Pattern 1");
		_pattern1StopLossBars = Param(nameof(Pattern1StopLossBars), 22)
		.SetGreaterThanZero()
		.SetDisplay("Stop Bars", "Stop loss lookback", "Pattern 1");
		_pattern1TakeProfitBars = Param(nameof(Pattern1TakeProfitBars), 32)
		.SetGreaterThanZero()
		.SetDisplay("Take Bars", "Take profit lookback", "Pattern 1");
		_pattern1OffsetPoints = Param(nameof(Pattern1OffsetPoints), 40)
		.SetGreaterThanZero()
		.SetDisplay("Offset", "Stop buffer in points", "Pattern 1");
		_pattern1FastPeriod = Param(nameof(Pattern1FastPeriod), 24)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "Pattern 1");
		_pattern1SlowPeriod = Param(nameof(Pattern1SlowPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "Pattern 1");
		_pattern1MaxThreshold = Param(nameof(Pattern1MaxThreshold), 0.0095m)
		.SetDisplay("Max", "Upper MACD threshold", "Pattern 1");
		_pattern1MinThreshold = Param(nameof(Pattern1MinThreshold), -0.0045m)
		.SetDisplay("Min", "Lower MACD threshold", "Pattern 1");

		_pattern2Enabled = Param(nameof(Pattern2Enabled), true)
		.SetDisplay("Enable", "Enable pattern 2", "Pattern 2");
		_pattern2StopLossBars = Param(nameof(Pattern2StopLossBars), 2)
		.SetGreaterThanZero()
		.SetDisplay("Stop Bars", "Stop loss lookback", "Pattern 2");
		_pattern2TakeProfitBars = Param(nameof(Pattern2TakeProfitBars), 2)
		.SetGreaterThanZero()
		.SetDisplay("Take Bars", "Take profit lookback", "Pattern 2");
		_pattern2OffsetPoints = Param(nameof(Pattern2OffsetPoints), 50)
		.SetGreaterThanZero()
		.SetDisplay("Offset", "Stop buffer in points", "Pattern 2");
		_pattern2FastPeriod = Param(nameof(Pattern2FastPeriod), 17)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "Pattern 2");
		_pattern2SlowPeriod = Param(nameof(Pattern2SlowPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "Pattern 2");
		_pattern2MaxThreshold = Param(nameof(Pattern2MaxThreshold), 0.0045m)
		.SetDisplay("Max", "Upper MACD threshold", "Pattern 2");
		_pattern2MinThreshold = Param(nameof(Pattern2MinThreshold), -0.0035m)
		.SetDisplay("Min", "Lower MACD threshold", "Pattern 2");

		_pattern3Enabled = Param(nameof(Pattern3Enabled), true)
		.SetDisplay("Enable", "Enable pattern 3", "Pattern 3");
		_pattern3StopLossBars = Param(nameof(Pattern3StopLossBars), 8)
		.SetGreaterThanZero()
		.SetDisplay("Stop Bars", "Stop loss lookback", "Pattern 3");
		_pattern3TakeProfitBars = Param(nameof(Pattern3TakeProfitBars), 12)
		.SetGreaterThanZero()
		.SetDisplay("Take Bars", "Take profit lookback", "Pattern 3");
		_pattern3OffsetPoints = Param(nameof(Pattern3OffsetPoints), 2)
		.SetGreaterThanZero()
		.SetDisplay("Offset", "Stop buffer in points", "Pattern 3");
		_pattern3FastPeriod = Param(nameof(Pattern3FastPeriod), 32)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "Pattern 3");
		_pattern3SlowPeriod = Param(nameof(Pattern3SlowPeriod), 2)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "Pattern 3");
		_pattern3InnerMaxThreshold = Param(nameof(Pattern3InnerMaxThreshold), 0.0015m)
		.SetDisplay("Inner Max", "Inner sell threshold", "Pattern 3");
		_pattern3OuterMaxThreshold = Param(nameof(Pattern3OuterMaxThreshold), 0.004m)
		.SetDisplay("Outer Max", "Outer sell threshold", "Pattern 3");
		_pattern3InnerMinThreshold = Param(nameof(Pattern3InnerMinThreshold), -0.005m)
		.SetDisplay("Inner Min", "Inner buy threshold", "Pattern 3");
		_pattern3OuterMinThreshold = Param(nameof(Pattern3OuterMinThreshold), -0.0005m)
		.SetDisplay("Outer Min", "Outer buy threshold", "Pattern 3");

		_pattern4Enabled = Param(nameof(Pattern4Enabled), true)
		.SetDisplay("Enable", "Enable pattern 4", "Pattern 4");
		_pattern4StopLossBars = Param(nameof(Pattern4StopLossBars), 10)
		.SetGreaterThanZero()
		.SetDisplay("Stop Bars", "Stop loss lookback", "Pattern 4");
		_pattern4TakeProfitBars = Param(nameof(Pattern4TakeProfitBars), 32)
		.SetGreaterThanZero()
		.SetDisplay("Take Bars", "Take profit lookback", "Pattern 4");
		_pattern4OffsetPoints = Param(nameof(Pattern4OffsetPoints), 45)
		.SetGreaterThanZero()
		.SetDisplay("Offset", "Stop buffer in points", "Pattern 4");
		_pattern4FastPeriod = Param(nameof(Pattern4FastPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "Pattern 4");
		_pattern4SlowPeriod = Param(nameof(Pattern4SlowPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "Pattern 4");
		_pattern4MaxThreshold = Param(nameof(Pattern4MaxThreshold), 0.0165m)
		.SetDisplay("Max", "Upper MACD threshold", "Pattern 4");
		_pattern4MinThreshold = Param(nameof(Pattern4MinThreshold), -0.0005m)
		.SetDisplay("Min", "Lower MACD threshold", "Pattern 4");

		_pattern5Enabled = Param(nameof(Pattern5Enabled), true)
		.SetDisplay("Enable", "Enable pattern 5", "Pattern 5");
		_pattern5StopLossBars = Param(nameof(Pattern5StopLossBars), 8)
		.SetGreaterThanZero()
		.SetDisplay("Stop Bars", "Stop loss lookback", "Pattern 5");
		_pattern5TakeProfitBars = Param(nameof(Pattern5TakeProfitBars), 47)
		.SetGreaterThanZero()
		.SetDisplay("Take Bars", "Take profit lookback", "Pattern 5");
		_pattern5OffsetPoints = Param(nameof(Pattern5OffsetPoints), 45)
		.SetGreaterThanZero()
		.SetDisplay("Offset", "Stop buffer in points", "Pattern 5");
		_pattern5FastPeriod = Param(nameof(Pattern5FastPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "Pattern 5");
		_pattern5SlowPeriod = Param(nameof(Pattern5SlowPeriod), 2)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "Pattern 5");
		_pattern5UpperReset = Param(nameof(Pattern5UpperReset), 0.0005m)
		.SetDisplay("Upper Reset", "Reset threshold above zero", "Pattern 5");
		_pattern5UpperTrigger = Param(nameof(Pattern5UpperTrigger), 0.0015m)
		.SetDisplay("Upper Trigger", "Trigger above zero", "Pattern 5");
		_pattern5UpperExitReset = Param(nameof(Pattern5UpperExitReset), 0m)
		.SetDisplay("Upper Exit Reset", "Reset level for long signal", "Pattern 5");
		_pattern5LowerReset = Param(nameof(Pattern5LowerReset), -0.0005m)
		.SetDisplay("Lower Reset", "Reset threshold below zero", "Pattern 5");
		_pattern5LowerTrigger = Param(nameof(Pattern5LowerTrigger), -0.003m)
		.SetDisplay("Lower Trigger", "Trigger below zero", "Pattern 5");
		_pattern5LowerExitReset = Param(nameof(Pattern5LowerExitReset), 0m)
		.SetDisplay("Lower Exit Reset", "Reset level for short signal", "Pattern 5");

		_pattern6Enabled = Param(nameof(Pattern6Enabled), true)
		.SetDisplay("Enable", "Enable pattern 6", "Pattern 6");
		_pattern6StopLossBars = Param(nameof(Pattern6StopLossBars), 26)
		.SetGreaterThanZero()
		.SetDisplay("Stop Bars", "Stop loss lookback", "Pattern 6");
		_pattern6TakeProfitBars = Param(nameof(Pattern6TakeProfitBars), 42)
		.SetGreaterThanZero()
		.SetDisplay("Take Bars", "Take profit lookback", "Pattern 6");
		_pattern6OffsetPoints = Param(nameof(Pattern6OffsetPoints), 20)
		.SetGreaterThanZero()
		.SetDisplay("Offset", "Stop buffer in points", "Pattern 6");
		_pattern6FastPeriod = Param(nameof(Pattern6FastPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "Pattern 6");
		_pattern6SlowPeriod = Param(nameof(Pattern6SlowPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "Pattern 6");
		_pattern6MaxThreshold = Param(nameof(Pattern6MaxThreshold), 0.0005m)
		.SetDisplay("Max", "Upper MACD threshold", "Pattern 6");
		_pattern6MinThreshold = Param(nameof(Pattern6MinThreshold), -0.001m)
		.SetDisplay("Min", "Lower MACD threshold", "Pattern 6");
		_pattern6CountBars = Param(nameof(Pattern6CountBars), 4)
		.SetGreaterThanZero()
		.SetDisplay("Count", "Bars beyond threshold", "Pattern 6");
		_pattern6MaxBars = Param(nameof(Pattern6MaxBars), 5)
		.SetGreaterThanZero()
		.SetDisplay("Max Count", "Maximum bar counter", "Pattern 6");
		_pattern6MinBars = Param(nameof(Pattern6MinBars), 5)
		.SetGreaterThanZero()
		.SetDisplay("Min Count", "Minimum bar counter", "Pattern 6");

		_ema1Period = Param(nameof(Ema1Period), 7)
		.SetGreaterThanZero()
		.SetDisplay("EMA 1", "EMA used for management", "Manager");
		_ema2Period = Param(nameof(Ema2Period), 21)
		.SetGreaterThanZero()
		.SetDisplay("EMA 2", "EMA used for management", "Manager");
		_smaPeriod = Param(nameof(SmaPeriod), 98)
		.SetGreaterThanZero()
		.SetDisplay("SMA", "SMA used for management", "Manager");
		_ema3Period = Param(nameof(Ema3Period), 365)
		.SetGreaterThanZero()
		.SetDisplay("EMA 3", "Slow EMA used for management", "Manager");
	}
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	public TimeSpan TradingStart
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	public TimeSpan TradingStop
	{
		get => _stopTime.Value;
		set => _stopTime.Value = value;
	}

	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	public decimal MinimumVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	public bool EnableSlowMartingale
	{
		get => _enableSlowMartingale.Value;
		set => _enableSlowMartingale.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_highHistory.Clear();
		_lowHistory.Clear();
		_closeHistory.Clear();
		_historyLimit = 0;

		_macd1Prev1 = null;
		_macd1Prev2 = null;
		_macd1Prev3 = null;
		_macd2Prev1 = null;
		_macd2Prev2 = null;
		_macd2Prev3 = null;
		_macd3Prev1 = null;
		_macd3Prev2 = null;
		_macd3Prev3 = null;
		_macd4Prev1 = null;
		_macd4Prev2 = null;
		_macd4Prev3 = null;
		_macd5Prev1 = null;
		_macd5Prev2 = null;
		_macd5Prev3 = null;
		_macd6Prev1 = null;
		_macd6Prev2 = null;
		_macd6Prev3 = null;

		_ema1Prev = null;
		_ema2Prev = null;
		_smaPrev = null;
		_ema3Prev = null;

		_pattern1MaxArmed = false;
		_pattern1MinArmed = false;
		_pattern1SellReady = false;
		_pattern1BuyReady = false;

		_pattern2AboveZero = false;
		_pattern2BelowZero = false;
		_pattern2SellReady = false;
		_pattern2BuyReady = false;
		_pattern2SellMagnitude = 0m;
		_pattern2BuyMagnitude = 0m;

		_pattern3MomentumUp = false;
		_pattern3FirstPeakArmed = false;
		_pattern3SecondPeakArmed = false;
		_pattern3SellReady = false;
		_pattern3Peak1 = 0m;
		_pattern3Peak2 = 0m;
		_pattern3Peak3 = 0m;

		_pattern3MomentumDown = false;
		_pattern3FirstTroughArmed = false;
		_pattern3SecondTroughArmed = false;
		_pattern3BuyReady = false;
		_pattern3Trough1 = 0m;
		_pattern3Trough2 = 0m;
		_pattern3Trough3 = 0m;

		_pattern4SellSetup = false;
		_pattern4SellReady = false;
		_pattern4SellPeak = 0m;
		_pattern4BuySetup = false;
		_pattern4BuyReady = false;
		_pattern4BuyTrough = 0m;

		_pattern5SellSetup = false;
		_pattern5SellTrigger = false;
		_pattern5SellReady = false;
		_pattern5BuySetup = false;
		_pattern5BuyTrigger = false;
		_pattern5BuyReady = false;

		_pattern6SellCount = 0;
		_pattern6BuyCount = 0;
		_pattern6SellLocked = false;
		_pattern6BuyLocked = false;
		_pattern6SellReady = false;
		_pattern6BuyReady = false;

		_baseVolume = 0m;
		_currentVolume = 0m;
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longEntryVolume = 0m;
		_shortEntryVolume = 0m;
		_longPartialCount = 0;
		_shortPartialCount = 0;
		_prevPosition = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd1 = CreateMacd(_pattern1FastPeriod.Value, _pattern1SlowPeriod.Value);
		_macd2 = CreateMacd(_pattern2FastPeriod.Value, _pattern2SlowPeriod.Value);
		_macd3 = CreateMacd(_pattern3FastPeriod.Value, _pattern3SlowPeriod.Value);
		_macd4 = CreateMacd(_pattern4FastPeriod.Value, _pattern4SlowPeriod.Value);
		_macd5 = CreateMacd(_pattern5FastPeriod.Value, _pattern5SlowPeriod.Value);
		_macd6 = CreateMacd(_pattern6FastPeriod.Value, _pattern6SlowPeriod.Value);

		_ema1 = new ExponentialMovingAverage { Length = _ema1Period.Value };
		_ema2 = new ExponentialMovingAverage { Length = _ema2Period.Value };
		_sma = new SimpleMovingAverage { Length = _smaPeriod.Value };
		_ema3 = new ExponentialMovingAverage { Length = _ema3Period.Value };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_macd1, _macd2, _macd3, _macd4, _macd5, _macd6, _ema1, _ema2, _sma, _ema3, ProcessCandle)
		.Start();

		_historyLimit = ComputeHistoryLimit();
		_baseVolume = _initialVolume.Value;
		_currentVolume = _baseVolume;

		StartProtection();
	}

	private MovingAverageConvergenceDivergence CreateMacd(int fast, int slow)
	{
		return new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = fast },
			LongMa = { Length = slow },
			SignalMa = { Length = 1 }
		};
	}

	private int ComputeHistoryLimit()
	{
		var maxStop = Math.Max(Math.Max(Math.Max(_pattern1StopLossBars.Value, _pattern2StopLossBars.Value), Math.Max(_pattern3StopLossBars.Value, _pattern4StopLossBars.Value)), Math.Max(_pattern5StopLossBars.Value, _pattern6StopLossBars.Value));
		var maxTake = Math.Max(Math.Max(Math.Max(_pattern1TakeProfitBars.Value, _pattern2TakeProfitBars.Value), Math.Max(_pattern3TakeProfitBars.Value, _pattern4TakeProfitBars.Value)), Math.Max(_pattern5TakeProfitBars.Value, _pattern6TakeProfitBars.Value));
		var length = Math.Max(maxStop, maxTake);
		if (length < 4)
		length = 4;
		return length * 6;
	}

	private decimal PriceStep => Security?.PriceStep ?? 0.0001m;

	private decimal VolumeStep => Security?.VolumeStep ?? 0.01m;

	private void ProcessCandle(
	ICandleMessage candle,
	decimal macd1, decimal macd1Signal, decimal macd1Hist,
	decimal macd2, decimal macd2Signal, decimal macd2Hist,
	decimal macd3, decimal macd3Signal, decimal macd3Hist,
	decimal macd4, decimal macd4Signal, decimal macd4Hist,
	decimal macd5, decimal macd5Signal, decimal macd5Hist,
	decimal macd6, decimal macd6Signal, decimal macd6Hist,
	decimal ema1Value,
	decimal ema2Value,
	decimal smaValue,
	decimal ema3Value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ManageActivePosition(candle);

		var blocked = _useTimeFilter.Value && !IsWithinTradingWindow(candle.OpenTime.TimeOfDay);

		if (!blocked)
		RunPatterns(candle);

		UpdateHistory(candle);

		UpdateIndicatorHistory(macd1, ref _macd1Prev1, ref _macd1Prev2, ref _macd1Prev3);
		UpdateIndicatorHistory(macd2, ref _macd2Prev1, ref _macd2Prev2, ref _macd2Prev3);
		UpdateIndicatorHistory(macd3, ref _macd3Prev1, ref _macd3Prev2, ref _macd3Prev3);
		UpdateIndicatorHistory(macd4, ref _macd4Prev1, ref _macd4Prev2, ref _macd4Prev3);
		UpdateIndicatorHistory(macd5, ref _macd5Prev1, ref _macd5Prev2, ref _macd5Prev3);
		UpdateIndicatorHistory(macd6, ref _macd6Prev1, ref _macd6Prev2, ref _macd6Prev3);

		_ema1Prev = ema1Value;
		_ema2Prev = ema2Value;
		_smaPrev = smaValue;
		_ema3Prev = ema3Value;

		_prevPosition = Position;
	}

	private bool IsWithinTradingWindow(TimeSpan time)
	{
		if (_startTime.Value <= _stopTime.Value)
		return time >= _startTime.Value && time <= _stopTime.Value;

		return time >= _startTime.Value || time <= _stopTime.Value;
	}

	private void UpdateIndicatorHistory(decimal value, ref decimal? prev1, ref decimal? prev2, ref decimal? prev3)
	{
		prev3 = prev2;
		prev2 = prev1;
		prev1 = value;
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_highHistory.Add(candle.HighPrice);
		_lowHistory.Add(candle.LowPrice);
		_closeHistory.Add(candle.ClosePrice);

		TrimHistory(_highHistory);
		TrimHistory(_lowHistory);
		TrimHistory(_closeHistory);
	}

	private void TrimHistory(List<decimal> list)
	{
		if (list.Count > _historyLimit && _historyLimit > 0)
		list.RemoveRange(0, list.Count - _historyLimit);
	}
	private void RunPatterns(ICandleMessage candle)
	{
		if (_macd1Prev3.HasValue)
		ProcessPattern1(candle);

		if (_macd2Prev3.HasValue)
		ProcessPattern2(candle);

		if (_macd3Prev3.HasValue)
		ProcessPattern3(candle);

		if (_macd4Prev3.HasValue)
		ProcessPattern4(candle);

		if (_macd5Prev3.HasValue)
		ProcessPattern5(candle);

		if (_macd6Prev3.HasValue)
		ProcessPattern6(candle);
	}

	private void ProcessPattern1(ICandleMessage candle)
	{
		if (!_pattern1Enabled.Value)
		return;

		var macdCurr = _macd1Prev1!.Value;
		var macdLast = _macd1Prev2!.Value;
		var macdPrev = _macd1Prev3!.Value;

		if (macdCurr > _pattern1MaxThreshold.Value)
		_pattern1MaxArmed = true;

		if (macdCurr < 0m)
		_pattern1MaxArmed = false;

		if (_pattern1MaxArmed && macdCurr < _pattern1MaxThreshold.Value && macdCurr < macdLast && macdLast > macdPrev && macdCurr > 0m && macdPrev < _pattern1MaxThreshold.Value)
		_pattern1SellReady = true;

		if (_pattern1SellReady)
		{
			if (TryOpenShort(candle, _pattern1StopLossBars.Value, _pattern1OffsetPoints.Value, _pattern1TakeProfitBars.Value, "Pattern1"))
			{
				_pattern1SellReady = false;
				_pattern1MaxArmed = false;
			}
		}

		if (macdCurr < _pattern1MinThreshold.Value)
		_pattern1MinArmed = true;

		if (macdCurr > 0m)
		_pattern1MinArmed = false;

		if (_pattern1MinArmed && macdCurr > _pattern1MinThreshold.Value && macdCurr > macdLast && macdLast < macdPrev && macdCurr < 0m && macdPrev > _pattern1MinThreshold.Value)
		_pattern1BuyReady = true;

		if (_pattern1BuyReady)
		{
			if (TryOpenLong(candle, _pattern1StopLossBars.Value, _pattern1OffsetPoints.Value, _pattern1TakeProfitBars.Value, "Pattern1"))
			{
				_pattern1BuyReady = false;
				_pattern1MinArmed = false;
			}
		}
	}

	private void ProcessPattern2(ICandleMessage candle)
	{
		if (!_pattern2Enabled.Value)
		return;

		var macdCurr = _macd2Prev1!.Value;
		var macdLast = _macd2Prev2!.Value;
		var macdPrev = _macd2Prev3!.Value;

		if (macdCurr > 0m)
		{
			_pattern2AboveZero = true;
			_pattern2SellReady = false;
		}

		if (_pattern2AboveZero && macdCurr > macdLast && macdLast < macdPrev && macdCurr > _pattern2MinThreshold.Value && macdCurr < 0m && !_pattern2SellReady)
		{
			_pattern2SellReady = true;
			_pattern2SellMagnitude = Math.Abs(macdLast * 10000m);
		}

		if (_pattern2SellReady)
		{
			var magnitude = Math.Abs(macdCurr * 10000m);

			if (macdCurr < macdLast && macdLast > macdPrev && macdCurr < 0m)
			{
				if (magnitude >= _pattern2SellMagnitude)
				_pattern2AboveZero = false;

				if (TryOpenShort(candle, _pattern2StopLossBars.Value, _pattern2OffsetPoints.Value, _pattern2TakeProfitBars.Value, "Pattern2"))
				{
					_pattern2SellReady = false;
					_pattern2AboveZero = false;
				}
			}
		}

		if (macdCurr < 0m)
		{
			_pattern2BelowZero = true;
			_pattern2BuyReady = false;
		}

		if (_pattern2BelowZero && macdCurr < _pattern2MaxThreshold.Value && macdCurr < macdLast && macdLast > macdPrev && macdCurr > 0m)
		{
			_pattern2BuyReady = true;
			_pattern2BuyMagnitude = Math.Abs(macdLast * 10000m);
		}

		if (_pattern2BuyReady)
		{
			var magnitude = Math.Abs(macdCurr * 10000m);

			if (macdCurr > macdLast && macdLast < macdPrev && macdCurr > 0m)
			{
				if (magnitude >= _pattern2BuyMagnitude)
				_pattern2BelowZero = false;

				if (TryOpenLong(candle, _pattern2StopLossBars.Value, _pattern2OffsetPoints.Value, _pattern2TakeProfitBars.Value, "Pattern2"))
				{
					_pattern2BuyReady = false;
					_pattern2BelowZero = false;
				}
			}
		}
	}

	private void ProcessPattern3(ICandleMessage candle)
	{
		if (!_pattern3Enabled.Value)
		return;

		var macdCurr = _macd3Prev1!.Value;
		var macdLast = _macd3Prev2!.Value;
		var macdPrev = _macd3Prev3!.Value;

		if (macdCurr > _pattern3OuterMaxThreshold.Value)
		_pattern3MomentumUp = true;

		if (_pattern3MomentumUp && macdCurr < macdLast && macdLast > macdPrev && macdLast > _pattern3Peak1 && !_pattern3FirstPeakArmed)
		{
			_pattern3Peak1 = macdLast;
			_pattern3FirstPeakArmed = true;
		}

		if (_pattern3FirstPeakArmed && macdCurr < _pattern3InnerMaxThreshold.Value)
		_pattern3SecondPeakArmed = true;

		if (macdCurr < _pattern3OuterMaxThreshold.Value)
		{
			if (!_pattern3SecondPeakArmed)
			{
				_pattern3FirstPeakArmed = false;
				_pattern3Peak1 = 0m;
				_pattern3MomentumUp = false;
			}
		}

		if (_pattern3SecondPeakArmed && macdCurr > _pattern3InnerMaxThreshold.Value && macdCurr < macdLast && macdLast > macdPrev && macdLast > _pattern3Peak1 && macdLast > _pattern3Peak2)
		_pattern3Peak2 = macdLast;

		if (_pattern3Peak2 > 0m && macdCurr < _pattern3InnerMaxThreshold.Value)
		_pattern3SellReady = macdCurr < macdLast && macdLast > macdPrev && macdLast < _pattern3Peak2;

		if (_pattern3SellReady)
		{
			if (TryOpenShort(candle, _pattern3StopLossBars.Value, _pattern3OffsetPoints.Value, _pattern3TakeProfitBars.Value, "Pattern3"))
			{
				_pattern3SellReady = false;
				_pattern3SecondPeakArmed = false;
				_pattern3FirstPeakArmed = false;
				_pattern3Peak1 = 0m;
				_pattern3Peak2 = 0m;
			}
		}

		if (macdCurr < _pattern3InnerMinThreshold.Value)
		_pattern3MomentumDown = true;

		if (_pattern3MomentumDown && macdCurr > macdLast && macdLast < macdPrev && macdLast < _pattern3Trough1 && !_pattern3FirstTroughArmed)
		{
			_pattern3Trough1 = macdLast;
			_pattern3FirstTroughArmed = true;
		}

		if (_pattern3FirstTroughArmed && macdCurr > _pattern3InnerMinThreshold.Value)
		{
			_pattern3SecondTroughArmed = true;
			_pattern3MomentumDown = false;
		}

		if (macdCurr > _pattern3OuterMinThreshold.Value)
		{
			if (!_pattern3SecondTroughArmed)
			{
				_pattern3FirstTroughArmed = false;
				_pattern3Trough1 = 0m;
			}
		}

		if (_pattern3SecondTroughArmed && macdCurr < _pattern3InnerMinThreshold.Value && macdCurr > macdLast && macdLast < macdPrev && macdLast < _pattern3Trough1 && macdLast < _pattern3Trough2)
		_pattern3Trough2 = macdLast;

		if (_pattern3Trough2 < 0m && macdCurr > _pattern3InnerMinThreshold.Value)
		{
			_pattern3BuyReady = macdCurr > macdLast && macdLast < macdPrev && macdLast > _pattern3Trough2;
		}

		if (_pattern3BuyReady)
		{
			if (TryOpenLong(candle, _pattern3StopLossBars.Value, _pattern3OffsetPoints.Value, _pattern3TakeProfitBars.Value, "Pattern3"))
			{
				_pattern3BuyReady = false;
				_pattern3SecondTroughArmed = false;
				_pattern3FirstTroughArmed = false;
				_pattern3Trough1 = 0m;
				_pattern3Trough2 = 0m;
			}
		}
	}

	private void ProcessPattern4(ICandleMessage candle)
	{
		if (!_pattern4Enabled.Value)
		return;

		var macdCurr = _macd4Prev1!.Value;
		var macdLast = _macd4Prev2!.Value;
		var macdPrev = _macd4Prev3!.Value;

		if (macdCurr > _pattern4MaxThreshold.Value && macdCurr < macdLast && macdLast > macdPrev && !_pattern4SellSetup)
		{
			_pattern4SellSetup = true;
			_pattern4SellPeak = macdLast;
		}

		if (macdCurr < _pattern4MaxThreshold.Value)
		{
			_pattern4SellSetup = false;
			_pattern4SellPeak = 0m;
		}

		if (_pattern4SellSetup && macdCurr > _pattern4MaxThreshold.Value && macdCurr < macdLast && macdLast > macdPrev && macdLast < _pattern4SellPeak)
		_pattern4SellReady = true;

		if (_pattern4SellReady)
		{
			if (TryOpenShort(candle, _pattern4StopLossBars.Value, _pattern4OffsetPoints.Value, _pattern4TakeProfitBars.Value, "Pattern4"))
			{
				_pattern4SellReady = false;
				_pattern4SellSetup = false;
				_pattern4SellPeak = 0m;
			}
		}

		if (macdCurr < _pattern4MinThreshold.Value && macdCurr > macdLast && macdLast < macdPrev && !_pattern4BuySetup)
		{
			_pattern4BuySetup = true;
			_pattern4BuyTrough = macdLast;
		}

		if (macdCurr > _pattern4MinThreshold.Value)
		{
			_pattern4BuySetup = false;
			_pattern4BuyTrough = 0m;
		}

		if (_pattern4BuySetup && macdCurr < _pattern4MinThreshold.Value && macdCurr > macdLast && macdLast < macdPrev && macdLast > _pattern4BuyTrough)
		_pattern4BuyReady = true;

		if (_pattern4BuyReady)
		{
			if (TryOpenLong(candle, _pattern4StopLossBars.Value, _pattern4OffsetPoints.Value, _pattern4TakeProfitBars.Value, "Pattern4"))
			{
				_pattern4BuyReady = false;
				_pattern4BuySetup = false;
				_pattern4BuyTrough = 0m;
			}
		}
	}

	private void ProcessPattern5(ICandleMessage candle)
	{
		if (!_pattern5Enabled.Value)
		return;

		var macdCurr = _macd5Prev1!.Value;
		var macdLast = _macd5Prev2!.Value;
		var macdPrev = _macd5Prev3!.Value;

		if (macdCurr < _pattern5LowerReset.Value && !_pattern5SellSetup)
		_pattern5SellSetup = true;

		if (_pattern5SellSetup && macdCurr > _pattern5LowerTrigger.Value)
		{
			_pattern5SellSetup = false;
			_pattern5SellTrigger = true;
		}

		if (_pattern5SellTrigger && macdCurr < macdLast && macdLast > macdPrev && macdCurr < _pattern5LowerTrigger.Value && macdLast > _pattern5LowerTrigger.Value)
		{
			_pattern5SellReady = true;
			_pattern5SellTrigger = false;
		}

		if (macdCurr > _pattern5LowerExitReset.Value)
		{
			_pattern5SellSetup = false;
			_pattern5SellTrigger = false;
			_pattern5SellReady = false;
		}

		if (_pattern5SellReady)
		{
			if (TryOpenShort(candle, _pattern5StopLossBars.Value, _pattern5OffsetPoints.Value, _pattern5TakeProfitBars.Value, "Pattern5"))
			{
				_pattern5SellReady = false;
			}
		}

		if (macdCurr > _pattern5UpperReset.Value && !_pattern5BuySetup)
		_pattern5BuySetup = true;

		if (_pattern5BuySetup && macdCurr < _pattern5UpperTrigger.Value)
		{
			_pattern5BuySetup = false;
			_pattern5BuyTrigger = true;
		}

		if (_pattern5BuyTrigger && macdCurr > macdLast && macdLast < macdPrev && macdCurr > _pattern5UpperTrigger.Value && macdLast < _pattern5UpperTrigger.Value)
		{
			_pattern5BuyReady = true;
			_pattern5BuyTrigger = false;
		}

		if (macdCurr < _pattern5UpperExitReset.Value)
		{
			_pattern5BuySetup = false;
			_pattern5BuyTrigger = false;
			_pattern5BuyReady = false;
		}

		if (_pattern5BuyReady)
		{
			if (TryOpenLong(candle, _pattern5StopLossBars.Value, _pattern5OffsetPoints.Value, _pattern5TakeProfitBars.Value, "Pattern5"))
			{
				_pattern5BuyReady = false;
			}
		}
	}

	private void ProcessPattern6(ICandleMessage candle)
	{
		if (!_pattern6Enabled.Value)
		return;

		var macdCurr = _macd6Prev1!.Value;

		if (macdCurr < _pattern6MaxThreshold.Value)
		_pattern6SellLocked = false;

		if (macdCurr > _pattern6MaxThreshold.Value && _pattern6SellCount <= _pattern6MaxBars.Value && !_pattern6SellLocked)
		_pattern6SellCount++;

		if (_pattern6SellCount > _pattern6MaxBars.Value)
		{
			_pattern6SellCount = 0;
			_pattern6SellLocked = true;
		}

		if (_pattern6SellCount < _pattern6MinBars.Value && macdCurr < _pattern6MaxThreshold.Value)
		_pattern6SellCount = 0;

		if (macdCurr < _pattern6MaxThreshold.Value && _pattern6SellCount > _pattern6CountBars.Value)
		_pattern6SellReady = true;

		if (_pattern6SellReady)
		{
			if (TryOpenShort(candle, _pattern6StopLossBars.Value, _pattern6OffsetPoints.Value, _pattern6TakeProfitBars.Value, "Pattern6"))
			{
				_pattern6SellReady = false;
				_pattern6SellCount = 0;
				_pattern6SellLocked = false;
			}
		}

		if (macdCurr > _pattern6MinThreshold.Value)
		_pattern6BuyLocked = false;

		if (macdCurr < _pattern6MinThreshold.Value && _pattern6BuyCount <= _pattern6MaxBars.Value && !_pattern6BuyLocked)
		_pattern6BuyCount++;

		if (_pattern6BuyCount > _pattern6MaxBars.Value)
		{
			_pattern6BuyCount = 0;
			_pattern6BuyLocked = true;
		}

		if (_pattern6BuyCount < _pattern6MinBars.Value && macdCurr > _pattern6MinThreshold.Value)
		_pattern6BuyCount = 0;

		if (macdCurr > _pattern6MinThreshold.Value && _pattern6BuyCount > _pattern6CountBars.Value)
		_pattern6BuyReady = true;

		if (_pattern6BuyReady)
		{
			if (TryOpenLong(candle, _pattern6StopLossBars.Value, _pattern6OffsetPoints.Value, _pattern6TakeProfitBars.Value, "Pattern6"))
			{
				_pattern6BuyReady = false;
				_pattern6BuyCount = 0;
				_pattern6BuyLocked = false;
			}
		}
	}
	private bool TryOpenLong(ICandleMessage candle, int stopBars, int offsetPoints, int takeBars, string label)
	{
		if (Position > 0)
		return false;

		var stop = ComputeStopLoss(Sides.Buy, stopBars, offsetPoints);
		var target = ComputeTakeProfit(Sides.Buy, takeBars);

		if (stop == null || target == null)
		return false;

		if (stop.Value >= candle.ClosePrice)
		stop = candle.ClosePrice - 10m * PriceStep;

		var volume = GetTradeVolume();

		BuyMarket(volume);
		_longEntryPrice = candle.ClosePrice;
		_longEntryVolume = volume;
		_longStop = stop;
		_longTarget = target;
		_longPartialCount = 0;
		_shortStop = null;
		_shortTarget = null;
		_shortPartialCount = 0;
		LogInfo($"{label}: buy volume={volume} stop={stop:F5} target={target:F5}");
		return true;
	}

	private bool TryOpenShort(ICandleMessage candle, int stopBars, int offsetPoints, int takeBars, string label)
	{
		if (Position < 0)
		return false;

		var stop = ComputeStopLoss(Sides.Sell, stopBars, offsetPoints);
		var target = ComputeTakeProfit(Sides.Sell, takeBars);

		if (stop == null || target == null)
		return false;

		if (stop.Value <= candle.ClosePrice)
		stop = candle.ClosePrice + 10m * PriceStep;

		var volume = GetTradeVolume();

		SellMarket(volume);
		_shortEntryPrice = candle.ClosePrice;
		_shortEntryVolume = volume;
		_shortStop = stop;
		_shortTarget = target;
		_shortPartialCount = 0;
		_longStop = null;
		_longTarget = null;
		_longPartialCount = 0;
		LogInfo($"{label}: sell volume={volume} stop={stop:F5} target={target:F5}");
		return true;
	}

	private decimal GetTradeVolume()
	{
		var step = VolumeStep;
		var lots = Math.Max(_currentVolume, _minVolume.Value);
		var multiplier = Math.Round(lots / step);
		if (multiplier < 1m)
		multiplier = 1m;
		return multiplier * step;
	}

	private decimal? ComputeStopLoss(Sides side, int stopBars, int offsetPoints)
	{
		if (stopBars <= 0)
		return null;

		if (side == Sides.Buy)
		{
			var lowest = GetExtreme(_lowHistory, stopBars, true);
			if (lowest == null)
			return null;
			return lowest.Value - offsetPoints * PriceStep;
		}

		var highest = GetExtreme(_highHistory, stopBars, false);
		if (highest == null)
		return null;
		return highest.Value + offsetPoints * PriceStep;
	}

	private decimal? ComputeTakeProfit(Sides side, int takeBars)
	{
		if (takeBars <= 0)
		return null;

		var searchLows = side == Sides.Sell;
		var source = searchLows ? _lowHistory : _highHistory;
		var offset = 0;
		decimal? current = null;

		while (true)
		{
			var segment = GetExtremeSegment(source, takeBars, offset, searchLows);
			if (segment == null)
			break;

			var next = GetExtremeSegment(source, takeBars, offset + takeBars, searchLows);

			if (next == null)
			{
				current = segment;
				break;
			}

			if (searchLows ? segment.Value > next.Value : segment.Value < next.Value)
			{
				offset += takeBars;
				current = next;
				continue;
			}

			current = segment;
			break;
		}

		return current;
	}

	private decimal? GetExtreme(List<decimal> list, int length, bool min)
	{
		if (list.Count < length)
		return null;

		decimal extreme = min ? decimal.MaxValue : decimal.MinValue;

		for (var i = 0; i < length; i++)
		{
			var index = list.Count - 1 - i;
			if (index < 0)
			break;

			var value = list[index];
			if (min)
			extreme = Math.Min(extreme, value);
			else
			extreme = Math.Max(extreme, value);
		}

		return extreme == decimal.MaxValue || extreme == decimal.MinValue ? null : extreme;
	}

	private decimal? GetExtremeSegment(List<decimal> list, int length, int startShift, bool min)
	{
		if (list.Count <= startShift)
		return null;

		decimal? extreme = null;

		for (var i = 0; i < length; i++)
		{
			var shift = startShift + i;
			if (shift >= list.Count)
			break;

			var index = list.Count - 1 - shift;
			if (index < 0)
			break;

			var value = list[index];
			extreme = extreme == null ? value : min ? Math.Min(extreme.Value, value) : Math.Max(extreme.Value, value);
		}

		return extreme;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				HandleExit(candle.ClosePrice, Sides.Buy, false);
				return;
			}

			if (_longTarget.HasValue && candle.HighPrice >= _longTarget.Value)
			{
				SellMarket(Position);
				HandleExit(_longTarget.Value, Sides.Buy, true);
				return;
			}

			ProcessPartialLong(candle);
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				HandleExit(candle.ClosePrice, Sides.Sell, false);
				return;
			}

			if (_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value)
			{
				BuyMarket(Math.Abs(Position));
				HandleExit(_shortTarget.Value, Sides.Sell, true);
				return;
			}

			ProcessPartialShort(candle);
		}
	}

	private void ProcessPartialLong(ICandleMessage candle)
	{
		if (_ema2Prev == null || _smaPrev == null || _ema3Prev == null)
		return;

		if (_closeHistory.Count < 2 || _highHistory.Count < 2)
		return;

		var prevClose = _closeHistory[_closeHistory.Count - 2];
		var prevHigh = _highHistory[_highHistory.Count - 2];
		var profitPoints = (candle.ClosePrice - _longEntryPrice) / PriceStep;

		if (profitPoints > 5m && prevClose > _ema2Prev && _longPartialCount == 0)
		{
			var volume = Math.Max(MinimumVolume, Math.Round(Position / 3m / VolumeStep) * VolumeStep);
			if (volume > 0m)
			{
				SellMarket(volume);
				_longPartialCount = 1;
			}
		}
		else if (profitPoints > 5m && prevHigh > (_smaPrev + _ema3Prev) / 2m && _longPartialCount == 1)
		{
			var volume = Math.Max(MinimumVolume, Math.Round(Position / 2m / VolumeStep) * VolumeStep);
			if (volume > 0m)
			{
				SellMarket(volume);
				_longPartialCount = 2;
			}
		}
	}

	private void ProcessPartialShort(ICandleMessage candle)
	{
		if (_ema2Prev == null || _smaPrev == null || _ema3Prev == null)
		return;

		if (_closeHistory.Count < 2 || _lowHistory.Count < 2)
		return;

		var prevClose = _closeHistory[_closeHistory.Count - 2];
		var prevLow = _lowHistory[_lowHistory.Count - 2];
		var profitPoints = (_shortEntryPrice - candle.ClosePrice) / PriceStep;

		if (profitPoints > 5m && prevClose < _ema2Prev && _shortPartialCount == 0)
		{
			var volume = Math.Max(MinimumVolume, Math.Round(Math.Abs(Position) / 3m / VolumeStep) * VolumeStep);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_shortPartialCount = 1;
			}
		}
		else if (profitPoints > 5m && prevLow < (_smaPrev + _ema3Prev) / 2m && _shortPartialCount == 1)
		{
			var volume = Math.Max(MinimumVolume, Math.Round(Math.Abs(Position) / 2m / VolumeStep) * VolumeStep);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_shortPartialCount = 2;
			}
		}
	}

	private void HandleExit(decimal exitPrice, Sides side, bool positive)
	{
		if (side == Sides.Buy)
		{
			var pnl = (exitPrice - _longEntryPrice) * _longEntryVolume;
			AdjustMartingale(pnl > 0m && positive);
		}
		else
		{
			var pnl = (_shortEntryPrice - exitPrice) * _shortEntryVolume;
			AdjustMartingale(pnl > 0m && positive);
		}

		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_longPartialCount = 0;
		_shortPartialCount = 0;
	}

	private void AdjustMartingale(bool profitable)
	{
		if (profitable)
		{
			_currentVolume = _baseVolume;
			return;
		}

		if (_enableSlowMartingale.Value)
		_currentVolume *= 2m;
	}
}
