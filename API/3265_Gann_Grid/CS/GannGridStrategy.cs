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
/// Gann grid breakout strategy translated from the original MQL implementation.
/// Combines weighted moving averages, momentum strength and MACD filters
/// with channel breakouts approximating the original Gann line logic.
/// Includes optional trailing stop and break-even automation.
/// </summary>
public class GannGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _anchorPeriod;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingActivation;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _breakEvenOffset;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private bool _trendReady;
	private bool _macdReady;
	private bool _breakEvenArmed;

	private bool _trendBullish;
	private bool _trendBearish;
	private bool _macdBullish;
	private bool _macdBearish;

	private decimal _momentumPercent;
	private decimal _anchorHigh;
	private decimal _anchorLow;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _previousPosition;

	/// <summary>
	/// The primary candle series for breakout calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle series used for trend and momentum filters.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Candle series that feeds the MACD confirmation filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average length.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum lookback period on the higher timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum percentage deviation from zero momentum required to trade.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Number of candles used to approximate the Gann grid with highest/lowest buffers.
	/// </summary>
	public int AnchorPeriod
	{
		get => _anchorPeriod.Value;
		set => _anchorPeriod.Value = value;
	}

	/// <summary>
	/// Absolute distance from entry price to the take-profit level.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Absolute distance from entry price to the stop-loss level.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Enables dynamic trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit distance that must be reached before the trailing stop activates.
	/// </summary>
	public decimal TrailingActivation
	{
		get => _trailingActivation.Value;
		set => _trailingActivation.Value = value;
	}

	/// <summary>
	/// Distance between the highest profit and the trailing stop once activated.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Enables automatic move-to-break-even logic.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit distance required before break-even protection is armed.
	/// </summary>
	public decimal BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Offset from the entry price where the break-even exit is placed.
	/// </summary>
	public decimal BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
	}

	/// <summary>
	/// Fast EMA length inside the MACD filter.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length inside the MACD filter.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length inside the MACD filter.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GannGridStrategy"/> class.
	/// </summary>
	public GannGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Primary Candle", "Timeframe used for breakouts", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Trend Candle", "Higher timeframe used for trend filters", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("MACD Candle", "Timeframe used for MACD confirmation", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Fast linear weighted moving average length", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Slow linear weighted moving average length", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Lookback for momentum calculation", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum %", "Minimal momentum deviation in percent", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1.5m, 0.1m);

		_anchorPeriod = Param(nameof(AnchorPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("Anchor Period", "Candles forming the synthetic Gann grid", "Breakout");

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0.005m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Absolute take-profit distance", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.001m, 0.02m, 0.001m);

		_stopLossOffset = Param(nameof(StopLossOffset), 0.002m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Absolute stop-loss distance", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.001m, 0.02m, 0.001m);

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Toggle trailing stop management", "Risk Management");

		_trailingActivation = Param(nameof(TrailingActivation), 0.003m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Activation", "Profit required before trailing starts", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.001m, 0.02m, 0.001m);

		_trailingStep = Param(nameof(TrailingStep), 0.0015m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Step", "Distance between peak profit and trailing stop", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.0005m, 0.01m, 0.0005m);

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Enable Break-Even", "Toggle automatic move-to-break-even", "Risk Management");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 0.0025m)
		.SetGreaterThanZero()
		.SetDisplay("Break-Even Trigger", "Profit needed to arm break-even", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.001m, 0.02m, 0.001m);

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 0m)
		.SetDisplay("Break-Even Offset", "Offset added to the entry when exiting at break-even", "Risk Management");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period inside MACD", "MACD Filter")
		.SetCanOptimize(true)
		.SetOptimize(8, 18, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period inside MACD", "MACD Filter")
		.SetCanOptimize(true)
		.SetOptimize(18, 40, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period inside MACD", "MACD Filter")
		.SetCanOptimize(true)
		.SetOptimize(4, 18, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (!Equals(TrendCandleType, CandleType))
			yield return (Security, TrendCandleType);

		if (!Equals(MacdCandleType, CandleType) && !Equals(MacdCandleType, TrendCandleType))
			yield return (Security, MacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendReady = false;
		_macdReady = false;
		_breakEvenArmed = false;

		_trendBullish = false;
		_trendBearish = false;
		_macdBullish = false;
		_macdBearish = false;

		_momentumPercent = 0m;
		_anchorHigh = 0m;
		_anchorLow = 0m;
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = Math.Max(1, FastMaPeriod), CandlePrice = CandlePrice.Typical };
		_slowMa = new WeightedMovingAverage { Length = Math.Max(1, SlowMaPeriod), CandlePrice = CandlePrice.Typical };
		_momentum = new Momentum { Length = Math.Max(1, MomentumPeriod) };
		_highest = new Highest { Length = Math.Max(1, AnchorPeriod) };
		_lowest = new Lowest { Length = Math.Max(1, AnchorPeriod) };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Math.Max(1, MacdFastPeriod) },
				LongMa = { Length = Math.Max(1, MacdSlowPeriod) },
			},
			SignalMa = { Length = Math.Max(1, MacdSignalPeriod) }
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(ProcessMainCandle)
			.Start();

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription
			.Bind(_fastMa, _slowMa, _momentum, ProcessTrendCandle)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.BindEx(_macd, ProcessMacdCandle)
			.Start();
	}

	private void ProcessTrendCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_trendReady = _fastMa.IsFormed && _slowMa.IsFormed && _momentum.IsFormed;

		if (!_trendReady)
			return;

		_trendBullish = fastValue > slowValue;
		_trendBearish = fastValue < slowValue;

		_momentumPercent = candle.ClosePrice != 0m
			? Math.Abs(momentumValue / candle.ClosePrice) * 100m
			: 0m;
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue typed)
			return;

		if (!typed.IsFinal)
			return;

		var macdLine = typed.Macd;
		var signalLine = typed.Signal;

		_macdBullish = macdLine > 0m && macdLine > signalLine;
		_macdBearish = macdLine < 0m && macdLine < signalLine;
		_macdReady = _macd.IsFormed;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		HandleActivePosition(candle);

		var previousAnchorHigh = _anchorHigh;
		var previousAnchorLow = _anchorLow;

		var highestValue = _highest.Process(candle.HighPrice, candle.OpenTime, true);
		if (highestValue.IsFinal)
			_anchorHigh = highestValue.ToDecimal();

		var lowestValue = _lowest.Process(candle.LowPrice, candle.OpenTime, true);
		if (lowestValue.IsFinal)
			_anchorLow = lowestValue.ToDecimal();

		var anchorsFormed = _highest.IsFormed && _lowest.IsFormed && previousAnchorHigh > 0m && previousAnchorLow > 0m;

		if (!anchorsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_trendReady || !_macdReady)
			return;

		if (_momentumPercent < MomentumThreshold)
			return;

		var breakoutUp = candle.HighPrice > previousAnchorHigh && candle.ClosePrice > previousAnchorHigh;
		var breakoutDown = candle.LowPrice < previousAnchorLow && candle.ClosePrice < previousAnchorLow;

		if (breakoutUp && _trendBullish && _macdBullish && Position <= 0m)
		{
			if (Position < 0m)
				BuyMarket(-Position);

			BuyMarket(Volume);
			LogInfo($"Long breakout at {candle.ClosePrice}.");
			return;
		}

		if (breakoutDown && _trendBearish && _macdBearish && Position >= 0m)
		{
			if (Position > 0m)
				SellMarket(Position);

			SellMarket(Volume);
			LogInfo($"Short breakout at {candle.ClosePrice}.");
		}
	}

	private void HandleActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			HandleLongPosition(candle);
		}
		else if (Position < 0m)
		{
			HandleShortPosition(candle);
		}
	}

	private void HandleLongPosition(ICandleMessage candle)
	{
		_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

		var exitReason = string.Empty;

		if (TakeProfitOffset > 0m)
		{
			var target = _entryPrice + TakeProfitOffset;
			if (candle.HighPrice >= target)
				exitReason = "Take profit reached";
		}

		if (exitReason.Length == 0 && StopLossOffset > 0m)
		{
			var stop = _entryPrice - StopLossOffset;
			if (candle.LowPrice <= stop)
				exitReason = "Stop loss hit";
		}

		if (EnableBreakEven && exitReason.Length == 0)
		{
			if (!_breakEvenArmed)
			{
				var trigger = _entryPrice + BreakEvenTrigger;
				if (candle.HighPrice >= trigger)
					_breakEvenArmed = true;
			}

			if (_breakEvenArmed)
			{
				var breakEvenPrice = _entryPrice + BreakEvenOffset;
				if (candle.LowPrice <= breakEvenPrice)
					exitReason = "Break-even protection";
			}
		}

		if (EnableTrailing && exitReason.Length == 0 && TrailingActivation > 0m && TrailingStep > 0m)
		{
			if (_highestPrice - _entryPrice >= TrailingActivation)
			{
				var trail = _highestPrice - TrailingStep;
				if (candle.LowPrice <= trail)
					exitReason = "Trailing stop hit";
			}
		}

		if (exitReason.Length == 0)
		{
			if ((_trendBearish || _macdBearish) && _trendReady && _macdReady)
				exitReason = "Trend filter reversal";
		}

		if (exitReason.Length == 0)
			return;

		var volume = Math.Abs(Position);
		if (volume > 0m)
		{
			SellMarket(volume);
			LogInfo($"Exit long: {exitReason}.");
		}

		ResetPositionState();
	}

	private void HandleShortPosition(ICandleMessage candle)
	{
		_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);

		var exitReason = string.Empty;

		if (TakeProfitOffset > 0m)
		{
			var target = _entryPrice - TakeProfitOffset;
			if (candle.LowPrice <= target)
				exitReason = "Take profit reached";
		}

		if (exitReason.Length == 0 && StopLossOffset > 0m)
		{
			var stop = _entryPrice + StopLossOffset;
			if (candle.HighPrice >= stop)
				exitReason = "Stop loss hit";
		}

		if (EnableBreakEven && exitReason.Length == 0)
		{
			if (!_breakEvenArmed)
			{
				var trigger = _entryPrice - BreakEvenTrigger;
				if (candle.LowPrice <= trigger)
					_breakEvenArmed = true;
			}

			if (_breakEvenArmed)
			{
				var breakEvenPrice = _entryPrice - BreakEvenOffset;
				if (candle.HighPrice >= breakEvenPrice)
					exitReason = "Break-even protection";
			}
		}

		if (EnableTrailing && exitReason.Length == 0 && TrailingActivation > 0m && TrailingStep > 0m)
		{
			if (_entryPrice - _lowestPrice >= TrailingActivation)
			{
				var trail = _lowestPrice + TrailingStep;
				if (candle.HighPrice >= trail)
					exitReason = "Trailing stop hit";
			}
		}

		if (exitReason.Length == 0)
		{
			if ((_trendBullish || _macdBullish) && _trendReady && _macdReady)
				exitReason = "Trend filter reversal";
		}

		if (exitReason.Length == 0)
			return;

		var volume = Math.Abs(Position);
		if (volume > 0m)
		{
			BuyMarket(volume);
			LogInfo($"Exit short: {exitReason}.");
		}

		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			ResetPositionState();
			_previousPosition = 0m;
			return;
		}

		if (Position > 0m && _previousPosition <= 0m)
		{
			_entryPrice = PositionPrice ?? _entryPrice;
			_highestPrice = _entryPrice;
			_lowestPrice = 0m;
			_breakEvenArmed = false;
		}
		else if (Position < 0m && _previousPosition >= 0m)
		{
			_entryPrice = PositionPrice ?? _entryPrice;
			_lowestPrice = _entryPrice;
			_highestPrice = 0m;
			_breakEvenArmed = false;
		}

		_previousPosition = Position;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_breakEvenArmed = false;
	}
}

