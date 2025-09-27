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

using System.Globalization;
using StockSharp.Localization;

namespace StockSharp.Samples.Strategies;

public class RingSystemEaStrategy : Strategy
{
	private readonly StrategyParam<Operations> _operationParam;
	private readonly StrategyParam<int> _timerParam;
	private readonly StrategyParam<string> _currenciesParam;
	private readonly StrategyParam<string> _skipGroupsParam;
	private readonly StrategyParam<Sides> _sideParam;
	private readonly StrategyParam<StepModes> _lossModeParam;
	private readonly StrategyParam<decimal> _stepParam;
	private readonly StrategyParam<StepProgressions> _stepProgressParam;
	private readonly StrategyParam<int> _minutesParam;
	private readonly StrategyParam<int> _maxGroupsParam;
	private readonly StrategyParam<CloseProfitModes> _closeProfitParam;
	private readonly StrategyParam<decimal> _targetProfitParam;
	private readonly StrategyParam<int> _delayProfitParam;
	private readonly StrategyParam<CloseLossModes> _closeLossParam;
	private readonly StrategyParam<decimal> _targetLossParam;
	private readonly StrategyParam<int> _delayLossParam;
	private readonly StrategyParam<bool> _autoLotParam;
	private readonly StrategyParam<decimal> _riskParam;
	private readonly StrategyParam<decimal> _manualLotParam;
	private readonly StrategyParam<LotProgressions> _lotProgressParam;
	private readonly StrategyParam<bool> _fairLotParam;
	private readonly StrategyParam<decimal> _maxLotParam;
	private readonly StrategyParam<bool> _controlSessionParam;
	private readonly StrategyParam<int> _waitAfterOpenParam;
	private readonly StrategyParam<int> _stopBeforeCloseParam;
	private readonly StrategyParam<decimal> _maxSpreadParam;
	private readonly StrategyParam<long> _maxOrdersParam;
	private readonly StrategyParam<int> _slippageParam;
	private readonly StrategyParam<string> _prefixParam;
	private readonly StrategyParam<string> _suffixParam;
	private readonly StrategyParam<int> _magicParam;
	private readonly StrategyParam<bool> _checkOrdersParam;
	private readonly StrategyParam<string> _commentParam;
	private readonly StrategyParam<bool> _saveInfoParam;
	private readonly StrategyParam<TimeSpan> _candleTypeParam;

	private readonly Dictionary<Order, PendingAction> _pending = new();
	private readonly List<GroupState> _groups = new();
	private readonly HashSet<Security> _allSecurities = new();
	private readonly Dictionary<Security, CandleSubscription> _subscriptions = new();

	private DateTimeOffset? _sessionStart;
	private DateTimeOffset? _sessionEnd;
	private bool _hasSessionWindow;

