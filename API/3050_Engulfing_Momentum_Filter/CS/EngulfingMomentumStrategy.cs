using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe engulfing strategy with momentum and MACD filters.
/// Converts the ENGULFING MetaTrader strategy to the StockSharp high level API.
/// </summary>
public class EngulfingMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _breakEvenTriggerSteps;
	private readonly StrategyParam<decimal> _breakEvenOffsetSteps;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentumIndicator = null!;
	private MovingAverageConvergenceDivergence _macdIndicator = null!;

	private decimal? _momentum1;
	private decimal? _momentum2;
	private decimal? _momentum3;
	private bool _momentumReady;
	private decimal? _macdValue;
	private decimal? _macdSignal;
	private bool _macdReady;

	private decimal? _prevOpen1;
	private decimal? _prevClose1;
	private decimal? _prevHigh1;
	private decimal? _prevLow1;
	private decimal? _prevHigh2;
	private decimal? _prevLow2;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private bool _breakEvenActivated;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private decimal _tickSize;

	/// <summary>
	/// Fast weighted moving average length.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow weighted moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator length on the higher timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Threshold for bullish momentum confirmation.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Threshold for bearish momentum confirmation.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal line length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in instrument steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take profit distance in instrument steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in instrument steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Distance in steps to trigger break-even protection.
	/// </summary>
	public decimal BreakEvenTriggerSteps
	{
		get => _breakEvenTriggerSteps.Value;
		set => _breakEvenTriggerSteps.Value = value;
	}

	/// <summary>
	/// Offset in steps applied when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetSteps
	{
		get => _breakEvenOffsetSteps.Value;
		set => _breakEvenOffsetSteps.Value = value;
	}

	/// <summary>
	/// Primary candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type for the momentum filter.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EngulfingMomentumStrategy"/> class.
	/// </summary>
	public EngulfingMomentumStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Length of the fast weighted MA", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Length of the slow weighted MA", "Indicators")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Length of the momentum indicator", "Filters");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Buy Momentum", "Minimum deviation from 100 for bullish signals", "Filters")
			.SetCanOptimize(true);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Sell Momentum", "Minimum deviation from 100 for bearish signals", "Filters")
			.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA for MACD", "Filters");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA for MACD", "Filters");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA for MACD", "Filters");

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop distance in price steps", "Risk")
			.SetCanOptimize(true);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk")
			.SetCanOptimize(true);

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 40m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "Risk");

		_breakEvenTriggerSteps = Param(nameof(BreakEvenTriggerSteps), 10m)
			.SetNotNegative()
			.SetDisplay("BreakEven Trigger", "Profit in steps before moving stop", "Risk");

		_breakEvenOffsetSteps = Param(nameof(BreakEvenOffsetSteps), 5m)
			.SetNotNegative()
			.SetDisplay("BreakEven Offset", "Extra steps added when stop moves to break-even", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Candles", "Candles for engulfing pattern", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Higher TF Candles", "Candles for momentum filter", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candles", "Candles for MACD trend filter", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherCandleType), (Security, MacdCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentum1 = null;
		_momentum2 = null;
		_momentum3 = null;
		_momentumReady = false;
		_macdValue = null;
		_macdSignal = null;
		_macdReady = false;

		_prevOpen1 = null;
		_prevClose1 = null;
		_prevHigh1 = null;
		_prevLow1 = null;
		_prevHigh2 = null;
		_prevLow2 = null;

		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 1m;

		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
		_momentumIndicator = new Momentum { Length = MomentumPeriod };
		_macdIndicator = new MovingAverageConvergenceDivergence
		{
			FastLength = MacdFastLength,
			SlowLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_fastMa, _slowMa, ProcessMainCandle)
			.Start();

		var momentumSubscription = SubscribeCandles(HigherCandleType);
		momentumSubscription
			.Bind(_momentumIndicator, ProcessMomentum)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.Bind(_macdIndicator, ProcessMacd)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_momentum3 = _momentum2;
		_momentum2 = _momentum1;
		_momentum1 = momentumValue;
		_momentumReady = _momentumIndicator.IsFormed;
	}

	private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macdValue = macdValue;
		_macdSignal = signalValue;
		_macdReady = _macdIndicator.IsFormed;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update trailing logic and exit if protective rules are hit.
		var exited = UpdatePositionState(candle);

		// Do not evaluate entries until the environment is ready.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(candle);
			return;
		}

		// Skip signal evaluation until every indicator produced valid values.
		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentumReady || !_macdReady)
		{
			UpdateHistory(candle);
			return;
		}

		// Only search for new opportunities when no exit was triggered on this bar.
		if (!exited)
		{
			if (CanEnterLong(candle, fastValue, slowValue))
			{
				EnterLong(candle);
			}
			else if (CanEnterShort(candle, fastValue, slowValue))
			{
				EnterShort(candle);
			}
		}

		UpdateHistory(candle);
	}

	private bool CanEnterLong(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (Position > 0)
			return false;

		if (_prevOpen1 is not decimal prevOpen || _prevClose1 is not decimal prevClose)
			return false;

		if (_prevHigh1 is not decimal prevHigh || _prevLow2 is not decimal lowTwoBarsAgo)
			return false;

		// Classic bullish engulfing: bearish candle followed by a larger bullish body.
		var previousBearish = prevClose < prevOpen;
		var currentBullish = candle.ClosePrice > candle.OpenPrice;

		if (!previousBearish || !currentBullish)
			return false;

		var bodyEngulfed = candle.ClosePrice >= prevOpen && candle.OpenPrice <= prevClose;
		if (!bodyEngulfed)
			return false;

		if (lowTwoBarsAgo >= prevHigh)
			return false;

		// Trend filter: fast LWMA must trade above the slow LWMA.
		if (fastValue <= slowValue)
			return false;

		// Higher timeframe momentum must confirm the breakout.
		if (!IsMomentumValid(true))
			return false;

		// Monthly MACD trend filter keeps trades aligned with the dominant direction.
		if (!IsMacdBullish())
			return false;

		return true;
	}

	private bool CanEnterShort(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (Position < 0)
			return false;

		if (_prevOpen1 is not decimal prevOpen || _prevClose1 is not decimal prevClose)
			return false;

		if (_prevLow1 is not decimal prevLow || _prevHigh2 is not decimal highTwoBarsAgo)
			return false;

		// Classic bearish engulfing mirrored for short setups.
		var previousBullish = prevClose > prevOpen;
		var currentBearish = candle.ClosePrice < candle.OpenPrice;

		if (!previousBullish || !currentBearish)
			return false;

		var bodyEngulfed = candle.OpenPrice >= prevClose && candle.ClosePrice <= prevOpen;
		if (!bodyEngulfed)
			return false;

		if (prevLow <= highTwoBarsAgo)
			return false;

		// Trend filter: fast LWMA must stay below the slow LWMA for shorts.
		if (fastValue >= slowValue)
			return false;

		// Momentum filter reuses the higher timeframe to avoid fading strong trends.
		if (!IsMomentumValid(false))
			return false;

		// MACD must confirm a bearish regime on the monthly filter.
		if (!IsMacdBearish())
			return false;

		return true;
	}

	private bool IsMomentumValid(bool isLong)
	{
		var threshold = isLong ? MomentumBuyThreshold : MomentumSellThreshold;
		if (threshold <= 0m)
			return true;

		return CheckMomentum(_momentum1, threshold) || CheckMomentum(_momentum2, threshold) || CheckMomentum(_momentum3, threshold);
	}

	private static bool CheckMomentum(decimal? value, decimal threshold)
	{
		if (value is not decimal actual)
			return false;

		return Math.Abs(actual - 100m) >= threshold;
	}

	private bool IsMacdBullish()
	{
		return _macdValue is decimal macd && _macdSignal is decimal signal && macd > signal;
	}

	private bool IsMacdBearish()
	{
		return _macdValue is decimal macd && _macdSignal is decimal signal && macd < signal;
	}

	private void EnterLong(ICandleMessage candle)
	{
		// Reverse any active short position and establish a new long exposure.
		var volume = Volume + Math.Abs(Position);
		_entryPrice = candle.ClosePrice;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		// Pre-calculate protective levels in price units derived from step counts.
		_stopPrice = StopLossSteps > 0m ? _entryPrice - GetStepValue(StopLossSteps) : null;
		_takeProfitPrice = TakeProfitSteps > 0m ? _entryPrice + GetStepValue(TakeProfitSteps) : null;
		_breakEvenActivated = false;

		BuyMarket(volume);
	}

	private void EnterShort(ICandleMessage candle)
	{
		// Reverse any active long position and establish a new short exposure.
		var volume = Volume + Math.Abs(Position);
		_entryPrice = candle.ClosePrice;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		// Convert stop and target distances from steps to absolute price offsets.
		_stopPrice = StopLossSteps > 0m ? _entryPrice + GetStepValue(StopLossSteps) : null;
		_takeProfitPrice = TakeProfitSteps > 0m ? _entryPrice - GetStepValue(TakeProfitSteps) : null;
		_breakEvenActivated = false;

		SellMarket(volume);
	}

	private bool UpdatePositionState(ICandleMessage candle)
	{
		if (Position > 0)
		{
			// Track new highs and lows to maintain trailing and break-even levels.
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			// Move the stop to break-even after price travels the configured distance.
			if (!_breakEvenActivated && BreakEvenTriggerSteps > 0m && _entryPrice is decimal entry)
			{
				var triggerPrice = entry + GetStepValue(BreakEvenTriggerSteps);
				if (candle.HighPrice >= triggerPrice)
				{
					var newStop = entry + GetStepValue(BreakEvenOffsetSteps);
					_stopPrice = _stopPrice is decimal currentStop ? Math.Max(currentStop, newStop) : newStop;
					_breakEvenActivated = true;
				}
			}

			if (TrailingStopSteps > 0m)
			{
				// Trail the stop behind the highest price by the configured distance.
				var trailCandidate = _highestSinceEntry - GetStepValue(TrailingStopSteps);
				if (_stopPrice is not decimal currentStop || trailCandidate > currentStop)
					_stopPrice = trailCandidate;
			}

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			// Mirror the same management rules for short positions.
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			// Break-even logic for shorts keeps risk controlled when price falls in our favor.
			if (!_breakEvenActivated && BreakEvenTriggerSteps > 0m && _entryPrice is decimal entry)
			{
				var triggerPrice = entry - GetStepValue(BreakEvenTriggerSteps);
				if (candle.LowPrice <= triggerPrice)
				{
					var newStop = entry - GetStepValue(BreakEvenOffsetSteps);
					_stopPrice = _stopPrice is decimal currentStop ? Math.Min(currentStop, newStop) : newStop;
					_breakEvenActivated = true;
				}
			}

			if (TrailingStopSteps > 0m)
			{
				// Mirror the trailing stop for downward moves when holding shorts.
				var trailCandidate = _lowestSinceEntry + GetStepValue(TrailingStopSteps);
				if (_stopPrice is not decimal currentStop || trailCandidate < currentStop)
					_stopPrice = trailCandidate;
			}

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private decimal GetStepValue(decimal steps)
	{
		// Convert user-provided step counts into actual price increments.
		return steps * _tickSize;
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		// Preserve the last two completed candles for pattern validation.
		_prevHigh2 = _prevHigh1;
		_prevLow2 = _prevLow1;
		_prevOpen1 = candle.OpenPrice;
		_prevClose1 = candle.ClosePrice;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;
	}

	private void ResetPositionState()
	{
		// Clear cached prices so a new trade starts with a clean slate.
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakEvenActivated = false;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
	}
}
