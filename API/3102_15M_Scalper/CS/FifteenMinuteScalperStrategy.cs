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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the 15M Scalper MetaTrader expert advisor using the StockSharp high level API.
/// Combines LWMA trend filters, stochastic reversals, momentum confirmation and MACD exits.
/// </summary>
public class FifteenMinuteScalperStrategy : Strategy
{
	private readonly StrategyParam<bool> _useProfitTargetMoney;
	private readonly StrategyParam<decimal> _profitTargetMoney;
	private readonly StrategyParam<bool> _useProfitTargetPercent;
	private readonly StrategyParam<decimal> _profitTargetPercent;
	private readonly StrategyParam<bool> _enableMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailingTake;
	private readonly StrategyParam<decimal> _moneyTrailingStop;
	private readonly StrategyParam<bool> _useExitByMacd;
	private readonly StrategyParam<decimal> _increaseFactor;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _totalEquityRisk;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerSteps;
	private readonly StrategyParam<decimal> _breakEvenOffsetSteps;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentumIndicator = null!;
	private ParabolicStopAndReverse _sar = null!;
	private StochasticOscillator _stochastic = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private decimal? _stochasticPrev1;
	private decimal? _stochasticPrev2;
	private decimal? _previousSar;
	private decimal? _previousOpen;
	private decimal? _momentumAbs1;
	private decimal? _momentumAbs2;
	private decimal? _momentumAbs3;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private decimal _pipSize;
	private decimal _stepPrice;
	private decimal _initialCapital;
	private decimal _equityPeak;
	private decimal _maxFloatingProfit;
	private decimal? _breakevenPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _previousRealizedPnL;
	private int _longTradeCount;
	private int _shortTradeCount;
	private int _consecutiveLosses;

	/// <summary>
	/// Initializes strategy parameters with defaults that match the MetaTrader version.
	/// </summary>
	public FifteenMinuteScalperStrategy()
	{
		_useProfitTargetMoney = Param(nameof(UseProfitTargetMoney), false)
			.SetDisplay("Use money take profit", "Close all positions once floating profit reaches ProfitTargetMoney.", "Money management");
		_profitTargetMoney = Param(nameof(ProfitTargetMoney), 40m)
			.SetDisplay("Money take profit", "Floating profit (account currency) target.", "Money management");
		_useProfitTargetPercent = Param(nameof(UseProfitTargetPercent), false)
			.SetDisplay("Use percent take profit", "Close positions when floating profit reaches ProfitTargetPercent of initial capital.", "Money management");
		_profitTargetPercent = Param(nameof(ProfitTargetPercent), 10m)
			.SetDisplay("Percent take profit", "Floating profit percentage target.", "Money management");
		_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
			.SetDisplay("Enable money trailing", "Activate trailing on floating profit measured in money.", "Money management");
		_moneyTrailingTake = Param(nameof(MoneyTrailingTakeProfit), 40m)
			.SetDisplay("Money trailing trigger", "Profit required before money trailing starts tracking.", "Money management");
		_moneyTrailingStop = Param(nameof(MoneyTrailingStop), 10m)
			.SetDisplay("Money trailing stop", "Allowed give-back after the money trailing trigger.", "Money management");
		_useExitByMacd = Param(nameof(UseExitByMacd), true)
			.SetDisplay("Use MACD exit", "Exit when the MACD main line crosses against the position.", "Exits");
		_increaseFactor = Param(nameof(IncreaseFactor), 0.001m)
			.SetDisplay("Increase factor", "Additional volume multiplier applied after consecutive losses.", "Position sizing");
		_baseVolume = Param(nameof(BaseVolume), 0.01m)
			.SetDisplay("Base volume", "Initial lot volume for the first trade.", "Position sizing");
		_lotExponent = Param(nameof(LotExponent), 1.44m)
			.SetDisplay("Lot exponent", "Multiplier applied when adding to an existing position.", "Position sizing");
		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
			.SetDisplay("Stop loss (points)", "Stop distance in price steps (MetaTrader pips).", "Risk");
		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetDisplay("Take profit (points)", "Take profit distance in price steps.", "Risk");
		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetDisplay("Fast LWMA period", "Length of the fast weighted moving average.", "Filters");
		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetDisplay("Slow LWMA period", "Length of the slow weighted moving average.", "Filters");
		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum period", "Length of the higher timeframe momentum indicator.", "Filters");
		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetDisplay("Momentum sell threshold", "Minimum absolute deviation from 100 required for shorts.", "Filters");
		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetDisplay("Momentum buy threshold", "Minimum absolute deviation from 100 required for longs.", "Filters");
		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetDisplay("Max trades", "Maximum number of pyramided entries per direction.", "Position sizing");
		_useEquityStop = Param(nameof(UseEquityStop), true)
			.SetDisplay("Use equity stop", "Monitor floating drawdown relative to the equity peak.", "Risk");
		_totalEquityRisk = Param(nameof(TotalEquityRisk), 1m)
			.SetDisplay("Equity risk (%)", "Allowed percentage drawdown before closing all trades.", "Risk");
		_trailingStopSteps = Param(nameof(TrailingStopSteps), 40m)
			.SetDisplay("Trailing stop (points)", "Trailing stop distance expressed in price steps.", "Risk");
		_useBreakEven = Param(nameof(UseMoveToBreakeven), true)
			.SetDisplay("Use break-even move", "Move the stop to break-even after a profitable move.", "Risk");
		_breakEvenTriggerSteps = Param(nameof(BreakEvenTriggerSteps), 30m)
			.SetDisplay("Break-even trigger (points)", "Distance required before activating the break-even stop.", "Risk");
		_breakEvenOffsetSteps = Param(nameof(BreakEvenOffsetSteps), 30m)
			.SetDisplay("Break-even offset (points)", "Offset applied when the stop jumps to break-even.", "Risk");
		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD fast EMA", "Fast EMA length for the MACD filter.", "Filters");
		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD slow EMA", "Slow EMA length for the MACD filter.", "Filters");
		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD signal EMA", "Signal EMA length for the MACD filter.", "Filters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary candles", "Main timeframe where the scalper operates.", "Data");
		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum candles", "Higher timeframe used for the momentum filter.", "Data");
		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD candles", "Timeframe used for the MACD trend filter.", "Data");
	}