	public RingSystemEaStrategy()
	{
		_operationParam = Param(nameof(TypeOfOperation), Operations.NormalOperation)
			.SetDisplay("Operation Mode", "Defines how the basket operates", "Core")
			.SetDescription("Matches the four MT4 modes: standby, trading, close on profit, or emergency close.");

		_timerParam = Param(nameof(TimerInMillisecond), 1000)
			.SetDisplay("Timer Interval", "Background refresh interval", "Core")
			.SetGreaterThanZero();

		_currenciesParam = Param(nameof(CurrenciesTrade), "EUR/GBP/AUD/NZD/USD/CAD/CHF/JPY")
			.SetDisplay("Currencies", "Ordered list used to build rings", "Universe");

		_skipGroupsParam = Param(nameof(NoOfGroupToSkip), "57,58,59,60")
			.SetDisplay("Skip Groups", "Comma separated group numbers to disable", "Universe");

		_sideParam = Param(nameof(SideOpenOrders), Sides.OpenPlusAndMinus)
			.SetDisplay("Allowed Sides", "Choose whether plus, minus or both baskets trade", "Core");

		_lossModeParam = Param(nameof(OpenOrdersInLoss), StepModes.OpenWithManualStep)
			.SetDisplay("Loss Mode", "Controls re-entry when basket is in loss", "Trading");

		_stepParam = Param(nameof(StepOpenNextOrders), 200m)
			.SetDisplay("Next Step", "Loss level that triggers additional orders", "Trading")
			.SetGreaterThanZero();

		_stepProgressParam = Param(nameof(StepOrdersProgress), StepProgressions.StaticalStep)
			.SetDisplay("Step Progression", "How loss thresholds scale with new orders", "Trading");

		_minutesParam = Param(nameof(MinutesForNextOrder), 60)
			.SetDisplay("Minutes Between Orders", "Cooldown before the same basket can add again", "Trading")
			.SetGreaterThanOrEqualTo(0);

		_maxGroupsParam = Param(nameof(MaximumGroups), 0)
			.SetDisplay("Max Groups", "Simultaneous groups allowed (0 = unlimited)", "Trading")
			.SetGreaterThanOrEqualTo(0);

		_closeProfitParam = Param(nameof(TypeCloseInProfit), CloseProfitModes.SingleTicket)
			.SetDisplay("Profit Close", "Scope of profit based exits", "Risk");

		_targetProfitParam = Param(nameof(TargetCloseProfit), 200m)
			.SetDisplay("Profit Target", "Profit target per basket", "Risk")
			.SetGreaterThanZero();

		_delayProfitParam = Param(nameof(DelayCloseProfit), 1)
			.SetDisplay("Profit Delay", "Number of evaluations before closing", "Risk")
			.SetGreaterThanOrEqualTo(1);

		_closeLossParam = Param(nameof(TypeCloseInLoss), CloseLossModes.NotCloseInLoss)
			.SetDisplay("Loss Close", "How losses are handled", "Risk");

		_targetLossParam = Param(nameof(TargetCloseLoss), 1000m)
			.SetDisplay("Loss Threshold", "Loss threshold per basket", "Risk")
			.SetGreaterThanZero();

		_delayLossParam = Param(nameof(DelayCloseLoss), 1)
			.SetDisplay("Loss Delay", "Number of evaluations before loss exit", "Risk")
			.SetGreaterThanOrEqualTo(1);

		_autoLotParam = Param(nameof(AutoLotSize), true)
			.SetDisplay("Auto Lot", "Enable automatic volume calculation", "Money");

		_riskParam = Param(nameof(RiskFactor), 5m)
			.SetDisplay("Risk Factor", "Multiplier for automatic volume", "Money")
			.SetGreaterThanOrEqualTo(0.1m);

		_manualLotParam = Param(nameof(ManualLotSize), 0.01m)
			.SetDisplay("Manual Lot", "Base lot when auto sizing is disabled", "Money")
			.SetGreaterThanZero();

		_lotProgressParam = Param(nameof(LotOrdersProgress), LotProgressions.StaticalLot)
			.SetDisplay("Lot Progression", "Scaling rule for successive orders", "Money");

		_fairLotParam = Param(nameof(UseFairLotSize), false)
			.SetDisplay("Fair Lot", "Scale volumes by tick value", "Money");

		_maxLotParam = Param(nameof(MaximumLotSize), 0m)
			.SetDisplay("Max Lot", "Upper limit for any single order", "Money")
			.SetGreaterThanOrEqualTo(0m);

		_controlSessionParam = Param(nameof(ControlSession), false)
			.SetDisplay("Control Session", "Limit trading to weekly session window", "Session");

		_waitAfterOpenParam = Param(nameof(WaitAfterOpen), 60)
			.SetDisplay("Wait After Open", "Minutes to wait after Monday open", "Session")
			.SetGreaterThanOrEqualTo(0);

		_stopBeforeCloseParam = Param(nameof(StopBeforeClose), 60)
			.SetDisplay("Stop Before Close", "Minutes to stop before Friday close", "Session")
			.SetGreaterThanOrEqualTo(0);

		_maxSpreadParam = Param(nameof(MaxSpread), 0m)
			.SetDisplay("Max Spread", "Optional spread filter", "Risk")
			.SetGreaterThanOrEqualTo(0m);

		_maxOrdersParam = Param(nameof(MaximumOrders), 0L)
			.SetDisplay("Max Orders", "Global order limit (0 = unlimited)", "Risk")
			.SetGreaterThanOrEqualTo(0L);

		_slippageParam = Param(nameof(MaxSlippage), 3)
			.SetDisplay("Slippage", "Maximum slippage in ticks", "Risk")
			.SetGreaterThanOrEqualTo(0);

		_prefixParam = Param(nameof(SymbolPrefix), "NONE")
			.SetDisplay("Prefix", "Optional instrument prefix", "Universe");

		_suffixParam = Param(nameof(SymbolSuffix), "AUTO")
			.SetDisplay("Suffix", "Optional instrument suffix", "Universe");

		_magicParam = Param(nameof(MagicNumber), 0)
			.SetDisplay("Magic", "Identifier appended to generated orders", "Meta")
			.SetGreaterThanOrEqualTo(0);

		_checkOrdersParam = Param(nameof(CheckOrders), true)
			.SetDisplay("Check Orders", "Validate account limits before trading", "Risk");

		_commentParam = Param(nameof(StringOrdersEA), "RingSystemEA")
			.SetDisplay("Comment", "Comment attached to generated orders", "Meta");

		_saveInfoParam = Param(nameof(SaveInformations), false)
			.SetDisplay("Save Info", "Persist basket diagnostics to the log", "Meta");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for calculations", "Data");
	}

