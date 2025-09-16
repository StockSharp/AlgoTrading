using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the "MacdPatternTraderAll" expert advisor.
/// Implements six independent MACD based entry patterns, partial exits,
/// adaptive stop-loss and take-profit levels, martingale position sizing and intraday time filtering.
/// </summary>
public class MacdPatternTraderStrategy : Strategy
{
	private const int MacdHistoryLength = 3;
	private const int CandleHistoryLimit = 1000;
	private const decimal MinPartialVolume = 0.01m;
	private const decimal ProfitThreshold = 5m;

	private readonly StrategyParam<bool> _pattern1Enabled;
	private readonly StrategyParam<int> _pattern1StopLossBars;
	private readonly StrategyParam<int> _pattern1TakeProfitBars;
	private readonly StrategyParam<int> _pattern1Offset;
	private readonly StrategyParam<int> _pattern1Slow;
	private readonly StrategyParam<int> _pattern1Fast;
	private readonly StrategyParam<decimal> _pattern1MaxThreshold;
	private readonly StrategyParam<decimal> _pattern1MinThreshold;

	private readonly StrategyParam<bool> _pattern2Enabled;
	private readonly StrategyParam<int> _pattern2StopLossBars;
	private readonly StrategyParam<int> _pattern2TakeProfitBars;
	private readonly StrategyParam<int> _pattern2Offset;
	private readonly StrategyParam<int> _pattern2Slow;
	private readonly StrategyParam<int> _pattern2Fast;
	private readonly StrategyParam<decimal> _pattern2MaxThreshold;
	private readonly StrategyParam<decimal> _pattern2MinThreshold;

	private readonly StrategyParam<bool> _pattern3Enabled;
	private readonly StrategyParam<int> _pattern3StopLossBars;
	private readonly StrategyParam<int> _pattern3TakeProfitBars;
	private readonly StrategyParam<int> _pattern3Offset;
	private readonly StrategyParam<int> _pattern3Slow;
	private readonly StrategyParam<int> _pattern3Fast;
	private readonly StrategyParam<decimal> _pattern3MaxThreshold;
	private readonly StrategyParam<decimal> _pattern3MaxLowThreshold;
	private readonly StrategyParam<decimal> _pattern3MinThreshold;
	private readonly StrategyParam<decimal> _pattern3MinHighThreshold;

	private readonly StrategyParam<bool> _pattern4Enabled;
	private readonly StrategyParam<int> _pattern4StopLossBars;
	private readonly StrategyParam<int> _pattern4TakeProfitBars;
	private readonly StrategyParam<int> _pattern4Offset;
	private readonly StrategyParam<int> _pattern4Slow;
	private readonly StrategyParam<int> _pattern4Fast;
	private readonly StrategyParam<int> _pattern4AdditionalBars;
	private readonly StrategyParam<decimal> _pattern4MaxThreshold;
	private readonly StrategyParam<decimal> _pattern4MaxLowThreshold;
	private readonly StrategyParam<decimal> _pattern4MinThreshold;
	private readonly StrategyParam<decimal> _pattern4MinHighThreshold;

	private readonly StrategyParam<bool> _pattern5Enabled;
	private readonly StrategyParam<int> _pattern5StopLossBars;
	private readonly StrategyParam<int> _pattern5TakeProfitBars;
	private readonly StrategyParam<int> _pattern5Offset;
	private readonly StrategyParam<int> _pattern5Slow;
	private readonly StrategyParam<int> _pattern5Fast;
	private readonly StrategyParam<decimal> _pattern5MaxNeutralThreshold;
	private readonly StrategyParam<decimal> _pattern5MaxThreshold;
	private readonly StrategyParam<decimal> _pattern5MinNeutralThreshold;
	private readonly StrategyParam<decimal> _pattern5MinThreshold;

	private readonly StrategyParam<bool> _pattern6Enabled;
	private readonly StrategyParam<int> _pattern6StopLossBars;
	private readonly StrategyParam<int> _pattern6TakeProfitBars;
	private readonly StrategyParam<int> _pattern6Offset;
	private readonly StrategyParam<int> _pattern6Slow;
	private readonly StrategyParam<int> _pattern6Fast;
	private readonly StrategyParam<decimal> _pattern6MaxThreshold;
	private readonly StrategyParam<decimal> _pattern6MinThreshold;
	private readonly StrategyParam<int> _pattern6MaxBars;
	private readonly StrategyParam<int> _pattern6MinBars;
	private readonly StrategyParam<int> _pattern6TriggerBars;

	private readonly StrategyParam<int> _emaPeriod1;
	private readonly StrategyParam<int> _emaPeriod2;
	private readonly StrategyParam<int> _smaPeriod3;
	private readonly StrategyParam<int> _emaPeriod4;

	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd1 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd2 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd3 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd4 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd5 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd6 = null!;
	private ExponentialMovingAverage _ema1 = null!;
	private ExponentialMovingAverage _ema2 = null!;
	private SimpleMovingAverage _sma3 = null!;
	private ExponentialMovingAverage _ema4 = null!;

	private readonly List<ICandleMessage> _candles = new();
	private readonly List<decimal> _macd1History = new();
	private readonly List<decimal> _macd2History = new();
	private readonly List<decimal> _macd3History = new();
	private readonly List<decimal> _macd4History = new();
	private readonly List<decimal> _macd5History = new();
	private readonly List<decimal> _macd6History = new();

	private bool _pattern1WasAbove;
	private bool _pattern1WasBelow;

	private bool _pattern2WasPositive;
	private bool _pattern2WasNegative;
	private bool _pattern2SellArmed;
	private bool _pattern2BuyArmed;

	private int _pattern3BarsBup;

	private int _pattern6BarsAbove;
	private int _pattern6BarsBelow;
	private bool _pattern6SellBlocked;
	private bool _pattern6BuyBlocked;
	private bool _pattern6SellReady;
	private bool _pattern6BuyReady;

	private decimal _currentVolume;
	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal _cycleRealizedPnL;
	private int _longPartialCount;
	private int _shortPartialCount;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private bool _pattern2WasPositive;
	private bool _pattern2WasNegative;
	private bool _pattern2SellArmed;
	private bool _pattern2BuyArmed;

	private int _pattern3BarsBup;

	private int _pattern6BarsAbove;
	private int _pattern6BarsBelow;
	private bool _pattern6SellBlocked;
	private bool _pattern6BuyBlocked;
	private bool _pattern6SellReady;
	private bool _pattern6BuyReady;

