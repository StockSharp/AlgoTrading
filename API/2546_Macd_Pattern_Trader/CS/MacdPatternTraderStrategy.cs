using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-pattern MACD strategy converted from the "MacdPatternTrader" expert.
/// </summary>
public class MacdPatternTraderStrategy : Strategy
{
	private const decimal MinPartialVolume = 0.01m;
	private const decimal ProfitThreshold = 5m;
	private const int HistoryLimit = 1024;

	private readonly StrategyParam<bool> _pattern1Enabled;
	private readonly StrategyParam<int> _pattern1StopLossBars;
	private readonly StrategyParam<int> _pattern1TakeProfitBars;
	private readonly StrategyParam<int> _pattern1Offset;
	private readonly StrategyParam<int> _pattern1FastEma;
	private readonly StrategyParam<int> _pattern1SlowEma;
	private readonly StrategyParam<decimal> _pattern1MaxThreshold;
	private readonly StrategyParam<decimal> _pattern1MinThreshold;

	private readonly StrategyParam<bool> _pattern2Enabled;
	private readonly StrategyParam<int> _pattern2StopLossBars;
	private readonly StrategyParam<int> _pattern2TakeProfitBars;
	private readonly StrategyParam<int> _pattern2Offset;
	private readonly StrategyParam<int> _pattern2FastEma;
	private readonly StrategyParam<int> _pattern2SlowEma;
	private readonly StrategyParam<decimal> _pattern2MaxThreshold;
	private readonly StrategyParam<decimal> _pattern2MinThreshold;

	private readonly StrategyParam<bool> _pattern3Enabled;
	private readonly StrategyParam<int> _pattern3StopLossBars;
	private readonly StrategyParam<int> _pattern3TakeProfitBars;
	private readonly StrategyParam<int> _pattern3Offset;
	private readonly StrategyParam<int> _pattern3FastEma;
	private readonly StrategyParam<int> _pattern3SlowEma;
	private readonly StrategyParam<decimal> _pattern3MaxThreshold;
	private readonly StrategyParam<decimal> _pattern3SecondaryMax;
	private readonly StrategyParam<decimal> _pattern3MinThreshold;
	private readonly StrategyParam<decimal> _pattern3SecondaryMin;

	private readonly StrategyParam<bool> _pattern4Enabled;
	private readonly StrategyParam<int> _pattern4StopLossBars;
	private readonly StrategyParam<int> _pattern4TakeProfitBars;
	private readonly StrategyParam<int> _pattern4Offset;
	private readonly StrategyParam<int> _pattern4FastEma;
	private readonly StrategyParam<int> _pattern4SlowEma;
	private readonly StrategyParam<decimal> _pattern4MaxThreshold;
	private readonly StrategyParam<decimal> _pattern4SecondaryMax;
	private readonly StrategyParam<decimal> _pattern4MinThreshold;
	private readonly StrategyParam<decimal> _pattern4SecondaryMin;

	private readonly StrategyParam<bool> _pattern5Enabled;
	private readonly StrategyParam<int> _pattern5StopLossBars;
	private readonly StrategyParam<int> _pattern5TakeProfitBars;
	private readonly StrategyParam<int> _pattern5Offset;
	private readonly StrategyParam<int> _pattern5FastEma;
	private readonly StrategyParam<int> _pattern5SlowEma;
	private readonly StrategyParam<decimal> _pattern5PrimaryMax;
	private readonly StrategyParam<decimal> _pattern5MaxThreshold;
	private readonly StrategyParam<decimal> _pattern5SecondaryMax;
	private readonly StrategyParam<decimal> _pattern5PrimaryMin;
	private readonly StrategyParam<decimal> _pattern5MinThreshold;
	private readonly StrategyParam<decimal> _pattern5SecondaryMin;

	private readonly StrategyParam<bool> _pattern6Enabled;
	private readonly StrategyParam<int> _pattern6StopLossBars;
	private readonly StrategyParam<int> _pattern6TakeProfitBars;
	private readonly StrategyParam<int> _pattern6Offset;
	private readonly StrategyParam<int> _pattern6FastEma;
	private readonly StrategyParam<int> _pattern6SlowEma;
	private readonly StrategyParam<decimal> _pattern6MaxThreshold;
	private readonly StrategyParam<decimal> _pattern6MinThreshold;
	private readonly StrategyParam<int> _pattern6MaxBars;
	private readonly StrategyParam<int> _pattern6MinBars;
	private readonly StrategyParam<int> _pattern6CountBars;

	private readonly StrategyParam<int> _ema1Period;
	private readonly StrategyParam<int> _ema2Period;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _ema3Period;

	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _history = new();

	private MovingAverageConvergenceDivergenceSignal _macd1 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd2 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd3 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd4 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd5 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd6 = null!;
	private ExponentialMovingAverage _ema1 = null!;
	private ExponentialMovingAverage _ema2 = null!;
	private SimpleMovingAverage _sma1 = null!;
	private ExponentialMovingAverage _ema3 = null!;

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

	private decimal _pointSize;
	private decimal _pointValue;
	private decimal _currentVolume;
	private decimal _entryPrice;
	private decimal _openVolume;
	private decimal _realizedPnL;
	private int _entryDirection;
	private decimal? _currentStopLoss;
	private decimal? _currentTakeProfit;

	private int _longPartialStage;
	private int _shortPartialStage;
	private int _barsBup;