	/// <summary>
	/// Close all positions once floating profit reaches the configured money target.
	/// </summary>
	public bool UseProfitTargetMoney
	{
		get => _useProfitTargetMoney.Value;
		set => _useProfitTargetMoney.Value = value;
	}

	/// <summary>
	/// Floating profit target expressed in account currency.
	/// </summary>
	public decimal ProfitTargetMoney
	{
		get => _profitTargetMoney.Value;
		set => _profitTargetMoney.Value = value;
	}

	/// <summary>
	/// Enable the percent based floating profit exit.
	/// </summary>
	public bool UseProfitTargetPercent
	{
		get => _useProfitTargetPercent.Value;
		set => _useProfitTargetPercent.Value = value;
	}

	/// <summary>
	/// Floating profit target as a percentage of initial capital.
	/// </summary>
	public decimal ProfitTargetPercent
	{
		get => _profitTargetPercent.Value;
		set => _profitTargetPercent.Value = value;
	}

	/// <summary>
	/// Enable trailing logic on floating profit measured in money.
	/// </summary>
	public bool EnableMoneyTrailing
	{
		get => _enableMoneyTrailing.Value;
		set => _enableMoneyTrailing.Value = value;
	}

	/// <summary>
	/// Profit required before the money trailing stop starts following the trade.
	/// </summary>
	public decimal MoneyTrailingTakeProfit
	{
		get => _moneyTrailingTake.Value;
		set => _moneyTrailingTake.Value = value;
	}

	/// <summary>
	/// Maximum allowed give-back once money trailing is active.
	/// </summary>
	public decimal MoneyTrailingStop
	{
		get => _moneyTrailingStop.Value;
		set => _moneyTrailingStop.Value = value;
	}

