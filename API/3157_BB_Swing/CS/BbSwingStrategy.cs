using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "BB SWING" MetaTrader expert that trades Bollinger swing reversals.
/// </summary>
public class BbSwingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<int> _breakEvenTrigger;
	private readonly StrategyParam<int> _breakEvenOffset;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _useMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailTarget;
	private readonly StrategyParam<decimal> _moneyTrailStop;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRiskPercent;
	private readonly StrategyParam<bool> _closeOnMacdCross;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private BollingerBands _bollinger = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private ICandleMessage _bar1;
	private ICandleMessage _bar2;
	private ICandleMessage _bar3;
	private ICandleMessage _bar4;

	private decimal? _momentum1;
	private decimal? _momentum2;
	private decimal? _momentum3;
	private bool _momentumReady;

	private decimal? _macdMain;
	private decimal? _macdSignal;
	private bool _macdReady;

	private decimal _tickSize;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private bool _breakEvenActivated;
	private decimal _moneyTrailPeak;
	private decimal _equityPeak;
	private decimal _initialEquity;
	private int _currentTradeCount;

	/// <summary>
	/// Base trade volume.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the volume of consecutive trades while a position is open.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Maximum number of sequential position adds allowed.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	/// Number of bars used for the momentum calculation.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum distance from 100 required to confirm long momentum.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum distance from 100 required to confirm short momentum.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Enables break-even stop logic.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Distance in price steps required before moving the stop to break-even.
	/// </summary>
	public int BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Offset in price steps applied when the stop is moved to break-even.
	/// </summary>
	public int BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
	}

	/// <summary>
	/// Enables the classic trailing stop.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop size expressed in price steps.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Enables fixed money take profit.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Profit in account currency required to close the position.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Enables equity-based profit targeting.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Percentage of initial equity that triggers profit taking.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Enables trailing logic on unrealized profit in money.
	/// </summary>
	public bool UseMoneyTrailing
	{
		get => _useMoneyTrailing.Value;
		set => _useMoneyTrailing.Value = value;
	}

	/// <summary>
	/// Profit that activates money trailing.
	/// </summary>
	public decimal MoneyTrailTarget
	{
		get => _moneyTrailTarget.Value;
		set => _moneyTrailTarget.Value = value;
	}

	/// <summary>
	/// Allowed pullback in money after the trailing target is reached.
	/// </summary>
	public decimal MoneyTrailStop
	{
		get => _moneyTrailStop.Value;
		set => _moneyTrailStop.Value = value;
	}

	/// <summary>
	/// Enables drawdown based equity protection.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum allowed equity drawdown in percent.
	/// </summary>
	public decimal EquityRiskPercent
	{
		get => _equityRiskPercent.Value;
		set => _equityRiskPercent.Value = value;
	}

	/// <summary>
	/// Enables closing positions on MACD signal crossovers.
	/// </summary>
	public bool CloseOnMacdCross
	{
		get => _closeOnMacdCross.Value;
		set => _closeOnMacdCross.Value = value;
	}

	/// <summary>
	/// Main candle type used for the setup.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate momentum on the higher timeframe.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for the long-term MACD filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BbSwingStrategy"/> parameters.
	/// </summary>
	public BbSwingStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Starting trade volume", "General")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);

		_lotExponent = Param(nameof(LotExponent), 1.44m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Exponent", "Multiplier for consecutive entries", "General")
		.SetCanOptimize(true)
		.SetOptimize(1m, 2m, 0.1m);

		_maxTrades = Param(nameof(MaxTrades), 3)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum number of sequential adds", "General");

		_takeProfit = Param(nameof(TakeProfit), 50)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit in price steps", "Risk Management");

		_stopLoss = Param(nameof(StopLoss), 20)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk Management");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Fast linear weighted moving average length", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Slow linear weighted moving average length", "Indicators");

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Momentum calculation period", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Buy", "Minimum distance from 100 for long entries", "Indicators");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Sell", "Minimum distance from 100 for short entries", "Indicators");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Enable Break Even", "Move the stop to break-even after reaching a target", "Risk Management");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 30)
		.SetGreaterThanZero()
		.SetDisplay("Break Even Trigger", "Steps required to activate break-even", "Risk Management");

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 30)
		.SetDisplay("Break Even Offset", "Offset applied when stop moves to break-even", "Risk Management");

		_enableTrailingStop = Param(nameof(EnableTrailingStop), true)
		.SetDisplay("Enable Trailing", "Enable trailing stop logic", "Risk Management");

		_trailingStop = Param(nameof(TrailingStop), 40)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop", "Trailing stop size in steps", "Risk Management");

		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
		.SetDisplay("Use Money TP", "Enable take profit in money", "Risk Management");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Money TP", "Profit in account currency that closes the trade", "Risk Management");

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
		.SetDisplay("Use Percent TP", "Enable take profit based on equity percent", "Risk Management");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Percent TP", "Equity percent required to close", "Risk Management");

		_useMoneyTrailing = Param(nameof(UseMoneyTrailing), true)
		.SetDisplay("Use Money Trailing", "Enable money based trailing stop", "Risk Management");

		_moneyTrailTarget = Param(nameof(MoneyTrailTarget), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Money Trail Target", "Profit required to activate money trailing", "Risk Management");

		_moneyTrailStop = Param(nameof(MoneyTrailStop), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Money Trail Stop", "Allowed pullback after activation", "Risk Management");

		_useEquityStop = Param(nameof(UseEquityStop), true)
		.SetDisplay("Use Equity Stop", "Enable equity drawdown protection", "Risk Management");

		_equityRiskPercent = Param(nameof(EquityRiskPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Equity Risk %", "Maximum allowed drawdown in percent", "Risk Management");

		_closeOnMacdCross = Param(nameof(CloseOnMacdCross), true)
		.SetDisplay("MACD Exit", "Close positions on MACD signal cross", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Entry Candles", "Primary timeframe for signals", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Momentum Candles", "Higher timeframe used for momentum", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Candles", "Long-term timeframe for MACD", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

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

		_bar1 = null;
		_bar2 = null;
		_bar3 = null;
		_bar4 = null;
		_momentum1 = null;
		_momentum2 = null;
		_momentum3 = null;
		_momentumReady = false;
		_macdMain = null;
		_macdSignal = null;
		_macdReady = false;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;
		_equityPeak = 0m;
		_initialEquity = 0m;
		_currentTradeCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 0m;
		if (_tickSize <= 0m)
		_tickSize = 1m;

		_initialEquity = GetPortfolioValue();
		_equityPeak = _initialEquity;

		_fastMa = new WeightedMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new WeightedMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_bollinger = new BollingerBands
		{
			Length = 20,
			Width = 2m
		};

		_momentum = new Momentum
		{
			Length = MomentumLength
		};

		_macd = new MovingAverageConvergenceDivergence
		{
			FastLength = 12,
			SlowLength = 26,
			SignalLength = 9
		};

		Volume = InitialVolume;

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(_fastMa, _slowMa, _bollinger, ProcessMainCandle)
		.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
		.Bind(_momentum, ProcessMomentum)
		.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
		.Bind(_macd, ProcessMacd)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
		ResetPositionState();
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// The original expert measures the distance from 100 on the momentum scale.
		var distance = Math.Abs(100m - momentumValue);

		_momentum3 = _momentum2;
		_momentum2 = _momentum1;
		_momentum1 = distance;
		_momentumReady = _momentum.IsFormed;
	}

	private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_macdMain = macdValue;
		_macdSignal = signalValue;
		_macdReady = _macd.IsFormed;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ShiftHistory(candle);

		ApplyMoneyTargets(candle);
		CheckEquityStop(candle);

		if (UpdateStops(candle))
		return;

		if (MaybeExitOnMacd())
		return;

		if (Position != 0)
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_bollinger.IsFormed)
		return;

		if (!_momentumReady || _momentum1 is not decimal mom1 || _momentum2 is not decimal mom2 || _momentum3 is not decimal mom3)
		return;

		if (_bar1 is not { } bar1 || _bar2 is not { } bar2 || _bar3 is not { } bar3 || _bar4 is not { } bar4)
		return;

		if (!TryGetNextVolume(out var volume) || volume <= 0m)
		return;

		var bullishBody = bar1.ClosePrice - bar1.OpenPrice;
		var prevBody = Math.Abs(bar2.OpenPrice - bar2.ClosePrice);
		var bearishBody = bar1.OpenPrice - bar1.ClosePrice;

		var momentumLongReady = mom1 > MomentumBuyThreshold || mom2 > MomentumBuyThreshold || mom3 > MomentumBuyThreshold;
		var momentumShortReady = mom1 > MomentumSellThreshold || mom2 > MomentumSellThreshold || mom3 > MomentumSellThreshold;

		var macdAllowsLong = !_macdReady || (_macdMain is decimal macd && _macdSignal is decimal signal && (macd > signal));
		var macdAllowsShort = !_macdReady || (_macdMain is decimal macd && _macdSignal is decimal signal && (macd < signal));

		if (macdAllowsLong && momentumLongReady && fastValue > slowValue)
		{
			var touchedLowerBand = bar4.LowPrice <= lower || bar3.LowPrice <= lower || bar2.LowPrice <= lower;
			if (touchedLowerBand && bullishBody > prevBody && bar1.HighPrice >= middle && bar1.ClosePrice >= bar1.OpenPrice)
			{
				EnterLong(candle, volume);
				return;
			}
		}

		if (macdAllowsShort && momentumShortReady && fastValue < slowValue)
		{
			var touchedUpperBand = bar4.HighPrice >= upper || bar3.HighPrice >= upper || bar2.HighPrice >= upper;
			if (touchedUpperBand && bearishBody > prevBody && bar1.LowPrice <= middle && bar1.ClosePrice <= bar1.OpenPrice)
			{
				EnterShort(candle, volume);
			}
		}
	}

	private void EnterLong(ICandleMessage candle, decimal volume)
	{
		if (volume <= 0m)
		return;

		if (Position == 0)
		{
			_highestSinceEntry = candle.HighPrice;
			_lowestSinceEntry = candle.LowPrice;
			_breakEvenActivated = false;
			_moneyTrailPeak = 0m;
		}
		else
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);
		}

		BuyMarket(volume);
		_currentTradeCount++;
	}

	private void EnterShort(ICandleMessage candle, decimal volume)
	{
		if (volume <= 0m)
		return;

		if (Position == 0)
		{
			_highestSinceEntry = candle.HighPrice;
			_lowestSinceEntry = candle.LowPrice;
			_breakEvenActivated = false;
			_moneyTrailPeak = 0m;
		}
		else
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);
		}

		SellMarket(volume);
		_currentTradeCount++;
	}

	private bool UpdateStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			var exitPrice = PositionAvgPrice;
			if (exitPrice <= 0m)
			exitPrice = candle.ClosePrice;

			decimal? stopCandidate = null;

			if (EnableBreakEven && !_breakEvenActivated && BreakEvenTrigger > 0)
			{
				var trigger = exitPrice + GetStepValue(BreakEvenTrigger);
				if (candle.HighPrice >= trigger)
				_breakEvenActivated = true;
			}

			if (_breakEvenActivated)
			{
				var beStop = exitPrice + GetStepValue(BreakEvenOffset);
				stopCandidate = stopCandidate is decimal current ? Math.Max(current, beStop) : beStop;
			}

			if (EnableTrailingStop && TrailingStop > 0)
			{
				var trailingStop = _highestSinceEntry - GetStepValue(TrailingStop);
				stopCandidate = stopCandidate is decimal current ? Math.Max(current, trailingStop) : trailingStop;
			}

			var hardStop = exitPrice - GetStepValue(StopLoss);
			if (StopLoss > 0)
			stopCandidate = stopCandidate is decimal current ? Math.Min(current, hardStop) : hardStop;

			if (stopCandidate is decimal stop && candle.LowPrice <= stop)
			{
				ClosePosition();
				ResetPositionState();
				return true;
			}

			if (TakeProfit > 0)
			{
				var target = exitPrice + GetStepValue(TakeProfit);
				if (candle.HighPrice >= target)
				{
					ClosePosition();
					ResetPositionState();
					return true;
				}
			}
		}
		else if (Position < 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			var exitPrice = PositionAvgPrice;
			if (exitPrice <= 0m)
			exitPrice = candle.ClosePrice;

			decimal? stopCandidate = null;

			if (EnableBreakEven && !_breakEvenActivated && BreakEvenTrigger > 0)
			{
				var trigger = exitPrice - GetStepValue(BreakEvenTrigger);
				if (candle.LowPrice <= trigger)
				_breakEvenActivated = true;
			}

			if (_breakEvenActivated)
			{
				var beStop = exitPrice - GetStepValue(BreakEvenOffset);
				stopCandidate = stopCandidate is decimal current ? Math.Min(current, beStop) : beStop;
			}

			if (EnableTrailingStop && TrailingStop > 0)
			{
				var trailingStop = _lowestSinceEntry + GetStepValue(TrailingStop);
				stopCandidate = stopCandidate is decimal current ? Math.Min(current, trailingStop) : trailingStop;
			}

			var hardStop = exitPrice + GetStepValue(StopLoss);
			if (StopLoss > 0)
			stopCandidate = stopCandidate is decimal current ? Math.Max(current, hardStop) : hardStop;

			if (stopCandidate is decimal stop && candle.HighPrice >= stop)
			{
				ClosePosition();
				ResetPositionState();
				return true;
			}

			if (TakeProfit > 0)
			{
				var target = exitPrice - GetStepValue(TakeProfit);
				if (candle.LowPrice <= target)
				{
					ClosePosition();
					ResetPositionState();
					return true;
				}
			}
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private bool MaybeExitOnMacd()
	{
		if (!CloseOnMacdCross || !_macdReady || _macdMain is not decimal macd || _macdSignal is not decimal signal)
		return false;

		if (Position > 0 && macd < signal)
		{
			ClosePosition();
			ResetPositionState();
			return true;
		}

		if (Position < 0 && macd > signal)
		{
			ClosePosition();
			ResetPositionState();
			return true;
		}

		return false;
	}

	private void ShiftHistory(ICandleMessage candle)
	{
		_bar4 = _bar3;
		_bar3 = _bar2;
		_bar2 = _bar1;
		_bar1 = candle;
	}

	private bool TryGetNextVolume(out decimal volume)
	{
		volume = 0m;

		var baseVolume = InitialVolume;
		if (baseVolume <= 0m)
		return false;

		var exponent = LotExponent <= 0m ? 1m : LotExponent;
		var factor = (decimal)Math.Pow((double)exponent, _currentTradeCount);
		volume = baseVolume * factor;

		var maxTrades = MaxTrades;
		if (maxTrades > 0)
		{
			var maxVolume = baseVolume * maxTrades;
			var remaining = maxVolume - Math.Abs(Position);
			if (remaining <= 0m)
			{
				volume = 0m;
				return false;
			}

			if (volume > remaining)
			volume = remaining;
		}

		return volume > 0m;
	}

	private void ApplyMoneyTargets(ICandleMessage candle)
	{
		if (Position == 0)
		{
			_moneyTrailPeak = 0m;
			return;
		}

		var unrealized = GetUnrealizedPnL(candle);

		if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && unrealized >= MoneyTakeProfit)
		{
			ClosePosition();
			ResetPositionState();
			return;
		}

		if (UsePercentTakeProfit && PercentTakeProfit > 0m && _initialEquity > 0m)
		{
			var target = _initialEquity * PercentTakeProfit / 100m;
			if (unrealized >= target)
			{
				ClosePosition();
				ResetPositionState();
				return;
			}
		}

		if (UseMoneyTrailing && MoneyTrailTarget > 0m && MoneyTrailStop > 0m)
		{
			if (unrealized >= MoneyTrailTarget)
			{
				_moneyTrailPeak = Math.Max(_moneyTrailPeak, unrealized);
				if (_moneyTrailPeak - unrealized >= MoneyTrailStop)
				{
					ClosePosition();
					ResetPositionState();
				}
			}
			else
			{
				_moneyTrailPeak = 0m;
			}
		}
	}

	private void CheckEquityStop(ICandleMessage candle)
	{
		if (!UseEquityStop || EquityRiskPercent <= 0m)
		return;

		var equity = GetPortfolioValue() + GetUnrealizedPnL(candle);
		_equityPeak = Math.Max(_equityPeak, equity);

		var drawdown = _equityPeak - equity;
		var threshold = _equityPeak * EquityRiskPercent / 100m;

		if (drawdown >= threshold && Position != 0)
		{
			ClosePosition();
			ResetPositionState();
		}
	}

	private decimal GetStepValue(int steps)
	{
		return steps * _tickSize;
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0)
		return 0m;

		var entry = PositionAvgPrice;
		if (entry == 0m)
		entry = candle.OpenPrice;

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

		return _initialEquity;
	}

	private void ClosePosition()
	{
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(-Position);
	}

	private void ResetPositionState()
	{
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;
		_currentTradeCount = 0;
	}
}