	private int _pattern6ShortCounter;
	private bool _pattern6ShortBlocked;
	private int _pattern6LongCounter;
	private bool _pattern6LongBlocked;
	private bool _pattern6ShortReady;
	private bool _pattern6LongReady;

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdPatternTraderStrategy"/> class.
	/// </summary>
	public MacdPatternTraderStrategy()
	{
		_pattern1Enabled = Param(nameof(Pattern1Enabled), true)
			.SetDisplay("Pattern 1 Enabled", "Enable MACD pattern 1", "Pattern 1");
		_pattern1StopLossBars = Param(nameof(Pattern1StopLossBars), 22)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 1 SL Bars", "Stop loss lookback", "Pattern 1");
		_pattern1TakeProfitBars = Param(nameof(Pattern1TakeProfitBars), 32)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 1 TP Bars", "Take profit scan length", "Pattern 1");
		_pattern1Offset = Param(nameof(Pattern1Offset), 40)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 1 Offset", "Stop loss offset in points", "Pattern 1");
		_pattern1FastEma = Param(nameof(Pattern1FastEma), 24)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 1 Fast EMA", "Fast EMA period", "Pattern 1");
		_pattern1SlowEma = Param(nameof(Pattern1SlowEma), 13)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 1 Slow EMA", "Slow EMA period", "Pattern 1");
		_pattern1MaxThreshold = Param(nameof(Pattern1MaxThreshold), 0.0095m)
			.SetDisplay("Pattern 1 Max", "Upper MACD threshold", "Pattern 1");
		_pattern1MinThreshold = Param(nameof(Pattern1MinThreshold), -0.0045m)
			.SetDisplay("Pattern 1 Min", "Lower MACD threshold", "Pattern 1");

		_pattern2Enabled = Param(nameof(Pattern2Enabled), true)
			.SetDisplay("Pattern 2 Enabled", "Enable MACD pattern 2", "Pattern 2");
		_pattern2StopLossBars = Param(nameof(Pattern2StopLossBars), 2)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 2 SL Bars", "Stop loss lookback", "Pattern 2");
		_pattern2TakeProfitBars = Param(nameof(Pattern2TakeProfitBars), 2)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 2 TP Bars", "Take profit scan length", "Pattern 2");
		_pattern2Offset = Param(nameof(Pattern2Offset), 50)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 2 Offset", "Stop loss offset in points", "Pattern 2");
		_pattern2FastEma = Param(nameof(Pattern2FastEma), 17)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 2 Fast EMA", "Fast EMA period", "Pattern 2");
		_pattern2SlowEma = Param(nameof(Pattern2SlowEma), 7)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 2 Slow EMA", "Slow EMA period", "Pattern 2");
		_pattern2MaxThreshold = Param(nameof(Pattern2MaxThreshold), 0.0045m)
			.SetDisplay("Pattern 2 Max", "Upper MACD threshold", "Pattern 2");
		_pattern2MinThreshold = Param(nameof(Pattern2MinThreshold), -0.0035m)
			.SetDisplay("Pattern 2 Min", "Lower MACD threshold", "Pattern 2");

		_pattern3Enabled = Param(nameof(Pattern3Enabled), true)
			.SetDisplay("Pattern 3 Enabled", "Enable MACD pattern 3", "Pattern 3");
		_pattern3StopLossBars = Param(nameof(Pattern3StopLossBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 3 SL Bars", "Stop loss lookback", "Pattern 3");
		_pattern3TakeProfitBars = Param(nameof(Pattern3TakeProfitBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 3 TP Bars", "Take profit scan length", "Pattern 3");
		_pattern3Offset = Param(nameof(Pattern3Offset), 2)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 3 Offset", "Stop loss offset in points", "Pattern 3");
		_pattern3FastEma = Param(nameof(Pattern3FastEma), 32)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 3 Fast EMA", "Fast EMA period", "Pattern 3");
		_pattern3SlowEma = Param(nameof(Pattern3SlowEma), 2)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 3 Slow EMA", "Slow EMA period", "Pattern 3");
		_pattern3MaxThreshold = Param(nameof(Pattern3MaxThreshold), 0.0015m)
			.SetDisplay("Pattern 3 Max", "Upper MACD threshold", "Pattern 3");
		_pattern3SecondaryMax = Param(nameof(Pattern3SecondaryMax), 0.004m)
			.SetDisplay("Pattern 3 Secondary Max", "Secondary upper MACD threshold", "Pattern 3");
		_pattern3MinThreshold = Param(nameof(Pattern3MinThreshold), -0.005m)
			.SetDisplay("Pattern 3 Min", "Lower MACD threshold", "Pattern 3");
		_pattern3SecondaryMin = Param(nameof(Pattern3SecondaryMin), -0.0005m)
			.SetDisplay("Pattern 3 Secondary Min", "Secondary lower MACD threshold", "Pattern 3");

		_pattern4Enabled = Param(nameof(Pattern4Enabled), true)
			.SetDisplay("Pattern 4 Enabled", "Enable MACD pattern 4", "Pattern 4");
		_pattern4StopLossBars = Param(nameof(Pattern4StopLossBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 4 SL Bars", "Stop loss lookback", "Pattern 4");
		_pattern4TakeProfitBars = Param(nameof(Pattern4TakeProfitBars), 32)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 4 TP Bars", "Take profit scan length", "Pattern 4");
		_pattern4Offset = Param(nameof(Pattern4Offset), 45)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 4 Offset", "Stop loss offset in points", "Pattern 4");
		_pattern4FastEma = Param(nameof(Pattern4FastEma), 4)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 4 Fast EMA", "Fast EMA period", "Pattern 4");
		_pattern4SlowEma = Param(nameof(Pattern4SlowEma), 9)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 4 Slow EMA", "Slow EMA period", "Pattern 4");
		_pattern4MaxThreshold = Param(nameof(Pattern4MaxThreshold), 0.0165m)
			.SetDisplay("Pattern 4 Max", "Upper MACD threshold", "Pattern 4");
		_pattern4SecondaryMax = Param(nameof(Pattern4SecondaryMax), 0.0001m)
			.SetDisplay("Pattern 4 Secondary Max", "Secondary upper MACD threshold", "Pattern 4");
		_pattern4MinThreshold = Param(nameof(Pattern4MinThreshold), -0.0005m)
			.SetDisplay("Pattern 4 Min", "Lower MACD threshold", "Pattern 4");
		_pattern4SecondaryMin = Param(nameof(Pattern4SecondaryMin), -0.0006m)
			.SetDisplay("Pattern 4 Secondary Min", "Secondary lower MACD threshold", "Pattern 4");

		_pattern5Enabled = Param(nameof(Pattern5Enabled), true)
			.SetDisplay("Pattern 5 Enabled", "Enable MACD pattern 5", "Pattern 5");
		_pattern5StopLossBars = Param(nameof(Pattern5StopLossBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 5 SL Bars", "Stop loss lookback", "Pattern 5");
		_pattern5TakeProfitBars = Param(nameof(Pattern5TakeProfitBars), 47)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 5 TP Bars", "Take profit scan length", "Pattern 5");
		_pattern5Offset = Param(nameof(Pattern5Offset), 45)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 5 Offset", "Stop loss offset in points", "Pattern 5");
		_pattern5FastEma = Param(nameof(Pattern5FastEma), 6)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 5 Fast EMA", "Fast EMA period", "Pattern 5");
		_pattern5SlowEma = Param(nameof(Pattern5SlowEma), 2)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 5 Slow EMA", "Slow EMA period", "Pattern 5");
		_pattern5PrimaryMax = Param(nameof(Pattern5PrimaryMax), 0.0005m)
			.SetDisplay("Pattern 5 Primary Max", "Initial ceiling trigger", "Pattern 5");
		_pattern5MaxThreshold = Param(nameof(Pattern5MaxThreshold), 0.0015m)
			.SetDisplay("Pattern 5 Max", "Upper MACD threshold", "Pattern 5");
		_pattern5SecondaryMax = Param(nameof(Pattern5SecondaryMax), 0m)
			.SetDisplay("Pattern 5 Secondary Max", "Secondary upper MACD", "Pattern 5");
		_pattern5PrimaryMin = Param(nameof(Pattern5PrimaryMin), -0.0005m)
			.SetDisplay("Pattern 5 Primary Min", "Initial floor trigger", "Pattern 5");
		_pattern5MinThreshold = Param(nameof(Pattern5MinThreshold), -0.003m)
			.SetDisplay("Pattern 5 Min", "Lower MACD threshold", "Pattern 5");
		_pattern5SecondaryMin = Param(nameof(Pattern5SecondaryMin), 0m)
			.SetDisplay("Pattern 5 Secondary Min", "Secondary lower MACD", "Pattern 5");

		_pattern6Enabled = Param(nameof(Pattern6Enabled), true)
			.SetDisplay("Pattern 6 Enabled", "Enable MACD pattern 6", "Pattern 6");
		_pattern6StopLossBars = Param(nameof(Pattern6StopLossBars), 26)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 6 SL Bars", "Stop loss lookback", "Pattern 6");
		_pattern6TakeProfitBars = Param(nameof(Pattern6TakeProfitBars), 42)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 6 TP Bars", "Take profit scan length", "Pattern 6");
		_pattern6Offset = Param(nameof(Pattern6Offset), 20)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 6 Offset", "Stop loss offset in points", "Pattern 6");
		_pattern6FastEma = Param(nameof(Pattern6FastEma), 4)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 6 Fast EMA", "Fast EMA period", "Pattern 6");
		_pattern6SlowEma = Param(nameof(Pattern6SlowEma), 8)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 6 Slow EMA", "Slow EMA period", "Pattern 6");
		_pattern6MaxThreshold = Param(nameof(Pattern6MaxThreshold), 0.0005m)
			.SetDisplay("Pattern 6 Max", "Upper MACD threshold", "Pattern 6");
		_pattern6MinThreshold = Param(nameof(Pattern6MinThreshold), -0.001m)
			.SetDisplay("Pattern 6 Min", "Lower MACD threshold", "Pattern 6");
		_pattern6MaxBars = Param(nameof(Pattern6MaxBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 6 Max Bars", "Maximum counted bars", "Pattern 6");
		_pattern6MinBars = Param(nameof(Pattern6MinBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 6 Min Bars", "Minimum counted bars", "Pattern 6");
		_pattern6CountBars = Param(nameof(Pattern6CountBars), 4)
			.SetGreaterThanZero()
			.SetDisplay("Pattern 6 Count Bars", "Trigger counter threshold", "Pattern 6");

		_ema1Period = Param(nameof(Ema1Period), 7)
			.SetGreaterThanZero()
			.SetDisplay("EMA1 Period", "First EMA for management", "Management");
		_ema2Period = Param(nameof(Ema2Period), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA2 Period", "Second EMA for management", "Management");
		_smaPeriod = Param(nameof(SmaPeriod), 98)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA for management", "Management");
		_ema3Period = Param(nameof(Ema3Period), 365)
			.SetGreaterThanZero()
			.SetDisplay("EMA3 Period", "Slow EMA for management", "Management");

		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Base trading volume", "Trading");
		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Enable trading window", "Trading");
		_sessionStart = Param(nameof(SessionStart), new TimeSpan(7, 0, 0))
			.SetDisplay("Session Start", "Trading start time", "Trading");
		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(17, 0, 0))
			.SetDisplay("Session End", "Trading end time", "Trading");
		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Double volume after losses", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Base candle type", "Trading");
	}

	/// <summary>
	/// Enable or disable the first MACD pattern.
	/// </summary>
	public bool Pattern1Enabled
	{
		get => _pattern1Enabled.Value;
		set => _pattern1Enabled.Value = value;
	}

	/// <summary>
	/// Stop loss lookback for pattern 1.
	/// </summary>
	public int Pattern1StopLossBars
	{
		get => _pattern1StopLossBars.Value;
		set => _pattern1StopLossBars.Value = value;
	}

	/// <summary>
	/// Take profit lookback for pattern 1.
	/// </summary>
	public int Pattern1TakeProfitBars
	{
		get => _pattern1TakeProfitBars.Value;
		set => _pattern1TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Stop loss offset for pattern 1.
	/// </summary>
	public int Pattern1Offset
	{
		get => _pattern1Offset.Value;
		set => _pattern1Offset.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern 1 MACD.
	/// </summary>
	public int Pattern1FastEma
	{
		get => _pattern1FastEma.Value;
		set => _pattern1FastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern 1 MACD.
	/// </summary>
	public int Pattern1SlowEma
	{
		get => _pattern1SlowEma.Value;
		set => _pattern1SlowEma.Value = value;
	}

	/// <summary>
	/// Upper MACD trigger for pattern 1.
	/// </summary>
	public decimal Pattern1MaxThreshold
	{
		get => _pattern1MaxThreshold.Value;
		set => _pattern1MaxThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD trigger for pattern 1.
	/// </summary>
	public decimal Pattern1MinThreshold
	{
		get => _pattern1MinThreshold.Value;
		set => _pattern1MinThreshold.Value = value;
	}

	/// <summary>
	/// Enable or disable the second MACD pattern.
	/// </summary>
	public bool Pattern2Enabled
	{
		get => _pattern2Enabled.Value;
		set => _pattern2Enabled.Value = value;
	}

	/// <summary>
	/// Stop loss lookback for pattern 2.
	/// </summary>
	public int Pattern2StopLossBars
	{
		get => _pattern2StopLossBars.Value;
		set => _pattern2StopLossBars.Value = value;
	}

	/// <summary>
	/// Take profit lookback for pattern 2.
	/// </summary>
	public int Pattern2TakeProfitBars
	{
		get => _pattern2TakeProfitBars.Value;
		set => _pattern2TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Stop loss offset for pattern 2.
	/// </summary>
	public int Pattern2Offset
	{
		get => _pattern2Offset.Value;
		set => _pattern2Offset.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern 2 MACD.
	/// </summary>
	public int Pattern2FastEma
	{
		get => _pattern2FastEma.Value;
		set => _pattern2FastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern 2 MACD.
	/// </summary>
	public int Pattern2SlowEma
	{
		get => _pattern2SlowEma.Value;
		set => _pattern2SlowEma.Value = value;
	}

	/// <summary>
	/// Upper MACD trigger for pattern 2.
	/// </summary>
	public decimal Pattern2MaxThreshold
	{
		get => _pattern2MaxThreshold.Value;
		set => _pattern2MaxThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD trigger for pattern 2.
	/// </summary>
	public decimal Pattern2MinThreshold
	{
		get => _pattern2MinThreshold.Value;
		set => _pattern2MinThreshold.Value = value;
	}

	/// <summary>
	/// Enable or disable the third MACD pattern.
	/// </summary>
	public bool Pattern3Enabled
	{
		get => _pattern3Enabled.Value;
		set => _pattern3Enabled.Value = value;
	}

	/// <summary>
	/// Stop loss lookback for pattern 3.
	/// </summary>
	public int Pattern3StopLossBars
	{
		get => _pattern3StopLossBars.Value;
		set => _pattern3StopLossBars.Value = value;
	}

	/// <summary>
	/// Take profit lookback for pattern 3.
	/// </summary>
	public int Pattern3TakeProfitBars
	{
		get => _pattern3TakeProfitBars.Value;
		set => _pattern3TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Stop loss offset for pattern 3.
	/// </summary>
	public int Pattern3Offset
	{
		get => _pattern3Offset.Value;
		set => _pattern3Offset.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern 3 MACD.
	/// </summary>
	public int Pattern3FastEma
	{
		get => _pattern3FastEma.Value;
		set => _pattern3FastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern 3 MACD.
	/// </summary>
	public int Pattern3SlowEma
	{
		get => _pattern3SlowEma.Value;
		set => _pattern3SlowEma.Value = value;
	}

	/// <summary>
	/// Primary upper MACD threshold for pattern 3.
	/// </summary>
	public decimal Pattern3MaxThreshold
	{
		get => _pattern3MaxThreshold.Value;
		set => _pattern3MaxThreshold.Value = value;
	}

	/// <summary>
	/// Secondary upper MACD threshold for pattern 3.
	/// </summary>
	public decimal Pattern3SecondaryMax
	{
		get => _pattern3SecondaryMax.Value;
		set => _pattern3SecondaryMax.Value = value;
	}

	/// <summary>
	/// Primary lower MACD threshold for pattern 3.
	/// </summary>
	public decimal Pattern3MinThreshold
	{
		get => _pattern3MinThreshold.Value;
		set => _pattern3MinThreshold.Value = value;
	}

	/// <summary>
	/// Secondary lower MACD threshold for pattern 3.
	/// </summary>
	public decimal Pattern3SecondaryMin
	{
		get => _pattern3SecondaryMin.Value;
		set => _pattern3SecondaryMin.Value = value;
	}

	/// <summary>
	/// Enable or disable the fourth MACD pattern.
	/// </summary>
	public bool Pattern4Enabled
	{
		get => _pattern4Enabled.Value;
		set => _pattern4Enabled.Value = value;
	}

	/// <summary>
	/// Stop loss lookback for pattern 4.
	/// </summary>
	public int Pattern4StopLossBars
	{
		get => _pattern4StopLossBars.Value;
		set => _pattern4StopLossBars.Value = value;
	}

	/// <summary>
	/// Take profit lookback for pattern 4.
	/// </summary>
	public int Pattern4TakeProfitBars
	{
		get => _pattern4TakeProfitBars.Value;
		set => _pattern4TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Stop loss offset for pattern 4.
	/// </summary>
	public int Pattern4Offset
	{
		get => _pattern4Offset.Value;
		set => _pattern4Offset.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern 4 MACD.
	/// </summary>
	public int Pattern4FastEma
	{
		get => _pattern4FastEma.Value;
		set => _pattern4FastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern 4 MACD.
	/// </summary>
	public int Pattern4SlowEma
	{
		get => _pattern4SlowEma.Value;
		set => _pattern4SlowEma.Value = value;
	}

	/// <summary>
	/// Primary upper MACD threshold for pattern 4.
	/// </summary>
	public decimal Pattern4MaxThreshold
	{
		get => _pattern4MaxThreshold.Value;
		set => _pattern4MaxThreshold.Value = value;
	}

	/// <summary>
	/// Secondary upper MACD threshold for pattern 4.
	/// </summary>
	public decimal Pattern4SecondaryMax
	{
		get => _pattern4SecondaryMax.Value;
		set => _pattern4SecondaryMax.Value = value;
	}

	/// <summary>
	/// Primary lower MACD threshold for pattern 4.
	/// </summary>
	public decimal Pattern4MinThreshold
	{
		get => _pattern4MinThreshold.Value;
		set => _pattern4MinThreshold.Value = value;
	}

	/// <summary>
	/// Secondary lower MACD threshold for pattern 4.
	/// </summary>
	public decimal Pattern4SecondaryMin
	{
		get => _pattern4SecondaryMin.Value;
		set => _pattern4SecondaryMin.Value = value;
	}

	/// <summary>
	/// Enable or disable the fifth MACD pattern.
	/// </summary>
	public bool Pattern5Enabled
	{
		get => _pattern5Enabled.Value;
		set => _pattern5Enabled.Value = value;
	}

	/// <summary>
	/// Stop loss lookback for pattern 5.
	/// </summary>
	public int Pattern5StopLossBars
	{
		get => _pattern5StopLossBars.Value;
		set => _pattern5StopLossBars.Value = value;
	}

	/// <summary>
	/// Take profit lookback for pattern 5.
	/// </summary>
	public int Pattern5TakeProfitBars
	{
		get => _pattern5TakeProfitBars.Value;
		set => _pattern5TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Stop loss offset for pattern 5.
	/// </summary>
	public int Pattern5Offset
	{
		get => _pattern5Offset.Value;
		set => _pattern5Offset.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern 5 MACD.
	/// </summary>
	public int Pattern5FastEma
	{
		get => _pattern5FastEma.Value;
		set => _pattern5FastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern 5 MACD.
	/// </summary>
	public int Pattern5SlowEma
	{
		get => _pattern5SlowEma.Value;
		set => _pattern5SlowEma.Value = value;
	}

	/// <summary>
	/// Primary trigger level for the bullish leg in pattern 5.
	/// </summary>
	public decimal Pattern5PrimaryMax
	{
		get => _pattern5PrimaryMax.Value;
		set => _pattern5PrimaryMax.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold for pattern 5.
	/// </summary>
	public decimal Pattern5MaxThreshold
	{
		get => _pattern5MaxThreshold.Value;
		set => _pattern5MaxThreshold.Value = value;
	}

	/// <summary>
	/// Secondary upper MACD threshold for pattern 5.
	/// </summary>
	public decimal Pattern5SecondaryMax
	{
		get => _pattern5SecondaryMax.Value;
		set => _pattern5SecondaryMax.Value = value;
	}

	/// <summary>
	/// Primary trigger level for the bearish leg in pattern 5.
	/// </summary>
	public decimal Pattern5PrimaryMin
	{
		get => _pattern5PrimaryMin.Value;
		set => _pattern5PrimaryMin.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold for pattern 5.
	/// </summary>
	public decimal Pattern5MinThreshold
	{
		get => _pattern5MinThreshold.Value;
		set => _pattern5MinThreshold.Value = value;
	}

	/// <summary>
	/// Secondary lower MACD threshold for pattern 5.
	/// </summary>
	public decimal Pattern5SecondaryMin
	{
		get => _pattern5SecondaryMin.Value;
		set => _pattern5SecondaryMin.Value = value;
	}

	/// <summary>
	/// Enable or disable the sixth MACD pattern.
	/// </summary>
	public bool Pattern6Enabled
	{
		get => _pattern6Enabled.Value;
		set => _pattern6Enabled.Value = value;
	}

	/// <summary>
	/// Stop loss lookback for pattern 6.
	/// </summary>
	public int Pattern6StopLossBars
	{
		get => _pattern6StopLossBars.Value;
		set => _pattern6StopLossBars.Value = value;
	}

	/// <summary>
	/// Take profit lookback for pattern 6.
	/// </summary>
	public int Pattern6TakeProfitBars
	{
		get => _pattern6TakeProfitBars.Value;
		set => _pattern6TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Stop loss offset for pattern 6.
	/// </summary>
	public int Pattern6Offset
	{
		get => _pattern6Offset.Value;
		set => _pattern6Offset.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern 6 MACD.
	/// </summary>
	public int Pattern6FastEma
	{
		get => _pattern6FastEma.Value;
		set => _pattern6FastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern 6 MACD.
	/// </summary>
	public int Pattern6SlowEma
	{
		get => _pattern6SlowEma.Value;
		set => _pattern6SlowEma.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold for pattern 6.
	/// </summary>
	public decimal Pattern6MaxThreshold
	{
		get => _pattern6MaxThreshold.Value;
		set => _pattern6MaxThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold for pattern 6.
	/// </summary>
	public decimal Pattern6MinThreshold
	{
		get => _pattern6MinThreshold.Value;
		set => _pattern6MinThreshold.Value = value;
	}

	/// <summary>
	/// Maximum counted bars for pattern 6.
	/// </summary>
	public int Pattern6MaxBars
	{
		get => _pattern6MaxBars.Value;
		set => _pattern6MaxBars.Value = value;
	}

	/// <summary>
	/// Minimum counted bars for pattern 6.
	/// </summary>
	public int Pattern6MinBars
	{
		get => _pattern6MinBars.Value;
		set => _pattern6MinBars.Value = value;
	}

	/// <summary>
	/// Counter threshold for pattern 6 triggers.
	/// </summary>
	public int Pattern6CountBars
	{
		get => _pattern6CountBars.Value;
		set => _pattern6CountBars.Value = value;
	}

	/// <summary>
	/// EMA used in partial exit logic.
	/// </summary>
	public int Ema1Period
	{
		get => _ema1Period.Value;
		set => _ema1Period.Value = value;
	}

	/// <summary>
	/// Second EMA for partial exit logic.
	/// </summary>
	public int Ema2Period
	{
		get => _ema2Period.Value;
		set => _ema2Period.Value = value;
	}

	/// <summary>
	/// SMA used for partial exits.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA used for partial exits.
	/// </summary>
	public int Ema3Period
	{
		get => _ema3Period.Value;
		set => _ema3Period.Value = value;
	}

	/// <summary>
	/// Base lot size for orders.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Enable trading window control.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Session start time.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Use martingale sizing when a trade loses.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = Security?.PriceStep ?? 0.0001m;
		_pointValue = Security?.PriceStepCost ?? 1m;
		_currentVolume = LotSize;
		Volume = LotSize;

		_macd1 = CreateMacd(Pattern1FastEma, Pattern1SlowEma);
		_macd2 = CreateMacd(Pattern2FastEma, Pattern2SlowEma);
		_macd3 = CreateMacd(Pattern3FastEma, Pattern3SlowEma);
		_macd4 = CreateMacd(Pattern4FastEma, Pattern4SlowEma);
		_macd5 = CreateMacd(Pattern5FastEma, Pattern5SlowEma);
		_macd6 = CreateMacd(Pattern6FastEma, Pattern6SlowEma);

		_ema1 = new ExponentialMovingAverage { Length = Ema1Period };
		_ema2 = new ExponentialMovingAverage { Length = Ema2Period };
		_sma1 = new SimpleMovingAverage { Length = SmaPeriod };
		_ema3 = new ExponentialMovingAverage { Length = Ema3Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd1, _macd2, _macd3, _macd4, _macd5, _macd6, _ema1, _ema2, _sma1, _ema3, ProcessCandle)
			.Start();
	}

	private static MovingAverageConvergenceDivergenceSignal CreateMacd(int fast, int slow)
	{
		return new()
		{
			Macd =
			{
				ShortMa = new ExponentialMovingAverage { Length = fast },
				LongMa = new ExponentialMovingAverage { Length = slow },
			},
			SignalMa = new ExponentialMovingAverage { Length = 1 },
		};
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue macd1Value,
		IIndicatorValue macd2Value,
		IIndicatorValue macd3Value,
		IIndicatorValue macd4Value,
		IIndicatorValue macd5Value,
		IIndicatorValue macd6Value,
		IIndicatorValue ema1Value,
		IIndicatorValue ema2Value,
		IIndicatorValue sma1Value,
		IIndicatorValue ema3Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_history.Add(candle);
		if (_history.Count > HistoryLimit)
			_history.RemoveAt(0);

		var macd1 = (MovingAverageConvergenceDivergenceSignalValue)macd1Value;
		var macd2 = (MovingAverageConvergenceDivergenceSignalValue)macd2Value;
		var macd3 = (MovingAverageConvergenceDivergenceSignalValue)macd3Value;
		var macd4 = (MovingAverageConvergenceDivergenceSignalValue)macd4Value;
		var macd5 = (MovingAverageConvergenceDivergenceSignalValue)macd5Value;
		var macd6 = (MovingAverageConvergenceDivergenceSignalValue)macd6Value;

		if (macd1.Macd is not decimal macd1Current ||
			macd2.Macd is not decimal macd2Current ||
			macd3.Macd is not decimal macd3Current ||
			macd4.Macd is not decimal macd4Current ||
			macd5.Macd is not decimal macd5Current ||
			macd6.Macd is not decimal macd6Current)
		{
			UpdatePreviousIndicators(ema1Value.ToDecimal(), ema2Value.ToDecimal(), sma1Value.ToDecimal(), ema3Value.ToDecimal());
			return;
		}

		var ema1Current = ema1Value.ToDecimal();
		var ema2Current = ema2Value.ToDecimal();
		var smaCurrent = sma1Value.ToDecimal();
		var ema3Current = ema3Value.ToDecimal();

		var macd1Ready = TryGetMacdSeries(ref _macd1Prev1, ref _macd1Prev2, ref _macd1Prev3, macd1Current, out var macd1Curr, out var macd1Last, out var macd1Last3);
		var macd2Ready = TryGetMacdSeries(ref _macd2Prev1, ref _macd2Prev2, ref _macd2Prev3, macd2Current, out var macd2Curr, out var macd2Last, out var macd2Last3);
		var macd3Ready = TryGetMacdSeries(ref _macd3Prev1, ref _macd3Prev2, ref _macd3Prev3, macd3Current, out var macd3Curr, out var macd3Last, out var macd3Last3);
		var macd4Ready = TryGetMacdSeries(ref _macd4Prev1, ref _macd4Prev2, ref _macd4Prev3, macd4Current, out var macd4Curr, out var macd4Last, out var macd4Last3);
		var macd5Ready = TryGetMacdSeries(ref _macd5Prev1, ref _macd5Prev2, ref _macd5Prev3, macd5Current, out var macd5Curr, out var macd5Last, out var macd5Last3);
		var macd6Ready = TryGetMacdSeries(ref _macd6Prev1, ref _macd6Prev2, ref _macd6Prev3, macd6Current, out var macd6Curr, out var macd6Last, out var macd6Last3);

		var ema1Prev = _ema1Prev;
		var ema2Prev = _ema2Prev;
		var smaPrev = _smaPrev;
		var ema3Prev = _ema3Prev;

		if (!macd1Ready || !macd2Ready || !macd3Ready || !macd4Ready || !macd5Ready || !macd6Ready ||
			!_macd1.IsFormed || !_macd2.IsFormed || !_macd3.IsFormed || !_macd4.IsFormed || !_macd5.IsFormed || !_macd6.IsFormed ||
			!_ema1.IsFormed || !_ema2.IsFormed || !_sma1.IsFormed || !_ema3.IsFormed ||
			ema1Prev is null || ema2Prev is null || smaPrev is null || ema3Prev is null)
		{
			UpdatePreviousIndicators(ema1Current, ema2Current, smaCurrent, ema3Current);
			return;
		}

		CheckRiskManagement(candle);

		var inSession = !UseTimeFilter || IsInSession(candle.OpenTime.TimeOfDay);
		var canTrade = inSession && IsFormedAndOnlineAndAllowTrading();

		if (canTrade)
		{
			ProcessPattern6(candle, macd6Curr, macd6Last, macd6Last3);
			ProcessPattern5(candle, macd5Curr, macd5Last, macd5Last3);
			ProcessPattern4(candle, macd4Curr, macd4Last, macd4Last3);
			ProcessPattern3(candle, macd3Curr, macd3Last, macd3Last3);
			ProcessPattern2(candle, macd2Curr, macd2Last, macd2Last3);
			ProcessPattern1(candle, macd1Curr, macd1Last, macd1Last3);
		}

		if (inSession)
			ManageActivePosition(candle, ema1Prev.Value, ema2Prev.Value, smaPrev.Value, ema3Prev.Value);

		UpdatePreviousIndicators(ema1Current, ema2Current, smaCurrent, ema3Current);
	}

	private void ProcessPattern1(ICandleMessage candle, decimal macdCurr, decimal macdLast, decimal macdLast3)
	{
		if (!Pattern1Enabled)
			return;

		if (macdCurr > Pattern1MaxThreshold && macdCurr < macdLast && macdLast > macdLast3 && macdCurr > 0m && macdLast3 < Pattern1MaxThreshold && Position >= 0)
		{
			var stop = CalculateStopPrice(isLong: false, Pattern1StopLossBars, Pattern1Offset);
			var take = CalculateTakePrice(isLong: false, Pattern1TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterShort(candle, stop.Value, take.Value);
				_shortPartialStage = 0;
			}
		}

		if (macdCurr < Pattern1MinThreshold && macdCurr > macdLast && macdLast < macdLast3 && macdCurr < 0m && macdLast3 > Pattern1MinThreshold && Position <= 0)
		{
			var stop = CalculateStopPrice(isLong: true, Pattern1StopLossBars, Pattern1Offset);
			var take = CalculateTakePrice(isLong: true, Pattern1TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterLong(candle, stop.Value, take.Value);
				_longPartialStage = 0;
			}
		}
	}

	private void ProcessPattern2(ICandleMessage candle, decimal macdCurr, decimal macdLast, decimal macdLast3)
	{
		if (!Pattern2Enabled)
			return;

		if (macdCurr > 0m && macdCurr > macdLast && macdLast < macdLast3 && macdCurr > Pattern2MinThreshold && macdCurr < 0m && Position >= 0)
		{
			var stop = CalculateStopPrice(isLong: false, Pattern2StopLossBars, Pattern2Offset);
			var take = CalculateTakePrice(isLong: false, Pattern2TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterShort(candle, stop.Value, take.Value);
				_shortPartialStage = 0;
			}
		}

		if (macdCurr < 0m && macdCurr < macdLast && macdLast > macdLast3 && macdCurr < Pattern2MaxThreshold && macdCurr > 0m && Position <= 0)
		{
			var stop = CalculateStopPrice(isLong: true, Pattern2StopLossBars, Pattern2Offset);
			var take = CalculateTakePrice(isLong: true, Pattern2TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterLong(candle, stop.Value, take.Value);
				_longPartialStage = 0;
			}
		}
	}

	private void ProcessPattern3(ICandleMessage candle, decimal macdCurr, decimal macdLast, decimal macdLast3)
	{
		if (!Pattern3Enabled)
			return;

		var secondaryMax = Pattern3SecondaryMax;
		var primaryMax = Pattern3MaxThreshold;
		var secondaryMin = Pattern3SecondaryMin;
		var primaryMin = Pattern3MinThreshold;

		if (macdCurr > secondaryMax)
			_barsBup++;

		if (macdCurr < primaryMax && macdCurr < macdLast && macdLast > macdLast3 && macdLast > primaryMax && macdLast > secondaryMax && Position >= 0)
		{
			var stop = CalculateStopPrice(isLong: false, Pattern3StopLossBars, Pattern3Offset);
			var take = CalculateTakePrice(isLong: false, Pattern3TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterShort(candle, stop.Value, take.Value);
				_shortPartialStage = 0;
				_barsBup = 0;
			}
		}

		if (macdCurr > primaryMin && macdCurr > macdLast && macdLast < macdLast3 && macdLast < primaryMin && macdLast < secondaryMin && Position <= 0)
		{
			var stop = CalculateStopPrice(isLong: true, Pattern3StopLossBars, Pattern3Offset);
			var take = CalculateTakePrice(isLong: true, Pattern3TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterLong(candle, stop.Value, take.Value);
				_longPartialStage = 0;
			}
		}
	}

	private void ProcessPattern4(ICandleMessage candle, decimal macdCurr, decimal macdLast, decimal macdLast3)
	{
		if (!Pattern4Enabled)
			return;

		if (macdCurr > Pattern4MaxThreshold && macdCurr < macdLast && macdLast > macdLast3 && macdLast < Pattern4SecondaryMax && Position >= 0)
		{
			var stop = CalculateStopPrice(isLong: false, Pattern4StopLossBars, Pattern4Offset);
			var take = CalculateTakePrice(isLong: false, Pattern4TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterShort(candle, stop.Value, take.Value);
				_shortPartialStage = 0;
			}
		}

		if (macdCurr < Pattern4MinThreshold && macdCurr > macdLast && macdLast < macdLast3 && macdLast > Pattern4SecondaryMin && Position <= 0)
		{
			var stop = CalculateStopPrice(isLong: true, Pattern4StopLossBars, Pattern4Offset);
			var take = CalculateTakePrice(isLong: true, Pattern4TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterLong(candle, stop.Value, take.Value);
				_longPartialStage = 0;
			}
		}
	}

	private void ProcessPattern5(ICandleMessage candle, decimal macdCurr, decimal macdLast, decimal macdLast3)
	{
		if (!Pattern5Enabled)
			return;

		if (macdCurr < Pattern5PrimaryMin && macdCurr > Pattern5MinThreshold && macdCurr < macdLast && macdLast > macdLast3 && macdLast > Pattern5MinThreshold && Position >= 0)
		{
			var stop = CalculateStopPrice(isLong: false, Pattern5StopLossBars, Pattern5Offset);
			var take = CalculateTakePrice(isLong: false, Pattern5TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterShort(candle, stop.Value, take.Value);
				_shortPartialStage = 0;
			}
		}

		if (macdCurr > Pattern5PrimaryMax && macdCurr < Pattern5MaxThreshold && macdCurr > macdLast && macdLast < macdLast3 && macdLast < Pattern5MaxThreshold && Position <= 0)
		{
			var stop = CalculateStopPrice(isLong: true, Pattern5StopLossBars, Pattern5Offset);
			var take = CalculateTakePrice(isLong: true, Pattern5TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterLong(candle, stop.Value, take.Value);
				_longPartialStage = 0;
			}
		}
	}

	private void ProcessPattern6(ICandleMessage candle, decimal macdCurr, decimal macdLast, decimal macdLast3)
	{
		if (!Pattern6Enabled)
			return;

		if (macdCurr < Pattern6MaxThreshold)
			_pattern6ShortBlocked = false;

		if (macdCurr > Pattern6MaxThreshold && _pattern6ShortCounter <= Pattern6MaxBars && !_pattern6ShortBlocked)
			_pattern6ShortCounter++;

		if (_pattern6ShortCounter > Pattern6MaxBars)
		{
			_pattern6ShortCounter = 0;
			_pattern6ShortBlocked = true;
		}

		if (_pattern6ShortCounter < Pattern6MinBars && macdCurr < Pattern6MaxThreshold)
			_pattern6ShortCounter = 0;

		if (macdCurr < Pattern6MaxThreshold && _pattern6ShortCounter > Pattern6CountBars)
			_pattern6ShortReady = true;

		if (_pattern6ShortReady && Position >= 0)
		{
			var stop = CalculateStopPrice(isLong: false, Pattern6StopLossBars, Pattern6Offset);
			var take = CalculateTakePrice(isLong: false, Pattern6TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterShort(candle, stop.Value, take.Value);
				_pattern6ShortReady = false;
				_pattern6ShortCounter = 0;
				_pattern6ShortBlocked = false;
				_shortPartialStage = 0;
			}
		}

		if (macdCurr > Pattern6MinThreshold)
			_pattern6LongBlocked = false;

		if (macdCurr < Pattern6MinThreshold && _pattern6LongCounter <= Pattern6MaxBars && !_pattern6LongBlocked)
			_pattern6LongCounter++;

		if (_pattern6LongCounter > Pattern6MaxBars)
		{
			_pattern6LongCounter = 0;
			_pattern6LongBlocked = true;
		}

		if (_pattern6LongCounter < Pattern6MinBars && macdCurr > Pattern6MinThreshold)
			_pattern6LongCounter = 0;

		if (macdCurr > Pattern6MinThreshold && _pattern6LongCounter > Pattern6CountBars)
			_pattern6LongReady = true;

		if (_pattern6LongReady && Position <= 0)
		{
			var stop = CalculateStopPrice(isLong: true, Pattern6StopLossBars, Pattern6Offset);
			var take = CalculateTakePrice(isLong: true, Pattern6TakeProfitBars);
			if (stop.HasValue && take.HasValue)
			{
				EnterLong(candle, stop.Value, take.Value);
				_pattern6LongReady = false;
				_pattern6LongCounter = 0;
				_pattern6LongBlocked = false;
				_longPartialStage = 0;
			}
		}
	}

	private void EnterLong(ICandleMessage candle, decimal stopPrice, decimal takePrice)
	{
		var closeShortVolume = Position < 0 ? Math.Abs(Position) : 0m;
		if (closeShortVolume > 0m && _entryDirection < 0)
			RegisterClose(closeShortVolume, candle.ClosePrice);

		var newVolume = _currentVolume;
		var totalVolume = newVolume + closeShortVolume;
		if (totalVolume <= 0m)
			return;

		BuyMarket(totalVolume);

		_entryDirection = 1;
		_entryPrice = candle.ClosePrice;
		_openVolume = newVolume;
		_realizedPnL = 0m;
		_currentStopLoss = stopPrice;
		_currentTakeProfit = takePrice;
		_longPartialStage = 0;
		_shortPartialStage = 0;
	}

	private void EnterShort(ICandleMessage candle, decimal stopPrice, decimal takePrice)
	{
		var closeLongVolume = Position > 0 ? Position : 0m;
		if (closeLongVolume > 0m && _entryDirection > 0)
			RegisterClose(closeLongVolume, candle.ClosePrice);

		var newVolume = _currentVolume;
		var totalVolume = newVolume + closeLongVolume;
		if (totalVolume <= 0m)
			return;

		SellMarket(totalVolume);

		_entryDirection = -1;
		_entryPrice = candle.ClosePrice;
		_openVolume = newVolume;
		_realizedPnL = 0m;
		_currentStopLoss = stopPrice;
		_currentTakeProfit = takePrice;
		_longPartialStage = 0;
		_shortPartialStage = 0;
		_barsBup = 0;
	}

	private void ManageActivePosition(ICandleMessage candle, decimal ema1Prev, decimal ema2Prev, decimal smaPrev, decimal ema3Prev)
	{
		if (_entryDirection == 0 || _openVolume <= 0m)
			return;

		var previousCandle = GetCandle(1);
		if (previousCandle is null)
			return;

		var profit = CalculateOpenProfit(candle.ClosePrice);
		if (_entryDirection > 0)
		{
			if (profit > ProfitThreshold && previousCandle.ClosePrice > ema2Prev && _longPartialStage == 0)
			{
				var volume = NormalizeVolume(_openVolume / 3m);
				if (volume > 0m)
				{
					SellMarket(volume);
					RegisterClose(volume, candle.ClosePrice);
					_longPartialStage = 1;
				}
			}
			else if (profit > ProfitThreshold && previousCandle.HighPrice > (smaPrev + ema3Prev) / 2m && _longPartialStage == 1)
			{
				var volume = NormalizeVolume(_openVolume / 2m);
				if (volume > 0m)
				{
					SellMarket(volume);
					RegisterClose(volume, candle.ClosePrice);
					_longPartialStage = 2;
				}
			}
		}
		else if (_entryDirection < 0)
		{
			if (profit > ProfitThreshold && previousCandle.ClosePrice < ema2Prev && _shortPartialStage == 0)
			{
				var volume = NormalizeVolume(_openVolume / 3m);
				if (volume > 0m)
				{
					BuyMarket(volume);
					RegisterClose(volume, candle.ClosePrice);
					_shortPartialStage = 1;
				}
			}
			else if (profit > ProfitThreshold && previousCandle.LowPrice < (smaPrev + ema3Prev) / 2m && _shortPartialStage == 1)
			{
				var volume = NormalizeVolume(_openVolume / 2m);
				if (volume > 0m)
				{
					BuyMarket(volume);
					RegisterClose(volume, candle.ClosePrice);
					_shortPartialStage = 2;
				}
			}
		}
	}

	private bool CheckRiskManagement(ICandleMessage candle)
	{
		if (_entryDirection == 0 || _openVolume <= 0m)
			return false;

		if (_entryDirection > 0)
		{
			if (_currentStopLoss.HasValue && candle.LowPrice <= _currentStopLoss.Value)
			{
				SellMarket(_openVolume);
				RegisterClose(_openVolume, _currentStopLoss.Value);
				return true;
			}

			if (_currentTakeProfit.HasValue && candle.HighPrice >= _currentTakeProfit.Value)
			{
				SellMarket(_openVolume);
				RegisterClose(_openVolume, _currentTakeProfit.Value);
				return true;
			}
		}
		else if (_entryDirection < 0)
		{
			if (_currentStopLoss.HasValue && candle.HighPrice >= _currentStopLoss.Value)
			{
				BuyMarket(_openVolume);
				RegisterClose(_openVolume, _currentStopLoss.Value);
				return true;
			}

			if (_currentTakeProfit.HasValue && candle.LowPrice <= _currentTakeProfit.Value)
			{
				BuyMarket(_openVolume);
				RegisterClose(_openVolume, _currentTakeProfit.Value);
				return true;
			}
		}

		return false;
	}

	private void RegisterClose(decimal volume, decimal executionPrice)
	{
		if (_entryDirection == 0 || volume <= 0m)
			return;

		var actualVolume = Math.Min(volume, _openVolume);
		if (actualVolume <= 0m)
			return;

		var profit = CalculateProfit(executionPrice, actualVolume);
		_realizedPnL += profit;
		_openVolume -= actualVolume;

		if (_openVolume <= 0m)
			CompleteTrade();
	}

	private decimal CalculateOpenProfit(decimal price)
	{
		if (_entryDirection == 0 || _openVolume <= 0m)
			return 0m;

		var diff = (price - _entryPrice) * _entryDirection;
		if (_pointSize == 0m)
			return diff * _openVolume;

		return diff / _pointSize * _pointValue * _openVolume;
	}

	private decimal CalculateProfit(decimal exitPrice, decimal volume)
	{
		var diff = (exitPrice - _entryPrice) * _entryDirection;
		if (_pointSize == 0m)
			return diff * volume;

		return diff / _pointSize * _pointValue * volume;
	}

	private void CompleteTrade()
	{
		if (UseMartingale && _realizedPnL < 0m)
			_currentVolume *= 2m;
		else
			_currentVolume = LotSize;

		_entryDirection = 0;
		_entryPrice = 0m;
		_openVolume = 0m;
		_realizedPnL = 0m;
		_currentStopLoss = null;
		_currentTakeProfit = null;
		_longPartialStage = 0;
		_shortPartialStage = 0;
	}

	private void UpdatePreviousIndicators(decimal ema1, decimal ema2, decimal sma, decimal ema3)
	{
		_ema1Prev = ema1;
		_ema2Prev = ema2;
		_smaPrev = sma;
		_ema3Prev = ema3;
	}

	private static bool TryGetMacdSeries(ref decimal? prev1, ref decimal? prev2, ref decimal? prev3, decimal current, out decimal macdCurr, out decimal macdLast, out decimal macdLast3)
	{
		macdCurr = 0m;
		macdLast = 0m;
		macdLast3 = 0m;

		if (!prev1.HasValue || !prev2.HasValue || !prev3.HasValue)
		{
			prev3 = prev2;
			prev2 = prev1;
			prev1 = current;
			return false;
		}

		macdCurr = prev1.Value;
		macdLast = prev2.Value;
		macdLast3 = prev3.Value;

		prev3 = prev2;
		prev2 = prev1;
		prev1 = current;
		return true;
	}

	private static decimal NormalizeVolume(decimal volume)
	{
		var normalized = Math.Round(volume, 2, MidpointRounding.AwayFromZero);
		return normalized < MinPartialVolume ? MinPartialVolume : normalized;
	}

	private decimal? CalculateStopPrice(bool isLong, int stopBars, int offsetPoints)
	{
		if (stopBars <= 0)
			return null;

		var offset = offsetPoints * _pointSize;

		if (isLong)
		{
			var lowest = GetLowestLow(stopBars);
			return lowest?.Minus(offset);
		}
		else
		{
			var highest = GetHighestHigh(stopBars);
			return highest?.Plus(offset);
		}
	}

	private decimal? CalculateTakePrice(bool isLong, int takeBars)
	{
		if (takeBars <= 0)
			return null;

		return GetChunkExtreme(isLong, takeBars, 0);
	}

	private decimal? GetChunkExtreme(bool isLong, int length, int offset)
	{
		var startIndex = _history.Count - 1 - offset;
		var endIndex = startIndex - (length - 1);
		if (startIndex < 0 || endIndex < 0)
			return null;

		decimal extreme = isLong ? decimal.MinValue : decimal.MaxValue;
		for (var i = startIndex; i >= endIndex; i--)
		{
			var candle = _history[i];
			var value = isLong ? candle.HighPrice : candle.LowPrice;

			if (isLong)
			{
				if (value > extreme)
					extreme = value;
			}
			else
			{
				if (value < extreme)
					extreme = value;
			}
		}

		var nextOffset = offset + length;
		var nextExtreme = GetChunkExtreme(isLong, length, nextOffset);
		if (nextExtreme.HasValue)
		{
			if (isLong)
			{
				if (nextExtreme.Value > extreme)
					return nextExtreme;
			}
			else
			{
				if (nextExtreme.Value < extreme)
					return nextExtreme;
			}
		}

		return extreme;
	}

	private decimal? GetHighestHigh(int bars)
	{
		if (bars <= 0 || _history.Count == 0)
			return null;

		decimal? result = null;
		var end = Math.Max(0, _history.Count - bars);
		for (var i = _history.Count - 1; i >= end; i--)
		{
			var value = _history[i].HighPrice;
			if (result is null || value > result.Value)
				result = value;
		}

		return result;
	}

	private decimal? GetLowestLow(int bars)
	{
		if (bars <= 0 || _history.Count == 0)
			return null;

		decimal? result = null;
		var end = Math.Max(0, _history.Count - bars);
		for (var i = _history.Count - 1; i >= end; i--)
		{
			var value = _history[i].LowPrice;
			if (result is null || value < result.Value)
				result = value;
		}

		return result;
	}

	private ICandleMessage? GetCandle(int shift)
	{
		var index = _history.Count - 1 - shift;
		return index >= 0 ? _history[index] : null;
	}

	private bool IsInSession(TimeSpan time)
	{
		var start = SessionStart;
		var end = SessionEnd;
		return start <= end ? time >= start && time <= end : time >= start || time <= end;
	}
}
