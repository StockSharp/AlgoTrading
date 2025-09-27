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
/// Scalping strategy that places symmetrical stop orders and manages them based on spread and time filters.
/// </summary>
public class FlySystemScalpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _pendingDistance;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _maxSpread;
	private readonly StrategyParam<decimal> _commissionInPips;
	private readonly StrategyParam<bool> _autoLotSize;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualVolume;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _tradeStartTime;
	private readonly StrategyParam<TimeSpan> _tradeStopTime;
	private readonly StrategyParam<decimal> _modifyThreshold;

	private ISubscriptionHandler<Level1ChangeMessage> _level1Subscription;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _pipSize;
	private bool _cycleReady;

	/// <summary>
	/// Pending order offset from the market in pips.
	/// </summary>
	public decimal PendingDistance
	{
		get => _pendingDistance.Value;
		set => _pendingDistance.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <summary>
	/// Flag indicating whether take profit should be attached.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in pips including commissions.
	/// </summary>
	public decimal MaxSpread
	{
		get => _maxSpread.Value;
		set => _maxSpread.Value = value;
	}

	/// <summary>
	/// Commission that will be added to the spread filter in pips.
	/// </summary>
	public decimal CommissionInPips
	{
		get => _commissionInPips.Value;
		set => _commissionInPips.Value = value;
	}

	/// <summary>
	/// Enables automatic lot size calculation based on the risk factor.
	/// </summary>
	public bool AutoLotSize
	{
		get => _autoLotSize.Value;
		set => _autoLotSize.Value = value;
	}

	/// <summary>
	/// Risk percentage used for automatic position sizing.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Fixed volume used when automatic sizing is disabled.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <summary>
	/// Flag controlling time filtering.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading session start time (inclusive).
	/// </summary>
	public TimeSpan TradeStartTime
	{
		get => _tradeStartTime.Value;
		set => _tradeStartTime.Value = value;
	}

	/// <summary>
	/// Trading session stop time (exclusive).
	/// </summary>
	public TimeSpan TradeStopTime
	{
		get => _tradeStopTime.Value;
		set => _tradeStopTime.Value = value;
	}

	/// <summary>
	/// Price difference threshold in pips before pending orders are modified.
	/// </summary>
	public decimal ModifyThreshold
	{
		get => _modifyThreshold.Value;
		set => _modifyThreshold.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public FlySystemScalpStrategy()
	{
		_pendingDistance = Param(nameof(PendingDistance), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Pending Distance", "Distance in pips used to place stop orders away from the market.", "Orders")
			.SetCanOptimize(true);

		_stopLossDistance = Param(nameof(StopLossDistance), 0.4m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Protective stop distance in pips.", "Orders")
			.SetCanOptimize(true);

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in pips when it is enabled.", "Orders")
			.SetCanOptimize(true);

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Attach take profit orders to new positions.", "Orders");

		_maxSpread = Param(nameof(MaxSpread), 1m)
			.SetDisplay("Max Spread", "Maximum allowed spread in pips (0 disables the filter).", "Risk")
			.SetCanOptimize(true);

		_commissionInPips = Param(nameof(CommissionInPips), 0m)
			.SetDisplay("Commission (pips)", "Commission in pips that is added to the spread filter.", "Risk");

		_autoLotSize = Param(nameof(AutoLotSize), false)
			.SetDisplay("Auto Lot", "Enable automatic volume calculation by risk.", "Sizing");

		_riskFactor = Param(nameof(RiskFactor), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Factor", "Risk percentage used when auto lot sizing is enabled.", "Sizing")
			.SetCanOptimize(true);

		_manualVolume = Param(nameof(ManualVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Manual Volume", "Fixed volume used when auto lot sizing is disabled.", "Sizing");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Restrict trading to a custom session.", "Timing");

		_tradeStartTime = Param(nameof(TradeStartTime), TimeSpan.Zero)
			.SetDisplay("Session Start", "Trading session start time (inclusive).", "Timing");

		_tradeStopTime = Param(nameof(TradeStopTime), TimeSpan.Zero)
			.SetDisplay("Session End", "Trading session end time (exclusive).", "Timing");

		_modifyThreshold = Param(nameof(ModifyThreshold), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Modify Threshold", "Minimal difference in pips before pending orders are updated.", "Orders");

		_cycleReady = true;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyStopOrder = null;
		_sellStopOrder = null;
		_bestBid = null;
		_bestAsk = null;
		_pipSize = 0m;
		_cycleReady = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_level1Subscription = SubscribeLevel1();
		_level1Subscription
			.Bind(ProcessLevel1)
			.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnStopping()
	{
		CancelPendingOrders();

		base.OnStopping();
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.Step ?? security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
			step = 0.0001m;

		var multiplier = 1m;
		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
			multiplier = 10m;
		else if (digits != null && digits.Value == 1)
			multiplier = 0.1m;

		return step * multiplier;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
			_bestBid = bid;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
			_bestAsk = ask;

		if (_bestBid is null || _bestAsk is null)
			return;

		ManageTradingCycle(message.ServerTime);
	}

	private void ManageTradingCycle(DateTimeOffset currentTime)
	{
		CleanFinalizedOrders();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsTradingTime(currentTime))
		{
			CancelPendingOrders();
			return;
		}

		if (!IsSpreadWithinLimit())
		{
			CancelPendingOrders();
			return;
		}

		if (Position == 0)
		{
			if (_cycleReady)
				PlaceOrUpdatePendingOrders();

			return;
		}

		if (Position > 0)
			CancelOrder(ref _sellStopOrder);
		else if (Position < 0)
			CancelOrder(ref _buyStopOrder);
	}

	private void CleanFinalizedOrders()
	{
		if (_buyStopOrder != null && _buyStopOrder.State.IsFinal())
			_buyStopOrder = null;

		if (_sellStopOrder != null && _sellStopOrder.State.IsFinal())
			_sellStopOrder = null;
	}

	private bool IsTradingTime(DateTimeOffset currentTime)
	{
		if (!UseTimeFilter)
			return true;

		var now = currentTime.TimeOfDay;
		var start = TradeStartTime;
		var stop = TradeStopTime;

		if (start == stop)
			return true;

		if (start < stop)
			return now >= start && now < stop;

		return now >= start || now < stop;
	}

	private bool IsSpreadWithinLimit()
	{
		if (MaxSpread <= 0m)
			return true;

		if (_bestBid is null || _bestAsk is null)
			return false;

		var spread = _bestAsk.Value - _bestBid.Value;
		if (spread < 0m)
			return false;

		var commission = CommissionInPips * _pipSize;
		var total = spread + commission;
		var limit = MaxSpread * _pipSize;

		return total <= limit;
	}

	private void PlaceOrUpdatePendingOrders()
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m || _bestBid is null || _bestAsk is null)
			return;

		var buyPrice = NormalizePrice(_bestAsk.Value + PendingDistance * _pipSize);
		var sellPrice = NormalizePrice(_bestBid.Value - PendingDistance * _pipSize);

		var stopOffset = StopLossDistance * _pipSize;
		var takeOffset = UseTakeProfit ? TakeProfitDistance * _pipSize : (decimal?)null;

		var buyStopLoss = stopOffset > 0m ? buyPrice - stopOffset : (decimal?)null;
		var buyTakeProfit = takeOffset != null ? buyPrice + takeOffset.Value : (decimal?)null;

		var sellStopLoss = stopOffset > 0m ? sellPrice + stopOffset : (decimal?)null;
		var sellTakeProfit = takeOffset != null ? sellPrice - takeOffset.Value : (decimal?)null;

		UpdatePendingOrder(ref _buyStopOrder, true, volume, buyPrice, buyStopLoss, buyTakeProfit);
		UpdatePendingOrder(ref _sellStopOrder, false, volume, sellPrice, sellStopLoss, sellTakeProfit);

		if (_buyStopOrder != null || _sellStopOrder != null)
			_cycleReady = false;
	}

	private void UpdatePendingOrder(ref Order order, bool isBuy, decimal volume, decimal price, decimal? stopLoss, decimal? takeProfit)
	{
		if (price <= 0m || volume <= 0m)
			return;

		if (order == null || order.State.IsFinal())
		{
			order = isBuy
				? BuyStop(volume, price, stopLoss, takeProfit)
				: SellStop(volume, price, stopLoss, takeProfit);
			return;
		}

		if (!order.Price.HasValue)
			return;

		var difference = Math.Abs(order.Price.Value - price);
		var threshold = ModifyThreshold * _pipSize;

		if (difference >= threshold || order.Volume != volume)
			ReRegisterOrder(order, price, volume);
	}

	private void CancelPendingOrders()
	{
		CancelOrder(ref _buyStopOrder);
		CancelOrder(ref _sellStopOrder);

		if (Position == 0)
			_cycleReady = true;
	}

	private void CancelOrder(ref Order order)
	{
		if (order == null)
			return;

		if (!order.State.IsFinal())
			CancelOrder(order);

		order = null;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		var step = security.Step ?? security.PriceStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(price / step, MidpointRounding.AwayFromZero);
			return steps * step;
		}

		var decimals = security.Decimals;
		return decimals != null ? Math.Round(price, decimals.Value) : price;
	}

	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_cycleReady = true;
			return;
		}

		_cycleReady = false;

		if (Position > 0)
			CancelOrder(ref _sellStopOrder);
		else if (Position < 0)
			CancelOrder(ref _buyStopOrder);
	}

	private decimal CalculateOrderVolume()
	{
		if (!AutoLotSize)
			return ManualVolume;

		var portfolio = Portfolio;
		if (portfolio == null)
			return ManualVolume;

		var capital = portfolio.CurrentValue ?? portfolio.BeginValue;
		if (capital == null || capital <= 0m)
			return ManualVolume;

		if (_pipSize <= 0m)
			return ManualVolume;

		var step = Security?.Step ?? Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return ManualVolume;

		var stepPrice = Security?.StepPrice ?? 0m;
		if (stepPrice <= 0m)
			return ManualVolume;

		var stopDistance = StopLossDistance * _pipSize;
		if (stopDistance <= 0m)
			return ManualVolume;

		var ticksToStop = stopDistance / step;
		if (ticksToStop <= 0m)
			return ManualVolume;

		var riskMoney = capital.Value * (RiskFactor / 100m);
		if (riskMoney <= 0m)
			return ManualVolume;

		var lossPerVolume = ticksToStop * stepPrice;
		if (lossPerVolume <= 0m)
			return ManualVolume;

		var rawVolume = riskMoney / lossPerVolume;

		var lotStep = Security?.LotStep ?? 0m;
		if (lotStep > 0m)
		{
			var steps = Math.Floor((double)(rawVolume / lotStep));
			rawVolume = (decimal)steps * lotStep;
		}

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && rawVolume < minVolume)
			rawVolume = minVolume;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && rawVolume > maxVolume.Value)
			rawVolume = maxVolume.Value;

		return rawVolume > 0m ? rawVolume : ManualVolume;
	}
}

