
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
/// Conversion of the MT4 expert advisor "5MIN SCALPING" to the StockSharp high level API.
/// The strategy looks for fast breakouts confirmed by multi-timeframe momentum and monthly MACD direction.
/// </summary>
public class FiveMinScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _scalperLookback;
	private readonly StrategyParam<int> _breakoutWindow;
	private readonly StrategyParam<int> _momentumHistorySize;
	private readonly StrategyParam<int> _fastTrendLength;
	private readonly StrategyParam<int> _middleTrendLength;
	private readonly StrategyParam<int> _slowTrendLength;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macroMacdCandleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<decimal> _tradeVolume;

	private WeightedMovingAverage _fastLwma = null!;
	private WeightedMovingAverage _slowLwma = null!;
	private WeightedMovingAverage _trendMa8 = null!;
	private WeightedMovingAverage _trendMa13 = null!;
	private WeightedMovingAverage _trendMa21 = null!;
	private Momentum _momentumIndicator = null!;
	private MovingAverageConvergenceDivergenceSignal _macroMacdIndicator = null!;

	private readonly List<decimal> _fastTrendHistory = new();
	private readonly List<decimal> _middleTrendHistory = new();
	private readonly List<decimal> _slowTrendHistory = new();
	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();
	private readonly List<decimal> _momentumDeviationHistory = new();

	private bool _momentumReady;
	private bool _hasMacroMacd;
	private decimal _macroMacdValue;
	private decimal _macroSignalValue;

	private decimal _entryPrice;
	private decimal _trailingStopPrice;
	private decimal _breakEvenPrice;
	private bool _breakEvenArmed;
	private bool _trailingArmed;

	/// <summary>
	/// Initializes a new instance of the <see cref="FiveMinScalpingStrategy"/> class.
	/// </summary>
	public FiveMinScalpingStrategy()
	{
		_scalperLookback = Param(nameof(ScalperLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Scalper Lookback", "Candles considered when searching for breakouts", "Trend");

		_breakoutWindow = Param(nameof(BreakoutWindow), 5)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Window", "Window used to track recent highs and lows", "Trend");

		_momentumHistorySize = Param(nameof(MomentumHistorySize), 3)
			.SetGreaterThanZero()
			.SetDisplay("Momentum History Size", "Number of deviations stored for momentum confirmation", "Filters");

		_fastTrendLength = Param(nameof(FastTrendLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Trend Length", "LWMA length for the fastest trend filter", "Trend");

		_middleTrendLength = Param(nameof(MiddleTrendLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Middle Trend Length", "LWMA length for the mid-term trend filter", "Trend");

		_slowTrendLength = Param(nameof(SlowTrendLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Trend Length", "LWMA length for the slow trend filter", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Signal Timeframe", "Primary timeframe used for entry signals", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Momentum Timeframe", "Higher timeframe used for the momentum filter", "Filters");

		_macroMacdCandleType = Param(nameof(MacroMacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("Macro MACD Timeframe", "Timeframe for the long-term MACD confirmation", "Filters");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast LWMA filter", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow LWMA filter", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(40, 120, 5);

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Lookback for the momentum confirmation", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Buy Threshold", "Minimum |Momentum-100| required for long trades", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Sell Threshold", "Minimum |Momentum-100| required for short trades", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance to the take-profit target in price steps", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance to the protective stop in price steps", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Distance used for the trailing stop logic", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Activates trailing stop management", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
			.SetDisplay("Enable Break Even", "Moves the stop to break-even once profit threshold is reached", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
			.SetNotNegative()
			.SetDisplay("Break Even Trigger", "Profit in pips required to arm break-even", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
			.SetNotNegative()
			.SetDisplay("Break Even Offset", "Extra pips added above/below entry when break-even is armed", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume used for entries", "General");
	}

	/// <summary>
	/// Primary candle type used for the scalping logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the momentum filter.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe for the macro MACD confirmation.
	/// </summary>
	public DataType MacroMacdCandleType
	{
		get => _macroMacdCandleType.Value;
		set => _macroMacdCandleType.Value = value;
	}

	/// <summary>
	/// Fast LWMA length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow LWMA length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum indicator length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Momentum deviation required for long entries.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Momentum deviation required for short entries.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enables the trailing stop module.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Enables break-even management.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in pips required to arm the break-even stop.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional pips added when the stop is moved to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Volume used for orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Number of candles considered for breakout detection.
	/// </summary>
	public int ScalperLookback
	{
		get => _scalperLookback.Value;
		set => _scalperLookback.Value = value;
	}

	/// <summary>
	/// Window length used when computing recent highs and lows.
	/// </summary>
	public int BreakoutWindow
	{
		get => _breakoutWindow.Value;
		set => _breakoutWindow.Value = value;
	}

	/// <summary>
	/// Stored deviations for momentum confirmation.
	/// </summary>
	public int MomentumHistorySize
	{
		get => _momentumHistorySize.Value;
		set => _momentumHistorySize.Value = value;
	}

	/// <summary>
	/// Length of the fastest trend LWMA.
	/// </summary>
	public int FastTrendLength
	{
		get => _fastTrendLength.Value;
		set => _fastTrendLength.Value = value;
	}

	/// <summary>
	/// Length of the middle trend LWMA.
	/// </summary>
	public int MiddleTrendLength
	{
		get => _middleTrendLength.Value;
		set => _middleTrendLength.Value = value;
	}

	/// <summary>
	/// Length of the slow trend LWMA.
	/// </summary>
	public int SlowTrendLength
	{
		get => _slowTrendLength.Value;
		set => _slowTrendLength.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, MomentumCandleType);
		yield return (Security, MacroMacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastTrendHistory.Clear();
		_middleTrendHistory.Clear();
		_slowTrendHistory.Clear();
		_highHistory.Clear();
		_lowHistory.Clear();
		_momentumDeviationHistory.Clear();

		_momentumReady = false;
		_hasMacroMacd = false;
		_macroMacdValue = 0m;
		_macroSignalValue = 0m;

		ResetRiskState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastLwma = new WeightedMovingAverage { Length = FastMaLength };
		_slowLwma = new WeightedMovingAverage { Length = SlowMaLength };
		_trendMa8 = new WeightedMovingAverage { Length = FastTrendLength };
		_trendMa13 = new WeightedMovingAverage { Length = MiddleTrendLength };
		_trendMa21 = new WeightedMovingAverage { Length = SlowTrendLength };
		_momentumIndicator = new Momentum { Length = MomentumLength };
		_macroMacdIndicator = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_fastLwma, _slowLwma, _trendMa8, _trendMa13, _trendMa21, ProcessMainCandle)
			.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
			.Bind(_momentumIndicator, ProcessMomentum)
			.Start();

		var macroSubscription = SubscribeCandles(MacroMacdCandleType);
		macroSubscription
			.Bind(_macroMacdIndicator, ProcessMacroMacd)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastLwma);
			DrawIndicator(area, _slowLwma);
			DrawIndicator(area, _trendMa8);
			DrawIndicator(area, _trendMa13);
			DrawIndicator(area, _trendMa21);
			DrawOwnTrades(area);
		}

		Volume = TradeVolume;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal trend8, decimal trend13, decimal trend21)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store indicator snapshots for the multi-bar pattern checks.
		UpdateHistory(_fastTrendHistory, trend8);
		UpdateHistory(_middleTrendHistory, trend13);
		UpdateHistory(_slowTrendHistory, trend21);
		UpdateHistory(_highHistory, candle.HighPrice);
		UpdateHistory(_lowHistory, candle.LowPrice);

		// Update protective logic before making new decisions.
		ManageOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_momentumReady || !_hasMacroMacd)
			return;

		if (_fastTrendHistory.Count <= ScalperLookback || _highHistory.Count <= BreakoutWindow)
			return;

		var momentumBullish = HasMomentumImpulse(MomentumBuyThreshold);
		var momentumBearish = HasMomentumImpulse(MomentumSellThreshold);

		var overlapBuy = HasOverlapForBuy();
		var overlapSell = HasOverlapForSell();

		var breakoutUp = overlapBuy && momentumBullish && fastMa > slowMa && _macroMacdValue > _macroSignalValue && HasBullishBreakout(candle.ClosePrice);
		var breakoutDown = overlapSell && momentumBearish && fastMa < slowMa && _macroMacdValue < _macroSignalValue && HasBearishBreakout(candle.ClosePrice);

		if (breakoutUp && Position <= 0m)
		{
			var volume = TradeVolume + Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				ArmRiskState(candle.ClosePrice, true);
			}
		}
		else if (breakoutDown && Position >= 0m)
		{
			var volume = TradeVolume + Math.Abs(Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				ArmRiskState(candle.ClosePrice, false);
			}
		}
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var deviation = Math.Abs(momentum - 100m);
		_momentumDeviationHistory.Add(deviation);
		if (_momentumDeviationHistory.Count > MomentumHistorySize)
			_momentumDeviationHistory.RemoveAt(0);

		_momentumReady = _momentumDeviationHistory.Count >= MomentumHistorySize;
	}

	private void ProcessMacroMacd(ICandleMessage candle, decimal macd, decimal signal, decimal histogram)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macroMacdValue = macd;
		_macroSignalValue = signal;
		_hasMacroMacd = true;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		var position = Position;
		if (position > 0m)
		{
			var volume = position;
			if (CheckTakeProfitHit(candle.HighPrice, _entryPrice))
			{
				SellMarket(volume);
				ResetRiskState();
				return;
			}

			if (CheckStopLossHit(candle.LowPrice, _entryPrice, true))
			{
				SellMarket(volume);
				ResetRiskState();
				return;
			}

			if (EnableBreakEven)
				HandleBreakEvenLong(candle);

			if (EnableTrailing && TrailingStopPips > 0m)
				HandleTrailingLong(candle);
		}
		else if (position < 0m)
		{
			var volume = Math.Abs(position);
			if (CheckTakeProfitHit(_entryPrice, candle.LowPrice))
			{
				BuyMarket(volume);
				ResetRiskState();
				return;
			}

			if (CheckStopLossHit(candle.HighPrice, _entryPrice, false))
			{
				BuyMarket(volume);
				ResetRiskState();
				return;
			}

			if (EnableBreakEven)
				HandleBreakEvenShort(candle);

			if (EnableTrailing && TrailingStopPips > 0m)
				HandleTrailingShort(candle);
		}
		else
		{
			ResetRiskState();
		}
	}

	private void HandleBreakEvenLong(ICandleMessage candle)
	{
		// Break-even is armed only after the price travels the configured distance.
		var triggerDistance = GetDistance(BreakEvenTriggerPips);
		if (!_breakEvenArmed && triggerDistance > 0m && candle.HighPrice >= _entryPrice + triggerDistance)
		{
			_breakEvenArmed = true;
			_breakEvenPrice = _entryPrice + GetDistance(BreakEvenOffsetPips);
		}

		if (_breakEvenArmed && candle.LowPrice <= _breakEvenPrice)
		{
			SellMarket(Position);
			ResetRiskState();
		}
	}

	private void HandleBreakEvenShort(ICandleMessage candle)
	{
		// Break-even is armed only after the price travels the configured distance.
		var triggerDistance = GetDistance(BreakEvenTriggerPips);
		if (!_breakEvenArmed && triggerDistance > 0m && candle.LowPrice <= _entryPrice - triggerDistance)
		{
			_breakEvenArmed = true;
			_breakEvenPrice = _entryPrice - GetDistance(BreakEvenOffsetPips);
		}

		if (_breakEvenArmed && candle.HighPrice >= _breakEvenPrice)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskState();
		}
	}

	private void HandleTrailingLong(ICandleMessage candle)
	{
		// Trailing stops follow the trade by the configured distance.
		var trailingDistance = GetDistance(TrailingStopPips);
		if (trailingDistance <= 0m)
			return;

		var candidate = candle.ClosePrice - trailingDistance;
		if (!_trailingArmed || candidate > _trailingStopPrice)
		{
			_trailingArmed = true;
			_trailingStopPrice = candidate;
		}

		if (_trailingArmed && candle.LowPrice <= _trailingStopPrice)
		{
			SellMarket(Position);
			ResetRiskState();
		}
	}

	private void HandleTrailingShort(ICandleMessage candle)
	{
		// Trailing stops follow the trade by the configured distance.
		var trailingDistance = GetDistance(TrailingStopPips);
		if (trailingDistance <= 0m)
			return;

		var candidate = candle.ClosePrice + trailingDistance;
		if (!_trailingArmed || candidate < _trailingStopPrice)
		{
			_trailingArmed = true;
			_trailingStopPrice = candidate;
		}

		if (_trailingArmed && candle.HighPrice >= _trailingStopPrice)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskState();
		}
	}

	private bool CheckTakeProfitHit(decimal upperPrice, decimal lowerPrice)
	{
		var distance = GetDistance(TakeProfitPips);
		if (distance <= 0m)
			return false;

		return upperPrice - lowerPrice >= distance;
	}

	private bool CheckStopLossHit(decimal price, decimal entry, bool isLong)
	{
		var distance = GetDistance(StopLossPips);
		if (distance <= 0m)
			return false;

		return isLong ? entry - price >= distance : price - entry >= distance;
	}

	private bool HasMomentumImpulse(decimal threshold)
	{
		if (!_momentumReady || threshold <= 0m)
			return false;

		foreach (var value in _momentumDeviationHistory)
		{
			if (value >= threshold)
				return true;
		}

		return false;
	}

	private bool HasOverlapForBuy()
	{
		if (_lowHistory.Count < 3 || _highHistory.Count < 2)
			return false;

		var lowTwoAgo = _lowHistory[^3];
		var highPrev = _highHistory[^2];
		return lowTwoAgo < highPrev;
	}

	private bool HasOverlapForSell()
	{
		if (_lowHistory.Count < 2 || _highHistory.Count < 3)
			return false;

		var lowPrev = _lowHistory[^2];
		var highTwoAgo = _highHistory[^3];
		return lowPrev < highTwoAgo;
	}

	private bool HasBullishBreakout(decimal currentClose)
	{
		if (_highHistory.Count < BreakoutWindow + 1)
			return false;

		// Inspect the previous candles to locate the breakout reference level.
		var highestHigh = decimal.MinValue;
		var lastIndex = _highHistory.Count - 1;
		for (var index = Math.Max(0, lastIndex - BreakoutWindow); index < lastIndex; index++)
		{
			var high = _highHistory[index];
			if (high > highestHigh)
				highestHigh = high;
		}

		if (highestHigh == decimal.MinValue)
			return false;

		var latestIndex = _fastTrendHistory.Count - 1;
		for (var offset = 3; offset <= ScalperLookback; offset++)
		{
			var index = latestIndex - offset;
			if (index < 0)
				break;

			var fast = _fastTrendHistory[index];
			var middle = _middleTrendHistory[index];
			var slow = _slowTrendHistory[index];
			var low = _lowHistory[index];

			if (fast <= middle || middle <= slow)
				continue;

			if (low > fast || low <= slow)
				continue;

			if (currentClose > highestHigh)
				return true;
		}

		return false;
	}

	private bool HasBearishBreakout(decimal currentClose)
	{
		if (_lowHistory.Count < BreakoutWindow + 1)
			return false;

		// Inspect the previous candles to find the bearish breakout trigger.
		var lowestLow = decimal.MaxValue;
		var lastIndex = _lowHistory.Count - 1;
		for (var index = Math.Max(0, lastIndex - BreakoutWindow); index < lastIndex; index++)
		{
			var low = _lowHistory[index];
			if (low < lowestLow)
				lowestLow = low;
		}

		if (lowestLow == decimal.MaxValue)
			return false;

		var latestIndex = _fastTrendHistory.Count - 1;
		for (var offset = 3; offset <= ScalperLookback; offset++)
		{
			var index = latestIndex - offset;
			if (index < 0)
				break;

			var fast = _fastTrendHistory[index];
			var middle = _middleTrendHistory[index];
			var slow = _slowTrendHistory[index];
			var high = _highHistory[index];

			if (fast >= middle || middle >= slow)
				continue;

			if (high < fast || high >= slow)
				continue;

			if (currentClose < lowestLow)
				return true;
		}

		return false;
	}

	private void UpdateHistory(List<decimal> target, decimal value)
	{
		target.Add(value);
		var maxSize = ScalperLookback + BreakoutWindow + 5;
		if (target.Count > maxSize)
			target.RemoveAt(0);
	}

	private decimal GetDistance(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0.0001m;
		return pips * step;
	}

	private void ArmRiskState(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;
		_breakEvenArmed = false;
		_trailingArmed = false;
		_breakEvenPrice = entryPrice;
		_trailingStopPrice = entryPrice;

		if (!isLong)
		{
			// Invert the trailing stop anchor for short positions.
			_trailingStopPrice = entryPrice;
		}
	}

	private void ResetRiskState()
	{
		_entryPrice = 0m;
		_trailingStopPrice = 0m;
		_breakEvenPrice = 0m;
		_breakEvenArmed = false;
		_trailingArmed = false;
	}
}

