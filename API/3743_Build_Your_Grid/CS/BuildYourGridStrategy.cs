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
/// Grid strategy converted from the MetaTrader expert "BuildYourGridEA".
/// It maintains layered long and short positions, optionally increases volume geometrically
/// and supports profit/loss group exits together with hedge rebalancing.
/// </summary>
public class BuildYourGridStrategy : Strategy
{
	private enum OrderPlacementMode
	{
		Both,
		LongOnly,
		ShortOnly,
	}

	private enum GridDirectionMode
	{
		WithTrend,
		AgainstTrend,
	}

	private enum StepProgressionMode
	{
		Static,
		Geometric,
		Exponential,
	}

	private enum CloseTargetMode
	{
		Pips,
		Currency,
	}

	private enum LossCloseMode
	{
		DoNothing,
		CloseFirst,
		CloseAll,
	}

	private enum LotProgressionMode
	{
		Static,
		Geometric,
		Exponential,
	}

	private sealed class PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; set; }

		public decimal Volume { get; set; }
	}

	private readonly StrategyParam<OrderPlacementMode> _orderPlacement;
	private readonly StrategyParam<GridDirectionMode> _gridDirection;
	private readonly StrategyParam<decimal> _pipsForNextOrder;
	private readonly StrategyParam<StepProgressionMode> _stepProgression;
	private readonly StrategyParam<CloseTargetMode> _closeTargetMode;
	private readonly StrategyParam<decimal> _pipsCloseInProfit;
	private readonly StrategyParam<decimal> _currencyCloseInProfit;
	private readonly StrategyParam<LossCloseMode> _lossCloseMode;
	private readonly StrategyParam<decimal> _pipsForCloseInLoss;
	private readonly StrategyParam<bool> _placeHedgeOrder;
	private readonly StrategyParam<decimal> _hedgeLossThreshold;
	private readonly StrategyParam<decimal> _hedgeVolumeMultiplier;
	private readonly StrategyParam<bool> _autoLotSize;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualLotSize;
	private readonly StrategyParam<LotProgressionMode> _lotProgression;
	private readonly StrategyParam<decimal> _maxMultiplierLot;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _maxSpread;
	private readonly StrategyParam<bool> _useCompletedBar;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;
	private decimal _pointSize;
	private decimal _priceStep;
	private decimal _stepPrice;
	private bool _barReady;

	private int _totalOrders;
	private int _buyOrders;
	private int _sellOrders;
	private decimal _totalLongVolume;
	private decimal _totalShortVolume;
	private decimal _buyProfit;
	private decimal _sellProfit;
	private decimal _buyPips;
	private decimal _sellPips;
	private decimal? _lastBuyPrice;
	private decimal? _lastSellPrice;
	private decimal _firstBuyVolume;
	private decimal _firstSellVolume;
	private decimal _lastBuyVolume;
	private decimal _lastSellVolume;
	private bool _isHedged;

	/// <summary>
	/// Controls whether the strategy may open both directions or a single side only.
	/// </summary>
	public OrderPlacementMode OrderPlacement
	{
		get => _orderPlacement.Value;
		set => _orderPlacement.Value = value;
	}

	/// <summary>
	/// Determines if new grid layers follow the trend or fade the movement.
	/// </summary>
	public GridDirectionMode GridDirection
	{
		get => _gridDirection.Value;
		set => _gridDirection.Value = value;
	}

	/// <summary>
	/// Base distance between consecutive grid layers measured in pips.
	/// </summary>
	public decimal PipsForNextOrder
	{
		get => _pipsForNextOrder.Value;
		set => _pipsForNextOrder.Value = value;
	}

	/// <summary>
	/// Defines how the grid step evolves with each new order.
	/// </summary>
	public StepProgressionMode StepProgression
	{
		get => _stepProgression.Value;
		set => _stepProgression.Value = value;
	}

	/// <summary>
	/// Target type used for closing the basket in profit.
	/// </summary>
	public CloseTargetMode CloseTarget
	{
		get => _closeTargetMode.Value;
		set => _closeTargetMode.Value = value;
	}

	/// <summary>
	/// Profit target expressed in pips.
	/// </summary>
	public decimal PipsCloseInProfit
	{
		get => _pipsCloseInProfit.Value;
		set => _pipsCloseInProfit.Value = value;
	}

	/// <summary>
	/// Profit target expressed in account currency.
	/// </summary>
	public decimal CurrencyCloseInProfit
	{
		get => _currencyCloseInProfit.Value;
		set => _currencyCloseInProfit.Value = value;
	}

	/// <summary>
	/// Defines how the basket is closed when the floating loss limit is reached.
	/// </summary>
	public LossCloseMode LossMode
	{
		get => _lossCloseMode.Value;
		set => _lossCloseMode.Value = value;
	}

	/// <summary>
	/// Maximal allowed loss in pips before defensive actions.
	/// </summary>
	public decimal PipsForCloseInLoss
	{
		get => _pipsForCloseInLoss.Value;
		set => _pipsForCloseInLoss.Value = value;
	}

	/// <summary>
	/// Enables hedge orders when losses exceed a percentage of the balance.
	/// </summary>
	public bool PlaceHedgeOrder
	{
		get => _placeHedgeOrder.Value;
		set => _placeHedgeOrder.Value = value;
	}

	/// <summary>
	/// Loss percentage of the balance that triggers hedging.
	/// </summary>
	public decimal HedgeLossThreshold
	{
		get => _hedgeLossThreshold.Value;
		set => _hedgeLossThreshold.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the imbalance volume when hedging.
	/// </summary>
	public decimal HedgeVolumeMultiplier
	{
		get => _hedgeVolumeMultiplier.Value;
		set => _hedgeVolumeMultiplier.Value = value;
	}

	/// <summary>
	/// Uses balance based sizing when enabled.
	/// </summary>
	public bool AutoLotSize
	{
		get => _autoLotSize.Value;
		set => _autoLotSize.Value = value;
	}

	/// <summary>
	/// Risk factor used for automatic volume calculation.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Manual order volume when automatic sizing is disabled.
	/// </summary>
	public decimal ManualLotSize
	{
		get => _manualLotSize.Value;
		set => _manualLotSize.Value = value;
	}

	/// <summary>
	/// Controls how the lot size grows with new orders.
	/// </summary>
	public LotProgressionMode LotProgression
	{
		get => _lotProgression.Value;
		set => _lotProgression.Value = value;
	}

	/// <summary>
	/// Caps the lot size to a multiple of the first entry.
	/// </summary>
	public decimal MaxMultiplierLot
	{
		get => _maxMultiplierLot.Value;
		set => _maxMultiplierLot.Value = value;
	}

	/// <summary>
	/// Maximum amount of simultaneous orders (0 means unlimited).
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Maximum acceptable spread expressed in pips.
	/// </summary>
	public decimal MaxSpread
	{
		get => _maxSpread.Value;
		set => _maxSpread.Value = value;
	}

	/// <summary>
	/// Processes signals only when a bar closes when enabled.
	/// </summary>
	public bool UseCompletedBar
	{
		get => _useCompletedBar.Value;
		set => _useCompletedBar.Value = value;
	}

	/// <summary>
	/// Candle type used when bar completion mode is active.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BuildYourGridStrategy"/> class.
	/// </summary>
	public BuildYourGridStrategy()
	{
		_orderPlacement = Param(nameof(OrderPlacement), OrderPlacementMode.Both)
			.SetDisplay("Order Placement", "Allowed entry direction", "General");

		_gridDirection = Param(nameof(GridDirection), GridDirectionMode.AgainstTrend)
			.SetDisplay("Grid Direction", "Whether layers follow or fade the trend", "Grid");

		_pipsForNextOrder = Param(nameof(PipsForNextOrder), 50m)
			.SetDisplay("Grid Step (pips)", "Base spacing between grid levels", "Grid")
			.SetGreaterThanZero();

		_stepProgression = Param(nameof(StepProgression), StepProgressionMode.Geometric)
			.SetDisplay("Step Progression", "How the distance grows with each layer", "Grid");

		_closeTargetMode = Param(nameof(CloseTarget), CloseTargetMode.Pips)
			.SetDisplay("Close Target", "Profit target type", "Risk");

		_pipsCloseInProfit = Param(nameof(PipsCloseInProfit), 10m)
			.SetDisplay("Target (pips)", "Basket profit target in pips", "Risk")
			.SetGreaterThanZero();

		_currencyCloseInProfit = Param(nameof(CurrencyCloseInProfit), 10m)
			.SetDisplay("Target (currency)", "Basket profit target in account currency", "Risk")
			.SetGreaterThanZero();

		_lossCloseMode = Param(nameof(LossMode), LossCloseMode.CloseAll)
			.SetDisplay("Loss Handling", "Action when the loss threshold is hit", "Risk");

		_pipsForCloseInLoss = Param(nameof(PipsForCloseInLoss), 100m)
			.SetDisplay("Loss (pips)", "Allowed drawdown before protective close", "Risk")
			.SetGreaterThanZero();

		_placeHedgeOrder = Param(nameof(PlaceHedgeOrder), false)
			.SetDisplay("Use Hedge", "Enable hedge rebalancing", "Risk");

		_hedgeLossThreshold = Param(nameof(HedgeLossThreshold), 10m)
			.SetDisplay("Hedge Threshold (%)", "Loss percentage that triggers hedging", "Risk")
			.SetGreaterThanZero();

		_hedgeVolumeMultiplier = Param(nameof(HedgeVolumeMultiplier), 1m)
			.SetDisplay("Hedge Multiplier", "Multiplier applied to imbalance volume", "Risk")
			.SetGreaterThanZero();

		_autoLotSize = Param(nameof(AutoLotSize), false)
			.SetDisplay("Auto Volume", "Use balance driven order size", "Volume");

		_riskFactor = Param(nameof(RiskFactor), 1m)
			.SetDisplay("Risk Factor", "Risk factor for automatic sizing", "Volume")
			.SetGreaterThanZero();

		_manualLotSize = Param(nameof(ManualLotSize), 0.01m)
			.SetDisplay("Manual Volume", "Order size when auto sizing is disabled", "Volume")
			.SetGreaterThanZero();

		_lotProgression = Param(nameof(LotProgression), LotProgressionMode.Static)
			.SetDisplay("Lot Progression", "How volumes scale with each layer", "Volume");

		_maxMultiplierLot = Param(nameof(MaxMultiplierLot), 50m)
			.SetDisplay("Max Multiplier", "Cap for lot growth relative to the first entry", "Volume")
			.SetGreaterThanZero();

		_maxOrders = Param(nameof(MaxOrders), 0)
			.SetDisplay("Max Orders", "Maximum simultaneous positions (0 = unlimited)", "General")
			.SetMinMax(0, 1000);

		_maxSpread = Param(nameof(MaxSpread), 0m)
			.SetDisplay("Max Spread", "Maximum allowed spread in pips (0 = ignore)", "General")
			.SetMin(0m);

		_useCompletedBar = Param(nameof(UseCompletedBar), false)
			.SetDisplay("Use Completed Bar", "Process signals only after a candle closes", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for bar completion", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);

		if (UseCompletedBar)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longEntries.Clear();
		_shortEntries.Clear();
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;
		_pointSize = 0m;
		_priceStep = 0m;
		_stepPrice = 0m;
		_barReady = !UseCompletedBar;
		_totalOrders = 0;
		_buyOrders = 0;
		_sellOrders = 0;
		_totalLongVolume = 0m;
		_totalShortVolume = 0m;
		_buyProfit = 0m;
		_sellProfit = 0m;
		_buyPips = 0m;
		_sellPips = 0m;
		_lastBuyPrice = null;
		_lastSellPrice = null;
		_firstBuyVolume = 0m;
		_firstSellVolume = 0m;
		_lastBuyVolume = 0m;
		_lastSellVolume = 0m;
		_isHedged = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();
		_priceStep = Security?.PriceStep ?? 0m;
		_stepPrice = Security?.StepPrice ?? 0m;
		_barReady = !UseCompletedBar;

		StartProtection();

		if (UseCompletedBar)
		{
			SubscribeCandles(CandleType)
				.Bind(ProcessCandle)
				.Start();
		}

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barReady = true;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = (decimal)bidObj;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBestBid = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = (decimal)askObj;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasBestAsk = true;
			}
		}

		if (_hasBestBid && _hasBestAsk)
			ProcessPrices();
	}

	private void ProcessPrices()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseCompletedBar && !_barReady)
			return;

		try
		{
			var spread = _bestAsk - _bestBid;
			var spreadPips = _pointSize > 0m ? spread / _pointSize : 0m;

			if (MaxSpread > 0m && spreadPips > MaxSpread)
				return;

			UpdateAggregates();

			if (_totalOrders > 0)
			{
				if (ShouldCloseInProfit())
				{
					if (CloseAllPositions())
						return;
				}

				if (LossMode != LossCloseMode.DoNothing && ShouldCloseInLoss())
				{
					var closed = LossMode == LossCloseMode.CloseFirst ? CloseFirstPositions() : CloseAllPositions();
					if (closed)
						return;
				}

				if (ShouldHedge())
				{
					if (ExecuteHedgeOrder())
						return;
				}
			}

			if (TryOpenInitialOrders())
				return;

			TryOpenNextOrders();
		}
		finally
		{
			if (UseCompletedBar)
				_barReady = false;
		}
	}

	private void UpdateAggregates()
	{
		_totalOrders = _longEntries.Count + _shortEntries.Count;
		_buyOrders = _longEntries.Count;
		_sellOrders = _shortEntries.Count;

		_totalLongVolume = 0m;
		_totalShortVolume = 0m;
		_buyProfit = 0m;
		_sellProfit = 0m;
		_buyPips = 0m;
		_sellPips = 0m;
		_lastBuyPrice = null;
		_lastSellPrice = null;

		foreach (var entry in _longEntries)
		{
			_totalLongVolume += entry.Volume;
			_lastBuyPrice = entry.Price;

			var diff = _bestBid - entry.Price;
			_buyProfit += CalculateProfit(diff, entry.Volume);
			_buyPips += CalculatePips(diff);
		}

		foreach (var entry in _shortEntries)
		{
			_totalShortVolume += entry.Volume;
			_lastSellPrice = entry.Price;

			var diff = entry.Price - _bestAsk;
			_sellProfit += CalculateProfit(diff, entry.Volume);
			_sellPips += CalculatePips(diff);
		}

		_firstBuyVolume = _longEntries.Count > 0 ? _longEntries[0].Volume : 0m;
		_firstSellVolume = _shortEntries.Count > 0 ? _shortEntries[0].Volume : 0m;
		_lastBuyVolume = _longEntries.Count > 0 ? _longEntries[^1].Volume : 0m;
		_lastSellVolume = _shortEntries.Count > 0 ? _shortEntries[^1].Volume : 0m;

		_isHedged = _buyOrders > 1 && _sellOrders > 1 && _totalLongVolume == _totalShortVolume && _totalLongVolume > 0m;
	}

	private decimal CalculateProfit(decimal diff, decimal volume)
	{
		if (_priceStep > 0m && _stepPrice > 0m)
			return diff / _priceStep * _stepPrice * volume;

		return diff * volume;
	}

	private decimal CalculatePips(decimal diff)
	{
		if (_pointSize > 0m)
			return diff / _pointSize;

		return diff;
	}

	private bool ShouldCloseInProfit()
	{
		return CloseTarget switch
		{
			CloseTargetMode.Pips => (_buyPips + _sellPips) >= PipsCloseInProfit,
			CloseTargetMode.Currency => (_buyProfit + _sellProfit) >= CurrencyCloseInProfit,
			_ => false,
		};
	}

	private bool ShouldCloseInLoss()
	{
		return (_buyPips + _sellPips) <= -PipsForCloseInLoss;
	}

	private bool CloseAllPositions()
	{
		var closed = false;

		if (_buyOrders > 0)
		{
			var volume = _totalLongVolume;
			if (volume > 0m)
			{
				SellMarket(volume);
				closed = true;
			}
		}

		if (_sellOrders > 0)
		{
			var volume = _totalShortVolume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				closed = true;
			}
		}

		return closed;
	}

	private bool CloseFirstPositions()
	{
		var closed = false;

		if (_buyOrders > 0)
		{
			var volume = _longEntries[0].Volume;
			if (volume > 0m)
			{
				SellMarket(volume);
				closed = true;
			}
		}

		if (_sellOrders > 0)
		{
			var volume = _shortEntries[0].Volume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				closed = true;
			}
		}

		return closed;
	}

	private bool ShouldHedge()
	{
		if (!PlaceHedgeOrder || HedgeLossThreshold <= 0m)
			return false;

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (balance <= 0m)
			return false;

		var floating = _buyProfit + _sellProfit;
		if (floating >= 0m)
			return false;

		var lossPercent = Math.Abs(floating) * 100m / balance;
		return lossPercent >= HedgeLossThreshold && !_isHedged;
	}

	private bool ExecuteHedgeOrder()
	{
		var imbalance = _totalLongVolume - _totalShortVolume;
		if (imbalance == 0m)
			return false;

		if (imbalance < 0m)
		{
			var volume = NormalizeVolume(Math.Abs(imbalance) * HedgeVolumeMultiplier);
			if (volume <= 0m)
				return false;

			BuyMarket(volume);
			return true;
		}

		var sellVolume = NormalizeVolume(imbalance * HedgeVolumeMultiplier);
		if (sellVolume <= 0m)
			return false;

		SellMarket(sellVolume);
		return true;
	}

	private bool TryOpenInitialOrders()
	{
		if (!CanOpenMoreOrders())
			return false;

		if (_buyOrders == 0 && (OrderPlacement == OrderPlacementMode.Both || OrderPlacement == OrderPlacementMode.LongOnly))
		{
			var volume = GetOrderVolume(Sides.Buy);
			if (SendMarketOrder(Sides.Buy, volume))
				return true;
		}

		if (_sellOrders == 0 && (OrderPlacement == OrderPlacementMode.Both || OrderPlacement == OrderPlacementMode.ShortOnly))
		{
			var volume = GetOrderVolume(Sides.Sell);
			if (SendMarketOrder(Sides.Sell, volume))
				return true;
		}

		return false;
	}

	private void TryOpenNextOrders()
	{
		if (!CanOpenMoreOrders())
			return;

		var allowBuy = OrderPlacement != OrderPlacementMode.ShortOnly;
		var allowSell = OrderPlacement != OrderPlacementMode.LongOnly;

		if (!allowBuy && !allowSell)
			return;

		if ((_buyOrders > 0 || OrderPlacement == OrderPlacementMode.ShortOnly)
			&& (_sellOrders > 0 || OrderPlacement == OrderPlacementMode.LongOnly))
		{
			var buyDistance = allowBuy ? GetNextDistance(Sides.Buy) : 0m;
			var sellDistance = allowSell ? GetNextDistance(Sides.Sell) : 0m;

			if (GridDirection == GridDirectionMode.WithTrend)
			{
				if (allowBuy && _lastBuyPrice.HasValue && buyDistance > 0m)
				{
					var trigger = _lastBuyPrice.Value + buyDistance;
					if (_bestAsk >= trigger)
					{
						var volume = GetOrderVolume(Sides.Buy);
						if (SendMarketOrder(Sides.Buy, volume))
							return;
					}
				}

				if (allowSell && _lastSellPrice.HasValue && sellDistance > 0m)
				{
					var trigger = _lastSellPrice.Value - sellDistance;
					if (_bestBid <= trigger)
					{
						var volume = GetOrderVolume(Sides.Sell);
						if (SendMarketOrder(Sides.Sell, volume))
							return;
					}
				}
			}
			else
			{
				if (allowBuy && _lastBuyPrice.HasValue && buyDistance > 0m)
				{
					var trigger = _lastBuyPrice.Value - buyDistance;
					if (_bestAsk <= trigger)
					{
						var volume = GetOrderVolume(Sides.Buy);
						if (SendMarketOrder(Sides.Buy, volume))
							return;
					}
				}

				if (allowSell && _lastSellPrice.HasValue && sellDistance > 0m)
				{
					var trigger = _lastSellPrice.Value + sellDistance;
					if (_bestBid >= trigger)
					{
						var volume = GetOrderVolume(Sides.Sell);
						if (SendMarketOrder(Sides.Sell, volume))
							return;
					}
				}
			}
		}
	}

	private bool CanOpenMoreOrders()
	{
		if (MaxOrders <= 0)
			return true;

		return _totalOrders < MaxOrders;
	}

	private decimal GetNextDistance(Sides side)
	{
		var baseDistance = PipsForNextOrder;
		var count = side == Sides.Buy ? _buyOrders : _sellOrders;

		var multiplier = StepProgression switch
		{
			StepProgressionMode.Static => 1m,
			StepProgressionMode.Geometric => Math.Max(1, count),
			StepProgressionMode.Exponential => count <= 0 ? 1m : (decimal)Math.Max(1, Math.Pow(2, count - 1)),
			_ => 1m,
		};

		return baseDistance * multiplier * _pointSize;
	}

	private decimal GetOrderVolume(Sides side)
	{
		var baseVolume = GetBaseVolume();
		var firstVolume = side == Sides.Buy ? _firstBuyVolume : _firstSellVolume;
		var lastVolume = side == Sides.Buy ? _lastBuyVolume : _lastSellVolume;
		var orders = side == Sides.Buy ? _buyOrders : _sellOrders;
		decimal result;

		switch (LotProgression)
		{
			case LotProgressionMode.Static:
				result = orders == 0 ? baseVolume : (firstVolume > 0m ? firstVolume : baseVolume);
				break;
			case LotProgressionMode.Geometric:
				if (orders == 0)
				{
					result = baseVolume;
				}
				else if (orders == 1)
				{
					result = lastVolume * 2m;
				}
				else
				{
					result = lastVolume + (firstVolume > 0m ? firstVolume : baseVolume);
				}
				break;
			case LotProgressionMode.Exponential:
				result = orders == 0 ? baseVolume : lastVolume * 2m;
				break;
			default:
				result = baseVolume;
				break;
		}

		if (MaxMultiplierLot > 0m && orders > 0 && firstVolume > 0m)
		{
			var cap = firstVolume * MaxMultiplierLot;
			if (result > cap)
				result = cap;
		}

		return NormalizeVolume(result);
	}

	private decimal GetBaseVolume()
	{
		decimal volume;

		if (AutoLotSize)
		{
			var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			volume = balance > 0m ? balance * RiskFactor / 100000m : ManualLotSize;
		}
		else
		{
			volume = ManualLotSize;
		}

		return NormalizeVolume(volume);
	}

	private bool SendMarketOrder(Sides side, decimal volume)
	{
		if (volume <= 0m)
			return false;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		return true;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var min = Security?.VolumeMin ?? 0m;
		var max = Security?.VolumeMax ?? 0m;
		var step = Security?.VolumeStep ?? 0m;

		if (step > 0m)
			volume = Math.Round(volume / step, 0, MidpointRounding.AwayFromZero) * step;

		if (min > 0m)
			volume = Math.Max(volume, min);

		if (max > 0m)
			volume = Math.Min(volume, max);

		return volume;
	}

	private decimal CalculatePointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null || trade.Order.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			var remainder = ReduceEntries(_shortEntries, volume);
			if (remainder > 0m)
				_longEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var remainder = ReduceEntries(_longEntries, volume);
			if (remainder > 0m)
				_shortEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
		}
	}

	private static decimal ReduceEntries(List<PositionEntry> entries, decimal volume)
	{
		var remaining = volume;

		while (remaining > 0m && entries.Count > 0)
		{
			var entry = entries[0];
			var used = Math.Min(entry.Volume, remaining);
			entry.Volume -= used;
			remaining -= used;

			if (entry.Volume <= 0m)
				entries.RemoveAt(0);
		}

		return remaining;
	}
}

