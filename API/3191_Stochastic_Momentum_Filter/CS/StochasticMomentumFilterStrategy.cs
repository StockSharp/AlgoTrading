using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the "Stochastic" MQL expert advisor.
/// Combines dual stochastic oscillators, linear weighted moving averages,
/// a momentum deviation filter and higher timeframe MACD confirmation.
/// </summary>
public class StochasticMomentumFilterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stochasticBuyLevel;
	private readonly StrategyParam<decimal> _stochasticSellLevel;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _fastStochasticPeriod;
	private readonly StrategyParam<int> _fastStochasticSignal;
	private readonly StrategyParam<int> _fastStochasticSmoothing;
	private readonly StrategyParam<int> _slowStochasticPeriod;
	private readonly StrategyParam<int> _slowStochasticSignal;
	private readonly StrategyParam<int> _slowStochasticSmoothing;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maxNetPositions;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframe;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private StochasticOscillator _fastStochastic = null!;
	private StochasticOscillator _slowStochastic = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private decimal? _momentumDeviation1;
	private decimal? _momentumDeviation2;
	private decimal? _momentumDeviation3;
	private decimal? _macdMain;
	private decimal? _macdSignal;

	/// <summary>
	/// Level that marks the oversold zone for both stochastic oscillators.
	/// </summary>
	public decimal StochasticBuyLevel
	{
		get => _stochasticBuyLevel.Value;
		set => _stochasticBuyLevel.Value = value;
	}

	/// <summary>
	/// Level that marks the overbought zone for both stochastic oscillators.
	/// </summary>
	public decimal StochasticSellLevel
	{
		get => _stochasticSellLevel.Value;
		set => _stochasticSellLevel.Value = value;
	}

	/// <summary>
	/// Period of the fast linear weighted moving average.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow linear weighted moving average.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Main length ("%K") of the fast stochastic oscillator.
	/// </summary>
	public int FastStochasticPeriod
	{
		get => _fastStochasticPeriod.Value;
		set => _fastStochasticPeriod.Value = value;
	}

	/// <summary>
	/// Signal length ("%D") of the fast stochastic oscillator.
	/// </summary>
	public int FastStochasticSignal
	{
		get => _fastStochasticSignal.Value;
		set => _fastStochasticSignal.Value = value;
	}

	/// <summary>
	/// Smoothing factor applied to the fast stochastic oscillator.
	/// </summary>
	public int FastStochasticSmoothing
	{
		get => _fastStochasticSmoothing.Value;
		set => _fastStochasticSmoothing.Value = value;
	}

	/// <summary>
	/// Main length of the slow stochastic oscillator.
	/// </summary>
	public int SlowStochasticPeriod
	{
		get => _slowStochasticPeriod.Value;
		set => _slowStochasticPeriod.Value = value;
	}

	/// <summary>
	/// Signal length of the slow stochastic oscillator.
	/// </summary>
	public int SlowStochasticSignal
	{
		get => _slowStochasticSignal.Value;
		set => _slowStochasticSignal.Value = value;
	}

	/// <summary>
	/// Smoothing factor applied to the slow stochastic oscillator.
	/// </summary>
	public int SlowStochasticSmoothing
	{
		get => _slowStochasticSmoothing.Value;
		set => _slowStochasticSmoothing.Value = value;
	}

	/// <summary>
	/// Look-back period of the momentum deviation filter.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute deviation from the momentum baseline (100) required for signals.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA period used in the higher timeframe MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used in the higher timeframe MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period used in the higher timeframe MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables trailing behaviour for the protective stop.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Target net position size when a new signal appears.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of net position units that may remain open.
	/// </summary>
	public int MaxNetPositions
	{
		get => _maxNetPositions.Value;
		set => _maxNetPositions.Value = value;
	}

	/// <summary>
	/// Trading timeframe used for the main indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe that feeds the MACD trend filter.
	/// </summary>
	public DataType HigherTimeframe
	{
		get => _higherTimeframe.Value;
		set => _higherTimeframe.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticMomentumFilterStrategy"/> class.
	/// </summary>
	public StochasticMomentumFilterStrategy()
	{
		_stochasticBuyLevel = Param(nameof(StochasticBuyLevel), 30m)
		.SetDisplay("Stochastic Buy", "Oversold threshold for signals", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_stochasticSellLevel = Param(nameof(StochasticSellLevel), 80m)
		.SetDisplay("Stochastic Sell", "Overbought threshold for signals", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted MA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted MA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(30, 150, 5);

		_fastStochasticPeriod = Param(nameof(FastStochasticPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast %K", "Period of the fast stochastic", "Oscillators")
		.SetCanOptimize(true)
		.SetOptimize(3, 9, 1);

		_fastStochasticSignal = Param(nameof(FastStochasticSignal), 2)
		.SetGreaterThanZero()
		.SetDisplay("Fast %D", "Signal period of the fast stochastic", "Oscillators")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_fastStochasticSmoothing = Param(nameof(FastStochasticSmoothing), 2)
		.SetGreaterThanZero()
		.SetDisplay("Fast Smoothing", "Smoothing factor of the fast stochastic", "Oscillators")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_slowStochasticPeriod = Param(nameof(SlowStochasticPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("Slow %K", "Period of the slow stochastic", "Oscillators")
		.SetCanOptimize(true)
		.SetOptimize(14, 42, 2);

		_slowStochasticSignal = Param(nameof(SlowStochasticSignal), 4)
		.SetGreaterThanZero()
		.SetDisplay("Slow %D", "Signal period of the slow stochastic", "Oscillators")
		.SetCanOptimize(true)
		.SetOptimize(2, 8, 1);

		_slowStochasticSmoothing = Param(nameof(SlowStochasticSmoothing), 10)
		.SetGreaterThanZero()
		.SetDisplay("Slow Smoothing", "Smoothing factor of the slow stochastic", "Oscillators")
		.SetCanOptimize(true)
		.SetOptimize(4, 14, 2);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Look-back of the momentum filter", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 1);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Threshold", "Minimum deviation from baseline", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1.0m, 0.1m);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Filters");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Filters");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period for MACD", "Filters");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit distance in points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Trailing Stop", "Trail the protective stop", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Target net volume per signal", "Trading");

		_maxNetPositions = Param(nameof(MaxNetPositions), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum stacked net positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Trading timeframe", "General");

		_higherTimeframe = Param(nameof(HigherTimeframe), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("Higher Timeframe", "Timeframe for the MACD filter", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
		yield break;

		var used = new HashSet<DataType>();

		foreach (var type in new[] { CandleType, HigherTimeframe })
		{
			if (used.Add(type))
			yield return (Security, type);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentumDeviation1 = null;
		_momentumDeviation2 = null;
		_momentumDeviation3 = null;
		_macdMain = null;
		_macdSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Instantiate indicators using the most recent parameter values.
		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };

		_fastStochastic = new StochasticOscillator
		{
			Length = FastStochasticPeriod,
			K = { Length = FastStochasticSignal },
			D = { Length = FastStochasticSmoothing }
		};

		_slowStochastic = new StochasticOscillator
		{
			Length = SlowStochasticPeriod,
			K = { Length = SlowStochasticSignal },
			D = { Length = SlowStochasticSmoothing }
		};

		_momentum = new Momentum { Length = MomentumPeriod };

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		// Subscribe to the trading timeframe and bind all lower timeframe indicators.
		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription
		.Bind(_fastMa, _slowMa, _fastStochastic, _slowStochastic, _momentum, ProcessTradingCandle)
		.Start();

		// Subscribe to the higher timeframe to evaluate the MACD filter.
		var higherSubscription = SubscribeCandles(HigherTimeframe);
		higherSubscription
		.Bind(_macd, ProcessHigherCandle)
		.Start();

		// Configure the desired trading volume.
		Volume = TradeVolume;

		// Configure built-in protective orders in line with the MQL expert behaviour.
		var step = Security?.PriceStep;
		if (step is not null && step > 0m)
		{
			var takeProfit = TakeProfitPoints > 0m
			? new Unit(TakeProfitPoints * step.Value, UnitTypes.Point)
			: new Unit();

			var stopLoss = StopLossPoints > 0m
			? new Unit(StopLossPoints * step.Value, UnitTypes.Point)
			: new Unit();

			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss, isStopTrailing: EnableTrailing);
		}

		// Draw charts when the environment provides a charting surface.
		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, tradingSubscription);
			DrawIndicator(priceArea, _fastMa);
			DrawIndicator(priceArea, _slowMa);
			DrawOwnTrades(priceArea);
		}

		var oscillatorArea = CreateChartArea();
		if (oscillatorArea != null)
		{
			DrawIndicator(oscillatorArea, _fastStochastic);
			DrawIndicator(oscillatorArea, _slowStochastic);
			DrawIndicator(oscillatorArea, _macd);
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal macdValue, decimal macdSignal, decimal _)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store the most recent higher timeframe MACD values for later use.
		_macdMain = macdValue;
		_macdSignal = macdSignal;
	}

	private void ProcessTradingCandle(
	ICandleMessage candle,
	decimal fastMaValue,
	decimal slowMaValue,
	decimal fastStochasticK,
	decimal fastStochasticD,
	decimal slowStochasticK,
	decimal slowStochasticD,
	decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Abort early when historical warm-up or trading restrictions apply.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_fastStochastic.IsFormed || !_slowStochastic.IsFormed || !_momentum.IsFormed)
		return;

		if (_macdMain is null || _macdSignal is null)
		return;

		// Calculate the absolute deviation of the momentum oscillator around the 100 baseline.
		var deviation = Math.Abs(momentumValue - 100m);

		_momentumDeviation3 = _momentumDeviation2;
		_momentumDeviation2 = _momentumDeviation1;
		_momentumDeviation1 = deviation;

		if (_momentumDeviation3 is null)
		return;

		var momentumStrong =
		_momentumDeviation1 >= MomentumThreshold ||
		_momentumDeviation2 >= MomentumThreshold ||
		_momentumDeviation3 >= MomentumThreshold;

		if (!momentumStrong)
		return;

		var macdBullish = _macdMain > _macdSignal;
		var macdBearish = _macdMain < _macdSignal;

		var bullishTrend = fastMaValue > slowMaValue;
		var bearishTrend = fastMaValue < slowMaValue;

		var oversold = fastStochasticK <= StochasticBuyLevel && slowStochasticK <= StochasticBuyLevel && fastStochasticD <= StochasticBuyLevel && slowStochasticD <= StochasticBuyLevel;
		var overbought = fastStochasticK >= StochasticSellLevel && slowStochasticK >= StochasticSellLevel && fastStochasticD >= StochasticSellLevel && slowStochasticD >= StochasticSellLevel;

		if (oversold && bullishTrend && macdBullish)
		{
			EnterLong();
		}
		else if (overbought && bearishTrend && macdBearish)
		{
			EnterShort();
		}
	}

	private void EnterLong()
	{
		var maxVolume = TradeVolume * MaxNetPositions;
		var desiredPosition = Math.Min(maxVolume, TradeVolume);
		var delta = desiredPosition - Position;
		if (delta <= 0m)
		return;

		// Add enough volume to flip existing shorts and reach the desired long exposure.
		BuyMarket(delta);
	}

	private void EnterShort()
	{
		var maxVolume = TradeVolume * MaxNetPositions;
		var desiredPosition = -Math.Min(maxVolume, TradeVolume);
		var delta = Position - desiredPosition;
		if (delta <= 0m)
		return;

		// Add enough volume to flip existing longs and reach the desired short exposure.
		SellMarket(delta);
	}
}