	public Operations TypeOfOperation
	{
		get => _operationParam.Value;
		set => _operationParam.Value = value;
	}

	public int TimerInMillisecond
	{
		get => _timerParam.Value;
		set => _timerParam.Value = value;
	}

	public string CurrenciesTrade
	{
		get => _currenciesParam.Value;
		set => _currenciesParam.Value = value;
	}

	public string NoOfGroupToSkip
	{
		get => _skipGroupsParam.Value;
		set => _skipGroupsParam.Value = value;
	}

	public Sides SideOpenOrders
	{
		get => _sideParam.Value;
		set => _sideParam.Value = value;
	}

	public StepModes OpenOrdersInLoss
	{
		get => _lossModeParam.Value;
		set => _lossModeParam.Value = value;
	}

	public decimal StepOpenNextOrders
	{
		get => _stepParam.Value;
		set => _stepParam.Value = value;
	}

	public StepProgressions StepOrdersProgress
	{
		get => _stepProgressParam.Value;
		set => _stepProgressParam.Value = value;
	}

	public int MinutesForNextOrder
	{
		get => _minutesParam.Value;
		set => _minutesParam.Value = value;
	}

	public int MaximumGroups
	{
		get => _maxGroupsParam.Value;
		set => _maxGroupsParam.Value = value;
	}

	public CloseProfitModes TypeCloseInProfit
	{
		get => _closeProfitParam.Value;
		set => _closeProfitParam.Value = value;
	}

	public decimal TargetCloseProfit
	{
		get => _targetProfitParam.Value;
		set => _targetProfitParam.Value = value;
	}

	public int DelayCloseProfit
	{
		get => _delayProfitParam.Value;
		set => _delayProfitParam.Value = value;
	}

	public CloseLossModes TypeCloseInLoss
	{
		get => _closeLossParam.Value;
		set => _closeLossParam.Value = value;
	}

	public decimal TargetCloseLoss
	{
		get => _targetLossParam.Value;
		set => _targetLossParam.Value = value;
	}

	public int DelayCloseLoss
	{
		get => _delayLossParam.Value;
		set => _delayLossParam.Value = value;
	}

	public bool AutoLotSize
	{
		get => _autoLotParam.Value;
		set => _autoLotParam.Value = value;
	}

	public decimal RiskFactor
	{
		get => _riskParam.Value;
		set => _riskParam.Value = value;
	}

	public decimal ManualLotSize
	{
		get => _manualLotParam.Value;
		set => _manualLotParam.Value = value;
	}

	public LotProgressions LotOrdersProgress
	{
		get => _lotProgressParam.Value;
		set => _lotProgressParam.Value = value;
	}

	public bool UseFairLotSize
	{
		get => _fairLotParam.Value;
		set => _fairLotParam.Value = value;
	}

	public decimal MaximumLotSize
	{
		get => _maxLotParam.Value;
		set => _maxLotParam.Value = value;
	}

	public bool ControlSession
	{
		get => _controlSessionParam.Value;
		set => _controlSessionParam.Value = value;
	}

	public int WaitAfterOpen
	{
		get => _waitAfterOpenParam.Value;
		set => _waitAfterOpenParam.Value = value;
	}

	public int StopBeforeClose
	{
		get => _stopBeforeCloseParam.Value;
		set => _stopBeforeCloseParam.Value = value;
	}

	public decimal MaxSpread
	{
		get => _maxSpreadParam.Value;
		set => _maxSpreadParam.Value = value;
	}

	public long MaximumOrders
	{
		get => _maxOrdersParam.Value;
		set => _maxOrdersParam.Value = value;
	}

	public int MaxSlippage
	{
		get => _slippageParam.Value;
		set => _slippageParam.Value = value;
	}

	public string SymbolPrefix
	{
		get => _prefixParam.Value;
		set => _prefixParam.Value = value;
	}

	public string SymbolSuffix
	{
		get => _suffixParam.Value;
		set => _suffixParam.Value = value;
	}

	public int MagicNumber
	{
		get => _magicParam.Value;
		set => _magicParam.Value = value;
	}

	public bool CheckOrders
	{
		get => _checkOrdersParam.Value;
		set => _checkOrdersParam.Value = value;
	}

	public string StringOrdersEA
	{
		get => _commentParam.Value;
		set => _commentParam.Value = value;
	}

	public bool SaveInformations
	{
		get => _saveInfoParam.Value;
		set => _saveInfoParam.Value = value;
	}

