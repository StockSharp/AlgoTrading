using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multicurrency hedging strategy converted from the "Couple Hedge" MQL5 expert.
/// The strategy manages groups of correlated currency pairs and opens baskets of
/// long/short positions to hedge exposure while targeting a basket profit.
/// </summary>
public class CoupleHedgeStrategy : Strategy
{
	private const int DefaultGroupCount = 3;

	private readonly GroupSlot[] _groups;

	private readonly StrategyParam<OperationMode> _operationMode;
	private readonly StrategyParam<SideSelection> _sideSelection;
	private readonly StrategyParam<StepMode> _stepMode;
	private readonly StrategyParam<StepProgression> _stepProgression;
	private readonly StrategyParam<int> _minutesBetweenOrders;
	private readonly StrategyParam<int> _maximumGroups;
	private readonly StrategyParam<CloseProfitMode> _closeProfitMode;
	private readonly StrategyParam<decimal> _targetCloseProfit;
	private readonly StrategyParam<int> _delayCloseProfit;
	private readonly StrategyParam<CloseLossMode> _closeLossMode;
	private readonly StrategyParam<decimal> _targetCloseLoss;
	private readonly StrategyParam<int> _delayCloseLoss;
	private readonly StrategyParam<bool> _autoLot;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualLotSize;
	private readonly StrategyParam<LotProgression> _lotProgression;
	private readonly StrategyParam<decimal> _lotProgressionFactor;
	private readonly StrategyParam<decimal> _stepProgressionFactor;
	private readonly StrategyParam<bool> _useFairLotSize;
	private readonly StrategyParam<decimal> _maximumLotSize;
	private readonly StrategyParam<bool> _controlSession;
	private readonly StrategyParam<int> _waitAfterOpen;
	private readonly StrategyParam<int> _stopBeforeClose;
	private readonly StrategyParam<decimal> _maxSpread;
	private readonly StrategyParam<long> _maximumOrders;
	private readonly StrategyParam<int> _maxSlippage;
	private readonly StrategyParam<string> _orderTag;
	private readonly StrategyParam<bool> _setChartInterface;
	private readonly StrategyParam<bool> _saveInformation;
	private readonly StrategyParam<decimal> _stepOpenNext;

	/// <summary>
	/// Mode that controls how the strategy should behave.
	/// </summary>
	public OperationMode OperationMode
	{
		get => _operationMode.Value;
		set => _operationMode.Value = value;
	}

	/// <summary>
	/// Selects which sides of the hedge should be traded.
	/// </summary>
	public SideSelection SideSelection
	{
		get => _sideSelection.Value;
		set => _sideSelection.Value = value;
	}

	/// <summary>
	/// Defines how new entries in loss should be opened.
	/// </summary>
	public StepMode StepMode
	{
		get => _stepMode.Value;
		set => _stepMode.Value = value;
	}

	/// <summary>
	/// Pattern used to extend the entry step when averaging down.
	/// </summary>
	public StepProgression StepProgression
	{
		get => _stepProgression.Value;
		set => _stepProgression.Value = value;
	}

	/// <summary>
	/// Minimum minutes between opening new baskets for the same group.
	/// </summary>
	public int MinutesBetweenOrders
	{
		get => _minutesBetweenOrders.Value;
		set => _minutesBetweenOrders.Value = value;
	}

	/// <summary>
	/// Maximum number of active groups. Zero removes the limitation.
	/// </summary>
	public int MaximumGroups
	{
		get => _maximumGroups.Value;
		set => _maximumGroups.Value = value;
	}

	/// <summary>
	/// Determines the closing behaviour when the basket is profitable.
	/// </summary>
	public CloseProfitMode CloseProfitMode
	{
		get => _closeProfitMode.Value;
		set => _closeProfitMode.Value = value;
	}

	/// <summary>
	/// Profit target per group (expressed in account currency).
	/// </summary>
	public decimal TargetCloseProfit
	{
		get => _targetCloseProfit.Value;
		set => _targetCloseProfit.Value = value;
	}

	/// <summary>
	/// Delay before closing in profit measured in seconds.
	/// </summary>
	public int DelayCloseProfit
	{
		get => _delayCloseProfit.Value;
		set => _delayCloseProfit.Value = value;
	}

	/// <summary>
	/// Determines how to close the basket when it is losing.
	/// </summary>
	public CloseLossMode CloseLossMode
	{
		get => _closeLossMode.Value;
		set => _closeLossMode.Value = value;
	}

