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
/// Zone recovery hedge strategy converted from the MetaTrader expert advisor "Zone Recovery Hedge V1".
/// Combines multi-timeframe RSI confirmation with a martingale-style recovery grid that alternates long and short positions.
/// Supports ATR based dynamic zone sizing, profit targeting expressed in money, and optional trading sessions.
/// </summary>
public class ZoneRecoveryHedgeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _atrCandleType;
	private readonly StrategyParam<ZoneRecoveryMode> _mode;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<bool> _useM1;
	private readonly StrategyParam<bool> _useM5;
	private readonly StrategyParam<bool> _useM15;
	private readonly StrategyParam<bool> _useM30;
	private readonly StrategyParam<bool> _useH1;
	private readonly StrategyParam<bool> _useH4;
	private readonly StrategyParam<bool> _useD1;
	private readonly StrategyParam<bool> _useW1;
	private readonly StrategyParam<bool> _useMn1;
	private readonly StrategyParam<int> _recoveryZoneSize;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _useAtr;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrZoneFraction;
	private readonly StrategyParam<decimal> _atrTakeProfitFraction;
	private readonly StrategyParam<decimal> _atrRecoveryFraction;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _setMaxLoss;
	private readonly StrategyParam<decimal> _maxLossMoney;
	private readonly StrategyParam<bool> _useRecoveryTakeProfit;
	private readonly StrategyParam<int> _recoveryTakeProfitPoints;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _initialLotSize;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _lotAddition;
	private readonly StrategyParam<decimal>[] _customLots;
	private readonly StrategyParam<bool> _useTimer;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<bool> _useLocalTime;
	private readonly StrategyParam<bool> _tradeOnBarOpen;
	private readonly StrategyParam<decimal> _pendingPrice;
	private readonly StrategyParam<decimal> _testCommission;

	private Subscription _entrySubscription = null!;
	private readonly Dictionary<DataType, RsiState> _rsiStates = new();
	private readonly List<RsiSetting> _rsiSettings = new();
	private AverageTrueRange _dailyAtr;
	private decimal? _dailyAtrValue;

	private readonly List<TradeStep> _steps = new();
	private bool _isLongCycle;
	private decimal _cycleBasePrice;
	private bool _recoveryStarted;
	private int _nextStepIndex;
	private decimal _lastTradeVolume;
	private decimal _initialVolume;

	/// <summary>
	/// Initializes a new instance of <see cref="ZoneRecoveryHedgeStrategy"/>.
	/// </summary>
	public ZoneRecoveryHedgeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Entry Candle", "Primary timeframe used for entries", "General");

		_atrCandleType = Param(nameof(AtrCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("ATR Candle", "Timeframe used for ATR based sizing", "General");

		_mode = Param(nameof(Mode), ZoneRecoveryMode.RsiMultiTimeframe)
		.SetDisplay("Mode", "Manual mode disables automatic entries", "Signals");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Period for every RSI confirmation", "Signals");

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
		.SetDisplay("RSI Overbought", "Upper RSI threshold", "Signals");

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
		.SetDisplay("RSI Oversold", "Lower RSI threshold", "Signals");

		_useM1 = Param(nameof(UseM1Timeframe), true)
		.SetDisplay("Use M1", "Include 1 minute RSI", "Signals");
		_useM5 = Param(nameof(UseM5Timeframe), false)
		.SetDisplay("Use M5", "Include 5 minute RSI", "Signals");
		_useM15 = Param(nameof(UseM15Timeframe), false)
		.SetDisplay("Use M15", "Include 15 minute RSI", "Signals");
		_useM30 = Param(nameof(UseM30Timeframe), false)
		.SetDisplay("Use M30", "Include 30 minute RSI", "Signals");
		_useH1 = Param(nameof(UseH1Timeframe), false)
		.SetDisplay("Use H1", "Include 1 hour RSI", "Signals");
		_useH4 = Param(nameof(UseH4Timeframe), false)
		.SetDisplay("Use H4", "Include 4 hour RSI", "Signals");
		_useD1 = Param(nameof(UseDailyTimeframe), false)
		.SetDisplay("Use D1", "Include daily RSI", "Signals");
		_useW1 = Param(nameof(UseWeeklyTimeframe), false)
		.SetDisplay("Use W1", "Include weekly RSI", "Signals");
		_useMn1 = Param(nameof(UseMonthlyTimeframe), false)
		.SetDisplay("Use MN1", "Include monthly RSI", "Signals");

		_recoveryZoneSize = Param(nameof(RecoveryZoneSize), 200)
		.SetGreaterThanZero()
		.SetDisplay("Zone Size", "Recovery zone width in points", "Recovery");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Initial take profit distance in points", "Recovery");

		_useAtr = Param(nameof(UseAtr), false)
		.SetDisplay("Use ATR", "Enable ATR based sizing", "Recovery");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Period for ATR calculation", "Recovery");

		_atrZoneFraction = Param(nameof(AtrZoneFraction), 0.2m)
		.SetDisplay("ATR Zone", "Fraction of ATR used for zone size", "Recovery");

		_atrTakeProfitFraction = Param(nameof(AtrTakeProfitFraction), 0.3m)
		.SetDisplay("ATR TP", "Fraction of ATR used for take profit", "Recovery");

		_atrRecoveryFraction = Param(nameof(AtrRecoveryFraction), 0.15m)
		.SetDisplay("ATR Recovery TP", "Fraction of ATR used for recovery target", "Recovery");

		_maxTrades = Param(nameof(MaxTrades), 0)
		.SetDisplay("Max Trades", "Maximum trades in one cycle (0 = unlimited)", "Risk");

		_setMaxLoss = Param(nameof(SetMaxLoss), false)
		.SetDisplay("Use Max Loss", "Close when max loss reached after max trades", "Risk");

		_maxLossMoney = Param(nameof(MaxLoss), 0m)
		.SetDisplay("Max Loss $", "Maximum allowed floating loss", "Risk");

		_useRecoveryTakeProfit = Param(nameof(UseRecoveryTakeProfit), true)
		.SetDisplay("Recovery TP", "Use dedicated recovery profit target", "Recovery");

		_recoveryTakeProfitPoints = Param(nameof(RecoveryTakeProfitPoints), 50)
		.SetDisplay("Recovery TP Points", "Target distance in points when recovering", "Recovery");

		_riskPercent = Param(nameof(RiskPercent), 0m)
		.SetDisplay("Risk %", "Account percentage for initial lot", "Money Management");

		_initialLotSize = Param(nameof(InitialLotSize), 0.1m)
		.SetDisplay("Initial Lot", "Fixed initial lot when risk % is disabled", "Money Management");

		_lotMultiplier = Param(nameof(LotMultiplier), 2m)
		.SetDisplay("Lot Multiplier", "Factor for the next recovery order", "Money Management");

		_lotAddition = Param(nameof(LotAddition), 0m)
		.SetDisplay("Lot Addition", "Volume added when multiplier disabled", "Money Management");

		_customLots = new[]
		{
			Param("CustomLotSize1", 0m).SetDisplay("Lot 1", "Custom lot for trade 1", "Money Management"),
			Param("CustomLotSize2", 0m).SetDisplay("Lot 2", "Custom lot for trade 2", "Money Management"),
			Param("CustomLotSize3", 0m).SetDisplay("Lot 3", "Custom lot for trade 3", "Money Management"),
			Param("CustomLotSize4", 0m).SetDisplay("Lot 4", "Custom lot for trade 4", "Money Management"),
			Param("CustomLotSize5", 0m).SetDisplay("Lot 5", "Custom lot for trade 5", "Money Management"),
			Param("CustomLotSize6", 0m).SetDisplay("Lot 6", "Custom lot for trade 6", "Money Management"),
			Param("CustomLotSize7", 0m).SetDisplay("Lot 7", "Custom lot for trade 7", "Money Management"),
			Param("CustomLotSize8", 0m).SetDisplay("Lot 8", "Custom lot for trade 8", "Money Management"),
			Param("CustomLotSize9", 0m).SetDisplay("Lot 9", "Custom lot for trade 9", "Money Management"),
			Param("CustomLotSize10", 0m).SetDisplay("Lot 10", "Custom lot for trade 10", "Money Management")
		};

		_useTimer = Param(nameof(UseTimer), false)
		.SetDisplay("Use Timer", "Restrict trading to specific hours", "Timer");

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Trading start hour", "Timer");
		_startMinute = Param(nameof(StartMinute), 0)
		.SetDisplay("Start Minute", "Trading start minute", "Timer");
		_endHour = Param(nameof(EndHour), 0)
		.SetDisplay("End Hour", "Trading end hour", "Timer");
		_endMinute = Param(nameof(EndMinute), 0)
		.SetDisplay("End Minute", "Trading end minute", "Timer");
		_useLocalTime = Param(nameof(UseLocalTime), false)
		.SetDisplay("Use Local Time", "Evaluate timer using local time", "Timer");

		_tradeOnBarOpen = Param(nameof(TradeOnBarOpen), true)
		.SetDisplay("Trade On Bar Open", "Use the previous bar for signals", "Signals");

		_pendingPrice = Param(nameof(PendingPrice), 0m)
		.SetDisplay("Pending Price", "Price used for manual pending cycles", "Manual");

		_testCommission = Param(nameof(TestCommission), 7m)
		.SetDisplay("Test Commission", "Simulated commission per net lot", "Risk");
	}

	/// <summary>
	/// Primary trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for ATR sizing.
	/// </summary>
	public DataType AtrCandleType
	{
		get => _atrCandleType.Value;
		set => _atrCandleType.Value = value;
	}

	/// <summary>
	/// Strategy operating mode.
	/// </summary>
	public ZoneRecoveryMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level considered overbought.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI level considered oversold.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Zone width in broker points when ATR is disabled.
	/// </summary>
	public int RecoveryZoneSize
	{
		get => _recoveryZoneSize.Value;
		set => _recoveryZoneSize.Value = value;
	}

	/// <summary>
	/// Base take profit distance in broker points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables ATR driven zone sizing.
	/// </summary>
	public bool UseAtr
	{
		get => _useAtr.Value;
		set => _useAtr.Value = value;
	}

	/// <summary>
	/// Maximum trades per recovery cycle (0 = unlimited).
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Use money based maximum loss when maximum trades are reached.
	/// </summary>
	public bool SetMaxLoss
	{
		get => _setMaxLoss.Value;
		set => _setMaxLoss.Value = value;
	}

	/// <summary>
	/// Money based maximum loss after reaching <see cref="MaxTrades"/>.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLossMoney.Value;
		set => _maxLossMoney.Value = value;
	}

	/// <summary>
	/// Enables dedicated recovery take profit size.
	/// </summary>
	public bool UseRecoveryTakeProfit
	{
		get => _useRecoveryTakeProfit.Value;
		set => _useRecoveryTakeProfit.Value = value;
	}

	/// <summary>
	/// Recovery take profit distance in broker points.
	/// </summary>
	public int RecoveryTakeProfitPoints
	{
		get => _recoveryTakeProfitPoints.Value;
		set => _recoveryTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Account percentage used for initial lot sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed initial lot size when <see cref="RiskPercent"/> equals zero.
	/// </summary>
	public decimal InitialLotSize
	{
		get => _initialLotSize.Value;
		set => _initialLotSize.Value = value;
	}

	/// <summary>
	/// Multiplier used for the next martingale trade.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Additional volume added for each trade.
	/// </summary>
	public decimal LotAddition
	{
		get => _lotAddition.Value;
		set => _lotAddition.Value = value;
	}

	/// <summary>
	/// Enables the trading timer.
	/// </summary>
	public bool UseTimer
	{
		get => _useTimer.Value;
		set => _useTimer.Value = value;
	}

	/// <summary>
	/// Pending price used for manual cycles.
	/// </summary>
	public decimal PendingPrice
	{
		get => _pendingPrice.Value;
		set => _pendingPrice.Value = value;
	}

	/// <summary>
	/// Starts a manual cycle using the current market price.
	/// </summary>
	public void StartManualMarketCycle(bool isBuy)
	{
		if (Mode != ZoneRecoveryMode.Manual)
		{
			LogInfo("Manual cycle ignored because the mode is not Manual.");
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var price = Security?.LastPrice ?? 0m;
		if (price <= 0m)
		{
			LogWarning("Manual cycle skipped because the market price is not available.");
			return;
		}

		TryStartCycle(isBuy, price);
	}

	/// <summary>
	/// Starts a manual cycle from the configured pending price.
	/// </summary>
	public void StartManualPendingCycle(bool isBuy)
	{
		if (Mode != ZoneRecoveryMode.Manual)
		{
			LogInfo("Manual cycle ignored because the mode is not Manual.");
			return;
		}

		if (PendingPrice <= 0m)
		{
			LogWarning("Manual cycle skipped because the pending price is not set.");
			return;
		}

		TryStartCycle(isBuy, PendingPrice);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeRsiSettings();

		_entrySubscription = SubscribeCandles(CandleType);

		var baseState = GetOrCreateRsiState(CandleType);
		_entrySubscription
		.Bind(baseState.Indicator, ProcessEntryCandle)
		.Start();

		foreach (var setting in _rsiSettings)
		{
			if (setting.Type == CandleType)
			continue;

			var state = GetOrCreateRsiState(setting.Type);
			var subscription = SubscribeCandles(setting.Type, allowBuildFromSmallerTimeFrame: true);
			subscription
			.Bind(state.Indicator, (candle, rsi) => ProcessRsiCandle(setting.Type, candle, rsi))
			.Start();
		}

		if (UseAtr)
		{
			_dailyAtr = new AverageTrueRange
			{
				Length = _atrPeriod.Value
			};

			var atrSubscription = SubscribeCandles(AtrCandleType, allowBuildFromSmallerTimeFrame: true);
			atrSubscription
			.Bind(_dailyAtr!, (candle, atr) => ProcessAtrCandle(candle, atr))
			.Start();
		}

		_initialVolume = 0m;
		_steps.Clear();
		_recoveryStarted = false;
		_nextStepIndex = 0;
		_lastTradeVolume = 0m;
	}

	private void ProcessEntryCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ProcessRsiCandle(CandleType, candle, rsiValue);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var tradingAllowed = !UseTimer || IsWithinTradingWindow(candle.CloseTime);
		if (!tradingAllowed)
		return;

		_initialVolume = CalculateInitialVolume();

		if (_steps.Count > 0)
		{
			HandleActiveCycle(candle);
			return;
		}

		if (Mode != ZoneRecoveryMode.RsiMultiTimeframe)
		return;

		var barShift = _tradeOnBarOpen.Value ? 1 : 0;
		var directionSignal = EvaluateRsiSignal(barShift + 1);
		var neutralSignal = EvaluateRsiSignal(barShift);

		if (directionSignal == 1 && neutralSignal == 0)
		{
			TryStartCycle(true, candle.ClosePrice);
		}
		else if (directionSignal == -1 && neutralSignal == 0)
		{
			TryStartCycle(false, candle.ClosePrice);
		}
	}

	private void ProcessRsiCandle(DataType type, ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_rsiStates.TryGetValue(type, out var state))
		{
			state.Update(rsiValue);
		}
	}

	private void ProcessAtrCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_dailyAtr != null && _dailyAtr.IsFormed)
		{
			_dailyAtrValue = atrValue;
		}
	}

	private void HandleActiveCycle(ICandleMessage candle)
	{
		if (Security == null)
		return;

		var closePrice = candle.ClosePrice;
		var priceStep = Security.PriceStep ?? 0m;
		var stepPrice = Security.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return;

		var currentProfit = CalculateCycleProfit(closePrice);
		var targetProfit = CalculateProfitTarget();
		var commissionCosts = CalculateCommissionCosts();

		if (currentProfit >= targetProfit + commissionCosts)
		{
			LogInfo($"Cycle closed by profit target. Profit={currentProfit:F2}, Target={targetProfit:F2}, Costs={commissionCosts:F2}.");
			CloseCycle();
			return;
		}

		if (MaxTrades > 0 && _steps.Count >= MaxTrades && SetMaxLoss && currentProfit <= -MaxLoss && MaxLoss > 0m)
		{
			LogInfo($"Cycle closed by max loss. Profit={currentProfit:F2}, Limit={-MaxLoss:F2}.");
			CloseCycle();
			return;
		}

		if (MaxTrades > 0 && _steps.Count >= MaxTrades)
		return;

		var zoneDistance = GetZoneDistance();
		if (zoneDistance <= 0m)
		return;

		var nextIsBuy = GetNextDirection();

		var triggerReached = ShouldOpenNextTrade(closePrice, zoneDistance, nextIsBuy);
		if (!triggerReached)
		return;

		var nextVolume = GetNextVolume();
		if (nextVolume <= 0m)
		return;

		ExecuteTrade(nextIsBuy, nextVolume, closePrice);
		_nextStepIndex++;
		_recoveryStarted = _steps.Count > 1;
	}

	private void TryStartCycle(bool isBuy, decimal price)
	{
		if (Security == null)
		return;

		if (_steps.Count > 0)
		{
			LogInfo("Cannot start a new cycle while the previous one is active.");
			return;
		}

		var volume = GetInitialVolume();
		if (volume <= 0m)
		{
			LogWarning("Initial volume is zero. Cycle will not be started.");
			return;
		}

		_isLongCycle = isBuy;
		_cycleBasePrice = price;
		_nextStepIndex = 1;
		_recoveryStarted = false;
		_lastTradeVolume = volume;

		ExecuteTrade(isBuy, volume, price);
	}

	private void ExecuteTrade(bool isBuy, decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		if (isBuy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_steps.Add(new TradeStep(isBuy, price, volume));
		LogInfo($"Opened {(isBuy ? "buy" : "sell")} trade. Volume={volume}, Price={price}.");
	}

	private decimal GetInitialVolume()
	{
		if (_customLots.Length > 0 && _customLots[0].Value > 0m)
		return NormalizeVolume(_customLots[0].Value);

		return NormalizeVolume(_initialVolume > 0m ? _initialVolume : CalculateInitialVolume());
	}

	private decimal CalculateInitialVolume()
	{
		var portfolio = Portfolio;
		var balance = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		var security = Security;
		if (security == null)
		return InitialLotSize;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return InitialLotSize;

		if (RiskPercent <= 0m)
		return InitialLotSize;

		var cappedPercent = Math.Min(RiskPercent, 10m);
		var margin = balance * cappedPercent / 100m;
		var stopDistance = GetInitialTakeProfitDistance();
		var stopSteps = stopDistance / priceStep;
		if (stopSteps <= 0m)
		return InitialLotSize;

		var rawVolume = margin / (stopSteps * stepPrice);
		return NormalizeVolume(rawVolume);
	}

	private decimal GetNextVolume()
	{
		var tradeIndex = _steps.Count;
		var customVolume = GetCustomLot(tradeIndex);
		if (customVolume > 0m)
		{
			_lastTradeVolume = customVolume;
			return customVolume;
		}

		decimal nextVolume;
		if (_lastTradeVolume <= 0m)
		{
			nextVolume = GetInitialVolume();
		}
		else
		{
			nextVolume = _lastTradeVolume * LotMultiplier + LotAddition;
		}

		nextVolume = NormalizeVolume(nextVolume);
		_lastTradeVolume = nextVolume;
		return nextVolume;
	}

	private decimal GetCustomLot(int tradeIndex)
	{
		if (tradeIndex < 0)
		return 0m;

		if (tradeIndex >= _customLots.Length)
		return 0m;

		var value = _customLots[tradeIndex].Value;
		return value <= 0m ? 0m : NormalizeVolume(value);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return Math.Round(volume, 2);

		var minVolume = security.MinVolume ?? 0m;
		var maxVolume = security.MaxVolume ?? decimal.MaxValue;
		var volumeStep = security.VolumeStep ?? 1m;

		var normalized = volume;
		if (minVolume > 0m && normalized < minVolume)
		normalized = minVolume;

		if (maxVolume > 0m && normalized > maxVolume)
		normalized = maxVolume;

		if (volumeStep > 0m)
		normalized = Math.Round(normalized / volumeStep) * volumeStep;

		return Math.Round(normalized, 6);
	}

	private bool GetNextDirection()
	{
		var isOddStep = _nextStepIndex % 2 == 1;
		return _isLongCycle ? !isOddStep : isOddStep;
	}

	private bool ShouldOpenNextTrade(decimal price, decimal zoneDistance, bool nextIsBuy)
	{
		if (_steps.Count == 0)
		return false;

		if (_isLongCycle)
		{
			if (nextIsBuy)
			return price >= _cycleBasePrice;

			return price <= _cycleBasePrice - zoneDistance;
		}

		if (nextIsBuy)
		return price >= _cycleBasePrice + zoneDistance;

		return price <= _cycleBasePrice;
	}

	private decimal CalculateCycleProfit(decimal price)
	{
		var security = Security;
		if (security == null)
		return 0m;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
		return 0m;

		decimal pnl = 0m;
		foreach (var step in _steps)
		{
			var diff = price - step.Price;
			var stepsCount = diff / priceStep;
			var direction = step.IsBuy ? 1m : -1m;
			pnl += stepsCount * stepPrice * step.Volume * direction;
		}

		return pnl;
	}

	private decimal CalculateProfitTarget()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
		return 0m;

		var volume = _customLots[0].Value > 0m ? _customLots[0].Value : (_initialVolume > 0m ? _initialVolume : _lastTradeVolume);
		if (volume <= 0m)
		volume = GetInitialVolume();

		var takeDistance = GetRecoveryTakeProfitDistance();
		var stepsCount = takeDistance / priceStep;
		return stepsCount * stepPrice * volume;
	}

	private decimal GetInitialTakeProfitDistance()
	{
		if (UseAtr && _dailyAtrValue is decimal atr && atr > 0m)
		{
			return atr * _atrTakeProfitFraction.Value;
		}

		var security = Security;
		var priceStep = security?.PriceStep ?? 0m;
		return priceStep * TakeProfitPoints;
	}

	private decimal GetRecoveryTakeProfitDistance()
	{
		if (!UseRecoveryTakeProfit || !_recoveryStarted)
		{
			return GetInitialTakeProfitDistance();
		}

		if (UseAtr && _dailyAtrValue is decimal atr && atr > 0m)
		{
			return atr * _atrRecoveryFraction.Value;
		}

		var security = Security;
		var priceStep = security?.PriceStep ?? 0m;
		return priceStep * RecoveryTakeProfitPoints;
	}

	private decimal GetZoneDistance()
	{
		if (UseAtr && _dailyAtrValue is decimal atr && atr > 0m)
		{
			return atr * _atrZoneFraction.Value;
		}

		var security = Security;
		var priceStep = security?.PriceStep ?? 0m;
		return priceStep * RecoveryZoneSize;
	}

	private decimal CalculateCommissionCosts()
	{
		if (_steps.Count == 0)
		return 0m;

		decimal netVolume = 0m;
		foreach (var step in _steps)
		{
			netVolume += step.Volume;
		}

		return netVolume * _testCommission.Value;
	}

	private void CloseCycle()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		_steps.Clear();
		_cycleBasePrice = 0m;
		_recoveryStarted = false;
		_nextStepIndex = 0;
		_lastTradeVolume = 0m;
	}

	private int EvaluateRsiSignal(int shift)
	{
		if (shift < 0)
		return 0;

		if (!CheckRsiLevels(shift, OversoldLevel, true))
		{
			if (CheckRsiLevels(shift, OverboughtLevel, false))
			return -1;

			return 0;
		}

		return 1;
	}

	private bool CheckRsiLevels(int shift, decimal level, bool isOversold)
	{
		foreach (var setting in _rsiSettings)
		{
			if (!setting.IsEnabled())
			continue;

			if (!_rsiStates.TryGetValue(setting.Type, out var state))
			return false;

			if (!state.TryGetValue(shift, out var value))
			return false;

			if (isOversold)
			{
				if (value >= level)
				return false;
			}
			else
			{
				if (value <= level)
				return false;
			}
		}

		return true;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTimer)
		return true;

		var reference = _useLocalTime.Value ? time.ToLocalTime() : time;
		var start = new DateTimeOffset(reference.Year, reference.Month, reference.Day, _startHour.Value, _startMinute.Value, 0, reference.Offset);
		var end = new DateTimeOffset(reference.Year, reference.Month, reference.Day, _endHour.Value, _endMinute.Value, 0, reference.Offset);

		if (end <= start)
		{
			if (reference <= end)
			{
				start = start.AddDays(-1);
			}
			else
			{
				end = end.AddDays(1);
			}
		}

		return reference >= start && reference < end;
	}

	private void InitializeRsiSettings()
	{
		_rsiSettings.Clear();
		_rsiStates.Clear();

		_rsiSettings.Add(new RsiSetting(TimeSpan.FromMinutes(1).TimeFrame(), () => _useM1.Value));
		_rsiSettings.Add(new RsiSetting(TimeSpan.FromMinutes(5).TimeFrame(), () => _useM5.Value));
		_rsiSettings.Add(new RsiSetting(TimeSpan.FromMinutes(15).TimeFrame(), () => _useM15.Value));
		_rsiSettings.Add(new RsiSetting(TimeSpan.FromMinutes(30).TimeFrame(), () => _useM30.Value));
		_rsiSettings.Add(new RsiSetting(TimeSpan.FromHours(1).TimeFrame(), () => _useH1.Value));
		_rsiSettings.Add(new RsiSetting(TimeSpan.FromHours(4).TimeFrame(), () => _useH4.Value));
		_rsiSettings.Add(new RsiSetting(TimeSpan.FromDays(1).TimeFrame(), () => _useD1.Value));
		_rsiSettings.Add(new RsiSetting(TimeSpan.FromDays(7).TimeFrame(), () => _useW1.Value));
		_rsiSettings.Add(new RsiSetting(TimeSpan.FromDays(30).TimeFrame(), () => _useMn1.Value));
	}

	private RsiState GetOrCreateRsiState(DataType type)
	{
		if (!_rsiStates.TryGetValue(type, out var state))
		{
			state = new RsiState(new RelativeStrengthIndex
			{
				Length = RsiPeriod
			});
			_rsiStates[type] = state;
		}

		state.Indicator.Length = RsiPeriod;
		return state;
	}

	private sealed class RsiState
	{
		private readonly RelativeStrengthIndex _indicator;
		private readonly decimal?[] _values = new decimal?[8];

		public RsiState(RelativeStrengthIndex indicator)
		{
			_indicator = indicator;
		}

		public RelativeStrengthIndex Indicator => _indicator;

		public void Update(decimal value)
		{
			for (var i = _values.Length - 1; i > 0; i--)
			{
				_values[i] = _values[i - 1];
			}

			_values[0] = value;
		}

		public bool TryGetValue(int shift, out decimal value)
		{
			if (shift < 0 || shift >= _values.Length)
			{
				value = 0m;
				return false;
			}

			var stored = _values[shift];
			if (stored is decimal result)
			{
				value = result;
				return true;
			}

			value = 0m;
			return false;
		}
	}

	private readonly struct RsiSetting
	{
		public RsiSetting(DataType type, Func<bool> isEnabled)
		{
			Type = type;
			IsEnabled = isEnabled;
		}

		public DataType Type { get; }

		public Func<bool> IsEnabled { get; }
	}

	private readonly record struct TradeStep(bool IsBuy, decimal Price, decimal Volume);
}

/// <summary>
/// Available operating modes for the zone recovery hedge strategy.
/// </summary>
public enum ZoneRecoveryMode
{
	/// <summary>
	/// Manual entries only. Use helper methods to start cycles.
	/// </summary>
	Manual,

	/// <summary>
	/// Automatically detect entries through multi-timeframe RSI filters.
	/// </summary>
	RsiMultiTimeframe
}

