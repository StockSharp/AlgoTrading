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
/// Enhanced martingale strategy converted from the MetaTrader expert "10points 3 v005".
/// </summary>
public class TenPoints3V005Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<decimal> _initialStopPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _entryDistancePips;
	private readonly StrategyParam<decimal> _secureProfit;
	private readonly StrategyParam<bool> _useAccountProtection;
	private readonly StrategyParam<int> _ordersToProtect;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _isStandardAccount;
	private readonly StrategyParam<decimal> _eurUsdPipValue;
	private readonly StrategyParam<decimal> _gbpUsdPipValue;
	private readonly StrategyParam<decimal> _usdChfPipValue;
	private readonly StrategyParam<decimal> _usdJpyPipValue;
	private readonly StrategyParam<decimal> _defaultPipValue;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _minuteToStop;
	private readonly StrategyParam<bool> _stopLossProtection;
	private readonly StrategyParam<decimal> _stopLossAmount;
	private readonly StrategyParam<bool> _profitTargetEnabled;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _profitBuffer;
	private readonly StrategyParam<bool> _startProtectionEnabled;
	private readonly StrategyParam<decimal> _startProtectionLevel;
	private readonly StrategyParam<bool> _reboundLock;
	private readonly StrategyParam<decimal> _martingaleFactor;
	private readonly StrategyParam<decimal> _highTradeFactor;
	private readonly StrategyParam<bool> _closeOnFriday;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private decimal? _previousMacd;
	private decimal? _previousPreviousMacd;
	private decimal _openVolume;
	private decimal _averagePrice;
	private int _openTrades;
	private bool _isLongPosition;
	private decimal _lastEntryPrice;
	private decimal _lastEntryVolume;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _pipSize;
	private decimal _pipValue;
	private decimal _volumeStep;
	private bool _allowNewTrades;
	private bool _continueOpening;
	private Sides? _currentDirection;
	private decimal _martingaleBaseVolume;
	private decimal _bestEntryPrice;
	private decimal _worstEntryPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="TenPoints3V005Strategy"/>.
	/// </summary>
	public TenPoints3V005Strategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 8m)
			.SetDisplay("Take Profit (pips)", "Distance of the take profit for every entry in pips", "Risk")
			.SetCanOptimize(true);

		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetDisplay("Base Lot Size", "Fixed lot size used when money management is disabled", "Risk")
			.SetCanOptimize(true);

		_initialStopPips = Param(nameof(InitialStopPips), 0m)
			.SetDisplay("Initial Stop (pips)", "Initial protective stop distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 20m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance that activates after the trigger threshold", "Risk")
			.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of simultaneously open martingale trades", "General")
			.SetCanOptimize(true);

		_entryDistancePips = Param(nameof(EntryDistancePips), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Distance (pips)", "Minimum adverse move required before adding a new position", "General")
			.SetCanOptimize(true);

		_secureProfit = Param(nameof(SecureProfit), 300m)
			.SetDisplay("Secure Profit", "Floating profit in currency units required to protect the account", "Risk")
			.SetCanOptimize(true);

		_useAccountProtection = Param(nameof(UseAccountProtection), true)
			.SetDisplay("Use Account Protection", "Enable partial liquidation when floating profit exceeds the threshold", "Risk");

		_ordersToProtect = Param(nameof(OrdersToProtect), 3)
			.SetGreaterThanZero()
			.SetDisplay("Orders To Protect", "Number of final trades protected by the secure profit rule", "Risk")
			.SetCanOptimize(true);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Reverse the MACD slope interpretation", "Filters");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Use Money Management", "Enable balance-based position sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 12m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Risk percentage used to derive the base lot size", "Risk")
			.SetCanOptimize(true);

		_isStandardAccount = Param(nameof(IsStandardAccount), false)
			.SetDisplay("Standard Account", "Use standard lot calculations instead of mini account scaling", "Risk");

		_eurUsdPipValue = Param(nameof(EurUsdPipValue), 10m)
			.SetDisplay("EURUSD Pip Value", "Monetary value of one pip for EURUSD", "Currency")
			.SetCanOptimize(true);

		_gbpUsdPipValue = Param(nameof(GbpUsdPipValue), 10m)
			.SetDisplay("GBPUSD Pip Value", "Monetary value of one pip for GBPUSD", "Currency")
			.SetCanOptimize(true);

		_usdChfPipValue = Param(nameof(UsdChfPipValue), 10m)
			.SetDisplay("USDCHF Pip Value", "Monetary value of one pip for USDCHF", "Currency")
			.SetCanOptimize(true);

		_usdJpyPipValue = Param(nameof(UsdJpyPipValue), 9.715m)
			.SetDisplay("USDJPY Pip Value", "Monetary value of one pip for USDJPY", "Currency")
			.SetCanOptimize(true);

		_defaultPipValue = Param(nameof(DefaultPipValue), 5m)
			.SetDisplay("Default Pip Value", "Fallback pip value used for other symbols", "Currency")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for MACD calculation", "General");

		_macdFastLength = Param(nameof(MacdFastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period used in MACD", "Filters")
			.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period used in MACD", "Filters")
			.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period used in MACD", "Filters")
			.SetCanOptimize(true);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow opening of long martingale sequences", "Filters");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow opening of short martingale sequences", "Filters");

		_openHour = Param(nameof(OpenHour), 0)
			.SetDisplay("Open Hour", "Hour when trading becomes active (0-23)", "Schedule")
			.SetCanOptimize(true);

		_closeHour = Param(nameof(CloseHour), 0)
			.SetDisplay("Close Hour", "Hour when trading stops (0-23, 0 keeps trading open)", "Schedule")
			.SetCanOptimize(true);

		_minuteToStop = Param(nameof(MinuteToStop), 55)
			.SetDisplay("Minute To Stop", "Minutes before the close hour when new trades stop", "Schedule")
			.SetCanOptimize(true);

		_stopLossProtection = Param(nameof(StopLossProtection), false)
			.SetDisplay("Stop Loss Protection", "Close all trades when the floating loss exceeds the threshold", "Risk");

		_stopLossAmount = Param(nameof(StopLossAmount), 200m)
			.SetDisplay("Stop Loss Amount", "Loss threshold (in currency) for stop-loss protection", "Risk")
			.SetCanOptimize(true);

		_profitTargetEnabled = Param(nameof(ProfitTargetEnabled), false)
			.SetDisplay("Profit Target Enabled", "Stop opening new baskets after reaching the equity target", "Risk");

		_profitTarget = Param(nameof(ProfitTarget), 1300m)
			.SetDisplay("Profit Target", "Equity level that triggers the profit lock", "Risk")
			.SetCanOptimize(true);

		_profitBuffer = Param(nameof(ProfitBuffer), 15m)
			.SetDisplay("Profit Buffer", "Additional equity buffer above the target before closing", "Risk")
			.SetCanOptimize(true);

		_startProtectionEnabled = Param(nameof(StartProtectionEnabled), false)
			.SetDisplay("Start Protection Enabled", "Stop trading if equity falls below the start level", "Risk");

		_startProtectionLevel = Param(nameof(StartProtectionLevel), 1000m)
			.SetDisplay("Start Protection Level", "Equity floor that triggers protective liquidation", "Risk")
			.SetCanOptimize(true);

		_reboundLock = Param(nameof(ReboundLock), true)
			.SetDisplay("Rebound Lock", "Align stop and take profit with the best entry when protection is active", "Risk");

		_martingaleFactor = Param(nameof(MartingaleFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Factor", "Multiplier applied to each additional order", "Risk")
			.SetCanOptimize(true);

		_highTradeFactor = Param(nameof(HighTradeFactor), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("High Trade Factor", "Multiplier used when the basket allows more than 12 trades", "Risk")
			.SetCanOptimize(true);

		_closeOnFriday = Param(nameof(CloseOnFriday), true)
			.SetDisplay("Close On Friday", "Exit positions and stop trading during Fridays", "Schedule");
	}

	/// <summary>
	/// Take profit distance in pips applied to every entry.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Fixed lot size when balance based sizing is disabled.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance expressed in pips.
	/// </summary>
	public decimal InitialStopPips
	{
		get => _initialStopPips.Value;
		set => _initialStopPips.Value = value;
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
	/// Maximum number of martingale entries allowed at the same time.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Required adverse movement before averaging into the position.
	/// </summary>
	public decimal EntryDistancePips
	{
		get => _entryDistancePips.Value;
		set => _entryDistancePips.Value = value;
	}

	/// <summary>
	/// Floating profit threshold that triggers protective liquidation.
	/// </summary>
	public decimal SecureProfit
	{
		get => _secureProfit.Value;
		set => _secureProfit.Value = value;
	}

	/// <summary>
	/// Enables protective liquidation when floating profit is high enough.
	/// </summary>
	public bool UseAccountProtection
	{
		get => _useAccountProtection.Value;
		set => _useAccountProtection.Value = value;
	}

	/// <summary>
	/// Number of final martingale positions guarded by the secure profit rule.
	/// </summary>
	public int OrdersToProtect
	{
		get => _ordersToProtect.Value;
		set => _ordersToProtect.Value = value;
	}

	/// <summary>
	/// Reverses the MACD slope interpretation used for signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Enables balance based position sizing logic.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk percentage used to compute the base lot size when money management is active.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Indicates whether the account uses standard lot sizing (as opposed to mini).
	/// </summary>
	public bool IsStandardAccount
	{
		get => _isStandardAccount.Value;
		set => _isStandardAccount.Value = value;
	}

	/// <summary>
	/// Monetary value of one pip for EURUSD.
	/// </summary>
	public decimal EurUsdPipValue
	{
		get => _eurUsdPipValue.Value;
		set => _eurUsdPipValue.Value = value;
	}

	/// <summary>
	/// Monetary value of one pip for GBPUSD.
	/// </summary>
	public decimal GbpUsdPipValue
	{
		get => _gbpUsdPipValue.Value;
		set => _gbpUsdPipValue.Value = value;
	}

	/// <summary>
	/// Monetary value of one pip for USDCHF.
	/// </summary>
	public decimal UsdChfPipValue
	{
		get => _usdChfPipValue.Value;
		set => _usdChfPipValue.Value = value;
	}

	/// <summary>
	/// Monetary value of one pip for USDJPY.
	/// </summary>
	public decimal UsdJpyPipValue
	{
		get => _usdJpyPipValue.Value;
		set => _usdJpyPipValue.Value = value;
	}

	/// <summary>
	/// Default pip value for other instruments.
	/// </summary>
	public decimal DefaultPipValue
	{
		get => _defaultPipValue.Value;
		set => _defaultPipValue.Value = value;
	}

	/// <summary>
	/// Candle type used for MACD calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length of the MACD indicator.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length of the MACD indicator.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length of the MACD indicator.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Allows opening of long martingale baskets.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allows opening of short martingale baskets.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Hour of the day when trading becomes active.
	/// </summary>
	public int OpenHour
	{
		get => _openHour.Value;
		set => _openHour.Value = value;
	}

	/// <summary>
	/// Hour of the day when trading stops.
	/// </summary>
	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <summary>
	/// Minute threshold before the close hour when trading stops.
	/// </summary>
	public int MinuteToStop
	{
		get => _minuteToStop.Value;
		set => _minuteToStop.Value = value;
	}

	/// <summary>
	/// Enables floating-loss based liquidation.
	/// </summary>
	public bool StopLossProtection
	{
		get => _stopLossProtection.Value;
		set => _stopLossProtection.Value = value;
	}

	/// <summary>
	/// Floating loss threshold used by stop-loss protection.
	/// </summary>
	public decimal StopLossAmount
	{
		get => _stopLossAmount.Value;
		set => _stopLossAmount.Value = value;
	}

	/// <summary>
	/// Enables the equity based profit target guard.
	/// </summary>
	public bool ProfitTargetEnabled
	{
		get => _profitTargetEnabled.Value;
		set => _profitTargetEnabled.Value = value;
	}

	/// <summary>
	/// Desired equity level that stops new baskets.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Equity buffer above the profit target before liquidation starts.
	/// </summary>
	public decimal ProfitBuffer
	{
		get => _profitBuffer.Value;
		set => _profitBuffer.Value = value;
	}

	/// <summary>
	/// Enables the protective floor based on starting equity.
	/// </summary>
	public bool StartProtectionEnabled
	{
		get => _startProtectionEnabled.Value;
		set => _startProtectionEnabled.Value = value;
	}

	/// <summary>
	/// Equity floor that triggers protective liquidation.
	/// </summary>
	public decimal StartProtectionLevel
	{
		get => _startProtectionLevel.Value;
		set => _startProtectionLevel.Value = value;
	}

	/// <summary>
	/// Aligns stops with the best entry when account protection is active.
	/// </summary>
	public bool ReboundLock
	{
		get => _reboundLock.Value;
		set => _reboundLock.Value = value;
	}

	/// <summary>
	/// Multiplier applied to additional martingale steps.
	/// </summary>
	public decimal MartingaleFactor
	{
		get => _martingaleFactor.Value;
		set => _martingaleFactor.Value = value;
	}

	/// <summary>
	/// Multiplier used when the basket allows more than twelve trades.
	/// </summary>
	public decimal HighTradeFactor
	{
		get => _highTradeFactor.Value;
		set => _highTradeFactor.Value = value;
	}

	/// <summary>
	/// Closes all trades and disables entries on Fridays.
	/// </summary>
	public bool CloseOnFriday
	{
		get => _closeOnFriday.Value;
		set => _closeOnFriday.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousMacd = null;
		_previousPreviousMacd = null;
		_openVolume = 0m;
		_averagePrice = 0m;
		_openTrades = 0;
		_isLongPosition = false;
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_pipSize = 0m;
		_pipValue = 0m;
		_volumeStep = 0m;
		_allowNewTrades = true;
		_continueOpening = true;
		_currentDirection = null;
		_martingaleBaseVolume = 0m;
		_bestEntryPrice = 0m;
		_worstEntryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
			_pipSize = 0.0001m;

		_volumeStep = Security?.VolumeStep ?? 0.01m;
		if (_volumeStep <= 0m)
			_volumeStep = 0.01m;

		_pipValue = DeterminePipValue();
		_martingaleBaseVolume = CalculateBaseVolume();

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not MovingAverageConvergenceDivergenceSignalValue macdValue)
			return;

		var macdMain = macdValue.Macd;
		var previousMacd = _previousMacd;
		var previousPreviousMacd = _previousPreviousMacd;

		_previousPreviousMacd = previousMacd;
		_previousMacd = macdMain;

		var time = candle.CloseTime;

		if (!IsTradingTime(time))
		{
			TryFlattenPosition();
			return;
		}

		var currentPrice = candle.ClosePrice;
		EvaluateBalanceGuards(currentPrice);

		if (_openTrades > 0)
		{
			ManageOpenPosition(currentPrice);
			if (_openTrades == 0)
				return;
		}

		_continueOpening = _openTrades < MaxTrades && _allowNewTrades;
		if (!_continueOpening)
			return;

		if (_openTrades == 0)
		{
			_currentDirection = DetermineDirection(previousMacd, previousPreviousMacd);
			if (_currentDirection.HasValue)
				TryOpenPosition(_currentDirection.Value, currentPrice);
		}
		else if (_currentDirection.HasValue)
		{
			TryAddPosition(_currentDirection.Value, currentPrice);
		}
	}

	private void EvaluateBalanceGuards(decimal currentPrice)
	{
		if (_openTrades == 0)
			return;

		var equity = Portfolio?.CurrentValue ?? 0m;
		var profit = CalculateFloatingProfit(currentPrice);

		if (StopLossProtection && profit <= -StopLossAmount && _openVolume > 0m)
		{
			TryFlattenPosition();
			return;
		}

		if (ProfitTargetEnabled && equity > 0m)
		{
			if (equity > ProfitTarget + ProfitBuffer && _openVolume > 0m)
			{
				TryFlattenPosition();
				_allowNewTrades = false;
			}
		}

		if (StartProtectionEnabled && equity > 0m && equity < StartProtectionLevel && _openVolume > 0m)
		{
			TryFlattenPosition();
			_allowNewTrades = false;
		}
	}

	private void ManageOpenPosition(decimal currentPrice)
	{
		if (_openVolume <= 0m)
			return;

		if (_stopLossPrice.HasValue)
		{
			if (_isLongPosition && currentPrice <= _stopLossPrice.Value)
			{
				SellMarket(_openVolume);
				return;
			}
			if (!_isLongPosition && currentPrice >= _stopLossPrice.Value)
			{
				BuyMarket(_openVolume);
				return;
			}
		}

		if (_takeProfitPrice.HasValue)
		{
			if (_isLongPosition && currentPrice >= _takeProfitPrice.Value)
			{
				SellMarket(_openVolume);
				return;
			}
			if (!_isLongPosition && currentPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(_openVolume);
				return;
			}
		}

		if (UseAccountProtection && ReboundLock)
			ApplyReboundLock();

		if (TrailingStopPips > 0m)
			UpdateTrailingStop(currentPrice);

		if (UseAccountProtection && _openTrades >= Math.Max(1, MaxTrades - OrdersToProtect))
		{
			var profit = CalculateFloatingProfit(currentPrice);
			if (profit >= SecureProfit && _lastEntryVolume > 0m)
			{
				if (_isLongPosition)
					SellMarket(_lastEntryVolume);
				else
					BuyMarket(_lastEntryVolume);
				_allowNewTrades = false;
			}
		}
	}

	private void ApplyReboundLock()
	{
		if (_openTrades < OrdersToProtect || _openVolume <= 0m)
			return;

		var tpOffset = ToPrice(EntryDistancePips);
		var slOffset = ToPrice(TrailingStopPips);

		if (_isLongPosition && _bestEntryPrice > 0m)
		{
			var target = _bestEntryPrice + tpOffset;
			var stop = _bestEntryPrice - slOffset;

			if (!_takeProfitPrice.HasValue || target < _takeProfitPrice.Value)
				_takeProfitPrice = target;

			if (TrailingStopPips > 0m)
			{
				if (!_stopLossPrice.HasValue || stop > _stopLossPrice.Value)
					_stopLossPrice = stop;
			}
		}
		else if (!_isLongPosition && _worstEntryPrice > 0m)
		{
			var target = _worstEntryPrice - tpOffset;
			var stop = _worstEntryPrice + slOffset;

			if (!_takeProfitPrice.HasValue || target > _takeProfitPrice.Value)
				_takeProfitPrice = target;

			if (TrailingStopPips > 0m)
			{
				if (!_stopLossPrice.HasValue || stop < _stopLossPrice.Value)
					_stopLossPrice = stop;
			}
		}
	}

	private void UpdateTrailingStop(decimal currentPrice)
	{
		var trailingDistance = ToPrice(TrailingStopPips);
		var threshold = trailingDistance + ToPrice(EntryDistancePips);

		if (_isLongPosition)
		{
			var profit = currentPrice - _averagePrice;
			if (profit >= threshold)
			{
				var newStop = currentPrice - trailingDistance;
				if (!_stopLossPrice.HasValue || newStop > _stopLossPrice.Value)
					_stopLossPrice = newStop;
			}
		}
		else
		{
			var profit = _averagePrice - currentPrice;
			if (profit >= threshold)
			{
				var newStop = currentPrice + trailingDistance;
				if (!_stopLossPrice.HasValue || newStop < _stopLossPrice.Value)
					_stopLossPrice = newStop;
			}
		}
	}

	private void TryOpenPosition(Sides direction, decimal currentPrice)
	{
		if (direction == Sides.Buy && !EnableLong)
			return;
		if (direction == Sides.Sell && !EnableShort)
			return;

		var volume = CalculateNextVolume();
		if (volume <= 0m)
			return;

		if (direction == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}

	private void TryAddPosition(Sides direction, decimal currentPrice)
	{
		var distance = ToPrice(EntryDistancePips);
		var canAdd = direction == Sides.Buy
			? (_lastEntryPrice - currentPrice) >= distance
			: (currentPrice - _lastEntryPrice) >= distance;

		if (!canAdd)
			return;

		TryOpenPosition(direction, currentPrice);
	}

	private Sides? DetermineDirection(decimal? macdPrev, decimal? macdPrevPrev)
	{
		if (!macdPrev.HasValue || !macdPrevPrev.HasValue)
			return null;

		var isBullish = macdPrev.Value > macdPrevPrev.Value;
		var isBearish = macdPrev.Value < macdPrevPrev.Value;

		if (!isBullish && !isBearish)
			return null;

		if (ReverseSignals)
		{
			(isBullish, isBearish) = (isBearish, isBullish);
		}

		if (isBullish && EnableLong)
			return Sides.Buy;
		if (isBearish && EnableShort)
			return Sides.Sell;

		return null;
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		if (CloseOnFriday && time.DayOfWeek == DayOfWeek.Friday)
			return false;

		var openHour = NormalizeHour(OpenHour);
		var closeHour = NormalizeHour(CloseHour);

		var hour = time.Hour;
		var minute = time.Minute;

		bool inside;
		if (openHour < closeHour)
			inside = hour >= openHour && hour < closeHour;
		else if (openHour > closeHour)
			inside = hour >= openHour || hour < closeHour;
		else
			inside = true;

		if (!inside)
			return false;

		var effectiveClose = closeHour == 0 ? 24 : closeHour;
		if (effectiveClose > 0 && hour == effectiveClose - 1 && minute >= MinuteToStop)
			return false;

		return true;
	}

	private static int NormalizeHour(int hour)
	{
		if (hour < 0)
			return 0;
		if (hour > 23)
			return 23;
		return hour;
	}

	private void TryFlattenPosition()
	{
		if (_openVolume <= 0m)
			return;

		if (_isLongPosition)
			SellMarket(_openVolume);
		else
			BuyMarket(_openVolume);
	}

	private decimal CalculateFloatingProfit(decimal currentPrice)
	{
		if (_openVolume <= 0m || _pipSize <= 0m)
			return 0m;

		var profitPips = _isLongPosition
			? (currentPrice - _averagePrice) / _pipSize * _openVolume
			: (_averagePrice - currentPrice) / _pipSize * _openVolume;

		return profitPips * _pipValue;
	}

	private decimal CalculateBaseVolume()
	{
		var volume = LotSize;

		if (UseMoneyManagement)
		{
			var balance = Portfolio?.CurrentValue ?? 0m;
			if (balance > 0m)
			{
				var riskValue = balance * RiskPercent / 10000m;
				var rounded = Math.Ceiling(riskValue);
				volume = IsStandardAccount ? rounded : rounded / 10m;
			}
		}

		if (volume > 100m)
			volume = 100m;

		return AlignVolume(volume);
	}

	private decimal CalculateNextVolume()
	{
		var baseVolume = _martingaleBaseVolume > 0m ? _martingaleBaseVolume : CalculateBaseVolume();
		var volume = baseVolume;

		if (_openTrades > 0)
		{
			var factor = MaxTrades > 12 ? HighTradeFactor : MartingaleFactor;
			var power = (decimal)Math.Pow((double)factor, _openTrades);
			volume = AlignVolume(Math.Round(baseVolume * power, 2, MidpointRounding.AwayFromZero));
		}

		if (volume > 100m)
			volume = 100m;

		return AlignVolume(volume);
	}

	private decimal AlignVolume(decimal volume)
	{
		if (_volumeStep <= 0m)
			return volume;

		var steps = Math.Max(1m, Math.Floor(volume / _volumeStep));
		return steps * _volumeStep;
	}

	private decimal DeterminePipValue()
	{
		var code = Security?.Code?.ToUpperInvariant();
		return code switch
		{
			"EURUSD" => EurUsdPipValue,
			"GBPUSD" => GbpUsdPipValue,
			"USDCHF" => UsdChfPipValue,
			"USDJPY" => UsdJpyPipValue,
			_ => DefaultPipValue,
		};
	}

	private decimal ToPrice(decimal pips)
	{
		return pips * _pipSize;
	}

	private void ResetPositionState()
	{
		_openVolume = 0m;
		_averagePrice = 0m;
		_openTrades = 0;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_allowNewTrades = true;
		_currentDirection = null;
		_bestEntryPrice = 0m;
		_worstEntryPrice = 0m;
	}

	private decimal? UpdateStopAfterEntry(bool isLong, decimal price)
	{
		if (InitialStopPips <= 0m)
			return _stopLossPrice;

		var stopOffset = ToPrice(InitialStopPips);
		if (isLong)
		{
			var candidate = price - stopOffset;
			return !_stopLossPrice.HasValue || candidate < _stopLossPrice.Value ? candidate : _stopLossPrice;
		}

		var candidateShort = price + stopOffset;
		return !_stopLossPrice.HasValue || candidateShort > _stopLossPrice.Value ? candidateShort : _stopLossPrice;
	}

	private decimal? UpdateTakeProfitAfterEntry(bool isLong, decimal price)
	{
		if (TakeProfitPips <= 0m)
			return _takeProfitPrice;

		var takeOffset = ToPrice(TakeProfitPips);
		if (isLong)
		{
			var candidate = price + takeOffset;
			return !_takeProfitPrice.HasValue || candidate > _takeProfitPrice.Value ? candidate : _takeProfitPrice;
		}

		var candidateShort = price - takeOffset;
		return !_takeProfitPrice.HasValue || candidateShort < _takeProfitPrice.Value ? candidateShort : _takeProfitPrice;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;
		var side = trade.Order.Side;

		if (side == Sides.Buy)
		{
			if (_openVolume > 0m && !_isLongPosition)
			{
				HandlePositionReduction(volume);
				return;
			}

			var newVolume = _openVolume + volume;
			_averagePrice = newVolume == 0m ? 0m : (_averagePrice * _openVolume + price * volume) / newVolume;
			_openVolume = newVolume;
			_isLongPosition = true;
			_openTrades++;
			_lastEntryPrice = price;
			_lastEntryVolume = volume;
			_stopLossPrice = UpdateStopAfterEntry(true, price);
			_takeProfitPrice = UpdateTakeProfitAfterEntry(true, price);
			_bestEntryPrice = _bestEntryPrice == 0m ? price : Math.Min(_bestEntryPrice, price);
			_martingaleBaseVolume = CalculateBaseVolume();
		}
		else if (side == Sides.Sell)
		{
			if (_openVolume > 0m && _isLongPosition)
			{
				HandlePositionReduction(volume);
				return;
			}

			var newVolume = _openVolume + volume;
			_averagePrice = newVolume == 0m ? 0m : (_averagePrice * _openVolume + price * volume) / newVolume;
			_openVolume = newVolume;
			_isLongPosition = false;
			_openTrades++;
			_lastEntryPrice = price;
			_lastEntryVolume = volume;
			_stopLossPrice = UpdateStopAfterEntry(false, price);
			_takeProfitPrice = UpdateTakeProfitAfterEntry(false, price);
			_worstEntryPrice = _worstEntryPrice == 0m ? price : Math.Max(_worstEntryPrice, price);
			_martingaleBaseVolume = CalculateBaseVolume();
		}

		_continueOpening = _openTrades < MaxTrades && _allowNewTrades;
	}

	private void HandlePositionReduction(decimal volume)
	{
		var closingVolume = Math.Min(_openVolume, volume);
		_openVolume -= closingVolume;
		if (_openVolume <= 0m)
		{
			ResetPositionState();
		}
		else if (_openTrades > 0)
		{
			_openTrades--;
		}
	}
}