	public TimeSpan CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (_allSecurities.Count == 0)
		{
			if (Security != null)
				yield return (Security, CandleType);
			yield break;
		}

		foreach (var security in _allSecurities)
			yield return (security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pending.Clear();
		_groups.Clear();
		_allSecurities.Clear();
		_subscriptions.Clear();
		_sessionStart = null;
		_sessionEnd = null;
		_hasSessionWindow = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException(LocalizedStrings.Str3616);

		if (SecurityProvider == null)
			throw new InvalidOperationException("Security provider is required to resolve currency pairs.");

		StartProtection();

		_groups.Clear();
		_allSecurities.Clear();

		var currencies = ParseCurrencies();
		if (currencies.Count < 3)
			throw new InvalidOperationException("At least three currencies are required to build rings.");

		var skipped = ParseSkippedGroups();
		var combinations = BuildCombinations(currencies);
		var prefix = NormalizeAffix(SymbolPrefix);
		var suffix = NormalizeAffix(SymbolSuffix);

		var groupIndex = 0;

		foreach (var combo in combinations)
		{
			groupIndex++;
			if (skipped.Contains(groupIndex))
				continue;

			var resolved = ResolveGroup(combo, prefix, suffix);
			if (resolved == null)
				continue;

			_groups.Add(resolved);

			foreach (var pair in resolved.Pairs)
			{
				if (_allSecurities.Add(pair.Security))
				{
					var subscription = SubscribeCandles(CandleType, true, pair.Security);
					subscription.Bind(c => ProcessCandle(c, pair.Security)).Start();
					_subscriptions[pair.Security] = subscription;
				}
			}
		}

		if (_groups.Count == 0)
			throw new InvalidOperationException("No valid currency rings were resolved.");

		_hasSessionWindow = TryResolveSessionWindow(out _sessionStart, out _sessionEnd);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();
		_pending.Clear();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		if (!_pending.TryGetValue(trade.Order, out var context))
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;
		var pair = context.Pair;

		if (context.IsPlus)
		{
			if (context.IsClosing)
			{
				ReducePosition(pair, true, volume, price);
			}
			else
			{
				IncreasePosition(pair, true, volume, price);
				context.Group.Plus.LastOpenTime = trade.Trade.ServerTime;
				context.Group.Plus.Orders++;
			}
		}
		else
		{
			if (context.IsClosing)
			{
				ReducePosition(pair, false, volume, price);
			}
			else
			{
				IncreasePosition(pair, false, volume, price);
				context.Group.Minus.LastOpenTime = trade.Trade.ServerTime;
				context.Group.Minus.Orders++;
			}
		}

		_pending.Remove(trade.Order);
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
			return;

		foreach (var group in _groups)
		{
			if (!group.TryUpdatePrice(security, candle.ClosePrice))
				continue;

			EvaluateGroup(group, candle.CloseTime);
		}
	}

	private void EvaluateGroup(GroupState group, DateTimeOffset time)
	{
		if (!group.HasAllPrices)
			return;

		UpdateProfits(group);

		if (TypeOfOperation == Operations.CloseImmediatelyAllOrders)
		{
			CloseGroup(group);
			return;
		}

		var totalOrders = Positions.Count(p => group.ContainsSecurity(p.Security) && p.CurrentValue != 0m);
		if (MaximumOrders > 0 && totalOrders >= MaximumOrders)
			return;

		if (!IsSessionAllowed(time))
			return;

		if (TypeCloseInProfit == CloseProfitModes.BasketTicket)
		{
			HandleBasketClose(group);
		}
		else
		{
			HandleSideClose(group, true);
			HandleSideClose(group, false);
		}

		if (TypeCloseInProfit == CloseProfitModes.PairByPair)
		{
			HandlePairClose(group, true);
			HandlePairClose(group, false);
		}

		if (TypeCloseInLoss != CloseLossModes.NotCloseInLoss)
		{
			HandleLossExit(group, true);
			HandleLossExit(group, false);
		}

		if (TypeOfOperation == Operations.CloseInProfitAndStop)
			return;

		if (TypeOfOperation == Operations.StandByMode)
			return;

		if (MaximumGroups > 0 && ActiveGroupCount() >= MaximumGroups && !group.IsActive)
			return;

		if (SideOpenOrders == Sides.OpenOnlyPlus || SideOpenOrders == Sides.OpenPlusAndMinus)
			HandleOpen(group, true, time);

		if (SideOpenOrders == Sides.OpenOnlyMinus || SideOpenOrders == Sides.OpenPlusAndMinus)
			HandleOpen(group, false, time);
	}