	/// <summary>
	/// Enable MACD based exits that mirror the MQL Close_BUY/Close_SELL functions.
	/// </summary>
	public bool UseExitByMacd
	{
		get => _useExitByMacd.Value;
		set => _useExitByMacd.Value = value;
	}

	/// <summary>
	/// Additional volume multiplier applied after consecutive losing trades.
	/// </summary>
	public decimal IncreaseFactor
	{
		get => _increaseFactor.Value;
		set => _increaseFactor.Value = value;
	}

	/// <summary>
	/// Base volume used for the first trade in a sequence.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied when stacking additional orders in the same direction.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in instrument price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Fast LWMA length.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow LWMA length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator length calculated on the higher timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute deviation from 100 required to allow short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Minimum absolute deviation from 100 required to allow long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of pyramided entries per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Enable floating equity drawdown control.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Allowed drawdown as a percentage of the equity peak.
	/// </summary>
	public decimal TotalEquityRisk
	{
		get => _totalEquityRisk.Value;
		set => _totalEquityRisk.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Enable the break-even move ("no loss") feature.
	/// </summary>
	public bool UseMoveToBreakeven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Distance required before the stop jumps to break-even.
	/// </summary>
	public decimal BreakEvenTriggerSteps
	{
		get => _breakEvenTriggerSteps.Value;
		set => _breakEvenTriggerSteps.Value = value;
	}

	/// <summary>
	/// Offset applied when the stop is moved to break-even.
	/// </summary>
	public decimal BreakEvenOffsetSteps
	{
		get => _breakEvenOffsetSteps.Value;
		set => _breakEvenOffsetSteps.Value = value;
	}

	/// <summary>
	/// Fast EMA length used in the MACD trend filter.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used in the MACD trend filter.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length used in the MACD trend filter.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Main candle type used for trade signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type used by the momentum filter.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, MomentumCandleType);
		yield return (Security, MacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stochasticPrev1 = null;
		_stochasticPrev2 = null;
		_previousSar = null;
		_previousOpen = null;
		_momentumAbs1 = null;
		_momentumAbs2 = null;
		_momentumAbs3 = null;
		_macdMain = null;
		_macdSignal = null;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_previousRealizedPnL = 0m;
		_longTradeCount = 0;
		_shortTradeCount = 0;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = AlignVolume(BaseVolume);

		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod, CandlePrice = CandlePrice.Typical };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod, CandlePrice = CandlePrice.Typical };
		_momentumIndicator = new Momentum { Length = MomentumPeriod };
		_sar = new ParabolicStopAndReverse { Step = 0.02m, MaxStep = 0.2m };
		_stochastic = new StochasticOscillator { KPeriod = 5, DPeriod = 3, Smooth = 3 };
		_macd = new MovingAverageConvergenceDivergence
		{
			FastLength = MacdFastLength,
			SlowLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		_pipSize = GetPipSize();
		_stepPrice = Security?.StepPrice ?? 0m;
		_initialCapital = Portfolio?.BeginValue ?? Portfolio?.CurrentValue ?? 0m;
		_equityPeak = _initialCapital;
		_previousRealizedPnL = PnL;
		_maxFloatingProfit = 0m;
		_consecutiveLosses = 0;
		_longTradeCount = Position > 0m ? 1 : 0;
		_shortTradeCount = Position < 0m ? 1 : 0;

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(_fastMa, _slowMa, _sar, ProcessMainCandle);
		mainSubscription.BindEx(_stochastic, ProcessStochastic);
		mainSubscription.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription.Bind(_momentumIndicator, ProcessMomentum).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.BindEx(_macd, ProcessMacd).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _sar);
			DrawOwnTrades(area);

			var oscArea = CreateChartArea();
			if (oscArea != null)
			{
				DrawIndicator(oscArea, _stochastic);
				DrawIndicator(oscArea, _momentumIndicator);
				DrawIndicator(oscArea, _macd);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			var realizedPnL = PnL - _previousRealizedPnL;
			if (realizedPnL < 0m)
				_consecutiveLosses++;
			else if (realizedPnL > 0m)
				_consecutiveLosses = 0;

			_previousRealizedPnL = PnL;
			_breakevenPrice = null;
			_maxFloatingProfit = 0m;
			_highestPrice = 0m;
			_lowestPrice = 0m;
			_longTradeCount = 0;
			_shortTradeCount = 0;
			return;
		}

