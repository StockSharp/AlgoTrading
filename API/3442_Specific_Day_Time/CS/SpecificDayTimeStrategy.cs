using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the "Expert Advisor specific day and time" MetaTrader expert.
/// The strategy opens market or pending orders at a scheduled timestamp and closes them at another.
/// Optional trailing-stop and break-even logic mimic the original MQL script.
/// </summary>
public class SpecificDayTimeStrategy : Strategy
{
	private enum OrderMode
	{
		Market,
		Stop,
		Limit,
	}

	private enum LotSizingMode
	{
		Manual,
		Automatic,
	}

	private sealed class PendingHolder
	{
		public PendingHolder(Order order, DateTimeOffset? expiration)
		{
			Order = order;
			Expiration = expiration;
		}

		public Order Order { get; }

		public DateTimeOffset? Expiration { get; }
	}

	private readonly StrategyParam<DateTimeOffset> _openTime;
	private readonly StrategyParam<DateTimeOffset> _closeTime;
	private readonly StrategyParam<OrderMode> _orderMode;
	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _trailingEnabled;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _breakEvenEnabled;
	private readonly StrategyParam<decimal> _breakEvenAfterPoints;
	private readonly StrategyParam<decimal> _orderDistancePoints;
	private readonly StrategyParam<int> _pendingExpireMinutes;
	private readonly StrategyParam<LotSizingMode> _lotSizing;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualVolume;
	private readonly StrategyParam<bool> _closeOwn;
	private readonly StrategyParam<bool> _closeAll;
	private readonly StrategyParam<bool> _deletePending;

	private readonly List<PendingHolder> _pendingOrders = new();

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _lastTrade;

	private bool _openProcessed;
	private bool _closeProcessed;
	private bool _buyPlaced;
	private bool _sellPlaced;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Initializes a new instance of the <see cref="SpecificDayTimeStrategy"/>.
	/// </summary>
	public SpecificDayTimeStrategy()
	{
		_openTime = Param(nameof(OpenTime), new DateTimeOffset(new DateTime(2021, 11, 29, 0, 0, 0, DateTimeKind.Utc)))
		.SetDisplay("Open Time", "Day and time to place the orders.", "Scheduling");

		_closeTime = Param(nameof(CloseTime), new DateTimeOffset(new DateTime(2021, 11, 29, 12, 0, 0, DateTimeKind.Utc)))
		.SetDisplay("Close Time", "Day and time to close orders.", "Scheduling");

		_orderMode = Param(nameof(OrderPlacement), OrderMode.Market)
		.SetDisplay("Order Mode", "Type of orders to place.", "Trading");

		_openBuy = Param(nameof(OpenBuyOrders), false)
		.SetDisplay("Open Buy", "Submit buy orders at the scheduled time.", "Trading");

		_openSell = Param(nameof(OpenSellOrders), false)
		.SetDisplay("Open Sell", "Submit sell orders at the scheduled time.", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
		.SetDisplay("Take Profit", "Take-profit distance in points (0 disables).", "Protection")
		.SetGreaterOrEqual(0m);

		_stopLossPoints = Param(nameof(StopLossPoints), 10m)
		.SetDisplay("Stop Loss", "Stop-loss distance in points (0 disables).", "Protection")
		.SetGreaterOrEqual(0m);

		_trailingEnabled = Param(nameof(TrailingStopEnabled), false)
		.SetDisplay("Trailing Stop", "Enable trailing stop adjustments.", "Protection");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 1m)
		.SetDisplay("Trailing Step", "Extra points required to move the stop.", "Protection")
		.SetGreaterOrEqual(0m);

		_breakEvenEnabled = Param(nameof(BreakEvenEnabled), false)
		.SetDisplay("Break Even", "Shift the stop to break-even after profit.", "Protection");

		_breakEvenAfterPoints = Param(nameof(BreakEvenAfterPoints), 10m)
		.SetDisplay("Break Even Trigger", "Profit points required before break-even.", "Protection")
		.SetGreaterOrEqual(0m);

		_orderDistancePoints = Param(nameof(OrderDistancePoints), 10m)
		.SetDisplay("Pending Distance", "Distance in points for pending orders.", "Trading")
		.SetGreaterOrEqual(0m);

		_pendingExpireMinutes = Param(nameof(PendingExpireMinutes), 60)
		.SetDisplay("Pending Expiry", "Minutes until pending orders expire (0 keeps them).", "Trading")
		.SetGreaterOrEqual(0);

		_lotSizing = Param(nameof(LotSizing), LotSizingMode.Manual)
		.SetDisplay("Lot Sizing", "Manual size or automatic risk factor.", "Risk");

		_riskFactor = Param(nameof(RiskFactor), 1m)
		.SetDisplay("Risk Factor", "Risk multiplier for automatic volume.", "Risk")
		.SetGreaterOrEqual(0m);

		_manualVolume = Param(nameof(ManualVolume), 0.01m)
		.SetDisplay("Manual Volume", "Fixed order volume when manual sizing is selected.", "Risk")
		.SetGreaterThanZero();

		_closeOwn = Param(nameof(CloseOwnOrders), false)
		.SetDisplay("Close Own", "Close this strategy positions at the close time.", "Exit");

		_closeAll = Param(nameof(CloseAllOrders), false)
		.SetDisplay("Close All", "Force a flat position at the close time.", "Exit");

		_deletePending = Param(nameof(DeletePendingOrders), true)
		.SetDisplay("Delete Pending", "Cancel pending orders at close time.", "Exit");
	}