	private int ActiveGroupCount()
	{
		return _groups.Count(g => g.IsActive);
	}

	private void HandleBasketClose(GroupState group)
	{
		var profit = group.Plus.Profit + group.Minus.Profit;

		if (profit >= TargetCloseProfit)
		{
			if (++group.Plus.ProfitDelay >= DelayCloseProfit && ++group.Minus.ProfitDelay >= DelayCloseProfit)
			{
				CloseGroup(group);
			}
		}
		else
		{
			group.Plus.ProfitDelay = 0;
			group.Minus.ProfitDelay = 0;
		}

		if (TypeCloseInLoss == CloseLossModes.WholeTicket && profit <= -TargetCloseLoss)
		{
			if (++group.Plus.LossDelay >= DelayCloseLoss && ++group.Minus.LossDelay >= DelayCloseLoss)
			{
				CloseGroup(group);
			}
		}
		else
		{
			group.Plus.LossDelay = 0;
			group.Minus.LossDelay = 0;
		}
	}

	private void HandleSideClose(GroupState group, bool isPlus)
	{
		var side = isPlus ? group.Plus : group.Minus;
		var profit = side.Profit;

		if (profit >= TargetCloseProfit && TypeCloseInProfit == CloseProfitModes.SingleTicket)
		{
			if (++side.ProfitDelay >= DelayCloseProfit)
			{
				CloseSide(group, isPlus);
			}
		}
		else
		{
			side.ProfitDelay = 0;
		}

		if (profit <= -TargetCloseLoss && TypeCloseInLoss == CloseLossModes.WholeTicket)
		{
			if (++side.LossDelay >= DelayCloseLoss)
			{
				CloseSide(group, isPlus);
			}
		}
		else
		{
			side.LossDelay = 0;
		}
	}

	private void HandlePairClose(GroupState group, bool isPlus)
	{
		foreach (var pair in group.Pairs)
		{
			var profit = isPlus ? pair.PlusProfit : pair.MinusProfit;
			if (profit < TargetCloseProfit)
			{
				pair.ResetProfitDelay(isPlus);
				continue;
			}

			if (pair.IncreaseProfitDelay(isPlus) >= DelayCloseProfit)
			{
				ClosePair(pair, isPlus);
			}
		}
	}

	private void HandleLossExit(GroupState group, bool isPlus)
	{
		var side = isPlus ? group.Plus : group.Minus;
		if (TypeCloseInLoss == CloseLossModes.PartialTicket && side.Profit <= -TargetCloseLoss)
		{
			if (++side.LossDelay >= DelayCloseLoss)
			{
				PartialCloseSide(group, isPlus);
			}
		}
	}

	private void HandleOpen(GroupState group, bool isPlus, DateTimeOffset time)
	{
		var side = isPlus ? group.Plus : group.Minus;
		var hasPosition = group.HasVolume(isPlus);

		if (!hasPosition)
		{
			OpenSide(group, isPlus, time, 0);
			return;
		}

		if (OpenOrdersInLoss == StepModes.NotOpenInLoss)
			return;

		var threshold = GetNextThreshold(side.Orders);
		var profit = side.Profit;

		if (profit > -threshold)
			return;

		if (MinutesForNextOrder > 0 && side.LastOpenTime.HasValue)
		{
			var elapsed = time - side.LastOpenTime.Value;
			if (elapsed < TimeSpan.FromMinutes(MinutesForNextOrder))
				return;
		}

		OpenSide(group, isPlus, time, side.Orders);
	}

	private void OpenSide(GroupState group, bool isPlus, DateTimeOffset time, int orderIndex)
	{
		var baseVolume = CalculateBaseVolume(group);
		if (baseVolume <= 0m)
			return;

		var multiplier = GetLotMultiplier(orderIndex);
		var targetVolume = baseVolume * multiplier;

		foreach (var pair in group.Pairs)
		{
			var volume = AdjustVolume(pair, targetVolume);
			if (volume <= 0m)
				continue;

			var order = ExecuteOrder(pair, isPlus, false, volume);
			if (order != null)
			{
				_pending[order] = new PendingAction(group, pair, isPlus, false);
			}
		}

		if (SaveInformations)
		{
			LogInfo($"Open {(isPlus ? "Plus" : "Minus")} group {group.Name} at {time:u} with volume {targetVolume:0.####}.");
		}

		if (isPlus)
		{
			group.Plus.LastOpenTime = time;
		}
		else
		{
			group.Minus.LastOpenTime = time;
		}
	}

	private void CloseGroup(GroupState group)
	{
		CloseSide(group, true);
		CloseSide(group, false);
	}