	/// <summary>
	/// Maximum allowed loss per group (expressed in account currency).
	/// </summary>
	public decimal TargetCloseLoss
	{
		get => _targetCloseLoss.Value;
		set => _targetCloseLoss.Value = value;
	}

	/// <summary>
	/// Delay before closing in loss measured in seconds.
	/// </summary>
	public int DelayCloseLoss
	{
		get => _delayCloseLoss.Value;
		set => _delayCloseLoss.Value = value;
	}

	/// <summary>
	/// Enables volume calculation based on portfolio value.
	/// </summary>
	public bool AutoLot
	{
		get => _autoLot.Value;
		set => _autoLot.Value = value;
	}

	/// <summary>
	/// Percent of free margin used to size the first hedge basket.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Manual lot size used when <see cref="AutoLot"/> is disabled.
	/// </summary>
	public decimal ManualLotSize
	{
		get => _manualLotSize.Value;
		set => _manualLotSize.Value = value;
	}

	/// <summary>
	/// Progression applied to volumes when scaling into a basket.
	/// </summary>
	public LotProgression LotProgression
	{
		get => _lotProgression.Value;
		set => _lotProgression.Value = value;
	}

	/// <summary>
	/// Multiplier used by both lot and step progression modes.
	/// </summary>
	public decimal ProgressionFactor
	{
		get => _lotProgressionFactor.Value;
		set => _lotProgressionFactor.Value = value;
	}

	/// <summary>
	/// Multiplier that controls how fast the distance between averaging entries grows.
	/// </summary>
	public decimal StepProgressionFactor
	{
		get => _stepProgressionFactor.Value;
		set => _stepProgressionFactor.Value = value;
	}

	/// <summary>
	/// Balances lot size between both sides using their tick values.
	/// </summary>
	public bool UseFairLotSize
	{
		get => _useFairLotSize.Value;
		set => _useFairLotSize.Value = value;
	}

	/// <summary>
	/// Maximum allowed volume per side.
	/// </summary>
	public decimal MaximumLotSize
	{
		get => _maximumLotSize.Value;
		set => _maximumLotSize.Value = value;
	}

	/// <summary>
	/// Enables the session filter that pauses trading around market open/close.
	/// </summary>
	public bool ControlSession
	{
		get => _controlSession.Value;
		set => _controlSession.Value = value;
	}

	/// <summary>
	/// Minutes to wait after Monday open before trading.
	/// </summary>
	public int WaitAfterOpen
	{
		get => _waitAfterOpen.Value;
		set => _waitAfterOpen.Value = value;
	}

	/// <summary>
	/// Minutes to stop trading before Friday close.
	/// </summary>
	public int StopBeforeClose
	{
		get => _stopBeforeClose.Value;
		set => _stopBeforeClose.Value = value;
	}