	/// <summary>
	/// Date and time to open orders.
	/// </summary>
	public DateTimeOffset OpenTime
	{
		get => _openTime.Value;
		set => _openTime.Value = value;
	}

	/// <summary>
	/// Date and time to close all activity.
	/// </summary>
	public DateTimeOffset CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Order placement mode.
	/// </summary>
	public OrderMode OrderPlacement
	{
		get => _orderMode.Value;
		set => _orderMode.Value = value;
	}

	/// <summary>
	/// Whether buy orders must be submitted at the open time.
	/// </summary>
	public bool OpenBuyOrders
	{
		get => _openBuy.Value;
		set => _openBuy.Value = value;
	}

	/// <summary>
	/// Whether sell orders must be submitted at the open time.
	/// </summary>
	public bool OpenSellOrders
	{
		get => _openSell.Value;
		set => _openSell.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enable trailing stop updates.
	/// </summary>
	public bool TrailingStopEnabled
	{
		get => _trailingEnabled.Value;
		set => _trailingEnabled.Value = value;
	}

	/// <summary>
	/// Extra profit required before the stop advances.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enable break-even behaviour.
	/// </summary>
	public bool BreakEvenEnabled
	{
		get => _breakEvenEnabled.Value;
		set => _breakEvenEnabled.Value = value;
	}

	/// <summary>
	/// Profit points required before the stop jumps to break-even.
	/// </summary>
	public decimal BreakEvenAfterPoints
	{
		get => _breakEvenAfterPoints.Value;
		set => _breakEvenAfterPoints.Value = value;
	}

	/// <summary>
	/// Distance in points used when placing pending orders.
	/// </summary>
	public decimal OrderDistancePoints
	{
		get => _orderDistancePoints.Value;
		set => _orderDistancePoints.Value = value;
	}

	/// <summary>
	/// Pending order expiration in minutes.
	/// </summary>
	public int PendingExpireMinutes
	{
		get => _pendingExpireMinutes.Value;
		set => _pendingExpireMinutes.Value = value;
	}

	/// <summary>
	/// Selects between manual or automatic lot sizing.
	/// </summary>
	public LotSizingMode LotSizing
	{
		get => _lotSizing.Value;
		set => _lotSizing.Value = value;
	}

	/// <summary>
	/// Risk factor used when automatic sizing is enabled.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Manual volume used in manual sizing mode.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <summary>
	/// Close only this strategy orders at the close time.
	/// </summary>
	public bool CloseOwnOrders
	{
		get => _closeOwn.Value;
		set => _closeOwn.Value = value;
	}

	/// <summary>
	/// Force closing of all exposure at the close time.
	/// </summary>
	public bool CloseAllOrders
	{
		get => _closeAll.Value;
		set => _closeAll.Value = value;
	}

	/// <summary>
	/// Delete pending orders at the close time.
	/// </summary>
	public bool DeletePendingOrders
	{
		get => _deletePending.Value;
		set => _deletePending.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pendingOrders.Clear();
		_bestBid = null;
		_bestAsk = null;
		_lastTrade = null;
		_openProcessed = false;
		_closeProcessed = false;
		_buyPlaced = false;
		_sellPlaced = false;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
		throw new InvalidOperationException("Portfolio is not specified.");

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		Timer.Start(TimeSpan.FromSeconds(1), ProcessTimer);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last))
		_lastTrade = (decimal)last;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_bestAsk = (decimal)ask;