	private decimal _currentVolume;
	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal _cycleRealizedPnL;
	private int _longPartialCount;
	private int _shortPartialCount;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Enable or disable pattern #1.
	/// </summary>
	public bool Pattern1Enabled
	{
		get => _pattern1Enabled.Value;
		set => _pattern1Enabled.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #1 stop-loss search.
	/// </summary>
	public int Pattern1StopLossBars
	{
		get => _pattern1StopLossBars.Value;
		set => _pattern1StopLossBars.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #1 take-profit search.
	/// </summary>
	public int Pattern1TakeProfitBars
	{
		get => _pattern1TakeProfitBars.Value;
		set => _pattern1TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Offset used when calculating the stop-loss for pattern #1.
	/// </summary>
	public int Pattern1Offset
	{
		get => _pattern1Offset.Value;
		set => _pattern1Offset.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern #1 MACD.
	/// </summary>
	public int Pattern1Slow
	{
		get => _pattern1Slow.Value;
		set => _pattern1Slow.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern #1 MACD.
	/// </summary>
	public int Pattern1Fast
	{
		get => _pattern1Fast.Value;
		set => _pattern1Fast.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold for pattern #1.
	/// </summary>
	public decimal Pattern1MaxThreshold
	{
		get => _pattern1MaxThreshold.Value;
		set => _pattern1MaxThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold for pattern #1.
	/// </summary>
	public decimal Pattern1MinThreshold
	{
		get => _pattern1MinThreshold.Value;
		set => _pattern1MinThreshold.Value = value;
	}

	/// <summary>
	/// Enable or disable pattern #2.
	/// </summary>
	public bool Pattern2Enabled
	{
		get => _pattern2Enabled.Value;
		set => _pattern2Enabled.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #2 stop-loss search.
	/// </summary>
	public int Pattern2StopLossBars
	{
		get => _pattern2StopLossBars.Value;
		set => _pattern2StopLossBars.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #2 take-profit search.
	/// </summary>
	public int Pattern2TakeProfitBars
	{
		get => _pattern2TakeProfitBars.Value;
		set => _pattern2TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Offset used when calculating the stop-loss for pattern #2.
	/// </summary>
	public int Pattern2Offset
	{
		get => _pattern2Offset.Value;
		set => _pattern2Offset.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern #2 MACD.
	/// </summary>
	public int Pattern2Slow
	{
		get => _pattern2Slow.Value;
		set => _pattern2Slow.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern #2 MACD.
	/// </summary>
	public int Pattern2Fast
	{
		get => _pattern2Fast.Value;
		set => _pattern2Fast.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold for pattern #2.
	/// </summary>
	public decimal Pattern2MaxThreshold
	{
		get => _pattern2MaxThreshold.Value;
		set => _pattern2MaxThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold for pattern #2.
	/// </summary>
	public decimal Pattern2MinThreshold
	{
		get => _pattern2MinThreshold.Value;
		set => _pattern2MinThreshold.Value = value;
	}

	/// <summary>
	/// Enable or disable pattern #3.
	/// </summary>
	public bool Pattern3Enabled
	{
		get => _pattern3Enabled.Value;
		set => _pattern3Enabled.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #3 stop-loss search.
	/// </summary>
	public int Pattern3StopLossBars
	{
		get => _pattern3StopLossBars.Value;
		set => _pattern3StopLossBars.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #3 take-profit search.
	/// </summary>
	public int Pattern3TakeProfitBars
	{
		get => _pattern3TakeProfitBars.Value;
		set => _pattern3TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Offset used when calculating the stop-loss for pattern #3.
	/// </summary>
	public int Pattern3Offset
	{
		get => _pattern3Offset.Value;
		set => _pattern3Offset.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern #3 MACD.
	/// </summary>
	public int Pattern3Slow
	{
		get => _pattern3Slow.Value;
		set => _pattern3Slow.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern #3 MACD.
	/// </summary>
	public int Pattern3Fast
	{
		get => _pattern3Fast.Value;
		set => _pattern3Fast.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold for pattern #3 trend detection.
	/// </summary>
	public decimal Pattern3MaxThreshold
	{
		get => _pattern3MaxThreshold.Value;
		set => _pattern3MaxThreshold.Value = value;
	}

	/// <summary>
	/// Secondary upper MACD threshold for pattern #3.
	/// </summary>
	public decimal Pattern3MaxLowThreshold
	{
		get => _pattern3MaxLowThreshold.Value;
		set => _pattern3MaxLowThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold for pattern #3.
	/// </summary>
	public decimal Pattern3MinThreshold
	{
		get => _pattern3MinThreshold.Value;
		set => _pattern3MinThreshold.Value = value;
	}

	/// <summary>
	/// Secondary lower MACD threshold for pattern #3.
	/// </summary>
	public decimal Pattern3MinHighThreshold
	{
		get => _pattern3MinHighThreshold.Value;
		set => _pattern3MinHighThreshold.Value = value;
	}

	/// <summary>
	/// Enable or disable pattern #4.
	/// </summary>
	public bool Pattern4Enabled
	{
		get => _pattern4Enabled.Value;
		set => _pattern4Enabled.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #4 stop-loss search.
	/// </summary>
	public int Pattern4StopLossBars
	{
		get => _pattern4StopLossBars.Value;
		set => _pattern4StopLossBars.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #4 take-profit search.
	/// </summary>
	public int Pattern4TakeProfitBars
	{
		get => _pattern4TakeProfitBars.Value;
		set => _pattern4TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Offset used when calculating the stop-loss for pattern #4.
	/// </summary>
	public int Pattern4Offset
	{
		get => _pattern4Offset.Value;
		set => _pattern4Offset.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern #4 MACD.
	/// </summary>
	public int Pattern4Slow
	{
		get => _pattern4Slow.Value;
		set => _pattern4Slow.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern #4 MACD.
	/// </summary>
	public int Pattern4Fast
	{
		get => _pattern4Fast.Value;
		set => _pattern4Fast.Value = value;
	}

	/// <summary>
	/// Additional bar counter for pattern #4 (kept for compatibility).
	/// </summary>
	public int Pattern4AdditionalBars
	{
		get => _pattern4AdditionalBars.Value;
		set => _pattern4AdditionalBars.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold for pattern #4.
	/// </summary>
	public decimal Pattern4MaxThreshold
	{
		get => _pattern4MaxThreshold.Value;
		set => _pattern4MaxThreshold.Value = value;
	}

	/// <summary>
	/// Secondary upper MACD threshold for pattern #4.
	/// </summary>
	public decimal Pattern4MaxLowThreshold
	{
		get => _pattern4MaxLowThreshold.Value;
		set => _pattern4MaxLowThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold for pattern #4.
	/// </summary>
	public decimal Pattern4MinThreshold
	{
		get => _pattern4MinThreshold.Value;
		set => _pattern4MinThreshold.Value = value;
	}

	/// <summary>
	/// Secondary lower MACD threshold for pattern #4.
	/// </summary>
	public decimal Pattern4MinHighThreshold
	{
		get => _pattern4MinHighThreshold.Value;
		set => _pattern4MinHighThreshold.Value = value;
	}

	/// <summary>
	/// Enable or disable pattern #5.
	/// </summary>
	public bool Pattern5Enabled
	{
		get => _pattern5Enabled.Value;
		set => _pattern5Enabled.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #5 stop-loss search.
	/// </summary>
	public int Pattern5StopLossBars
	{
		get => _pattern5StopLossBars.Value;
		set => _pattern5StopLossBars.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #5 take-profit search.
	/// </summary>
	public int Pattern5TakeProfitBars
	{
		get => _pattern5TakeProfitBars.Value;
		set => _pattern5TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Offset used when calculating the stop-loss for pattern #5.
	/// </summary>
	public int Pattern5Offset
	{
		get => _pattern5Offset.Value;
		set => _pattern5Offset.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern #5 MACD.
	/// </summary>
	public int Pattern5Slow
	{
		get => _pattern5Slow.Value;
		set => _pattern5Slow.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern #5 MACD.
	/// </summary>
	public int Pattern5Fast
	{
		get => _pattern5Fast.Value;
		set => _pattern5Fast.Value = value;
	}

	/// <summary>
	/// Neutral upper MACD threshold for pattern #5.
	/// </summary>
	public decimal Pattern5MaxNeutralThreshold
	{
		get => _pattern5MaxNeutralThreshold.Value;
		set => _pattern5MaxNeutralThreshold.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold for pattern #5.
	/// </summary>
	public decimal Pattern5MaxThreshold
	{
		get => _pattern5MaxThreshold.Value;
		set => _pattern5MaxThreshold.Value = value;
	}

	/// <summary>
	/// Neutral lower MACD threshold for pattern #5.
	/// </summary>
	public decimal Pattern5MinNeutralThreshold
	{
		get => _pattern5MinNeutralThreshold.Value;
		set => _pattern5MinNeutralThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold for pattern #5.
	/// </summary>
	public decimal Pattern5MinThreshold
	{
		get => _pattern5MinThreshold.Value;
		set => _pattern5MinThreshold.Value = value;
	}

	/// <summary>
	/// Enable or disable pattern #6.
	/// </summary>
	public bool Pattern6Enabled
	{
		get => _pattern6Enabled.Value;
		set => _pattern6Enabled.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #6 stop-loss search.
	/// </summary>
	public int Pattern6StopLossBars
	{
		get => _pattern6StopLossBars.Value;
		set => _pattern6StopLossBars.Value = value;
	}

	/// <summary>
	/// Number of bars for the pattern #6 take-profit search.
	/// </summary>
	public int Pattern6TakeProfitBars
	{
		get => _pattern6TakeProfitBars.Value;
		set => _pattern6TakeProfitBars.Value = value;
	}

	/// <summary>
	/// Offset used when calculating the stop-loss for pattern #6.
	/// </summary>
	public int Pattern6Offset
	{
		get => _pattern6Offset.Value;
		set => _pattern6Offset.Value = value;
	}

	/// <summary>
	/// Slow EMA length for pattern #6 MACD.
	/// </summary>
	public int Pattern6Slow
	{
		get => _pattern6Slow.Value;
		set => _pattern6Slow.Value = value;
	}

	/// <summary>
	/// Fast EMA length for pattern #6 MACD.
	/// </summary>
	public int Pattern6Fast
	{
		get => _pattern6Fast.Value;
		set => _pattern6Fast.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold for pattern #6.
	/// </summary>
	public decimal Pattern6MaxThreshold
	{
		get => _pattern6MaxThreshold.Value;
		set => _pattern6MaxThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold for pattern #6.
	/// </summary>
	public decimal Pattern6MinThreshold
	{
		get => _pattern6MinThreshold.Value;
		set => _pattern6MinThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of bars above/below the threshold before blocking pattern #6 signals.
	/// </summary>
	public int Pattern6MaxBars
	{
		get => _pattern6MaxBars.Value;
		set => _pattern6MaxBars.Value = value;
	}

	/// <summary>
	/// Minimum number of bars above/below the threshold before pattern #6 can reset.
	/// </summary>
	public int Pattern6MinBars
	{
		get => _pattern6MinBars.Value;
		set => _pattern6MinBars.Value = value;
	}

	/// <summary>
	/// Number of bars required before pattern #6 can trigger after crossing the threshold.
	/// </summary>
	public int Pattern6TriggerBars
	{
		get => _pattern6TriggerBars.Value;
		set => _pattern6TriggerBars.Value = value;
	}

	/// <summary>
	/// EMA period used by the position manager (first EMA).
	/// </summary>
	public int EmaPeriod1
	{
		get => _emaPeriod1.Value;
		set => _emaPeriod1.Value = value;
	}

	/// <summary>
	/// EMA period used by the position manager (second EMA).
	/// </summary>
	public int EmaPeriod2
	{
		get => _emaPeriod2.Value;
		set => _emaPeriod2.Value = value;
	}

	/// <summary>
	/// SMA period used by the position manager.
	/// </summary>
	public int SmaPeriod3
	{
		get => _smaPeriod3.Value;
		set => _smaPeriod3.Value = value;
	}

	/// <summary>
	/// EMA period used in the hybrid average for the position manager.
	/// </summary>
	public int EmaPeriod4
	{
		get => _emaPeriod4.Value;
		set => _emaPeriod4.Value = value;
	}

	/// <summary>
	/// Base volume for new entries.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Flag indicating whether the trading window filter is active.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Start time of the trading window.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End time of the trading window.
	/// </summary>
	public TimeSpan StopTime
	{
		get => _stopTime.Value;
		set => _stopTime.Value = value;
	}

	/// <summary>
	/// Enable or disable martingale volume adjustment.
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

	/// <summary>
	/// Constructor.
	/// </summary>
	public MacdPatternTraderStrategy()
	{
		_pattern1Enabled = Param(nameof(Pattern1Enabled), true)
			.SetDisplay("Pattern 1", "Enable first MACD pattern", "Patterns");
		_pattern1StopLossBars = Param(nameof(Pattern1StopLossBars), 22)
			.SetGreaterThanZero()
			.SetDisplay("P1 Stop Bars", "Bars used for stop-loss search", "Pattern 1");
		_pattern1TakeProfitBars = Param(nameof(Pattern1TakeProfitBars), 32)
			.SetGreaterThanZero()
			.SetDisplay("P1 Take Bars", "Bars used for take-profit search", "Pattern 1");
		_pattern1Offset = Param(nameof(Pattern1Offset), 40)
			.SetGreaterThanZero()
			.SetDisplay("P1 Offset", "Stop-loss offset in points", "Pattern 1");
		_pattern1Slow = Param(nameof(Pattern1Slow), 13)
			.SetGreaterThanZero()
			.SetDisplay("P1 Slow EMA", "Slow EMA length for MACD", "Pattern 1");
		_pattern1Fast = Param(nameof(Pattern1Fast), 24)
			.SetGreaterThanZero()
			.SetDisplay("P1 Fast EMA", "Fast EMA length for MACD", "Pattern 1");
		_pattern1MaxThreshold = Param(nameof(Pattern1MaxThreshold), 0.0095m)
			.SetDisplay("P1 Max", "Upper MACD threshold", "Pattern 1");
		_pattern1MinThreshold = Param(nameof(Pattern1MinThreshold), -0.0045m)
			.SetDisplay("P1 Min", "Lower MACD threshold", "Pattern 1");

		_pattern2Enabled = Param(nameof(Pattern2Enabled), true)
			.SetDisplay("Pattern 2", "Enable second MACD pattern", "Patterns");
		_pattern2StopLossBars = Param(nameof(Pattern2StopLossBars), 2)
			.SetGreaterThanZero()
			.SetDisplay("P2 Stop Bars", "Bars used for stop-loss search", "Pattern 2");
		_pattern2TakeProfitBars = Param(nameof(Pattern2TakeProfitBars), 2)
			.SetGreaterThanZero()
			.SetDisplay("P2 Take Bars", "Bars used for take-profit search", "Pattern 2");
		_pattern2Offset = Param(nameof(Pattern2Offset), 50)
			.SetGreaterThanZero()
			.SetDisplay("P2 Offset", "Stop-loss offset in points", "Pattern 2");
		_pattern2Slow = Param(nameof(Pattern2Slow), 7)
			.SetGreaterThanZero()
			.SetDisplay("P2 Slow EMA", "Slow EMA length for MACD", "Pattern 2");
		_pattern2Fast = Param(nameof(Pattern2Fast), 17)
			.SetGreaterThanZero()
			.SetDisplay("P2 Fast EMA", "Fast EMA length for MACD", "Pattern 2");
		_pattern2MaxThreshold = Param(nameof(Pattern2MaxThreshold), 0.0045m)
			.SetDisplay("P2 Max", "Upper MACD threshold", "Pattern 2");
		_pattern2MinThreshold = Param(nameof(Pattern2MinThreshold), -0.0035m)
			.SetDisplay("P2 Min", "Lower MACD threshold", "Pattern 2");

		_pattern3Enabled = Param(nameof(Pattern3Enabled), true)
			.SetDisplay("Pattern 3", "Enable third MACD pattern", "Patterns");
		_pattern3StopLossBars = Param(nameof(Pattern3StopLossBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("P3 Stop Bars", "Bars used for stop-loss search", "Pattern 3");
		_pattern3TakeProfitBars = Param(nameof(Pattern3TakeProfitBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("P3 Take Bars", "Bars used for take-profit search", "Pattern 3");
		_pattern3Offset = Param(nameof(Pattern3Offset), 2)
			.SetGreaterThanZero()
			.SetDisplay("P3 Offset", "Stop-loss offset in points", "Pattern 3");
		_pattern3Slow = Param(nameof(Pattern3Slow), 2)
			.SetGreaterThanZero()
			.SetDisplay("P3 Slow EMA", "Slow EMA length for MACD", "Pattern 3");
		_pattern3Fast = Param(nameof(Pattern3Fast), 32)
			.SetGreaterThanZero()
			.SetDisplay("P3 Fast EMA", "Fast EMA length for MACD", "Pattern 3");
		_pattern3MaxThreshold = Param(nameof(Pattern3MaxThreshold), 0.0015m)
			.SetDisplay("P3 Max", "Primary upper MACD threshold", "Pattern 3");
		_pattern3MaxLowThreshold = Param(nameof(Pattern3MaxLowThreshold), 0.0040m)
			.SetDisplay("P3 Max Low", "Secondary upper MACD threshold", "Pattern 3");
		_pattern3MinThreshold = Param(nameof(Pattern3MinThreshold), -0.0050m)
			.SetDisplay("P3 Min", "Primary lower MACD threshold", "Pattern 3");
		_pattern3MinHighThreshold = Param(nameof(Pattern3MinHighThreshold), -0.0005m)
			.SetDisplay("P3 Min High", "Secondary lower MACD threshold", "Pattern 3");

		_pattern4Enabled = Param(nameof(Pattern4Enabled), true)
			.SetDisplay("Pattern 4", "Enable fourth MACD pattern", "Patterns");
		_pattern4StopLossBars = Param(nameof(Pattern4StopLossBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("P4 Stop Bars", "Bars used for stop-loss search", "Pattern 4");
		_pattern4TakeProfitBars = Param(nameof(Pattern4TakeProfitBars), 32)
			.SetGreaterThanZero()
			.SetDisplay("P4 Take Bars", "Bars used for take-profit search", "Pattern 4");
		_pattern4Offset = Param(nameof(Pattern4Offset), 45)
			.SetGreaterThanZero()
			.SetDisplay("P4 Offset", "Stop-loss offset in points", "Pattern 4");
		_pattern4Slow = Param(nameof(Pattern4Slow), 9)
			.SetGreaterThanZero()
			.SetDisplay("P4 Slow EMA", "Slow EMA length for MACD", "Pattern 4");
		_pattern4Fast = Param(nameof(Pattern4Fast), 4)
			.SetGreaterThanZero()
			.SetDisplay("P4 Fast EMA", "Fast EMA length for MACD", "Pattern 4");
		_pattern4AdditionalBars = Param(nameof(Pattern4AdditionalBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("P4 Extra Bars", "Compatibility counter, kept for completeness", "Pattern 4");
		_pattern4MaxThreshold = Param(nameof(Pattern4MaxThreshold), 0.0165m)
			.SetDisplay("P4 Max", "Primary upper MACD threshold", "Pattern 4");
		_pattern4MaxLowThreshold = Param(nameof(Pattern4MaxLowThreshold), 0.0001m)
			.SetDisplay("P4 Max Low", "Secondary upper MACD threshold", "Pattern 4");
		_pattern4MinThreshold = Param(nameof(Pattern4MinThreshold), -0.0005m)
			.SetDisplay("P4 Min", "Primary lower MACD threshold", "Pattern 4");
		_pattern4MinHighThreshold = Param(nameof(Pattern4MinHighThreshold), -0.0006m)
			.SetDisplay("P4 Min High", "Secondary lower MACD threshold", "Pattern 4");

		_pattern5Enabled = Param(nameof(Pattern5Enabled), true)
			.SetDisplay("Pattern 5", "Enable fifth MACD pattern", "Patterns");
		_pattern5StopLossBars = Param(nameof(Pattern5StopLossBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("P5 Stop Bars", "Bars used for stop-loss search", "Pattern 5");
		_pattern5TakeProfitBars = Param(nameof(Pattern5TakeProfitBars), 47)
			.SetGreaterThanZero()
			.SetDisplay("P5 Take Bars", "Bars used for take-profit search", "Pattern 5");
		_pattern5Offset = Param(nameof(Pattern5Offset), 45)
			.SetGreaterThanZero()
			.SetDisplay("P5 Offset", "Stop-loss offset in points", "Pattern 5");
		_pattern5Slow = Param(nameof(Pattern5Slow), 2)
			.SetGreaterThanZero()
			.SetDisplay("P5 Slow EMA", "Slow EMA length for MACD", "Pattern 5");
		_pattern5Fast = Param(nameof(Pattern5Fast), 6)
			.SetGreaterThanZero()
			.SetDisplay("P5 Fast EMA", "Fast EMA length for MACD", "Pattern 5");
		_pattern5MaxNeutralThreshold = Param(nameof(Pattern5MaxNeutralThreshold), 0.0005m)
			.SetDisplay("P5 Neutral Max", "Neutral upper MACD threshold", "Pattern 5");
		_pattern5MaxThreshold = Param(nameof(Pattern5MaxThreshold), 0.0015m)
			.SetDisplay("P5 Max", "Upper MACD threshold", "Pattern 5");
		_pattern5MinNeutralThreshold = Param(nameof(Pattern5MinNeutralThreshold), -0.0005m)
			.SetDisplay("P5 Neutral Min", "Neutral lower MACD threshold", "Pattern 5");
		_pattern5MinThreshold = Param(nameof(Pattern5MinThreshold), -0.0030m)
			.SetDisplay("P5 Min", "Lower MACD threshold", "Pattern 5");

		_pattern6Enabled = Param(nameof(Pattern6Enabled), true)
			.SetDisplay("Pattern 6", "Enable sixth MACD pattern", "Patterns");
		_pattern6StopLossBars = Param(nameof(Pattern6StopLossBars), 26)
			.SetGreaterThanZero()
			.SetDisplay("P6 Stop Bars", "Bars used for stop-loss search", "Pattern 6");
		_pattern6TakeProfitBars = Param(nameof(Pattern6TakeProfitBars), 42)
			.SetGreaterThanZero()
			.SetDisplay("P6 Take Bars", "Bars used for take-profit search", "Pattern 6");
		_pattern6Offset = Param(nameof(Pattern6Offset), 20)
			.SetGreaterThanZero()
			.SetDisplay("P6 Offset", "Stop-loss offset in points", "Pattern 6");
		_pattern6Slow = Param(nameof(Pattern6Slow), 8)
			.SetGreaterThanZero()
			.SetDisplay("P6 Slow EMA", "Slow EMA length for MACD", "Pattern 6");
		_pattern6Fast = Param(nameof(Pattern6Fast), 4)
			.SetGreaterThanZero()
			.SetDisplay("P6 Fast EMA", "Fast EMA length for MACD", "Pattern 6");
		_pattern6MaxThreshold = Param(nameof(Pattern6MaxThreshold), 0.0005m)
			.SetDisplay("P6 Max", "Upper MACD threshold", "Pattern 6");
		_pattern6MinThreshold = Param(nameof(Pattern6MinThreshold), -0.0010m)
			.SetDisplay("P6 Min", "Lower MACD threshold", "Pattern 6");
		_pattern6MaxBars = Param(nameof(Pattern6MaxBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("P6 Max Bars", "Maximum bar counter above/below the threshold", "Pattern 6");
		_pattern6MinBars = Param(nameof(Pattern6MinBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("P6 Min Bars", "Minimum bar counter above/below the threshold", "Pattern 6");
		_pattern6TriggerBars = Param(nameof(Pattern6TriggerBars), 4)
			.SetGreaterThanZero()
			.SetDisplay("P6 Trigger Bars", "Required bars before triggering", "Pattern 6");

		_emaPeriod1 = Param(nameof(EmaPeriod1), 7)
			.SetGreaterThanZero()
			.SetDisplay("EMA 1", "First EMA length for position manager", "Management");
		_emaPeriod2 = Param(nameof(EmaPeriod2), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA 2", "Second EMA length for position manager", "Management");
		_smaPeriod3 = Param(nameof(SmaPeriod3), 98)
			.SetGreaterThanZero()
			.SetDisplay("SMA", "Simple MA length for position manager", "Management");
		_emaPeriod4 = Param(nameof(EmaPeriod4), 365)
			.SetGreaterThanZero()
			.SetDisplay("EMA 4", "Fourth EMA length for position manager", "Management");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Base volume for orders", "General");
		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Time Filter", "Enable intraday trading window", "General");
		_startTime = Param(nameof(StartTime), new TimeSpan(7, 0, 0))
			.SetDisplay("Start Time", "Trading window start", "General");
		_stopTime = Param(nameof(StopTime), new TimeSpan(17, 0, 0))
			.SetDisplay("Stop Time", "Trading window end", "General");
		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Martingale", "Enable martingale volume adjustment", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for analysis", "General");
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

		_candles.Clear();
		_macd1History.Clear();
		_macd2History.Clear();
		_macd3History.Clear();
		_macd4History.Clear();
		_macd5History.Clear();
		_macd6History.Clear();

		_pattern1WasAbove = false;
		_pattern1WasBelow = false;
		_pattern2WasPositive = false;
		_pattern2WasNegative = false;
		_pattern2SellArmed = false;
		_pattern2BuyArmed = false;
		_pattern3BarsBup = 0;
		_pattern6BarsAbove = 0;
		_pattern6BarsBelow = 0;
		_pattern6SellBlocked = false;
		_pattern6BuyBlocked = false;
		_pattern6SellReady = false;
		_pattern6BuyReady = false;

		_currentVolume = InitialVolume;
		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
		_cycleRealizedPnL = 0m;
		_longPartialCount = 0;
		_shortPartialCount = 0;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentVolume = InitialVolume;

		_macd1 = CreateMacd(Pattern1Fast, Pattern1Slow);
		_macd2 = CreateMacd(Pattern2Fast, Pattern2Slow);
		_macd3 = CreateMacd(Pattern3Fast, Pattern3Slow);
		_macd4 = CreateMacd(Pattern4Fast, Pattern4Slow);
		_macd5 = CreateMacd(Pattern5Fast, Pattern5Slow);
		_macd6 = CreateMacd(Pattern6Fast, Pattern6Slow);

		_ema1 = new ExponentialMovingAverage { Length = EmaPeriod1 };
		_ema2 = new ExponentialMovingAverage { Length = EmaPeriod2 };
		_sma3 = new SimpleMovingAverage { Length = SmaPeriod3 };
		_ema4 = new ExponentialMovingAverage { Length = EmaPeriod4 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd1, _macd2, _macd3, _macd4, _macd5, _macd6, _ema1, _ema2, _sma3, _ema4, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd1);
			DrawOwnTrades(area);
		}
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
		IIndicatorValue sma3Value,
		IIndicatorValue ema4Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(candle);
		TrimCandles();

		CheckRiskManagement(candle);

		if (!macd1Value.IsFinal || !macd2Value.IsFinal || !macd3Value.IsFinal ||
			!macd4Value.IsFinal || !macd5Value.IsFinal || !macd6Value.IsFinal)
		{
			UpdateMacdHistories(macd1Value, macd2Value, macd3Value, macd4Value, macd5Value, macd6Value);
			return;
		}

		if (macd1Value is not MovingAverageConvergenceDivergenceSignalValue macd1Signal ||
			macd2Value is not MovingAverageConvergenceDivergenceSignalValue macd2Signal ||
			macd3Value is not MovingAverageConvergenceDivergenceSignalValue macd3Signal ||
			macd4Value is not MovingAverageConvergenceDivergenceSignalValue macd4Signal ||
			macd5Value is not MovingAverageConvergenceDivergenceSignalValue macd5Signal ||
			macd6Value is not MovingAverageConvergenceDivergenceSignalValue macd6Signal)
		{
			UpdateMacdHistories(macd1Value, macd2Value, macd3Value, macd4Value, macd5Value, macd6Value);
			return;
		}

		if (macd1Signal.Macd is not decimal macd1Curr ||
			macd2Signal.Macd is not decimal macd2Curr ||
			macd3Signal.Macd is not decimal macd3Curr ||
			macd4Signal.Macd is not decimal macd4Curr ||
			macd5Signal.Macd is not decimal macd5Curr ||
			macd6Signal.Macd is not decimal macd6Curr)
		{
			UpdateMacdHistories(macd1Value, macd2Value, macd3Value, macd4Value, macd5Value, macd6Value);
			return;
		}

		if (!ema2Value.IsFormed || !sma3Value.IsFormed || !ema4Value.IsFormed)
		{
			UpdateMacdHistories(macd1Value, macd2Value, macd3Value, macd4Value, macd5Value, macd6Value);
			return;
		}

		var ema2 = ema2Value.GetValue<decimal>();
		var sma3 = sma3Value.GetValue<decimal>();
		var ema4 = ema4Value.GetValue<decimal>();

		ManageActivePositions(candle, ema2, sma3, ema4);

		var allowTrade = IsFormedAndOnlineAndAllowTrading();
		var timeOk = !UseTimeFilter || IsWithinTradingWindow(candle.OpenTime.TimeOfDay);

		var macd1Prev = GetPrevious(_macd1History, 1);
		var macd1Prev2 = GetPrevious(_macd1History, 2);
		var macd2Prev = GetPrevious(_macd2History, 1);
		var macd2Prev2 = GetPrevious(_macd2History, 2);
		var macd3Prev = GetPrevious(_macd3History, 1);
		var macd3Prev2 = GetPrevious(_macd3History, 2);
		var macd4Prev = GetPrevious(_macd4History, 1);
		var macd4Prev2 = GetPrevious(_macd4History, 2);
		var macd5Prev = GetPrevious(_macd5History, 1);
		var macd5Prev2 = GetPrevious(_macd5History, 2);
		var macd6Prev = GetPrevious(_macd6History, 1);
		var macd6Prev2 = GetPrevious(_macd6History, 2);

		if (allowTrade && timeOk)
		{
			if (Pattern1Enabled && macd1Prev.HasValue && macd1Prev2.HasValue)
				ProcessPattern1(candle, macd1Curr, macd1Prev.Value, macd1Prev2.Value);

			if (Pattern2Enabled && macd2Prev.HasValue && macd2Prev2.HasValue)
				ProcessPattern2(candle, macd2Curr, macd2Prev.Value, macd2Prev2.Value);

			if (Pattern3Enabled && macd3Prev.HasValue && macd3Prev2.HasValue)
				ProcessPattern3(candle, macd3Curr, macd3Prev.Value, macd3Prev2.Value);

			if (Pattern4Enabled && macd4Prev.HasValue && macd4Prev2.HasValue)
				ProcessPattern4(candle, macd4Curr, macd4Prev.Value, macd4Prev2.Value);

			if (Pattern5Enabled && macd5Prev.HasValue && macd5Prev2.HasValue)
				ProcessPattern5(candle, macd5Curr, macd5Prev.Value, macd5Prev2.Value);

			if (Pattern6Enabled && macd6Prev.HasValue && macd6Prev2.HasValue)
				ProcessPattern6(candle, macd6Curr, macd6Prev.Value, macd6Prev2.Value);
		}

		UpdateMacdHistories(macd1Value, macd2Value, macd3Value, macd4Value, macd5Value, macd6Value);
	}

	private void ProcessPattern1(ICandleMessage candle, decimal macdcurr, decimal macdlast, decimal macdlast3)
	{
		if (macdcurr > Pattern1MaxThreshold)
			_pattern1WasAbove = true;

		if (macdcurr < 0m)
			_pattern1WasAbove = false;

		if (macdcurr < Pattern1MaxThreshold && macdcurr < macdlast && macdlast > macdlast3 && _pattern1WasAbove && macdcurr > 0m && macdlast3 < Pattern1MaxThreshold)
		{
			if (TryOpenShort(candle, Pattern1StopLossBars, Pattern1Offset, Pattern1TakeProfitBars, "Pattern1"))
			{
				_pattern1WasAbove = false;
				_longPartialCount = 0;
			}
		}

		if (macdcurr < Pattern1MinThreshold)
			_pattern1WasBelow = true;

		if (macdcurr > 0m)
			_pattern1WasBelow = false;

		if (macdcurr > Pattern1MinThreshold && macdcurr < 0m && macdcurr > macdlast && macdlast < macdlast3 && _pattern1WasBelow && macdlast3 > Pattern1MinThreshold)
		{
			if (TryOpenLong(candle, Pattern1StopLossBars, Pattern1Offset, Pattern1TakeProfitBars, "Pattern1"))
			{
				_pattern1WasBelow = false;
				_shortPartialCount = 0;
			}
		}
	}

	private void ProcessPattern2(ICandleMessage candle, decimal macdcurr, decimal macdlast, decimal macdlast3)
	{
		if (macdcurr > 0m)
		{
			_pattern2WasPositive = true;
			_pattern2SellArmed = false;
		}

		if (macdcurr > macdlast && macdlast < macdlast3 && _pattern2WasPositive && macdcurr > Pattern2MinThreshold && macdcurr < 0m && !_pattern2SellArmed)
		{
			_pattern2SellArmed = true;
		}

		var valueMin2 = Math.Abs(macdlast * 10000m);
		var valueCurr2 = Math.Abs(macdcurr * 10000m);

		if (_pattern2SellArmed && macdcurr < macdlast && macdlast > macdlast3 && macdcurr < 0m && valueMin2 <= valueCurr2)
			_pattern2WasPositive = false;

		if (_pattern2SellArmed && macdcurr < macdlast && macdlast > macdlast3 && macdcurr < 0m)
		{
			if (TryOpenShort(candle, Pattern2StopLossBars, Pattern2Offset, Pattern2TakeProfitBars, "Pattern2"))
			{
				_pattern2WasPositive = false;
				_pattern2SellArmed = false;
			}
		}

		if (macdcurr < 0m)
		{
			_pattern2WasNegative = true;
			_pattern2BuyArmed = false;
		}

		if (macdcurr < Pattern2MaxThreshold && macdcurr < macdlast && macdlast > macdlast3 && _pattern2WasNegative && macdcurr > 0m)
		{
			_pattern2BuyArmed = true;
		}

		var valueMax2 = Math.Abs(macdlast * 10000m);

		if (_pattern2BuyArmed && macdcurr > macdlast && macdlast < macdlast3 && macdcurr > 0m && valueMax2 <= valueCurr2)
			_pattern2WasNegative = false;

		if (_pattern2BuyArmed && macdcurr > macdlast && macdlast < macdlast3 && macdcurr > 0m)
		{
			if (TryOpenLong(candle, Pattern2StopLossBars, Pattern2Offset, Pattern2TakeProfitBars, "Pattern2"))
			{
				_pattern2WasNegative = false;
				_pattern2BuyArmed = false;
			}
		}
	}

	private void ProcessPattern3(ICandleMessage candle, decimal macdcurr, decimal macdlast, decimal macdlast3)
	{
		var aopSell = false;
		var aopBuy = false;

		var S3 = macdcurr > Pattern3MaxLowThreshold ? 1 : 0;
		if (macdcurr > Pattern3MaxLowThreshold)
			_pattern3BarsBup++;

		double max13 = 0, max23 = 0;
		var stops3 = 0;
		var stops13 = 0;

		if (S3 == 1 && macdcurr < macdlast && macdlast > macdlast3 && macdlast > max13 && stops3 == 0)
			max13 = (double)macdlast;

		if (max13 > 0 && macdcurr < Pattern3MaxThreshold)
			stops3 = 1;

		if (macdcurr < Pattern3MaxLowThreshold)
		{
			stops3 = 0;
			max13 = 0;
			S3 = 0;
		}

		if (stops3 == 1 && macdcurr > Pattern3MaxThreshold && macdcurr < macdlast && macdlast > macdlast3 && macdlast > max13 && macdlast > max23 && stops13 == 0)
			max23 = (double)macdlast;

		if (max23 > 0 && macdcurr < Pattern3MaxThreshold)
			stops13 = 1;

		if (macdcurr < Pattern3MaxLowThreshold)
		{
			stops13 = 0;
			max23 = 0;
		}

		if (stops13 == 1 && macdcurr < Pattern3MaxThreshold && macdlast < Pattern3MaxThreshold && macdlast3 < Pattern3MaxThreshold &&
			macdcurr < macdlast && macdlast > macdlast3 && macdlast < (decimal)max23)
			aopSell = true;

		if (macdcurr < Pattern3MaxLowThreshold)
			aopSell = false;

		if (aopSell)
		{
			if (TryOpenShort(candle, Pattern3StopLossBars, Pattern3Offset, Pattern3TakeProfitBars, "Pattern3"))
			{
				_pattern3BarsBup = 0;
				return;
			}
		}

		var bS3 = macdcurr < Pattern3MinThreshold ? 1 : 0;
		var sstops3 = 0;
		var sstops13 = 0;
		double min13 = 0, min23 = 0;

		if (bS3 == 1 && macdcurr > macdlast && macdlast < macdlast3 && macdlast < min13 && sstops3 == 0)
			min13 = (double)macdlast;

		if (min13 < 0 && macdcurr > Pattern3MinThreshold)
		{
			sstops3 = 1;
			bS3 = 0;
		}

		if (macdcurr > Pattern3MinHighThreshold)
		{
			sstops3 = 0;
			min13 = 0;
			bS3 = 0;
		}

		if (sstops3 == 1 && macdcurr < Pattern3MaxThreshold && macdcurr > macdlast && macdlast < macdlast3 && macdlast < min13 && macdlast < min23 && sstops13 == 0)
			min23 = (double)macdlast;

		if (min23 < 0 && macdcurr > Pattern3MinThreshold)
		{
			sstops13 = 1;
			sstops3 = 0;
		}

		if (macdcurr > Pattern3MinHighThreshold)
		{
			sstops13 = 0;
			min23 = 0;
		}

		if (sstops13 == 1 && macdcurr > Pattern3MinThreshold && macdlast > Pattern3MinThreshold && macdlast3 > Pattern3MinThreshold &&
			macdcurr > macdlast && macdlast < macdlast3 && macdlast > min23)
		{
			aopBuy = true;
			sstops13 = 0;
		}

		if (macdcurr > Pattern3MaxThreshold)
			aopBuy = false;

		if (aopBuy)
		{
			TryOpenLong(candle, Pattern3StopLossBars, Pattern3Offset, Pattern3TakeProfitBars, "Pattern3");
		}
	}

	private void ProcessPattern4(ICandleMessage candle, decimal macdcurr, decimal macdlast, decimal macdlast3)
	{
		var aopSell = false;
		var aopBuy = false;
		double max14 = 0;
		double min14 = 0;
		var stops4 = 0;
		var sstop4 = 0;

		if (macdcurr > Pattern4MaxThreshold && macdcurr < macdlast && macdlast > macdlast3 && stops4 == 0)
		{
			max14 = (double)macdlast;
			stops4 = 1;
		}

		if (macdcurr < Pattern4MaxThreshold)
		{
			stops4 = 0;
			max14 = 0;
		}

		if (stops4 == 1 && macdcurr > Pattern4MaxThreshold && macdcurr < macdlast && macdlast > macdlast3 && macdlast < max14)
			aopSell = true;

		if (macdcurr < Pattern4MaxThreshold)
			aopSell = false;

		if (aopSell)
		{
			if (TryOpenShort(candle, Pattern4StopLossBars, Pattern4Offset, Pattern4TakeProfitBars, "Pattern4"))
				return;
		}

		if (macdcurr < Pattern4MinThreshold && macdcurr > macdlast && macdlast < macdlast3 && sstop4 == 0)
		{
			min14 = (double)macdlast;
			sstop4 = 1;
		}

		if (macdcurr > Pattern4MinThreshold)
		{
			sstop4 = 0;
			min14 = 0;
		}

		if (sstop4 == 1 && macdcurr < Pattern4MinThreshold && macdcurr > macdlast && macdlast < macdlast3 && macdlast > min14)
			aopBuy = true;

		if (macdcurr > Pattern4MaxThreshold)
			aopBuy = false;

		if (aopBuy)
		{
			TryOpenLong(candle, Pattern4StopLossBars, Pattern4Offset, Pattern4TakeProfitBars, "Pattern4");
		}
	}

	private void ProcessPattern5(ICandleMessage candle, decimal macdcurr, decimal macdlast, decimal macdlast3)
	{
		var aopSell = false;
		var aopBuy = false;
		var stops5 = 0;
		var stopb5 = 0;
		var Sb5 = 0;
		var Ss5 = 0;

		if (macdcurr < Pattern5MinNeutralThreshold && stops5 == 0)
			stops5 = 1;

		if (macdcurr > Pattern5MinThreshold && stops5 == 1)
		{
			stops5 = 0;
			Sb5 = 1;
		}

		if (Sb5 == 1 && macdcurr < macdlast && macdlast > macdlast3 && macdcurr < Pattern5MinThreshold && macdlast > Pattern5MinThreshold)
		{
			aopSell = true;
			Sb5 = 0;
		}

		if (macdcurr > 0m)
		{
			stops5 = 0;
			aopSell = false;
			Sb5 = 0;
		}

		if (aopSell)
		{
			if (TryOpenShort(candle, Pattern5StopLossBars, Pattern5Offset, Pattern5TakeProfitBars, "Pattern5"))
				return;
		}

		if (macdcurr > Pattern5MaxNeutralThreshold && stopb5 == 0)
			stopb5 = 1;

		if (macdcurr < 0m)
		{
			stopb5 = 0;
			aopBuy = false;
			Ss5 = 0;
		}

		if (macdcurr < Pattern5MaxThreshold && stopb5 == 1)
		{
			stopb5 = 0;
			Ss5 = 1;
		}

		if (Ss5 == 1 && macdcurr > macdlast && macdlast < macdlast3 && macdcurr > Pattern5MaxThreshold && macdlast < Pattern5MaxThreshold)
		{
			aopBuy = true;
			Ss5 = 0;
		}

		if (aopBuy)
		{
			TryOpenLong(candle, Pattern5StopLossBars, Pattern5Offset, Pattern5TakeProfitBars, "Pattern5");
		}
	}

	private void ProcessPattern6(ICandleMessage candle, decimal macdcurr, decimal macdlast, decimal macdlast3)
	{
		if (macdcurr < Pattern6MaxThreshold)
			_pattern6SellBlocked = false;

		if (macdcurr > Pattern6MaxThreshold && _pattern6BarsAbove <= Pattern6MaxBars && !_pattern6SellBlocked)
			_pattern6BarsAbove++;

		if (_pattern6BarsAbove > Pattern6MaxBars)
		{
			_pattern6BarsAbove = 0;
			_pattern6SellBlocked = true;
		}

		if (_pattern6BarsAbove < Pattern6MinBars && macdcurr < Pattern6MaxThreshold)
			_pattern6BarsAbove = 0;

		if (macdcurr < Pattern6MaxThreshold && _pattern6BarsAbove > Pattern6TriggerBars)
			_pattern6SellReady = true;

		if (_pattern6SellReady)
		{
			if (TryOpenShort(candle, Pattern6StopLossBars, Pattern6Offset, Pattern6TakeProfitBars, "Pattern6"))
			{
				_pattern6SellReady = false;
				_pattern6BarsAbove = 0;
				_pattern6SellBlocked = false;
				return;
			}
		}

		if (macdcurr > Pattern6MinThreshold)
			_pattern6BuyBlocked = false;

		if (macdcurr < Pattern6MinThreshold && _pattern6BarsBelow <= Pattern6MaxBars && !_pattern6BuyBlocked)
			_pattern6BarsBelow++;

		if (_pattern6BarsBelow > Pattern6MaxBars)
		{
			_pattern6BuyBlocked = true;
			_pattern6BarsBelow = 0;
		}

		if (_pattern6BarsBelow < Pattern6MinBars && macdcurr > Pattern6MinThreshold)
			_pattern6BarsBelow = 0;

		if (macdcurr > Pattern6MinThreshold && _pattern6BarsBelow > Pattern6TriggerBars)
			_pattern6BuyReady = true;

		if (_pattern6BuyReady)
		{
			if (TryOpenLong(candle, Pattern6StopLossBars, Pattern6Offset, Pattern6TakeProfitBars, "Pattern6"))
			{
				_pattern6BuyReady = false;
				_pattern6BarsBelow = 0;
				_pattern6BuyBlocked = false;
			}
		}
	}

	private bool TryOpenLong(ICandleMessage candle, int stopBars, int offset, int takeBars, string tag)
	{
		var stop = CalculateStopPrice(Sides.Buy, stopBars, offset);
		var take = CalculateTakeProfit(Sides.Buy, takeBars);

		if (stop == null || take == null)
			return false;

		var volume = _currentVolume + Math.Max(0m, -Position);
		if (volume <= 0m)
			return false;

		BuyMarket(volume, tag);
		_longStop = stop;
		_longTake = take;
		_shortStop = null;
		_shortTake = null;
		_longPartialCount = 0;
		_shortPartialCount = 0;
		return true;
	}

	private bool TryOpenShort(ICandleMessage candle, int stopBars, int offset, int takeBars, string tag)
	{
		var stop = CalculateStopPrice(Sides.Sell, stopBars, offset);
		var take = CalculateTakeProfit(Sides.Sell, takeBars);

		if (stop == null || take == null)
			return false;

		var volume = _currentVolume + Math.Max(0m, Position);
		if (volume <= 0m)
			return false;

		SellMarket(volume, tag);
		_shortStop = stop;
		_shortTake = take;
		_longStop = null;
		_longTake = null;
		_longPartialCount = 0;
		_shortPartialCount = 0;
		return true;
	}

	private decimal? CalculateStopPrice(Sides side, int stopLossBars, int offset)
	{
		if (stopLossBars <= 0 || _candles.Count < stopLossBars)
			return null;

		var step = Security?.PriceStep ?? 1m;
		var digitsAdjust = (Security?.Decimals is 3 or 5) ? 10m : 1m;
		var offsetValue = offset * step * digitsAdjust;
		var minDistance = 10m * step * digitsAdjust;
		var currentPrice = _candles[^1].ClosePrice;

		if (side == Sides.Sell)
		{
			var highest = GetSegmentExtreme(Sides.Sell, stopLossBars, 0, true);
			if (highest == null)
				return null;

			var stop = highest.Value + offsetValue;
			if (stop < currentPrice)
				stop = currentPrice + minDistance;
			return stop;
		}
		else
		{
			var lowest = GetSegmentExtreme(Sides.Buy, stopLossBars, 0, true);
			if (lowest == null)
				return null;

			var stop = lowest.Value - offsetValue;
			if (stop > currentPrice)
				stop = currentPrice - minDistance;
			return stop;
		}
	}

	private decimal? CalculateTakeProfit(Sides side, int takeProfitBars)
	{
		if (takeProfitBars <= 0)
			return null;

		var x = 0;
		var best = GetSegmentExtreme(side, takeProfitBars, x, false);
		if (best == null)
			return null;

		while (true)
		{
			x += takeProfitBars;
			var next = GetSegmentExtreme(side, takeProfitBars, x, false);
			if (next == null)
				break;

			if (side == Sides.Sell)
			{
				if (best.Value > next.Value)
				{
					best = next;
					continue;
				}
			}
			else
			{
				if (best.Value < next.Value)
				{
					best = next;
					continue;
				}
			}

			break;
		}

		return best;
	}

	private decimal? GetSegmentExtreme(Sides side, int count, int start, bool forStop)
	{
		if (count <= 0)
			return null;

		var startIndex = _candles.Count - 1 - start;
		var endIndex = startIndex - (count - 1);

		if (startIndex < 0 || endIndex < 0)
			return null;

		decimal extreme = side == Sides.Sell ? decimal.MaxValue : decimal.MinValue;

		for (var i = startIndex; i >= endIndex; i--)
		{
			var candle = _candles[i];
			var value = side == Sides.Sell ? candle.LowPrice : candle.HighPrice;

			if (side == Sides.Sell)
			{
				if (value < extreme)
					extreme = value;
			}
			else
			{
				if (value > extreme)
					extreme = value;
			}
		}

		return extreme;
	}

	private void ManageActivePositions(ICandleMessage candle, decimal ema2, decimal sma3, decimal ema4)
	{
		if (Position > 0m && _longVolume > 0m)
		{
			var profit = (candle.ClosePrice - _longAveragePrice) * _longVolume;
			if (profit > ProfitThreshold && candle.ClosePrice > ema2 && _longPartialCount == 0)
			{
				var volume = Math.Max(Math.Round(_longVolume / 3m, 2, MidpointRounding.AwayFromZero), MinPartialVolume);
				volume = Math.Min(volume, Position);
				SellMarket(volume, "PartialLong");
				_longPartialCount++;
				return;
			}

			if (profit > ProfitThreshold && candle.HighPrice > (sma3 + ema4) / 2m && _longPartialCount == 1)
			{
				var volume = Math.Max(Math.Round(_longVolume / 2m, 2, MidpointRounding.AwayFromZero), MinPartialVolume);
				volume = Math.Min(volume, Position);
				SellMarket(volume, "PartialLong");
				_longPartialCount++;
			}
		}
		else if (Position < 0m && _shortVolume > 0m)
		{
			var profit = (_shortAveragePrice - candle.ClosePrice) * _shortVolume;
			if (profit > ProfitThreshold && candle.ClosePrice < ema2 && _shortPartialCount == 0)
			{
				var volume = Math.Max(Math.Round(_shortVolume / 3m, 2, MidpointRounding.AwayFromZero), MinPartialVolume);
				volume = Math.Min(volume, Math.Abs(Position));
				BuyMarket(volume, "PartialShort");
				_shortPartialCount++;
				return;
			}

			if (profit > ProfitThreshold && candle.LowPrice < (sma3 + ema4) / 2m && _shortPartialCount == 1)
			{
				var volume = Math.Max(Math.Round(_shortVolume / 2m, 2, MidpointRounding.AwayFromZero), MinPartialVolume);
				volume = Math.Min(volume, Math.Abs(Position));
				BuyMarket(volume, "PartialShort");
				_shortPartialCount++;
			}
		}
	}

	private void CheckRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position), "StopLong");
				_longStop = null;
				_longTake = null;
			}
			else if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Math.Abs(Position), "TakeLong");
				_longStop = null;
				_longTake = null;
			}
		}
		else if (Position < 0m)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position), "StopShort");
				_shortStop = null;
				_shortTake = null;
			}
			else if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position), "TakeShort");
				_shortStop = null;
				_shortTake = null;
			}
		}
	}

	private void UpdateMacdHistories(IIndicatorValue macd1Value, IIndicatorValue macd2Value, IIndicatorValue macd3Value,
		IIndicatorValue macd4Value, IIndicatorValue macd5Value, IIndicatorValue macd6Value)
	{
		AppendMacdValue(_macd1History, macd1Value);
		AppendMacdValue(_macd2History, macd2Value);
		AppendMacdValue(_macd3History, macd3Value);
		AppendMacdValue(_macd4History, macd4Value);
		AppendMacdValue(_macd5History, macd5Value);
		AppendMacdValue(_macd6History, macd6Value);
	}

	private static void AppendMacdValue(List<decimal> history, IIndicatorValue value)
	{
		if (value is not MovingAverageConvergenceDivergenceSignalValue macdValue)
			return;

		if (macdValue.Macd is not decimal macd)
			return;

		history.Add(macd);
		if (history.Count > MacdHistoryLength)
			history.RemoveAt(0);
	}

	private static decimal? GetPrevious(List<decimal> history, int index)
	{
		if (history.Count <= index)
			return null;

		return history[^ (index + 1)];
	}

	private void TrimCandles()
	{
		if (_candles.Count > CandleHistoryLimit)
			_candles.RemoveRange(0, _candles.Count - CandleHistoryLimit);
	}

	private bool IsWithinTradingWindow(TimeSpan time)
	{
		var start = StartTime;
		var stop = StopTime;

		if (start == stop)
			return true;

		if (start < stop)
			return time > start && time < stop;

		return time > start || time < stop;
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd(int fast, int slow)
	{
		return new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = fast },
				LongMa = { Length = slow }
			},
			SignalMa = { Length = 1 }
		};
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var volume = trade.Trade?.Volume ?? 0m;
		var price = trade.Trade?.Price ?? 0m;

		if (volume <= 0m)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			var covered = Math.Min(_shortVolume, volume);
			if (covered > 0m)
			{
				_cycleRealizedPnL += (_shortAveragePrice - price) * covered;
				_shortVolume -= covered;
				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
					_shortStop = null;
					_shortTake = null;
					_shortPartialCount = 0;
				}
			}

			var remaining = volume - covered;
			if (remaining > 0m)
			{
				if (_longVolume == 0m)
					_cycleRealizedPnL = 0m;

				var newVolume = _longVolume + remaining;
				_longAveragePrice = (_longAveragePrice * _longVolume + price * remaining) / newVolume;
				_longVolume = newVolume;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var covered = Math.Min(_longVolume, volume);
			if (covered > 0m)
			{
				_cycleRealizedPnL += (price - _longAveragePrice) * covered;
				_longVolume -= covered;
				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
					_longStop = null;
					_longTake = null;
					_longPartialCount = 0;
				}
			}

			var remaining = volume - covered;
			if (remaining > 0m)
			{
				if (_shortVolume == 0m)
					_cycleRealizedPnL = 0m;

				var newVolume = _shortVolume + remaining;
				_shortAveragePrice = (_shortAveragePrice * _shortVolume + price * remaining) / newVolume;
				_shortVolume = newVolume;
			}
		}

		if (Position == 0m)
			AdjustVolumeOnFlat();
	}

	private void AdjustVolumeOnFlat()
	{
		if (_cycleRealizedPnL > 0m || !UseMartingale)
		{
			_currentVolume = InitialVolume;
		}
		else if (UseMartingale)
		{
			_currentVolume *= 2m;
		}

		_cycleRealizedPnL = 0m;
		_longPartialCount = 0;
		_shortPartialCount = 0;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}
}