	private void CloseSide(GroupState group, bool isPlus)
	{
		foreach (var pair in group.Pairs)
		{
			var volume = pair.GetVolume(isPlus);
			if (volume <= 0m)
				continue;

			var order = ExecuteOrder(pair, isPlus, true, volume);
			if (order != null)
			{
				_pending[order] = new PendingAction(group, pair, isPlus, true);
			}
		}

		if (isPlus)
		{
			group.Plus.Reset();
		}
		else
		{
			group.Minus.Reset();
		}
	}

	private void PartialCloseSide(GroupState group, bool isPlus)
	{
		foreach (var pair in group.Pairs)
		{
			var volume = pair.GetVolume(isPlus);
			if (volume <= 0m)
				continue;

			var partial = volume / 2m;
			var normalized = NormalizeVolume(pair.Security, partial);
			if (normalized <= 0m)
				continue;

			var order = ExecuteOrder(pair, isPlus, true, normalized);
			if (order != null)
			{
				_pending[order] = new PendingAction(group, pair, isPlus, true);
			}
		}
	}

	private void ClosePair(PairState pair, bool isPlus)
	{
		var volume = pair.GetVolume(isPlus);
		if (volume <= 0m)
			return;

		var order = ExecuteOrder(pair, isPlus, true, volume);
		if (order != null)
		{
			var group = _groups.FirstOrDefault(g => g.ContainsSecurity(pair.Security));
			if (group != null)
				_pending[order] = new PendingAction(group, pair, isPlus, true);
		}

		pair.ResetProfitDelay(isPlus);
	}

	private Order ExecuteOrder(PairState pair, bool isPlus, bool isClosing, decimal volume)
	{
		if (volume <= 0m)
			return null;

		var security = pair.Security;
		var normalized = NormalizeVolume(security, volume);
		if (normalized <= 0m)
			return null;

		if (MaximumLotSize > 0m)
			normalized = Math.Min(normalized, MaximumLotSize);

		var direction = pair.GetSide(isPlus);
		if (isClosing)
			direction = direction == Sides.Buy ? Sides.Sell : Sides.Buy;

		return direction == Sides.Buy
			? BuyMarket(normalized, security, MaxSlippage, StringOrdersEA)
			: SellMarket(normalized, security, MaxSlippage, StringOrdersEA);
	}

	private decimal CalculateBaseVolume(GroupState group)
	{
		decimal volume;

		if (AutoLotSize)
		{
			var balance = Portfolio.CurrentValue;
			if (balance <= 0m)
				return 0m;

			volume = balance * (RiskFactor / 1000m);
		}
		else
		{
			volume = ManualLotSize;
		}

		if (UseFairLotSize)
		{
			var baseStep = group.Pairs[0].Security.StepPrice ?? 1m;
			if (baseStep > 0m)
			{
				volume *= 1m / baseStep;
			}
		}

		return volume;
	}

	private decimal AdjustVolume(PairState pair, decimal baseVolume)
	{
		var volume = baseVolume;

		if (UseFairLotSize)
		{
			var step = pair.Security.StepPrice ?? 1m;
			if (step > 0m)
			{
				volume *= step;
			}
		}

		return volume;
	}

	private decimal GetLotMultiplier(int orderIndex)
	{
		return LotOrdersProgress switch
		{
			LotProgressions.StaticalLot => 1m,
			LotProgressions.GeometricalLot => (decimal)Math.Pow(2, orderIndex),
			LotProgressions.ExponentialLot => (decimal)Math.Pow(3, orderIndex),
			LotProgressions.DecreasesLot => 1m / Math.Max(1, orderIndex + 1),
			_ => 1m
		};
	}

	private decimal GetNextThreshold(int orderIndex)
	{
		var multiplier = StepOrdersProgress switch
		{
			StepProgressions.StaticalStep => 1m,
			StepProgressions.GeometricalStep => orderIndex + 1m,
			StepProgressions.ExponentialStep => (decimal)Math.Pow(2, orderIndex),
			_ => 1m
		};

		return StepOpenNextOrders * multiplier;
	}

	private void UpdateProfits(GroupState group)
	{
		group.Plus.Profit = 0m;
		group.Minus.Profit = 0m;

		foreach (var pair in group.Pairs)
		{
			pair.PlusProfit = CalculatePnL(pair.Security, pair.PlusVolume, pair.PlusAveragePrice, pair.PlusSide, pair.LastPrice);
			pair.MinusProfit = CalculatePnL(pair.Security, pair.MinusVolume, pair.MinusAveragePrice, pair.MinusSide, pair.LastPrice);

			group.Plus.Profit += pair.PlusProfit;
			group.Minus.Profit += pair.MinusProfit;
		}
	}