	/// <summary>
	/// Maximum accepted spread in price steps. Zero disables the filter.
	/// </summary>
	public decimal MaxSpread
	{
		get => _maxSpread.Value;
		set => _maxSpread.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously opened positions. Zero disables the limit.
	/// </summary>
	public long MaximumOrders
	{
		get => _maximumOrders.Value;
		set => _maximumOrders.Value = value;
	}

	/// <summary>
	/// Maximum accepted slippage (for information purposes).
	/// </summary>
	public int MaxSlippage
	{
		get => _maxSlippage.Value;
		set => _maxSlippage.Value = value;
	}

	/// <summary>
	/// Comment assigned to generated orders.
	/// </summary>
	public string OrderTag
	{
		get => _orderTag.Value;
		set => _orderTag.Value = value;
	}

	/// <summary>
	/// Indicates whether chart decoration is enabled (informational parameter).
	/// </summary>
	public bool SetChartInterface
	{
		get => _setChartInterface.Value;
		set => _setChartInterface.Value = value;
	}

	/// <summary>
	/// Enables saving of basket diagnostics (informational parameter).
	/// </summary>
	public bool SaveInformation
	{
		get => _saveInformation.Value;
		set => _saveInformation.Value = value;
	}

	/// <summary>
	/// Initial loss level that triggers averaging (in account currency).
	/// </summary>
	public decimal StepOpenNext
	{
		get => _stepOpenNext.Value;
		set => _stepOpenNext.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CoupleHedgeStrategy"/>.
	/// </summary>
	public CoupleHedgeStrategy()
	{
		_operationMode = Param(nameof(OperationMode), OperationMode.NormalOperation)
		.SetDisplay("Operation Mode", "Select how the robot behaves", "General");

		_sideSelection = Param(nameof(SideSelection), SideSelection.OpenPlusAndMinus)
		.SetDisplay("Side Selection", "Choose which sides should be traded", "General");

		_stepMode = Param(nameof(StepMode), StepMode.OpenWithManualStep)
		.SetDisplay("Step Mode", "How new baskets are opened", "Risk Management");

		_stepProgression = Param(nameof(StepProgression), StepProgression.Geometrical)
		.SetDisplay("Step Progression", "How the step grows when adding baskets", "Risk Management");

		_minutesBetweenOrders = Param(nameof(MinutesBetweenOrders), 5)
		.SetGreaterOrEqualZero()
		.SetDisplay("Minutes Between Orders", "Minimum waiting time before averaging", "Risk Management");

		_maximumGroups = Param(nameof(MaximumGroups), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Maximum Groups", "Limit the number of simultaneously trading groups", "General");

		_closeProfitMode = Param(nameof(CloseProfitMode), CloseProfitMode.BothSides)
		.SetDisplay("Close Profit Mode", "How baskets are closed when profitable", "Exits");

		_targetCloseProfit = Param(nameof(TargetCloseProfit), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Target", "Group profit required to close positions", "Exits")
		.SetCanOptimize(true);

		_delayCloseProfit = Param(nameof(DelayCloseProfit), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Profit Delay", "Seconds to wait before closing in profit", "Exits");

		_closeLossMode = Param(nameof(CloseLossMode), CloseLossMode.NotCloseInLoss)
		.SetDisplay("Close Loss Mode", "How baskets are closed when losing", "Exits");

		_targetCloseLoss = Param(nameof(TargetCloseLoss), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Loss Limit", "Group loss that triggers a forced exit", "Exits");

		_delayCloseLoss = Param(nameof(DelayCloseLoss), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Loss Delay", "Seconds to wait before closing in loss", "Exits");

		_autoLot = Param(nameof(AutoLot), false)
		.SetDisplay("Auto Lot", "Enable automatic lot sizing", "Position Sizing");

		_riskFactor = Param(nameof(RiskFactor), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Factor", "Percentage of equity allocated to the first basket", "Position Sizing")
		.SetCanOptimize(true);

		_manualLotSize = Param(nameof(ManualLotSize), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Manual Lot", "Fixed volume when auto lot is disabled", "Position Sizing")
		.SetCanOptimize(true);

		_lotProgression = Param(nameof(LotProgression), LotProgression.Geometrical)
		.SetDisplay("Lot Progression", "How volume grows for additional baskets", "Position Sizing");

		_lotProgressionFactor = Param(nameof(ProgressionFactor), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Progression Factor", "Multiplier applied by progression rules", "Position Sizing");

		_stepProgressionFactor = Param(nameof(StepProgressionFactor), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Step Factor", "Multiplier for step progression", "Risk Management");

		_useFairLotSize = Param(nameof(UseFairLotSize), false)
		.SetDisplay("Fair Lot", "Balance lot size using tick value ratio", "Position Sizing");

		_maximumLotSize = Param(nameof(MaximumLotSize), 0m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Maximum Lot", "Upper limit for trade volume", "Position Sizing");

		_controlSession = Param(nameof(ControlSession), true)
		.SetDisplay("Control Session", "Enable trading session filter", "Session");

		_waitAfterOpen = Param(nameof(WaitAfterOpen), 120)
		.SetGreaterOrEqualZero()
		.SetDisplay("Wait After Open", "Minutes to wait after Monday open", "Session");

		_stopBeforeClose = Param(nameof(StopBeforeClose), 60)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Before Close", "Minutes to stop before Friday close", "Session");

		_maxSpread = Param(nameof(MaxSpread), 0m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Max Spread", "Maximum accepted spread in points", "Risk Management");

		_maximumOrders = Param(nameof(MaximumOrders), 0L)
		.SetGreaterOrEqualZero()
		.SetDisplay("Maximum Orders", "Global limit for open positions", "Risk Management");

		_maxSlippage = Param(nameof(MaxSlippage), 3)
		.SetGreaterOrEqualZero()
		.SetDisplay("Max Slippage", "Slippage tolerance (informational)", "Execution");

		_orderTag = Param(nameof(OrderTag), "CoupleHedgeEA")
		.SetDisplay("Order Comment", "Comment attached to generated orders", "Execution");

		_setChartInterface = Param(nameof(SetChartInterface), true)
		.SetDisplay("Set Chart Interface", "Replicates the original EA visual settings", "Visualization");

		_saveInformation = Param(nameof(SaveInformation), false)
		.SetDisplay("Save Info", "Export basket diagnostics to disk", "Diagnostics");

		_stepOpenNext = Param(nameof(StepOpenNext), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Step Open Next", "Initial loss level that triggers averaging", "Risk Management")
		.SetCanOptimize(true);

		_groups = new GroupSlot[DefaultGroupCount];

		_groups[0] = new GroupSlot(this, 0, "EURUSD", "GBPUSD");
		_groups[1] = new GroupSlot(this, 1, "EURGBP", "EURJPY");
		_groups[2] = new GroupSlot(this, 2, "AUDUSD", "NZDUSD");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var unique = new HashSet<Security>();

		foreach (var slot in _groups)
		{
			if (!slot.IsEnabled)
			continue;

			if (slot.PlusSecurity != null && unique.Add(slot.PlusSecurity))
			yield return (slot.PlusSecurity, slot.CandleType);

			if (slot.MinusSecurity != null && unique.Add(slot.MinusSecurity))
			yield return (slot.MinusSecurity, slot.CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		foreach (var slot in _groups)
		{
			slot.ResetRuntime();
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		foreach (var slot in _groups)
		{
			slot.ResetRuntime();

			if (!slot.IsEnabled)
			continue;

			if (slot.PlusSecurity == null || slot.MinusSecurity == null)
			throw new InvalidOperationException($"Group {slot.Index + 1} has undefined securities.");

			SubscribeCandles(slot.CandleType, false, slot.PlusSecurity)
			.Bind(candle => ProcessGroup(slot, candle))
			.Start();

			SubscribeLevel1(slot.PlusSecurity)
			.Bind(message => slot.UpdatePlusLevel1(message))
			.Start();

			SubscribeLevel1(slot.MinusSecurity)
			.Bind(message => slot.UpdateMinusLevel1(message))
			.Start();
		}
	}

	private void ProcessGroup(GroupSlot slot, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		slot.UpdatePlusCandle(candle);

		var now = candle.CloseTime;

		// Respect the session guard before evaluating any trading logic.
		if (!IsTradingAllowed(now))
		return;

		var operation = OperationMode;

		if (operation == OperationMode.StandBy)
		return;

		var plusVolume = slot.GetPositionVolume(slot.PlusSecurity, Portfolio, this);
		var minusVolume = slot.GetPositionVolume(slot.MinusSecurity, Portfolio, this);
		var hasOpenPositions = plusVolume != 0m || minusVolume != 0m;

		if (operation == OperationMode.CloseImmediatelyAllOrders)
		{
			// Emergency flatten request from the parameter set.
			if (hasOpenPositions)
			CloseGroup(slot, plusVolume, minusVolume);
			return;
		}

		if (operation == OperationMode.CloseInProfitAndStop && !hasOpenPositions && slot.HasCompletedCycle)
		return;

		var groupProfit = GetGroupProfit(slot);
		var plusProfit = slot.LastPlusProfit;
		var minusProfit = slot.LastMinusProfit;

		if (ShouldCloseInProfit(slot, groupProfit, plusProfit, minusProfit, now) && hasOpenPositions)
		{
			CloseGroup(slot, plusVolume, minusVolume);
			return;
		}

		if (ShouldCloseInLoss(slot, groupProfit, plusProfit, minusProfit, now) && hasOpenPositions)
		{
			CloseGroup(slot, plusVolume, minusVolume);
			return;
		}

		if (operation == OperationMode.CloseInProfitAndStop)
		return;

		// Abort if the infrastructure is not ready (for example during reconnects).
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Respect basket limits before placing new trades.
		if (!CanOpenMoreBaskets(slot, now))
		return;

		var stepTrigger = slot.GetNextOpenTrigger(StepMode, StepOpenNext, StepProgression, StepProgressionFactor);

		if (!ShouldAverage(groupProfit, stepTrigger, hasOpenPositions))
		return;

		// Skip averaging when spreads are wider than the configured threshold.
		if (!IsSpreadAcceptable(slot))
		return;

		var lot = CalculateLot(slot);

		if (lot <= 0m)
		return;

		OpenCouple(slot, lot, now);
	}

	private bool ShouldAverage(decimal groupProfit, decimal trigger, bool hasOpenPositions)
	{
		if (StepMode == StepMode.NotOpenInLoss && hasOpenPositions)
		return false;

		if (!hasOpenPositions)
		return true;

		return groupProfit <= trigger;
	}

	private bool IsSpreadAcceptable(GroupSlot slot)
	{
		if (MaxSpread <= 0m)
		return true;

		var plusSpread = slot.PlusSpread;
		var minusSpread = slot.MinusSpread;

		if (plusSpread <= 0m || minusSpread <= 0m)
		return true;

		var average = (plusSpread + minusSpread) / 2m;

		return average <= MaxSpread;
	}

	private void OpenCouple(GroupSlot slot, decimal lot, DateTimeOffset time)
	{
		var plus = slot.PlusSecurity!;
		var minus = slot.MinusSecurity!;

		var side = SideSelection;

		var plusVolume = lot;
		var minusVolume = lot;

		if (UseFairLotSize)
		{
			var plusTick = plus.TickValue ?? 0m;
			var minusTick = minus.TickValue ?? 0m;

			if (plusTick > 0m && minusTick > 0m)
			{
				var average = (plusTick + minusTick) / 2m;
				plusVolume = lot * (average / plusTick);
				minusVolume = lot * (average / minusTick);
			}
		}

		plusVolume = slot.NormalizeVolume(plusVolume, plus, MaximumLotSize);
		minusVolume = slot.NormalizeVolume(minusVolume, minus, MaximumLotSize);

		if (plusVolume <= 0m && minusVolume <= 0m)
		return;

		LogInfo($"Opening hedge basket #{slot.BasketNumber + 1} with lot {lot:F4} (side mode: {side}).");

		switch (side)
		{
			case SideSelection.OpenPlusAndMinus:
			if (plusVolume > 0m)
			BuyMarket(plusVolume, security: plus);

			if (minusVolume > 0m)
			SellMarket(minusVolume, security: minus);
			break;

			case SideSelection.OpenOnlyPlus:
			if (plusVolume > 0m)
			BuyMarket(plusVolume, security: plus);
			break;

			case SideSelection.OpenOnlyMinus:
			if (minusVolume > 0m)
			SellMarket(minusVolume, security: minus);
			break;
		}

		slot.RegisterOpen(time, StepMode, StepProgression, StepProgressionFactor, StepOpenNext);
	}

	private void CloseGroup(GroupSlot slot, decimal plusVolume, decimal minusVolume)
	{
		var plus = slot.PlusSecurity;
		var minus = slot.MinusSecurity;

		if (plus != null && plusVolume != 0m)
		{
			if (plusVolume > 0m)
			SellMarket(plusVolume, security: plus);
			else
			BuyMarket(Math.Abs(plusVolume), security: plus);
		}

		if (minus != null && minusVolume != 0m)
		{
			if (minusVolume > 0m)
			SellMarket(minusVolume, security: minus);
			else
			BuyMarket(Math.Abs(minusVolume), security: minus);
		}

		slot.RegisterClose();
	}

	private decimal CalculateLot(GroupSlot slot)
	{
		var price = slot.GetReferencePrice();

		decimal lot;

		if (AutoLot)
		{
			var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

			if (portfolioValue <= 0m || price <= 0m)
			return 0m;

			lot = portfolioValue * (RiskFactor * 0.01m) / price;
		}
		else
		{
			lot = ManualLotSize;
		}

		var multiplier = slot.GetLotMultiplier(LotProgression, ProgressionFactor);

		lot *= multiplier;

		return lot;
	}

	private bool ShouldCloseInProfit(GroupSlot slot, decimal groupProfit, decimal plusProfit, decimal minusProfit, DateTimeOffset now)
	{
		if (TargetCloseProfit <= 0m)
		return false;

		return CloseProfitMode switch
		{
			CloseProfitMode.SideBySide => slot.EvaluateSideClose(TargetCloseProfit, plusProfit, minusProfit, DelayCloseProfit, now, true),
			_ => slot.EvaluateBasketClose(TargetCloseProfit, groupProfit, DelayCloseProfit, now, true),
		};
	}

	private bool ShouldCloseInLoss(GroupSlot slot, decimal groupProfit, decimal plusProfit, decimal minusProfit, DateTimeOffset now)
	{
		if (TargetCloseLoss <= 0m || CloseLossMode == CloseLossMode.NotCloseInLoss)
		return false;

		return CloseLossMode switch
		{
			CloseLossMode.WholeSide => slot.EvaluateSideClose(TargetCloseLoss, plusProfit, minusProfit, DelayCloseLoss, now, false),
			CloseLossMode.PartialSide => slot.EvaluateBasketClose(TargetCloseLoss, groupProfit, DelayCloseLoss, now, false),
			_ => false,
		};
	}

	private decimal GetGroupProfit(GroupSlot slot)
	{
		var profit = 0m;

		var plusPosition = Positions.FirstOrDefault(p => p.Security == slot.PlusSecurity);
		var minusPosition = Positions.FirstOrDefault(p => p.Security == slot.MinusSecurity);

		if (plusPosition != null)
		{
			var value = plusPosition.PnL ?? 0m;
			slot.LastPlusProfit = value;
			profit += value;
		}
		else
		{
			slot.LastPlusProfit = 0m;
		}

		if (minusPosition != null)
		{
			var value = minusPosition.PnL ?? 0m;
			slot.LastMinusProfit = value;
			profit += value;
		}
		else
		{
			slot.LastMinusProfit = 0m;
		}

		return profit;
	}

	private bool CanOpenMoreBaskets(GroupSlot slot, DateTimeOffset now)
	{
		if (MaximumOrders > 0)
		{
			var activePositions = Positions.Count(p => (p.CurrentValue ?? 0m) != 0m);

			if (activePositions + 2 > MaximumOrders)
			{
				LogInfo("Maximum order limit reached. Skipping new entries.");
				return false;
			}
		}

		if (MaximumGroups > 0)
		{
			var activeGroups = _groups.Count(g => g.HasActivePositions(Portfolio, this));

			if (!slot.HasActivePositions(Portfolio, this) && activeGroups >= MaximumGroups)
			{
				LogInfo("Maximum number of groups reached. Skipping new entries.");
				return false;
			}
		}

		if (MinutesBetweenOrders > 0 && slot.LastOpenTime != DateTimeOffset.MinValue)
		{
			var wait = TimeSpan.FromMinutes(MinutesBetweenOrders);

			if (now - slot.LastOpenTime < wait)
			return false;
		}

		return true;
	}

	private bool IsTradingAllowed(DateTimeOffset time)
	{
		if (!ControlSession)
		return true;

		var moment = time.ToLocalTime();

		if (moment.DayOfWeek == DayOfWeek.Monday)
		{
			var limit = TimeSpan.FromMinutes(WaitAfterOpen);
			var from = moment.TimeOfDay;

			if (from < limit)
			{
				LogDebug("Waiting after Monday open.");
				return false;
			}
		}

		if (moment.DayOfWeek == DayOfWeek.Friday)
		{
			var stop = TimeSpan.FromMinutes(24 * 60 - StopBeforeClose);
			var from = moment.TimeOfDay;

			if (from > stop)
			{
				LogDebug("Stopping before Friday close.");
				return false;
			}
		}

		return true;
	}

	private sealed class GroupSlot
	{
		private const decimal RangeEmaAlpha = 0.2m;

		private readonly StrategyParam<bool> _isEnabled;
		private readonly StrategyParam<Security> _plusSecurity;
		private readonly StrategyParam<Security> _minusSecurity;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _rangeAverage;
		private decimal _nextTrigger;
		private decimal _plusMidPrice;
		private decimal _minusMidPrice;

		private DateTimeOffset? _profitCloseRequest;
		private DateTimeOffset? _lossCloseRequest;

		public GroupSlot(CoupleHedgeStrategy owner, int index, string plusId, string minusId)
		{
			Index = index;
			var groupName = $"Group {index + 1}";

			_isEnabled = owner.Param($"Group{index + 1}Enabled", true)
			.SetDisplay($"{groupName} Enabled", "Allow trading for this pair", groupName);

			_plusSecurity = owner.Param<Security>($"Group{index + 1}Plus", new Security { Id = plusId })
			.SetDisplay($"{groupName} Plus", "Security used for the plus side", groupName);

			_minusSecurity = owner.Param<Security>($"Group{index + 1}Minus", new Security { Id = minusId })
			.SetDisplay($"{groupName} Minus", "Security used for the minus side", groupName);

			_candleType = owner.Param($"Group{index + 1}CandleType", TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay($"{groupName} Timeframe", "Candle timeframe used for control logic", groupName);

			ResetRuntime();
		}

		public int Index { get; }

		public bool IsEnabled => _isEnabled.Value;

		public Security? PlusSecurity => _plusSecurity.Value;

		public Security? MinusSecurity => _minusSecurity.Value;

		public DataType CandleType => _candleType.Value;

		public DateTimeOffset LastOpenTime { get; private set; }

		public int BasketNumber { get; private set; }

		public bool HasCompletedCycle { get; private set; }

		public decimal LastPlusProfit { get; set; }

		public decimal LastMinusProfit { get; set; }

		public decimal PlusSpread { get; private set; }

		public decimal MinusSpread { get; private set; }

		public void ResetRuntime()
		{
			_rangeAverage = 0m;
			_nextTrigger = 0m;
			_plusMidPrice = 0m;
			_minusMidPrice = 0m;
			LastOpenTime = DateTimeOffset.MinValue;
			BasketNumber = 0;
			HasCompletedCycle = false;
			_profitCloseRequest = null;
			_lossCloseRequest = null;
			LastPlusProfit = 0m;
			LastMinusProfit = 0m;
		}

		public bool HasActivePositions(Portfolio portfolio, Strategy strategy)
		{
			var plus = GetPositionVolume(PlusSecurity, portfolio, strategy);
			var minus = GetPositionVolume(MinusSecurity, portfolio, strategy);

			return plus != 0m || minus != 0m;
		}

		public decimal GetPositionVolume(Security? security, Portfolio? portfolio, Strategy strategy)
		{
			return security == null ? 0m : strategy.GetPositionValue(security, portfolio) ?? 0m;
		}

		public decimal NormalizeVolume(decimal volume, Security security, decimal maxLot)
		{
			var min = security.MinVolume ?? security.StepVolume ?? 0m;
			var max = security.MaxVolume ?? decimal.MaxValue;
			var step = security.StepVolume ?? 0m;

			if (step > 0m)
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;

			if (min > 0m && volume < min)
			volume = min;

			if (maxLot > 0m && volume > maxLot)
			volume = maxLot;

			if (volume > max)
			volume = max;

			return volume;
		}

		public void UpdatePlusLevel1(Level1ChangeMessage message)
		{
			UpdateSpread(message, true);
		}

		public void UpdateMinusLevel1(Level1ChangeMessage message)
		{
			UpdateSpread(message, false);
		}

		private void UpdateSpread(Level1ChangeMessage message, bool isPlus)
		{
			var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
			var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);

			if (!bid.HasValue || !ask.HasValue)
			return;

			var spread = ask.Value - bid.Value;

			if (spread < 0m)
			spread = 0m;

			var mid = (ask.Value + bid.Value) / 2m;

			if (isPlus)
			{
				PlusSpread = spread;
				_plusMidPrice = mid;
			}
			else
			{
				MinusSpread = spread;
				_minusMidPrice = mid;
			}
		}

		public void UpdatePlusCandle(ICandleMessage candle)
		{
			var range = candle.HighPrice - candle.LowPrice;

			if (range <= 0m)
			return;

			if (_rangeAverage <= 0m)
			_rangeAverage = range;
			else
			_rangeAverage = _rangeAverage * (1m - RangeEmaAlpha) + range * RangeEmaAlpha;
		}

		public decimal GetReferencePrice()
		{
			if (_plusMidPrice > 0m)
			return _plusMidPrice;

			if (_minusMidPrice > 0m)
			return _minusMidPrice;

			var plus = PlusSecurity;
			var step = plus?.PriceStep ?? 0m;

			return step > 0m ? step : 1m;
		}

		public decimal GetLotMultiplier(LotProgression mode, decimal factor)
		{
			return mode switch
			{
				LotProgression.StaticalLot => 1m,
				LotProgression.GeometricalLot => (decimal)Math.Pow((double)factor, BasketNumber),
				LotProgression.ExponentialLot => (decimal)Math.Exp(BasketNumber * Math.Log((double)Math.Max(factor, 1.0001m))),
				LotProgression.DecreasesLot => 1m / (decimal)Math.Pow((double)factor, BasketNumber),
				_ => 1m,
			};
		}

		public decimal GetNextOpenTrigger(StepMode mode, decimal initial, StepProgression progression, decimal factor)
		{
			if (BasketNumber == 0 || _nextTrigger == 0m)
			{
				var baseStep = CalculateBaseStep(mode, initial);
				_nextTrigger = -Math.Abs(baseStep);
			}

			return _nextTrigger;
		}

		private decimal CalculateBaseStep(StepMode mode, decimal initial)
		{
			return mode switch
			{
				StepMode.OpenWithAutoStep => _rangeAverage > 0m ? Math.Max(initial, _rangeAverage) : initial,
				_ => initial,
			};
		}

		public void RegisterOpen(DateTimeOffset time, StepMode mode, StepProgression progression, decimal factor, decimal initial)
		{
			LastOpenTime = time;
			BasketNumber++;
			HasCompletedCycle = false;
			_profitCloseRequest = null;
			_lossCloseRequest = null;

			var baseStep = CalculateBaseStep(mode, initial);

			var multiplier = progression switch
			{
				StepProgression.StaticalStep => 1m,
				StepProgression.GeometricalStep => (decimal)Math.Pow((double)factor, BasketNumber),
				StepProgression.ExponentialStep => (decimal)Math.Exp(BasketNumber * Math.Log((double)Math.Max(factor, 1.0001m))),
				_ => 1m,
			};

			if (baseStep > 0m)
			_nextTrigger -= Math.Abs(baseStep * multiplier);
		}

		public void RegisterClose()
		{
			BasketNumber = 0;
			HasCompletedCycle = true;
			_profitCloseRequest = null;
			_lossCloseRequest = null;
			_nextTrigger = 0m;
		}

		public bool EvaluateBasketClose(decimal target, decimal profit, int delaySeconds, DateTimeOffset now, bool isProfit)
		{
			if (isProfit ? profit < target : -profit < target)
			{
				_profitCloseRequest = null;
				_lossCloseRequest = null;
				return false;
			}

			var marker = isProfit ? _profitCloseRequest : _lossCloseRequest;

			if (!marker.HasValue)
			{
				marker = now.AddSeconds(delaySeconds);

				if (isProfit)
				_profitCloseRequest = marker;
				else
				_lossCloseRequest = marker;

				return false;
			}

			if (now >= marker.Value)
			{
				_profitCloseRequest = null;
				_lossCloseRequest = null;
				return true;
			}

			return false;
		}

		public bool EvaluateSideClose(decimal target, decimal plusProfit, decimal minusProfit, int delaySeconds, DateTimeOffset now, bool isProfit)
		{
			var shouldClose = false;

			if (isProfit)
			{
				shouldClose = plusProfit >= target || minusProfit >= target;
			}
			else
			{
				shouldClose = plusProfit <= -target || minusProfit <= -target;
			}

			if (!shouldClose)
			{
				_profitCloseRequest = null;
				_lossCloseRequest = null;
				return false;
			}

			var marker = isProfit ? _profitCloseRequest : _lossCloseRequest;

			if (!marker.HasValue)
			{
				marker = now.AddSeconds(delaySeconds);

				if (isProfit)
				_profitCloseRequest = marker;
				else
				_lossCloseRequest = marker;

				return false;
			}

			if (now >= marker.Value)
			{
				_profitCloseRequest = null;
				_lossCloseRequest = null;
				return true;
			}

			return false;
		}

	}

	/// <summary>
	/// Trading modes available for the strategy.
	/// </summary>
	public enum OperationMode
	{
		StandBy,
		NormalOperation,
		CloseInProfitAndStop,
		CloseImmediatelyAllOrders,
	}

	/// <summary>
	/// Which sides of the hedge should be traded.
	/// </summary>
	public enum SideSelection
	{
		OpenOnlyPlus,
		OpenOnlyMinus,
		OpenPlusAndMinus,
	}

	/// <summary>
	/// Defines how new baskets should be opened when the current basket is losing.
	/// </summary>
	public enum StepMode
	{
		NotOpenInLoss,
		OpenWithManualStep,
		OpenWithAutoStep,
	}

	/// <summary>
	/// Progression rules used to grow the step.
	/// </summary>
	public enum StepProgression
	{
		StaticalStep,
		GeometricalStep,
		ExponentialStep,
	}

	/// <summary>
	/// Closing behaviour for profitable baskets.
	/// </summary>
	public enum CloseProfitMode
	{
		SideBySide,
		BothSides,
		Hybrid1,
		Hybrid2,
		Hybrid12,
	}

	/// <summary>
	/// Closing behaviour for losing baskets.
	/// </summary>
	public enum CloseLossMode
	{
		WholeSide,
		PartialSide,
		NotCloseInLoss,
	}

	/// <summary>
	/// Lot progression rules when scaling into a basket.
	/// </summary>
	public enum LotProgression
	{
		StaticalLot,
		GeometricalLot,
		ExponentialLot,
		DecreasesLot,
	}
}