		if (Position > 0m)
			_shortTradeCount = 0;
		else if (Position < 0m)
			_longTradeCount = 0;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update trailing anchors with the latest candle range.
		UpdateExtremes(candle);

		// Abort early if the floating drawdown exceeds the equity stop threshold.
		if (TryApplyEquityStop(candle.ClosePrice))
		{
			_previousOpen = candle.OpenPrice;
			_previousSar = sarValue;
			return;
		}

		// Manage protective logic before evaluating new entries.
		if (Position != 0m)
		{
			// Enable the break-even stop once price travels far enough.
			TryActivateBreakeven(candle.ClosePrice);

			// Close the trade if price falls back through the break-even stop.
			if (TryApplyBreakEvenExit(candle.ClosePrice))
			{
				_previousOpen = candle.OpenPrice;
				_previousSar = sarValue;
				return;
			}

			// Apply step-based trailing stop analogous to the MQL OrderModify logic.
			if (ApplyStepTrailing(candle))
			{
				_previousOpen = candle.OpenPrice;
				_previousSar = sarValue;
				return;
			}

			// Handle money-based targets and trailing in account currency.
			if (TryApplyMoneyTargets(candle.ClosePrice))
			{
				_previousOpen = candle.OpenPrice;
				_previousSar = sarValue;
				return;
			}

			// Respect the monthly MACD exit filter from the source EA.
			if (TryExitByMacd(candle.ClosePrice))
			{
				_previousOpen = candle.OpenPrice;
				_previousSar = sarValue;
				return;
			}
		}

