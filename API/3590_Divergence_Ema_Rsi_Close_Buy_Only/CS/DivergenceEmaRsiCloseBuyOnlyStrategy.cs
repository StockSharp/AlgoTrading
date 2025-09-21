using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Divergence + EMA + RSI close buy only strategy.
/// </summary>
public class DivergenceEmaRsiCloseBuyOnlyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _hourTimeFrame;
	private readonly StrategyParam<DataType> _dayTimeFrame;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _macdThreshold;
	private readonly StrategyParam<int> _dailyFastPeriod;
	private readonly StrategyParam<int> _dailySlowPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticUpperBound;
	private readonly StrategyParam<decimal> _stochasticLowerBound;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiExitLevel;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private RelativeStrengthIndex _rsi = null!;
	private ExponentialMovingAverage _dailyFastEma = null!;
	private ExponentialMovingAverage _dailySlowEma = null!;
	private StochasticOscillator _stochastic = null!;

	private decimal? _previousClose;
	private decimal? _previousMacdHistogram;

	private decimal? _previousStochasticK;
	private decimal? _previousStochasticD;
	private decimal? _currentStochasticK;
	private decimal? _currentStochasticD;

	private decimal? _dailyFastValue;
	private decimal? _dailySlowValue;

	private decimal _pipSize;
	/// <summary>
	/// Primary timeframe used for MACD and RSI calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Hourly timeframe used for stochastic confirmation.
	/// </summary>
	public DataType HourTimeFrame
	{
		get => _hourTimeFrame.Value;
		set => _hourTimeFrame.Value = value;
	}

	/// <summary>
	/// Daily timeframe used for EMA trend filter.
	/// </summary>
	public DataType DayTimeFrame
	{
		get => _dayTimeFrame.Value;
		set => _dayTimeFrame.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Minimal MACD histogram improvement to confirm divergence.
	/// </summary>
	public decimal MacdThreshold
	{
		get => _macdThreshold.Value;
		set => _macdThreshold.Value = value;
	}

	/// <summary>
	/// Fast daily EMA period.
	/// </summary>
	public int DailyFastPeriod
	{
		get => _dailyFastPeriod.Value;
		set => _dailyFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow daily EMA period.
	/// </summary>
	public int DailySlowPeriod
	{
		get => _dailySlowPeriod.Value;
		set => _dailySlowPeriod.Value = value;
	}

	/// <summary>
	/// %K period for the stochastic oscillator.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// %D period for the stochastic oscillator.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period applied to the %K line.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Upper bound for stochastic confirmation.
	/// </summary>
	public decimal StochasticUpperBound
	{
		get => _stochasticUpperBound.Value;
		set => _stochasticUpperBound.Value = value;
	}

	/// <summary>
	/// Lower bound for stochastic confirmation.
	/// </summary>
	public decimal StochasticLowerBound
	{
		get => _stochasticLowerBound.Value;
		set => _stochasticLowerBound.Value = value;
	}

	/// <summary>
	/// RSI period used for exit management.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold that triggers position closure.
	/// </summary>
	public decimal RsiExitLevel
	{
		get => _rsiExitLevel.Value;
		set => _rsiExitLevel.Value = value;
	}

	/// <summary>
	/// Order volume for long entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DivergenceEmaRsiCloseBuyOnlyStrategy"/>.
	/// </summary>
	public DivergenceEmaRsiCloseBuyOnlyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Primary Timeframe", "Trading timeframe for MACD and RSI", "General");
		_hourTimeFrame = Param(nameof(HourTimeFrame), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Hourly Timeframe", "Higher timeframe for stochastic filter", "General");
		_dayTimeFrame = Param(nameof(DayTimeFrame), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Daily Timeframe", "Higher timeframe for EMA trend", "General");
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast EMA", "Fast EMA length", "MACD")
		.SetCanOptimize(true);
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow EMA", "Slow EMA length", "MACD")
		.SetCanOptimize(true);
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal smoothing length", "MACD")
		.SetCanOptimize(true);
		_macdThreshold = Param(nameof(MacdThreshold), 0.0003m)
		.SetRange(0m, 1m)
		.SetDisplay("MACD Threshold", "Minimum histogram improvement", "MACD")
		.SetCanOptimize(true);
		_dailyFastPeriod = Param(nameof(DailyFastPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("Daily Fast EMA", "Fast EMA period on daily candles", "Trend")
		.SetCanOptimize(true);
		_dailySlowPeriod = Param(nameof(DailySlowPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Daily Slow EMA", "Slow EMA period on daily candles", "Trend")
		.SetCanOptimize(true);
		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "Base %K length on hourly candles", "Stochastic")
		.SetCanOptimize(true);
		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "Signal smoothing length", "Stochastic")
		.SetCanOptimize(true);
		_stochasticSlowing = Param(nameof(StochasticSlowing), 9)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Slowing", "Smoothing applied to %K", "Stochastic")
		.SetCanOptimize(true);
		_stochasticUpperBound = Param(nameof(StochasticUpperBound), 40m)
		.SetRange(0m, 100m)
		.SetDisplay("Stochastic Upper", "Maximum %K level for entries", "Stochastic")
		.SetCanOptimize(true);
		_stochasticLowerBound = Param(nameof(StochasticLowerBound), 0m)
		.SetRange(0m, 100m)
		.SetDisplay("Stochastic Lower", "Minimum %K level for entries", "Stochastic")
		.SetCanOptimize(true);
		_rsiPeriod = Param(nameof(RsiPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of the exit RSI", "RSI")
		.SetCanOptimize(true);
		_rsiExitLevel = Param(nameof(RsiExitLevel), 77m)
		.SetRange(0m, 100m)
		.SetDisplay("RSI Exit", "RSI threshold used to close longs", "RSI")
		.SetCanOptimize(true);
		_volume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order size for entries", "Trading")
		.SetCanOptimize(true);
		_stopLossPips = Param(nameof(StopLossPips), 100m)
		.SetRange(0m, 1000m)
		.SetDisplay("Stop Loss (pips)", "Distance to the protective stop", "Risk")
		.SetCanOptimize(true);
		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
		.SetRange(0m, 1000m)
		.SetDisplay("Take Profit (pips)", "Distance to the profit target", "Risk")
		.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
		yield break;

		var used = new HashSet<DataType>();
		var frames = new[] { CandleType, HourTimeFrame, DayTimeFrame };

		foreach (var frame in frames)
		{
			if (used.Add(frame))
			yield return (Security, frame);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousClose = null;
		_previousMacdHistogram = null;

		_previousStochasticK = null;
		_previousStochasticD = null;
		_currentStochasticK = null;
		_currentStochasticD = null;

		_dailyFastValue = null;
		_dailySlowValue = null;

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod },
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_dailyFastEma = new ExponentialMovingAverage { Length = DailyFastPeriod };
		_dailySlowEma = new ExponentialMovingAverage { Length = DailySlowPeriod };

		_stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDPeriod }
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.BindEx(_macd, _rsi, ProcessMainCandle)
		.Start();

		var hourSubscription = SubscribeCandles(HourTimeFrame);
		hourSubscription
		.BindEx(_stochastic, ProcessHourCandle)
		.Start();

		var daySubscription = SubscribeCandles(DayTimeFrame);
		daySubscription
		.Bind(_dailyFastEma, _dailySlowEma, ProcessDayCandle)
		.Start();

		Volume = TradeVolume;

		Unit? stopLoss = null;
		if (StopLossPips > 0m && _pipSize > 0m)
		stopLoss = new Unit(StopLossPips * _pipSize, UnitTypes.Absolute);

		Unit? takeProfit = null;
		if (TakeProfitPips > 0m && _pipSize > 0m)
		takeProfit = new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute);

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);
		}
		else
		{
			StartProtection();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDayCandle(ICandleMessage candle, decimal fastEma, decimal slowEma)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store the latest daily EMA values to evaluate the higher time frame trend.
		_dailyFastValue = fastEma;
		_dailySlowValue = slowEma;
	}

	private void ProcessHourCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (stochasticValue is not StochasticOscillatorValue stoch || stoch.K is not decimal currentK || stoch.D is not decimal currentD)
		return;

		// Remember previous and current stochastic readings for crossover detection.
		_previousStochasticK = _currentStochasticK;
		_previousStochasticD = _currentStochasticD;
		_currentStochasticK = currentK;
		_currentStochasticD = currentD;
	}

	private void ProcessMainCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal || !rsiValue.IsFinal)
		return;

		var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdData.Macd is not decimal macdLine || macdData.Signal is not decimal signalLine)
		return;

		var macdHistogram = macdLine - signalLine;
		var rsi = rsiValue.GetValue<decimal>();

		if (_previousClose is null || _previousMacdHistogram is null)
		{
			_previousClose = candle.ClosePrice;
			_previousMacdHistogram = macdHistogram;
			return;
		}

		if (_dailyFastValue is not decimal dailyFast || _dailySlowValue is not decimal dailySlow)
		{
			_previousClose = candle.ClosePrice;
			_previousMacdHistogram = macdHistogram;
			return;
		}

		if (_currentStochasticK is not decimal currentK || _currentStochasticD is not decimal currentD)
		{
			_previousClose = candle.ClosePrice;
			_previousMacdHistogram = macdHistogram;
			return;
		}

		if (_previousStochasticK is not decimal previousK || _previousStochasticD is not decimal previousD)
		{
			_previousClose = candle.ClosePrice;
			_previousMacdHistogram = macdHistogram;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousClose = candle.ClosePrice;
			_previousMacdHistogram = macdHistogram;
			return;
		}

		// Handle the exit first to avoid conflicting signals on the same bar.
		CloseLongOnRsi(rsi);

		if (Position <= 0m && CanEnterLong(candle, macdHistogram, dailyFast, dailySlow, currentK, currentD, previousK, previousD))
		{
			TryEnterLong();
		}

		_previousClose = candle.ClosePrice;
		_previousMacdHistogram = macdHistogram;
	}

	private bool CanEnterLong(ICandleMessage candle, decimal macdHistogram, decimal dailyFast, decimal dailySlow, decimal currentK, decimal currentD, decimal previousK, decimal previousD)
	{
		if (dailyFast <= dailySlow)
		return false;

		if (candle.ClosePrice >= dailyFast)
		return false;

		if (currentK < StochasticLowerBound || currentK > StochasticUpperBound)
		return false;

		var crossUp = previousK <= previousD && currentK > currentD;
		if (!crossUp)
		return false;

		var previousClose = _previousClose!.Value;
		var previousHistogram = _previousMacdHistogram!.Value;
		var histogramImprovement = macdHistogram - previousHistogram;

		var bullishDivergence = candle.ClosePrice < previousClose && histogramImprovement >= MacdThreshold;
		if (!bullishDivergence)
		return false;

		return true;
	}

	private void TryEnterLong()
	{
		var baseVolume = Math.Abs(TradeVolume);
		if (baseVolume <= 0m)
		return;

		CancelActiveOrders();

		var requiredVolume = baseVolume;
		if (Position < 0m)
		requiredVolume += Math.Abs(Position);

		if (requiredVolume <= 0m)
		return;

		BuyMarket(requiredVolume);
	}

	private void CloseLongOnRsi(decimal rsi)
	{
		if (Position <= 0m)
		return;

		if (rsi < RsiExitLevel)
		return;

		// Exit the long exposure when RSI reaches the configured threshold.
		SellMarket(Position);
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
		return step;

		return 1m;
	}
}