	private static decimal CalculatePnL(Security security, decimal volume, decimal averagePrice, Sides side, decimal? lastPrice)
	{
		if (volume <= 0m || lastPrice == null || averagePrice <= 0m)
			return 0m;

		var priceStep = security.PriceStep ?? 0.0001m;
		var stepPrice = security.StepPrice ?? priceStep;
		if (priceStep <= 0m || stepPrice <= 0m)
			return 0m;

		var diff = side == Sides.Buy ? lastPrice.Value - averagePrice : averagePrice - lastPrice.Value;
		return diff / priceStep * stepPrice * volume;
	}

	private void IncreasePosition(PairState pair, bool isPlus, decimal volume, decimal price)
	{
		if (isPlus)
		{
			var total = pair.PlusVolume + volume;
			pair.PlusAveragePrice = (pair.PlusAveragePrice * pair.PlusVolume + price * volume) / Math.Max(total, 1m);
			pair.PlusVolume = total;
		}
		else
		{
			var total = pair.MinusVolume + volume;
			pair.MinusAveragePrice = (pair.MinusAveragePrice * pair.MinusVolume + price * volume) / Math.Max(total, 1m);
			pair.MinusVolume = total;
		}
	}

	private void ReducePosition(PairState pair, bool isPlus, decimal volume, decimal price)
	{
		if (isPlus)
		{
			pair.PlusVolume = Math.Max(0m, pair.PlusVolume - volume);
			if (pair.PlusVolume <= 0m)
			{
				pair.PlusAveragePrice = 0m;
			}
		}
		else
		{
			pair.MinusVolume = Math.Max(0m, pair.MinusVolume - volume);
			if (pair.MinusVolume <= 0m)
			{
				pair.MinusAveragePrice = 0m;
			}
		}
	}

	private bool IsSessionAllowed(DateTimeOffset time)
	{
		if (!ControlSession || !_hasSessionWindow)
			return true;

		if (time.DayOfWeek == DayOfWeek.Monday && _sessionStart.HasValue)
		{
			var start = _sessionStart.Value.AddMinutes(WaitAfterOpen);
			if (time < start)
				return false;
		}

		if (time.DayOfWeek == DayOfWeek.Friday && _sessionEnd.HasValue)
		{
			var end = _sessionEnd.Value.AddMinutes(-StopBeforeClose);
			if (time > end)
				return false;
		}

		return true;
	}

	private bool TryResolveSessionWindow(out DateTimeOffset? start, out DateTimeOffset? end)
	{
		start = null;
		end = null;

		if (Security == null)
			return false;

		var board = Security.Board;
		if (board == null)
			return false;

		var sessions = board.WorkingTime;
		if (sessions == null)
			return false;

		if (sessions.Periods.Count == 0)
			return false;

		start = sessions.Periods[0].Start;
		end = sessions.Periods[^1].End;
		return true;
	}