		// Skip trading when the strategy infrastructure is not fully ready.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousOpen = candle.OpenPrice;
			_previousSar = sarValue;
			return;
		}

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_sar.IsFormed ||
			_stochasticPrev1 is not decimal stochPrev1 ||
			_stochasticPrev2 is not decimal stochPrev2 ||
			_previousOpen is not decimal prevOpen ||
			_previousSar is not decimal prevSar ||
			_momentumAbs1 is not decimal momentum1 ||
			_momentumAbs2 is not decimal momentum2 ||
			_momentumAbs3 is not decimal momentum3 ||
			_macdMain is not decimal macdMain ||
			_macdSignal is not decimal macdSignal)
		{
			_previousOpen = candle.OpenPrice;
			_previousSar = sarValue;
			return;
		}

		if (TakeProfitSteps > 0m && TakeProfitSteps < 10m)
		{
			_previousOpen = candle.OpenPrice;
			_previousSar = sarValue;
			return;
		}

		// Momentum must confirm the move on at least one of the last three higher timeframe readings.
		var buyMomentumOk = momentum1 >= MomentumBuyThreshold || momentum2 >= MomentumBuyThreshold || momentum3 >= MomentumBuyThreshold;
		var sellMomentumOk = momentum1 >= MomentumSellThreshold || momentum2 >= MomentumSellThreshold || momentum3 >= MomentumSellThreshold;

		// Long setup reproducing the nested if-chain from the original EA.
		var canBuy = stochPrev2 < 20m && stochPrev1 >= 20m &&
			prevSar < prevOpen &&
			fastValue > slowValue &&
			buyMomentumOk &&
			macdMain > macdSignal;

		// Short setup mirrored from the original conditions.
		var canSell = stochPrev2 > 80m && stochPrev1 <= 80m &&
			prevSar > prevOpen &&
			fastValue < slowValue &&
			sellMomentumOk &&
			macdMain < macdSignal;

		if (canBuy)
		{
			if (Position < 0m)
				CloseShort(candle.ClosePrice);
			else if (_longTradeCount < MaxTrades)
				EnterLong(candle.ClosePrice);
		}
		else if (canSell)
		{
			if (Position > 0m)
				CloseLong(candle.ClosePrice);
			else if (_shortTradeCount < MaxTrades)
				EnterShort(candle.ClosePrice);
		}

		_previousOpen = candle.OpenPrice;
		_previousSar = sarValue;
	}

	private void ProcessStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;

		if (value is not StochasticOscillatorValue stoch || stoch.Main is not decimal main)
			return;

		// Shift the %K history so we can mimic Stoc1/Stoc2 comparisons.
		_stochasticPrev2 = _stochasticPrev1;
		_stochasticPrev1 = main;
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var distance = Math.Abs(momentum - 100m);
		// Keep the last three momentum distances from the neutral 100 level.
		_momentumAbs3 = _momentumAbs2;
		_momentumAbs2 = _momentumAbs1;
		_momentumAbs1 = distance;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;

		if (value is MovingAverageConvergenceDivergenceSignalValue macdSignalValue)
		{
			_macdMain = macdSignalValue.Macd;
			_macdSignal = macdSignalValue.Signal;
		}
		else if (value is MovingAverageConvergenceDivergenceValue macdValue)
		{
			_macdMain = macdValue.Macd;
			_macdSignal = macdValue.Signal;
		}
	}

	private void EnterLong(decimal price)
	{
		var volume = CalculateNextVolume(Sides.Buy);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_longTradeCount++;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
		_highestPrice = price;
		_lowestPrice = 0m;
	}

	private void EnterShort(decimal price)
	{
		var volume = CalculateNextVolume(Sides.Sell);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_shortTradeCount++;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
		_lowestPrice = price;
		_highestPrice = 0m;
	}

	private void CloseLong(decimal price)
	{
		if (Position <= 0m)
			return;

		SellMarket(Position);
	}

	private void CloseShort(decimal price)
	{
		if (Position >= 0m)
			return;

		BuyMarket(Math.Abs(Position));
	}

	private bool TryApplyMoneyTargets(decimal closePrice)
	{
		// Floating profit combines the current position, price move and step price.
		var profit = GetFloatingProfit(closePrice);

		if (UseProfitTargetMoney && profit >= ProfitTargetMoney && profit > 0m)
		{
			ClosePosition();
			return true;
		}

		if (UseProfitTargetPercent && _initialCapital > 0m)
		{
			var target = _initialCapital * ProfitTargetPercent / 100m;
			if (profit >= target && profit > 0m)
			{
				ClosePosition();
				return true;
			}
		}

		if (EnableMoneyTrailing && profit > 0m)
		{
			if (profit >= MoneyTrailingTakeProfit)
				// Store the best profit achieved to know when the pullback exceeds MoneyTrailingStop.
				_maxFloatingProfit = Math.Max(_maxFloatingProfit, profit);

			if (_maxFloatingProfit > 0m && _maxFloatingProfit - profit >= MoneyTrailingStop)
			{
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private bool TryApplyEquityStop(decimal closePrice)
	{
		if (!UseEquityStop)
			return false;

		var profit = GetFloatingProfit(closePrice);
		var realized = PnL;
		var equity = _initialCapital + realized + profit;
		// Track the highest equity to emulate AccountEquityHigh from MQL.
		_equityPeak = Math.Max(_equityPeak, equity);

		if (profit >= 0m || _equityPeak <= 0m)
			return false;

		var threshold = _equityPeak * TotalEquityRisk / 100m;
		if (Math.Abs(profit) >= threshold)
		{
			ClosePosition();
			return true;
		}

		return false;
	}

	private bool TryApplyBreakEvenExit(decimal closePrice)
	{
		if (_breakevenPrice is not decimal breakEven || Position == 0m)
			return false;

		if (Position > 0m && closePrice <= breakEven)
		{
			CloseLong(closePrice);
			return true;
		}

		if (Position < 0m && closePrice >= breakEven)
		{
			CloseShort(closePrice);
			return true;
		}

		return false;
	}

	private bool TryExitByMacd(decimal closePrice)
	{
		if (!UseExitByMacd)
			return false;

		if (_macdMain is not decimal macdMain || _macdSignal is not decimal macdSignal)
			return false;

		if (Position > 0m && macdMain <= macdSignal)
		{
			CloseLong(closePrice);
			return true;
		}

		if (Position < 0m && macdMain >= macdSignal)
		{
			CloseShort(closePrice);
			return true;
		}

		return false;
	}

	private bool ApplyStepTrailing(ICandleMessage candle)
	{
		if (TrailingStopSteps <= 0m || Position == 0m)
			return false;

		if (PositionPrice is not decimal entryPrice)
			return false;

		// Convert trailing distance from pips to absolute price.
		var trailingDistance = StepsToPrice(TrailingStopSteps);
		if (trailingDistance <= 0m)
			return false;

		if (Position > 0m)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			if (_highestPrice - entryPrice < trailingDistance)
				return false;

			if (_highestPrice - candle.ClosePrice >= trailingDistance)
			{
				CloseLong(candle.ClosePrice);
				return true;
			}
		}
		else if (Position < 0m)
		{
			_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);
			if (entryPrice - _lowestPrice < trailingDistance)
				return false;

			if (candle.ClosePrice - _lowestPrice >= trailingDistance)
			{
				CloseShort(candle.ClosePrice);
				return true;
			}
		}

		return false;
	}

	private void TryActivateBreakeven(decimal closePrice)
	{
		if (!UseMoveToBreakeven || _breakevenPrice.HasValue)
			return;

		if (PositionPrice is not decimal entryPrice)
			return;

		// Convert pip distances to absolute price before applying the break-even rules.
		var trigger = StepsToPrice(BreakEvenTriggerSteps);
		if (trigger <= 0m)
			return;

		var offset = StepsToPrice(BreakEvenOffsetSteps);
		if (Position > 0m && closePrice >= entryPrice + trigger)
			_breakevenPrice = entryPrice + offset;
		else if (Position < 0m && closePrice <= entryPrice - trigger)
			_breakevenPrice = entryPrice - offset;
	}

	private void ClosePosition()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
	}

	private decimal CalculateNextVolume(Sides side)
	{
		// Start with the configured base volume before applying martingale adjustments.
		var volume = BaseVolume;
		if (volume <= 0m)
			return 0m;

		var currentCount = side == Sides.Buy ? _longTradeCount : _shortTradeCount;
		var exponent = Math.Pow((double)LotExponent, currentCount);
		volume *= (decimal)exponent;

		if (IncreaseFactor > 0m && _consecutiveLosses > 1)
			volume += volume * _consecutiveLosses * IncreaseFactor;

		return AlignVolume(volume);
	}

	private decimal AlignVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		// Respect exchange-imposed volume constraints.
		var step = Security.VolumeStep ?? 0m;
		var min = Security.VolumeMin ?? 0m;
		var max = Security.VolumeMax ?? decimal.MaxValue;

		if (step > 0m)
		{
			var ratio = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			if (ratio == 0m && volume > 0m)
				ratio = 1m;
			volume = ratio * step;
		}

		if (min > 0m && volume < min)
			volume = min;

		if (volume > max)
			volume = max;

		return volume;
	}

	private decimal StepsToPrice(decimal steps)
	{
		if (_pipSize <= 0m)
			return 0m;

		return steps * _pipSize;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		if (Security?.PriceStep is decimal s && s < 1m)
			return s * 10m;

		return step;
	}

	private void UpdateExtremes(ICandleMessage candle)
	{
		if (Position > 0m)
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
		else if (Position < 0m)
			_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);
	}

	private decimal GetFloatingProfit(decimal price)
	{
		if (Position == 0m || PositionPrice is not decimal entryPrice)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = _stepPrice;
		if (priceStep <= 0m || stepPrice <= 0m)
			return 0m;

		var direction = Position > 0m ? 1m : -1m;
		var priceDiff = (price - entryPrice) * direction;
		var steps = priceDiff / priceStep;
		return steps * stepPrice * Math.Abs(Position);
	}
}

