using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy "Trend Line By Angle".
/// Automates the original manual MACD button entries, manages Bollinger exits,
/// supports break-even and trailing stops, and adds account level profit control.
/// </summary>
public class TrendLineByAngleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useBollingerExit;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<bool> _useProfitMoneyTarget;
	private readonly StrategyParam<decimal> _profitMoneyTarget;
	private readonly StrategyParam<bool> _useProfitPercentTarget;
	private readonly StrategyParam<decimal> _profitPercentTarget;
	private readonly StrategyParam<bool> _enableMoneyTrail;
	private readonly StrategyParam<decimal> _moneyTrailTrigger;
	private readonly StrategyParam<decimal> _moneyTrailStop;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _signalCandleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private BollingerBands _bollinger = null!;

	private decimal _pipSize;
	private decimal _stepPrice;

	private decimal? _macdMain;
	private decimal? _macdSignal;
	private decimal? _previousMacdMain;
	private decimal? _previousMacdSignal;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private bool _longBreakEvenActivated;
	private bool _shortBreakEvenActivated;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;

	private bool _moneyTrailActive;
	private decimal _moneyTrailPeak;

	/// <summary>
	/// Volume traded on each incremental entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of volume blocks the strategy maintains.
	/// </summary>
	public int MaxEntries
	{
		get => _maxEntries.Value;
		set => _maxEntries.Value = value;
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
	/// Enables the break-even logic once price advances by the configured trigger.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit distance in pips required to activate the break-even move.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional offset added to the break-even stop once triggered.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enables Bollinger based exits that mimic the original stop logic.
	/// </summary>
	public bool UseBollingerExit
	{
		get => _useBollingerExit.Value;
		set => _useBollingerExit.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Enables closing all exposure after reaching the absolute profit target.
	/// </summary>
	public bool UseProfitMoneyTarget
	{
		get => _useProfitMoneyTarget.Value;
		set => _useProfitMoneyTarget.Value = value;
	}

	/// <summary>
	/// Profit target in account currency before flattening positions.
	/// </summary>
	public decimal ProfitMoneyTarget
	{
		get => _profitMoneyTarget.Value;
		set => _profitMoneyTarget.Value = value;
	}

	/// <summary>
	/// Enables closing all exposure once the configured profit percent is reached.
	/// </summary>
	public bool UseProfitPercentTarget
	{
		get => _useProfitPercentTarget.Value;
		set => _useProfitPercentTarget.Value = value;
	}
	/// <summary>
	/// Percentage of the account balance used as a take-profit threshold.
	/// </summary>
	public decimal ProfitPercentTarget
	{
		get => _profitPercentTarget.Value;
		set => _profitPercentTarget.Value = value;
	}

	/// <summary>
	/// Enables money based trailing of total strategy profit.
	/// </summary>
	public bool EnableMoneyTrail
	{
		get => _enableMoneyTrail.Value;
		set => _enableMoneyTrail.Value = value;
	}

	/// <summary>
	/// Profit level that activates the money trailing stop.
	/// </summary>
	public decimal MoneyTrailTrigger
	{
		get => _moneyTrailTrigger.Value;
		set => _moneyTrailTrigger.Value = value;
	}

	/// <summary>
	/// Maximum allowed drawdown from the trailing peak before exiting.
	/// </summary>
	public decimal MoneyTrailStop
	{
		get => _moneyTrailStop.Value;
		set => _moneyTrailStop.Value = value;
	}

	/// <summary>
	/// Fast period of the MACD filter.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow period of the MACD filter.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal period of the MACD filter.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for execution timing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate the MACD direction filter.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Initializes the parameter set.
	/// </summary>
	public TrendLineByAngleStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size placed per entry", "Orders")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_maxEntries = Param(nameof(MaxEntries), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Entries", "Number of sequential volume blocks", "Orders")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
			.SetDisplay("Trailing Stop (pips)", "Trailing distance maintained after price advances", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use Break-Even", "Move the stop to safety after reaching the trigger", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 10m)
			.SetDisplay("Break-Even Trigger (pips)", "Profit required before break-even activates", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
			.SetDisplay("Break-Even Offset (pips)", "Extra pips added to the break-even stop", "Risk");

		_useBollingerExit = Param(nameof(UseBollingerExit), true)
			.SetDisplay("Use Bollinger Exit", "Close when price touches the outer band", "Logic");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Number of candles used for Bollinger Bands", "Logic");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Logic");

		_useProfitMoneyTarget = Param(nameof(UseProfitMoneyTarget), false)
			.SetDisplay("Use Money Take-Profit", "Close all orders after reaching the money target", "Capital");

		_profitMoneyTarget = Param(nameof(ProfitMoneyTarget), 10m)
			.SetDisplay("Money Take-Profit", "Account currency profit target", "Capital");

		_useProfitPercentTarget = Param(nameof(UseProfitPercentTarget), false)
			.SetDisplay("Use Percent Take-Profit", "Close all orders after reaching the percent target", "Capital");

		_profitPercentTarget = Param(nameof(ProfitPercentTarget), 10m)
			.SetDisplay("Percent Take-Profit", "Target percent of account balance", "Capital");

		_enableMoneyTrail = Param(nameof(EnableMoneyTrail), true)
			.SetDisplay("Enable Money Trail", "Protect floating profit with a trailing stop", "Capital");

		_moneyTrailTrigger = Param(nameof(MoneyTrailTrigger), 40m)
			.SetDisplay("Money Trail Trigger", "Profit required before money trail activates", "Capital");

		_moneyTrailStop = Param(nameof(MoneyTrailStop), 10m)
			.SetDisplay("Money Trail Stop", "Maximum drawdown from the profit peak", "Capital");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Filter");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Filter");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period for MACD", "Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Execution Candle", "Candle type that drives orders", "Data");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("Signal Candle", "Candle type used for MACD analysis", "Data");

		ResetState();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		ResetState();
		base.OnReseted();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		_stepPrice = CalculateStepPrice();

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Fast = MacdFastPeriod,
			Slow = MacdSlowPeriod,
			Signal = MacdSignalPeriod
		};

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_bollinger, ProcessMainCandle)
			.Start();

		var macdSubscription = SubscribeCandles(SignalCandleType);
		macdSubscription
			.BindEx(_macd, ProcessMacdCandle)
			.Start();

		StartProtection();
	}
	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (!indicatorValue.IsFinal || candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not MovingAverageConvergenceDivergenceSignalValue macdValue)
			return;

		if (macdValue.Macd is not decimal macdMain || macdValue.Signal is not decimal macdSignal)
			return;

		_previousMacdMain = _macdMain;
		_previousMacdSignal = _macdSignal;
		_macdMain = macdMain;
		_macdSignal = macdSignal;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var totalPnL = CalculateTotalPnL(candle);

		if (CheckProfitTargets(totalPnL))
		{
			return;
		}

		if (ApplyMoneyTrailing(totalPnL))
		{
			return;
		}

		ManageOpenPositions(candle, upperBand, lowerBand);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsMacdReady())
			return;

		var macdMain = _macdMain!.Value;
		var macdSignal = _macdSignal!.Value;
		var prevMacdMain = _previousMacdMain!.Value;
		var prevMacdSignal = _previousMacdSignal!.Value;

		var crossUp = prevMacdMain <= prevMacdSignal && macdMain > macdSignal;
		var crossDown = prevMacdMain >= prevMacdSignal && macdMain < macdSignal;

		if (crossUp)
		{
			HandleLongSignal(candle);
		}
		else if (crossDown)
		{
			HandleShortSignal(candle);
		}
	}

	private void HandleLongSignal(ICandleMessage candle)
	{
		if (TradeVolume <= 0m)
			return;

		if (Position < 0m)
		{
			var coverVolume = Math.Abs(Position);
			if (coverVolume > 0m)
			{
				BuyMarket(coverVolume);
				ResetShortTargets();
			}
		}

		var targetVolume = TradeVolume * MaxEntries;
		var currentLong = Position > 0m ? Position : 0m;
		var volumeToBuy = targetVolume - currentLong;

		if (volumeToBuy <= 0m)
			return;

		BuyMarket(volumeToBuy);
		UpdateLongTargets(candle);
	}

	private void HandleShortSignal(ICandleMessage candle)
	{
		if (TradeVolume <= 0m)
			return;

		if (Position > 0m)
		{
			var flattenVolume = Math.Abs(Position);
			if (flattenVolume > 0m)
			{
				SellMarket(flattenVolume);
				ResetLongTargets();
			}
		}

		var targetVolume = TradeVolume * MaxEntries;
		var currentShort = Position < 0m ? Math.Abs(Position) : 0m;
		var volumeToSell = targetVolume - currentShort;

		if (volumeToSell <= 0m)
			return;

		SellMarket(volumeToSell);
		UpdateShortTargets(candle);
	}

	private void ManageOpenPositions(ICandleMessage candle, decimal upperBand, decimal lowerBand)
	{
		if (Position > 0m)
		{
			UpdateLongState(candle, upperBand);
		}
		else
		{
			ResetLongTargets();
		}

		if (Position < 0m)
		{
			UpdateShortState(candle, lowerBand);
		}
		else
		{
			ResetShortTargets();
		}
	}
	private void UpdateLongState(ICandleMessage candle, decimal upperBand)
	{
		var entryPrice = PositionPrice is decimal price ? price : _longEntryPrice;
		if (entryPrice > 0m)
		{
			_longEntryPrice = entryPrice;
		}

		if (UseBreakEven && !_longBreakEvenActivated)
		{
			var triggerDistance = ConvertPips(BreakEvenTriggerPips);
			if (triggerDistance > 0m && candle.HighPrice - _longEntryPrice >= triggerDistance)
			{
				var offset = ConvertPips(BreakEvenOffsetPips);
				_longStop = _longEntryPrice + offset;
				_longBreakEvenActivated = true;
			}
		}

		var trailingDistance = ConvertPips(TrailingStopPips);
		if (trailingDistance > 0m && candle.ClosePrice - _longEntryPrice >= trailingDistance)
		{
			var newStop = candle.ClosePrice - trailingDistance;
			if (_longStop is null || newStop > _longStop)
			{
				_longStop = newStop;
			}
		}

		var stopPrice = _longStop;
		var takePrice = _longTake;

		if (takePrice is decimal take && candle.HighPrice >= take)
		{
			CloseLongPosition();
			return;
		}

		if (stopPrice is decimal stop && candle.LowPrice <= stop)
		{
			CloseLongPosition();
			return;
		}

		if (UseBollingerExit && upperBand > 0m && candle.ClosePrice >= upperBand)
		{
			CloseLongPosition();
		}
	}

	private void UpdateShortState(ICandleMessage candle, decimal lowerBand)
	{
		var entryPrice = PositionPrice is decimal price ? price : _shortEntryPrice;
		if (entryPrice > 0m)
		{
			_shortEntryPrice = entryPrice;
		}

		if (UseBreakEven && !_shortBreakEvenActivated)
		{
			var triggerDistance = ConvertPips(BreakEvenTriggerPips);
			if (triggerDistance > 0m && _shortEntryPrice - candle.LowPrice >= triggerDistance)
			{
				var offset = ConvertPips(BreakEvenOffsetPips);
				_shortStop = _shortEntryPrice - offset;
				_shortBreakEvenActivated = true;
			}
		}

		var trailingDistance = ConvertPips(TrailingStopPips);
		if (trailingDistance > 0m && _shortEntryPrice - candle.ClosePrice >= trailingDistance)
		{
			var newStop = candle.ClosePrice + trailingDistance;
			if (_shortStop is null || newStop < _shortStop)
			{
				_shortStop = newStop;
			}
		}

		var stopPrice = _shortStop;
		var takePrice = _shortTake;

		if (takePrice is decimal take && candle.LowPrice <= take)
		{
			CloseShortPosition();
			return;
		}

		if (stopPrice is decimal stop && candle.HighPrice >= stop)
		{
			CloseShortPosition();
			return;
		}

		if (UseBollingerExit && lowerBand > 0m && candle.ClosePrice <= lowerBand)
		{
			CloseShortPosition();
		}
	}

	private void UpdateLongTargets(ICandleMessage candle)
	{
		var entryPrice = PositionPrice is decimal price ? price : candle.ClosePrice;
		_longEntryPrice = entryPrice;

		var stopDistance = ConvertPips(StopLossPips);
		_longStop = stopDistance > 0m ? entryPrice - stopDistance : null;

		var takeDistance = ConvertPips(TakeProfitPips);
		_longTake = takeDistance > 0m ? entryPrice + takeDistance : null;

		_longBreakEvenActivated = false;
	}

	private void UpdateShortTargets(ICandleMessage candle)
	{
		var entryPrice = PositionPrice is decimal price ? price : candle.ClosePrice;
		_shortEntryPrice = entryPrice;

		var stopDistance = ConvertPips(StopLossPips);
		_shortStop = stopDistance > 0m ? entryPrice + stopDistance : null;

		var takeDistance = ConvertPips(TakeProfitPips);
		_shortTake = takeDistance > 0m ? entryPrice - takeDistance : null;

		_shortBreakEvenActivated = false;
	}
	private bool CheckProfitTargets(decimal? totalPnL)
	{
		if (totalPnL is null)
			return false;

		var pnl = totalPnL.Value;

		if (UseProfitMoneyTarget && ProfitMoneyTarget > 0m && pnl >= ProfitMoneyTarget)
		{
			CloseAllPositions();
			ResetMoneyTrail();
			return true;
		}

		if (UseProfitPercentTarget && ProfitPercentTarget > 0m)
		{
			var accountValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
			if (accountValue is decimal balance && balance > 0m)
			{
				var target = balance * ProfitPercentTarget / 100m;
				if (pnl >= target)
				{
					CloseAllPositions();
					ResetMoneyTrail();
					return true;
				}
			}
		}

		return false;
	}

	private bool ApplyMoneyTrailing(decimal? totalPnL)
	{
		if (!EnableMoneyTrail || MoneyTrailTrigger <= 0m || MoneyTrailStop <= 0m)
		{
			ResetMoneyTrail();
			return false;
		}

		if (totalPnL is null)
			return false;

		var pnl = totalPnL.Value;

		if (!_moneyTrailActive)
		{
			if (pnl >= MoneyTrailTrigger)
			{
				_moneyTrailActive = true;
				_moneyTrailPeak = pnl;
			}

			return false;
		}

		if (pnl > _moneyTrailPeak)
			_moneyTrailPeak = pnl;

		if (pnl <= _moneyTrailPeak - MoneyTrailStop)
		{
			CloseAllPositions();
			ResetMoneyTrail();
			return true;
		}

		return false;
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			var volume = Position;
			SellMarket(volume);
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);
			BuyMarket(volume);
		}

		ResetLongTargets();
		ResetShortTargets();
	}

	private void CloseLongPosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}

		ResetLongTargets();
	}

	private void CloseShortPosition()
	{
		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		ResetShortTargets();
	}

	private void ResetLongTargets()
	{
		_longStop = null;
		_longTake = null;
		_longEntryPrice = 0m;
		_longBreakEvenActivated = false;
	}

	private void ResetShortTargets()
	{
		_shortStop = null;
		_shortTake = null;
		_shortEntryPrice = 0m;
		_shortBreakEvenActivated = false;
	}

	private void ResetMoneyTrail()
	{
		_moneyTrailActive = false;
		_moneyTrailPeak = 0m;
	}

	private bool IsMacdReady()
	{
		return _macdMain.HasValue && _macdSignal.HasValue && _previousMacdMain.HasValue && _previousMacdSignal.HasValue;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep > 0m)
			return priceStep;

		return 0.0001m;
	}

	private decimal CalculateStepPrice()
	{
		var stepPrice = Security?.StepPrice ?? 0m;
		if (stepPrice > 0m)
			return stepPrice;

		return 1m;
	}

	private decimal ConvertPips(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		return pips * _pipSize;
	}

	private decimal? CalculateTotalPnL(ICandleMessage candle)
	{
		var realizedPnL = PnL;

		if (Position == 0m)
			return realizedPnL;

		if (_pipSize <= 0m || _stepPrice <= 0m)
			return null;

		if (PositionPrice is not decimal avgPrice || avgPrice <= 0m)
			return realizedPnL;

		var priceDiff = candle.ClosePrice - avgPrice;
		var steps = priceDiff / _pipSize;
		var openPnL = steps * _stepPrice * Position;

		return realizedPnL + openPnL;
	}

	private void ResetState()
	{
		_macdMain = null;
		_macdSignal = null;
		_previousMacdMain = null;
		_previousMacdSignal = null;

		ResetLongTargets();
		ResetShortTargets();
		ResetMoneyTrail();

		_pipSize = 0m;
		_stepPrice = 0m;
	}
}