	private List<string> ParseCurrencies()
	{
		var tokens = CurrenciesTrade.Split(new[] { '/', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		return tokens.Select(t => t.Trim().ToUpperInvariant()).Where(t => t.Length == 3).Distinct().ToList();
	}

	private HashSet<int> ParseSkippedGroups()
	{
		var result = new HashSet<int>();
		if (NoOfGroupToSkip.IsEmptyOrWhiteSpace())
			return result;

		var tokens = NoOfGroupToSkip.Split(new[] { ',', ';', ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (var token in tokens)
		{
			if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
				result.Add(value);
		}

		return result;
	}

	private List<(string Base, string Middle, string Quote)> BuildCombinations(IReadOnlyList<string> currencies)
	{
		var combinations = new List<(string, string, string)>();
		for (var i = 0; i < currencies.Count - 2; i++)
		{
			for (var j = i + 1; j < currencies.Count - 1; j++)
			{
				for (var k = j + 1; k < currencies.Count; k++)
				{
					combinations.Add((currencies[i], currencies[j], currencies[k]));
				}
			}
		}

		return combinations;
	}

	private GroupState ResolveGroup((string Base, string Middle, string Quote) combo, string prefix, string suffix)
	{
		Security Resolve(string left, string right)
		{
			var code = prefix + left + right + suffix;
			var security = SecurityProvider.LookupById(code);
			if (security == null && suffix.Equals("AUTO", StringComparison.OrdinalIgnoreCase))
			{
				security = SecurityProvider.LookupById(prefix + left + right);
			}
			return security;
		}

		var first = Resolve(combo.Base, combo.Middle);
		var second = Resolve(combo.Base, combo.Quote);
		var third = Resolve(combo.Middle, combo.Quote);

		if (first == null || second == null || third == null)
		{
			LogWarning($"Ring {combo.Base}-{combo.Middle}-{combo.Quote} skipped. One or more symbols were not resolved.");
			return null;
		}

		return new GroupState($"{combo.Base}-{combo.Middle}-{combo.Quote}", new[]
		{
			new PairState(first, Sides.Buy, Sides.Sell),
			new PairState(second, Sides.Sell, Sides.Buy),
			new PairState(third, Sides.Buy, Sides.Sell)
		});
	}

	private static string NormalizeAffix(string value)
	{
		if (value.IsEmptyOrWhiteSpace() || value.Equals("NONE", StringComparison.OrdinalIgnoreCase) || value.Equals("AUTO", StringComparison.OrdinalIgnoreCase))
			return string.Empty;

		return value.Trim();
	}

	private sealed class PendingAction
	{
		public PendingAction(GroupState group, PairState pair, bool isPlus, bool isClosing)
		{
			Group = group;
			Pair = pair;
			IsPlus = isPlus;
			IsClosing = isClosing;
		}

		public GroupState Group { get; }
		public PairState Pair { get; }
		public bool IsPlus { get; }
		public bool IsClosing { get; }
	}

	private sealed class GroupState
	{
		public GroupState(string name, PairState[] pairs)
		{
			Name = name;
			Pairs = pairs;
			Plus = new SideState();
			Minus = new SideState();
		}

		public string Name { get; }
		public PairState[] Pairs { get; }
		public SideState Plus { get; }
		public SideState Minus { get; }

		public bool HasAllPrices => Pairs.All(p => p.LastPrice != null);

		public bool TryUpdatePrice(Security security, decimal price)
		{
			var pair = Pairs.FirstOrDefault(p => p.Security == security);
			if (pair == null)
				return false;

			pair.LastPrice = price;
			return true;
		}

		public bool ContainsSecurity(Security security)
		{
			return Pairs.Any(p => p.Security == security);
		}

		public bool HasVolume(bool isPlus)
		{
			return Pairs.Any(p => p.GetVolume(isPlus) > 0m);
		}

		public bool IsActive => HasVolume(true) || HasVolume(false);
	}

	private sealed class SideState
	{
		public decimal Profit { get; set; }
		public int ProfitDelay { get; set; }
		public int LossDelay { get; set; }
		public DateTimeOffset? LastOpenTime { get; set; }
		public int Orders { get; set; }

		public void Reset()
		{
			Profit = 0m;
			ProfitDelay = 0;
			LossDelay = 0;
			LastOpenTime = null;
			Orders = 0;
		}
	}

	private sealed class PairState
	{
		private int _plusDelay;
		private int _minusDelay;

		public PairState(Security security, Sides plus, Sides minus)
		{
			Security = security;
			PlusSide = plus;
			MinusSide = minus;
		}

		public Security Security { get; }
		public Sides PlusSide { get; }
		public Sides MinusSide { get; }
		public decimal? LastPrice { get; set; }
		public decimal PlusVolume { get; set; }
		public decimal MinusVolume { get; set; }
		public decimal PlusAveragePrice { get; set; }
		public decimal MinusAveragePrice { get; set; }
		public decimal PlusProfit { get; set; }
		public decimal MinusProfit { get; set; }

		public decimal GetVolume(bool isPlus) => isPlus ? PlusVolume : MinusVolume;
		public Sides GetSide(bool isPlus) => isPlus ? PlusSide : MinusSide;

		public int IncreaseProfitDelay(bool isPlus)
		{
			if (isPlus)
				return ++_plusDelay;
			return ++_minusDelay;
		}

		public void ResetProfitDelay(bool isPlus)
		{
			if (isPlus)
				_plusDelay = 0;
			else
				_minusDelay = 0;
		}
	}

	public enum Operations
	{
		StandByMode,
		NormalOperation,
		CloseInProfitAndStop,
		CloseImmediatelyAllOrders
	}

	public enum Sides
	{
		OpenOnlyPlus,
		OpenOnlyMinus,
		OpenPlusAndMinus
	}

	public enum StepModes
	{
		NotOpenInLoss,
		OpenWithManualStep,
		OpenWithAutoStep
	}

	public enum StepProgressions
	{
		StaticalStep,
		GeometricalStep,
		ExponentialStep
	}

	public enum CloseProfitModes
	{
		SingleTicket,
		BasketTicket,
		PairByPair
	}

	public enum CloseLossModes
	{
		WholeTicket,
		PartialTicket,
		NotCloseInLoss
	}

	public enum LotProgressions
	{
		StaticalLot,
		GeometricalLot,
		ExponentialLot,
		DecreasesLot
	}
}

