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
/// Translates the MetaTrader "10PIPS" expert advisor to the StockSharp high level API.
/// </summary>
public class TenPipsMomentumStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableMoneyTrail;
	private readonly StrategyParam<decimal> _moneyTrailTarget;
	private readonly StrategyParam<decimal> _moneyTrailStop;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRiskPercent;
	private readonly StrategyParam<bool> _useMacdExit;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;
	private ICandleMessage _previousBaseCandle;

	private decimal? _momentumDistance1;
	private decimal? _momentumDistance2;
	private decimal? _momentumDistance3;

	private decimal? _macdMain;
	private decimal? _macdSignal;

	private decimal _tickSize;
	private decimal _pipSize;
	private decimal _initialEquity;
	private decimal _equityPeak;
	private decimal _moneyTrailPeak;

	private Sides? _activeSide;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _highestSinceEntry;
	private decimal? _lowestSinceEntry;
	private bool _breakEvenActivated;

	/// <summary>
/// Initializes a new instance of <see cref="TenPipsMomentumStrategy"/>.
/// </summary>
public TenPipsMomentumStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetDisplay("Trade Volume", "Default market order volume", "Trading")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Primary Candle", "Time frame used for entries", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Momentum Candle", "Higher time frame used for momentum", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Candle", "Macro time frame used for MACD confirmation", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 8)
		.SetDisplay("Fast MA", "Length of the fast LWMA", "Indicators")
		.SetGreaterThanZero();

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 50)
		.SetDisplay("Slow MA", "Length of the slow LWMA", "Indicators")
		.SetGreaterThanZero();

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetDisplay("Momentum Period", "Lookback period for the momentum ratio", "Indicators")
		.SetGreaterThanZero();

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetDisplay("Momentum Threshold", "Minimum distance from 100 required for momentum", "Trading Rules")
		.SetNotNegative();

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
		.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Protective take profit distance in pips", "Risk")
		.SetNotNegative();

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
		.SetNotNegative();

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break Even", "Move the stop to break even once profit target is reached", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetDisplay("Break Even Trigger", "Profit in pips required to lock the trade", "Risk")
		.SetNotNegative();

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
		.SetDisplay("Break Even Offset", "Additional pips applied when locking", "Risk")
		.SetNotNegative();

		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
		.SetDisplay("Money Take Profit", "Close all trades after reaching the money target", "Risk");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 10m)
		.SetDisplay("Money Target", "Profit target expressed in account currency", "Risk")
		.SetNotNegative();

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
		.SetDisplay("Percent Take Profit", "Close all trades after reaching the percentage target", "Risk");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
		.SetDisplay("Percent Target", "Profit target expressed as account percent", "Risk")
		.SetNotNegative();

		_enableMoneyTrail = Param(nameof(EnableMoneyTrailing), true)
		.SetDisplay("Money Trailing", "Enable balance based trailing stop", "Risk");

		_moneyTrailTarget = Param(nameof(MoneyTrailTarget), 40m)
		.SetDisplay("Money Trail Target", "Profit required to arm the money trail", "Risk")
		.SetNotNegative();

		_moneyTrailStop = Param(nameof(MoneyTrailStop), 10m)
		.SetDisplay("Money Trail Stop", "Give-back in currency allowed after arming", "Risk")
		.SetNotNegative();

		_useEquityStop = Param(nameof(UseEquityStop), true)
		.SetDisplay("Use Equity Stop", "Enable account level equity protection", "Risk");

		_equityRiskPercent = Param(nameof(EquityRiskPercent), 1m)
		.SetDisplay("Equity Risk %", "Maximum drawdown from the equity peak", "Risk")
		.SetNotNegative();

		_useMacdExit = Param(nameof(UseMacdExit), false)
		.SetDisplay("MACD Exit", "Close positions on opposite MACD signal", "Trading Rules");
	}

	/// <summary>
	/// Default trade volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Base candle type used for entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher time frame used for momentum confirmation.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Macro time frame used for the MACD filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Period of the fast LWMA.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow LWMA.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Lookback used for the momentum calculation.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute distance from 100 required for momentum confirmation.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
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
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Enables moving the stop to break-even.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in pips required to arm the break-even logic.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Extra pips added when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enables the money based take-profit.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Money based profit target.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Enables the percent based take-profit.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Percentage profit target based on initial equity.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Enables the money trailing stop.
	/// </summary>
	public bool EnableMoneyTrailing
	{
		get => _enableMoneyTrail.Value;
		set => _enableMoneyTrail.Value = value;
	}

	/// <summary>
	/// Profit required to arm the money trailing stop.
	/// </summary>
	public decimal MoneyTrailTarget
	{
		get => _moneyTrailTarget.Value;
		set => _moneyTrailTarget.Value = value;
	}

	/// <summary>
	/// Maximum give-back allowed once the money trail is armed.
	/// </summary>
	public decimal MoneyTrailStop
	{
		get => _moneyTrailStop.Value;
		set => _moneyTrailStop.Value = value;
	}

	/// <summary>
	/// Enables the equity based drawdown stop.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum equity drawdown from the peak in percent.
	/// </summary>
	public decimal EquityRiskPercent
	{
		get => _equityRiskPercent.Value;
		set => _equityRiskPercent.Value = value;
	}

	/// <summary>
	/// Enables closing positions when the monthly MACD flips direction.
	/// </summary>
	public bool UseMacdExit
	{
		get => _useMacdExit.Value;
		set => _useMacdExit.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (!Equals(MomentumCandleType, CandleType))
		yield return (Security, MomentumCandleType);

		if (!Equals(MacdCandleType, CandleType) && !Equals(MacdCandleType, MomentumCandleType))
		yield return (Security, MacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMaValue = null;
		_slowMaValue = null;
		_previousBaseCandle = null;

		_momentumDistance1 = null;
		_momentumDistance2 = null;
		_momentumDistance3 = null;

		_macdMain = null;
		_macdSignal = null;

		_activeSide = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestSinceEntry = null;
		_lowestSinceEntry = null;
		_breakEvenActivated = false;

		_moneyTrailPeak = 0m;
		_initialEquity = 0m;
		_equityPeak = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		// Align the strategy volume with the MetaTrader lot size.

		_tickSize = Security?.PriceStep ?? 0m;
		// Convert the broker tick size into the pip distance used by the EA.
		if (_tickSize <= 0m)
		_tickSize = 0.0001m;

		_pipSize = _tickSize;
		if (_tickSize == 0.00001m || _tickSize == 0.001m)
		_pipSize = _tickSize * 10m;

		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		// Subscribe to the three time frames required by the original expert.
		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.Bind(ProcessBaseCandle).Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription.Bind(ProcessMomentum).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.BindEx(_macd, ProcessMacd).Start();

		_initialEquity = GetPortfolioValue();
		_equityPeak = _initialEquity;
		_moneyTrailPeak = 0m;

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, baseSubscription);
			DrawIndicator(priceArea, _fastMa);
			DrawIndicator(priceArea, _slowMa);
			DrawOwnTrades(priceArea);
		}

		var momentumArea = CreateChartArea();
		if (momentumArea != null)
		{
			DrawIndicator(momentumArea, _momentum);
		}

		var macdArea = CreateChartArea();
		if (macdArea != null)
		{
			DrawIndicator(macdArea, _macd);
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		// Process only completed candles to stay synchronized with MetaTrader.
		if (candle.State != CandleStates.Finished)
		return;

		var typicalPrice = GetTypicalPrice(candle);
		// Feed the weighted moving averages with typical price, matching MODE_LWMA/PRICE_TYPICAL.
		var fastResult = _fastMa.Process(new DecimalIndicatorValue(_fastMa, typicalPrice, candle.OpenTime));
		var slowResult = _slowMa.Process(new DecimalIndicatorValue(_slowMa, typicalPrice, candle.OpenTime));

		if (!fastResult.IsFinal || fastResult is not DecimalIndicatorValue fastValue)
		return;

		if (!slowResult.IsFinal || slowResult is not DecimalIndicatorValue slowValue)
		return;

		var previousFast = _fastMaValue;
		var previousSlow = _slowMaValue;
		var previousCandle = _previousBaseCandle;

		_fastMaValue = fastValue.Value;
		_slowMaValue = slowValue.Value;
		_previousBaseCandle = candle;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (UseMacdExit)
		{
			// Optional safety net replicating the Exit flag in the EA.
			ApplyMacdExit();
		}

		if (UpdatePositionState(candle))
		return;

		if (previousFast is null || previousSlow is null || previousCandle is null)
		return;

		// Evaluate the trading rules on the freshly closed bar.
		TryEnter(previousCandle, previousFast.Value, previousSlow.Value);

		if (UpdatePositionState(candle))
		return;

		ApplyMoneyTargets(candle);
		CheckEquityStop(candle);
	}

	private void ProcessMomentum(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var momentumValue = _momentum.Process(new DecimalIndicatorValue(_momentum, candle.ClosePrice, candle.OpenTime));
		if (!momentumValue.IsFinal)
		return;

		// The StockSharp momentum returns a price difference. Convert it to the MetaTrader style ratio.
		var diff = momentumValue.ToDecimal();
		var previousPrice = candle.ClosePrice - diff;
		if (previousPrice == 0m)
		return;

		var ratio = (previousPrice == 0m) ? 100m : candle.ClosePrice / previousPrice * 100m;
		var distance = Math.Abs(100m - ratio);

		_momentumDistance3 = _momentumDistance2;
		_momentumDistance2 = _momentumDistance1;
		_momentumDistance1 = distance;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!value.IsFinal || value is not MovingAverageConvergenceDivergenceSignalValue macdValue)
		return;

		// Track the monthly MACD line and signal values for later checks.
		if (macdValue.Macd is decimal macd)
		_macdMain = macd;

		if (macdValue.Signal is decimal signal)
		_macdSignal = signal;
	}

	private void TryEnter(ICandleMessage previousCandle, decimal previousFast, decimal previousSlow)
	{
		if (!HasMomentumData())
		return;

		if (!HasMacdData())
		return;

		var volume = Volume + Math.Abs(Position);

		var buySignal = previousFast > previousSlow
		&& previousCandle.LowPrice < previousFast
		&& previousCandle.ClosePrice > previousFast
		&& MomentumDistanceAboveThreshold()
		&& IsMacdBullish();

		if (buySignal && Position <= 0)
		{
			BuyMarket(volume);
			return;
		}

		var sellSignal = previousFast < previousSlow
		&& previousCandle.HighPrice > previousFast
		&& previousCandle.ClosePrice < previousFast
		&& MomentumDistanceAboveThreshold()
		&& IsMacdBearish();

		if (sellSignal && Position >= 0)
		{
			SellMarket(volume);
		}
	}

	private bool UpdatePositionState(ICandleMessage candle)
	{
		// Manage protective logic (break-even, trailing, fixed stops) exactly like the EA.
		if (Position > 0)
		{
			InitializeLongStateIfNeeded(candle);

			_highestSinceEntry = _highestSinceEntry.HasValue
			? Math.Max(_highestSinceEntry.Value, candle.HighPrice)
			: candle.HighPrice;

			_lowestSinceEntry = _lowestSinceEntry.HasValue
			? Math.Min(_lowestSinceEntry.Value, candle.LowPrice)
			: candle.LowPrice;

			if (UseBreakEven)
			ApplyBreakEvenForLong(candle);

			if (TrailingStopPips > 0m)
			ApplyTrailingForLong();

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
			InitializeShortStateIfNeeded(candle);

			_highestSinceEntry = _highestSinceEntry.HasValue
			? Math.Max(_highestSinceEntry.Value, candle.HighPrice)
			: candle.HighPrice;

			_lowestSinceEntry = _lowestSinceEntry.HasValue
			? Math.Min(_lowestSinceEntry.Value, candle.LowPrice)
			: candle.LowPrice;

			if (UseBreakEven)
			ApplyBreakEvenForShort(candle);

			if (TrailingStopPips > 0m)
			ApplyTrailingForShort();

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

	private void ApplyBreakEvenForLong(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
		return;

		if (_breakEvenActivated)
		return;

		if (BreakEvenTriggerPips <= 0m)
		return;

		var trigger = entry + GetPipDistance(BreakEvenTriggerPips);
		if (candle.HighPrice < trigger)
		return;

		var newStop = entry + GetPipDistance(BreakEvenOffsetPips);
		_stopPrice = _stopPrice is decimal current ? Math.Max(current, newStop) : newStop;
		_breakEvenActivated = true;
	}

	private void ApplyBreakEvenForShort(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
		return;

		if (_breakEvenActivated)
		return;

		if (BreakEvenTriggerPips <= 0m)
		return;

		var trigger = entry - GetPipDistance(BreakEvenTriggerPips);
		if (candle.LowPrice > trigger)
		return;

		var newStop = entry - GetPipDistance(BreakEvenOffsetPips);
		_stopPrice = _stopPrice is decimal current ? Math.Min(current, newStop) : newStop;
		_breakEvenActivated = true;
	}

	private void ApplyTrailingForLong()
	{
		if (_highestSinceEntry is not decimal high)
		return;

		var candidate = high - GetPipDistance(TrailingStopPips);
		if (_stopPrice is not decimal current || candidate > current)
		_stopPrice = candidate;
	}

	private void ApplyTrailingForShort()
	{
		if (_lowestSinceEntry is not decimal low)
		return;

		var candidate = low + GetPipDistance(TrailingStopPips);
		if (_stopPrice is not decimal current || candidate < current)
		_stopPrice = candidate;
	}

	private void InitializeLongStateIfNeeded(ICandleMessage candle)
	{
		if (_activeSide == Sides.Buy && _entryPrice.HasValue)
		return;

		_activeSide = Sides.Buy;
		_entryPrice = PositionAvgPrice;
		_stopPrice = StopLossPips > 0m ? _entryPrice - GetPipDistance(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0m ? _entryPrice + GetPipDistance(TakeProfitPips) : null;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;
	}

	private void InitializeShortStateIfNeeded(ICandleMessage candle)
	{
		if (_activeSide == Sides.Sell && _entryPrice.HasValue)
		return;

		_activeSide = Sides.Sell;
		_entryPrice = PositionAvgPrice;
		_stopPrice = StopLossPips > 0m ? _entryPrice + GetPipDistance(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0m ? _entryPrice - GetPipDistance(TakeProfitPips) : null;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;
	}

	private void ResetPositionState()
	{
		_activeSide = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestSinceEntry = null;
		_lowestSinceEntry = null;
		_breakEvenActivated = false;
	}

	private void ApplyMoneyTargets(ICandleMessage candle)
	{
		// Account level profit management replicates TP_In_Money, TP_In_Percent and the trailing lock.
		if (Position == 0)
		{
			_moneyTrailPeak = 0m;
			return;
		}

		var unrealized = GetUnrealizedPnL(candle);

		if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && unrealized >= MoneyTakeProfit)
		{
			ClosePosition();
			return;
		}

		if (UsePercentTakeProfit && PercentTakeProfit > 0m && _initialEquity > 0m)
		{
			var target = _initialEquity * PercentTakeProfit / 100m;
			if (unrealized >= target)
			{
				ClosePosition();
				return;
			}
		}

		if (EnableMoneyTrailing && MoneyTrailTarget > 0m && MoneyTrailStop > 0m)
		{
			if (unrealized >= MoneyTrailTarget)
			{
				_moneyTrailPeak = Math.Max(_moneyTrailPeak, unrealized);
				if (_moneyTrailPeak - unrealized >= MoneyTrailStop)
				ClosePosition();
			}
			else
			{
				_moneyTrailPeak = 0m;
			}
		}
	}

	private void CheckEquityStop(ICandleMessage candle)
	{
		// TotalEquityRisk closes trades once the floating equity drawdown exceeds the threshold.
		if (!UseEquityStop || EquityRiskPercent <= 0m)
		return;

		var equity = GetPortfolioValue() + GetUnrealizedPnL(candle);
		_equityPeak = Math.Max(_equityPeak, equity);

		var drawdown = _equityPeak - equity;
		var threshold = _equityPeak * EquityRiskPercent / 100m;

		if (drawdown >= threshold && Position != 0)
		ClosePosition();
	}

	private void ApplyMacdExit()
	{
		if (!HasMacdData())
		return;

		if (Position > 0 && IsMacdBearish())
		{
			SellMarket(Position);
			ResetPositionState();
		}
		else if (Position < 0 && IsMacdBullish())
		{
			BuyMarket(-Position);
			ResetPositionState();
		}
	}

	private bool HasMomentumData()
	{
		return _momentumDistance1.HasValue && _momentumDistance2.HasValue && _momentumDistance3.HasValue;
	}

	private bool MomentumDistanceAboveThreshold()
	{
		var threshold = MomentumThreshold;

		return (_momentumDistance1 is decimal m1 && m1 >= threshold)
		|| (_momentumDistance2 is decimal m2 && m2 >= threshold)
		|| (_momentumDistance3 is decimal m3 && m3 >= threshold);
	}

	private bool HasMacdData()
	{
		return _macdMain.HasValue && _macdSignal.HasValue;
	}

	private bool IsMacdBullish()
	{
		return _macdMain is decimal main && _macdSignal is decimal signal && main > signal;
	}

	private bool IsMacdBearish()
	{
		return _macdMain is decimal main && _macdSignal is decimal signal && main < signal;
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}
	}

	private static decimal GetTypicalPrice(ICandleMessage candle)
	{
		return (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
	}

	private decimal GetPipDistance(decimal pips)
	{
		return pips * _pipSize;
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0)
		return 0m;

		var entry = PositionAvgPrice;
		if (entry == 0m)
		return 0m;

		var diff = candle.ClosePrice - entry;
		return diff * Position;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
		return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
		return portfolio.BeginValue;

		return 0m;
	}
}