		ManagePosition();
		CancelExpiredPending();
	}

	private void ProcessTimer()
	{
		CheckOpenWindow();
		CheckCloseWindow();
		CancelExpiredPending();
	}

	private void CheckOpenWindow()
	{
		if (_openProcessed)
		return;

		var now = CurrentTime;
		if (now < OpenTime || now >= OpenTime + TimeSpan.FromMinutes(1))
		return;

		_openProcessed = true;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!OpenBuyOrders && !OpenSellOrders)
		return;

		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		switch (OrderPlacement)
		{
			case OrderMode.Market:
			OpenMarketOrders(volume);
			break;
			case OrderMode.Stop:
			OpenPendingOrders(volume, true);
			break;
			case OrderMode.Limit:
			OpenPendingOrders(volume, false);
			break;
		}
	}

	private void CheckCloseWindow()
	{
		if (_closeProcessed)
		return;

		var now = CurrentTime;
		if (now < CloseTime || now >= CloseTime + TimeSpan.FromMinutes(1))
		return;

		_closeProcessed = true;

		var needClose = CloseOwnOrders || CloseAllOrders;
		if (needClose && Position != 0)
		CloseMarketPosition();

		if (DeletePendingOrders)
		CancelAllPending();
	}

	private void OpenMarketOrders(decimal volume)
	{
		if (OpenBuyOrders && !_buyPlaced && Position <= 0)
		{
			BuyMarket(volume + Math.Max(0m, -Position));
			_buyPlaced = true;
		}

		if (OpenSellOrders && !_sellPlaced && Position >= 0)
		{
			SellMarket(volume + Math.Max(0m, Position));
			_sellPlaced = true;
		}
	}

	private void OpenPendingOrders(decimal volume, bool isStop)
	{
		var pip = GetPipSize();
		if (pip <= 0m)
		pip = 0.0001m;

		var distance = OrderDistancePoints * pip;
		var ask = GetAskPrice();
		var bid = GetBidPrice();

		DateTimeOffset? expiration = null;
		if (PendingExpireMinutes > 0)
		expiration = CurrentTime + TimeSpan.FromMinutes(PendingExpireMinutes);

		if (OpenBuyOrders && !_buyPlaced && !_pendingOrders.Any(p => p.Order.Side == Sides.Buy))
		{
			var price = isStop
			? ask + distance
			: bid - distance;

			if (price is decimal buyPrice && buyPrice > 0m)
			{
				var order = isStop ? BuyStop(volume, buyPrice) : BuyLimit(volume, buyPrice);
				TrackPending(order, expiration);
				_buyPlaced = true;
			}
		}

		if (OpenSellOrders && !_sellPlaced && !_pendingOrders.Any(p => p.Order.Side == Sides.Sell))
		{
			var price = isStop
			? bid - distance
			: ask + distance;

			if (price is decimal sellPrice && sellPrice > 0m)
			{
				var order = isStop ? SellStop(volume, sellPrice) : SellLimit(volume, sellPrice);
				TrackPending(order, expiration);
				_sellPlaced = true;
			}
		}
	}

	private void CloseMarketPosition()
	{
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(Math.Abs(Position));
	}

	private void CancelAllPending()
	{
		foreach (var holder in _pendingOrders.ToArray())
		{
			CancelOrder(holder.Order);
		}
	}

	private void TrackPending(Order order, DateTimeOffset? expiration)
	{
		if (order == null)
		return;

		_pendingOrders.Add(new PendingHolder(order, expiration));
	}

	private void CancelExpiredPending()
	{
		if (_pendingOrders.Count == 0)
		return;

		var now = CurrentTime;
		foreach (var holder in _pendingOrders.ToArray())
		{
			if (holder.Expiration is DateTimeOffset expiry && now >= expiry)
			CancelOrder(holder.Order);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null)
		return;

		if (order.State.IsFinal())
		_pendingOrders.RemoveAll(p => p.Order == order);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
		return;

		_pendingOrders.RemoveAll(p => p.Order == trade.Order);

		InitializeProtectionLevels();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_longStop = null;
			_longTake = null;
			_shortStop = null;
			_shortTake = null;
		}
	}

	private void ManagePosition()
	{
		if (Position == 0)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice is not decimal entry || entry <= 0m)
		return;

		var bid = GetBidPrice();
		var ask = GetAskPrice();

		var pip = GetPipSize();
		if (pip <= 0m)
		pip = 0.0001m;

		var stopDistance = StopLossPoints * pip;
		var takeDistance = TakeProfitPoints * pip;
		var trailingStep = TrailingStepPoints * pip;
		var breakEven = BreakEvenAfterPoints * pip;

		if (Position > 0)
		{
			var price = bid ?? _lastTrade;
			if (price == null)
			return;

			if (StopLossPoints > 0m && _longStop == null)
			_longStop = entry - stopDistance;

			if (TakeProfitPoints > 0m && _longTake == null)
			_longTake = entry + takeDistance;

			UpdateLongStop(entry, price.Value, stopDistance, trailingStep, breakEven);

			if (_longStop is decimal stop && price.Value <= stop)
			{
				SellMarket(Position);
				return;
			}

			if (_longTake is decimal take && price.Value >= take)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			var price = ask ?? _lastTrade;
			if (price == null)
			return;

			if (StopLossPoints > 0m && _shortStop == null)
			_shortStop = entry + stopDistance;

			if (TakeProfitPoints > 0m && _shortTake == null)
			_shortTake = entry - takeDistance;

			UpdateShortStop(entry, price.Value, stopDistance, trailingStep, breakEven);

			if (_shortStop is decimal stop && price.Value >= stop)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_shortTake is decimal take && price.Value <= take)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	private void InitializeProtectionLevels()
	{
		if (Position == 0)
		return;

		var entry = PositionPrice;
		if (entry is not decimal price)
		return;

		var pip = GetPipSize();
		if (pip <= 0m)
		pip = 0.0001m;

		var stopDistance = StopLossPoints * pip;
		var takeDistance = TakeProfitPoints * pip;

		if (Position > 0)
		{
			_longStop = StopLossPoints > 0m ? price - stopDistance : null;
			_longTake = TakeProfitPoints > 0m ? price + takeDistance : null;
		}
		else if (Position < 0)
		{
			_shortStop = StopLossPoints > 0m ? price + stopDistance : null;
			_shortTake = TakeProfitPoints > 0m ? price - takeDistance : null;
		}
	}

	private void UpdateLongStop(decimal entry, decimal price, decimal stopDistance, decimal trailingStep, decimal breakEven)
	{
		if (StopLossPoints <= 0m || _longStop == null)
		return;

		var currentStop = _longStop.Value;

		if (TrailingStopEnabled && BreakEvenEnabled)
		{
			if (price - currentStop > stopDistance + trailingStep && price - entry >= breakEven + stopDistance)
			_longStop = price - stopDistance;
		}
		else if (TrailingStopEnabled)
		{
			if (price - currentStop > stopDistance + trailingStep)
			_longStop = price - stopDistance;
		}
		else if (BreakEvenEnabled)
		{
			if (price - entry >= breakEven + stopDistance && currentStop < entry)
			_longStop = price - stopDistance;
		}
	}

	private void UpdateShortStop(decimal entry, decimal price, decimal stopDistance, decimal trailingStep, decimal breakEven)
	{
		if (StopLossPoints <= 0m || _shortStop == null)
		return;

		var currentStop = _shortStop.Value;

		if (TrailingStopEnabled && BreakEvenEnabled)
		{
			if (currentStop - price > stopDistance + trailingStep && entry - price >= breakEven + stopDistance)
			_shortStop = price + stopDistance;
		}
		else if (TrailingStopEnabled)
		{
			if (currentStop - price > stopDistance + trailingStep)
			_shortStop = price + stopDistance;
		}
		else if (BreakEvenEnabled)
		{
			if (entry - price >= breakEven + stopDistance && currentStop > entry)
			_shortStop = price + stopDistance;
		}
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? security.Step ?? 0m;
		var decimals = security.Decimals;

		if (step <= 0m)
		return 0m;

		return decimals is 3 or 5 ? step * 10m : step;
	}

	private decimal? GetBidPrice()
	{
		return _bestBid ?? Security.BestBid?.Price ?? _lastTrade;
	}

	private decimal? GetAskPrice()
	{
		return _bestAsk ?? Security.BestAsk?.Price ?? _lastTrade;
	}

	private decimal CalculateVolume()
	{
		if (LotSizing == LotSizingMode.Manual)
		return ManualVolume;

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (portfolioValue <= 0m || RiskFactor <= 0m)
		return ManualVolume;

		var contractSize = GetContractSize();
		if (contractSize <= 0m)
		contractSize = 100000m;

		var estimated = (portfolioValue / contractSize) * RiskFactor;

		var volumeStep = Security?.VolumeStep;
		if (volumeStep is > 0m)
		estimated = Math.Round(estimated / volumeStep.Value) * volumeStep.Value;

		var minVolume = Security?.MinVolume;
		if (minVolume is > 0m && estimated < minVolume.Value)
		estimated = minVolume.Value;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume is > 0m && estimated > maxVolume.Value)
		estimated = maxVolume.Value;

		return estimated > 0m ? estimated : ManualVolume;
	}

	private decimal GetContractSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var type = security.GetType();

		decimal TryGet(string name)
		{
			var prop = type.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (prop == null)
			return 0m;

			return prop.GetValue(security) switch
			{
				decimal d => d,
				double dbl => (decimal)dbl,
				int i => i,
				long l => l,
				_ => 0m,
			};
		}

		var contractSize = TryGet("ContractSize");
		if (contractSize <= 0m)
		contractSize = TryGet("LotSize");

		return contractSize;
	}
}
