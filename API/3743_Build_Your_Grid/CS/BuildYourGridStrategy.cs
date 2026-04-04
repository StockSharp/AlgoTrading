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
	public enum OrderPlacementModes
	{
		Both,
		LongOnly,
		ShortOnly,
	}

	public enum GridDirectionModes
	{
		WithTrend,
		AgainstTrend,
	}

	public enum StepProgressionModes
	{
		Static,
		Geometric,
		Exponential,
	}

	public enum CloseTargetModes
	{
		Pips,
		Currency,
	}

	public enum LossCloseModes
	{
		DoNothing,
		CloseFirst,
		CloseAll,
	}

	public enum LotProgressionModes
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

	private readonly StrategyParam<OrderPlacementModes> _orderPlacement;
	private readonly StrategyParam<GridDirectionModes> _gridDirection;
	private readonly StrategyParam<decimal> _pipsForNextOrder;
	private readonly StrategyParam<StepProgressionModes> _stepProgression;
	private readonly StrategyParam<CloseTargetModes> _closeTargetMode;
	private readonly StrategyParam<decimal> _pipsCloseInProfit;
	private readonly StrategyParam<decimal> _currencyCloseInProfit;
	private readonly StrategyParam<LossCloseModes> _lossCloseMode;
	private readonly StrategyParam<decimal> _pipsForCloseInLoss;
	private readonly StrategyParam<bool> _placeHedgeOrder;
	private readonly StrategyParam<decimal> _hedgeLossThreshold;
	private readonly StrategyParam<decimal> _hedgeVolumeMultiplier;
	private readonly StrategyParam<bool> _autoLotSize;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualLotSize;
	private readonly StrategyParam<LotProgressionModes> _lotProgression;
	private readonly StrategyParam<decimal> _maxMultiplierLot;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _maxSpread;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal _currentPrice;
	private decimal _pointSize;
	private decimal _priceStep;
	private decimal _stepPrice;

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
	private int _cooldownBars;

	/// <summary>
	/// Controls whether the strategy may open both directions or a single side only.
	/// </summary>
	public OrderPlacementModes OrderPlacement
	{
		get => _orderPlacement.Value;
		set => _orderPlacement.Value = value;
	}

	/// <summary>
	/// Determines if new grid layers follow the trend or fade the movement.
	/// </summary>
	public GridDirectionModes GridDirection
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
	public StepProgressionModes StepProgression
	{
		get => _stepProgression.Value;
		set => _stepProgression.Value = value;
	}

	/// <summary>
	/// Target type used for closing the basket in profit.
	/// </summary>
	public CloseTargetModes CloseTarget
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
	public LossCloseModes LossMode
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
	public LotProgressionModes LotProgression
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
	/// Candle type used for price data.
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
		_orderPlacement = Param(nameof(OrderPlacement), OrderPlacementModes.LongOnly)
			.SetDisplay("Order Placement", "Allowed entry direction", "General");

		_gridDirection = Param(nameof(GridDirection), GridDirectionModes.AgainstTrend)
			.SetDisplay("Grid Direction", "Whether layers follow or fade the trend", "Grid");

		_pipsForNextOrder = Param(nameof(PipsForNextOrder), 500000m)
			.SetDisplay("Grid Step (pips)", "Base spacing between grid levels", "Grid")
			.SetGreaterThanZero();

		_stepProgression = Param(nameof(StepProgression), StepProgressionModes.Static)
			.SetDisplay("Step Progression", "How the distance grows with each layer", "Grid");

		_closeTargetMode = Param(nameof(CloseTarget), CloseTargetModes.Pips)
			.SetDisplay("Close Target", "Profit target type", "Risk");

		_pipsCloseInProfit = Param(nameof(PipsCloseInProfit), 500000m)
			.SetDisplay("Target (pips)", "Basket profit target in pips", "Risk")
			.SetGreaterThanZero();

		_currencyCloseInProfit = Param(nameof(CurrencyCloseInProfit), 10m)
			.SetDisplay("Target (currency)", "Basket profit target in account currency", "Risk")
			.SetGreaterThanZero();

		_lossCloseMode = Param(nameof(LossMode), LossCloseModes.DoNothing)
			.SetDisplay("Loss Handling", "Action when the loss threshold is hit", "Risk");

		_pipsForCloseInLoss = Param(nameof(PipsForCloseInLoss), 200000m)
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

		_lotProgression = Param(nameof(LotProgression), LotProgressionModes.Static)
			.SetDisplay("Lot Progression", "How volumes scale with each layer", "Volume");

		_maxMultiplierLot = Param(nameof(MaxMultiplierLot), 50m)
			.SetDisplay("Max Multiplier", "Cap for lot growth relative to the first entry", "Volume")
			.SetGreaterThanZero();

		_maxOrders = Param(nameof(MaxOrders), 2)
			.SetDisplay("Max Orders", "Maximum simultaneous positions (0 = unlimited)", "General")
			.SetRange(0, 1000);

		_maxSpread = Param(nameof(MaxSpread), 0m)
			.SetDisplay("Max Spread", "Maximum allowed spread in pips (0 = ignore)", "General")
			.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for price data", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longEntries.Clear();
		_shortEntries.Clear();
		_currentPrice = 0m;
		_pointSize = 0m;
		_priceStep = 0m;
		_stepPrice = 0m;
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
		_cooldownBars = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pointSize = CalculatePointSize();
		_priceStep = Security?.PriceStep ?? 0m;
		_stepPrice = GetSecurityValue<decimal?>(Level1Fields.StepPrice) ?? 0m;

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_currentPrice = candle.ClosePrice;
		if (_currentPrice <= 0m)
			return;

		ProcessPrices();
	}

	private void ProcessPrices()
	{
		if (_cooldownBars > 0)
		{
			_cooldownBars--;
			return;
		}

		UpdateAggregates();

		if (_totalOrders > 0)
		{
			if (ShouldCloseInProfit())
			{
				if (CloseAllPositions())
				{
					_cooldownBars = 200;
					return;
				}
			}

			if (LossMode != LossCloseModes.DoNothing && ShouldCloseInLoss())
			{
				var closed = LossMode == LossCloseModes.CloseFirst ? CloseFirstPositions() : CloseAllPositions();
				if (closed)
				{
					_cooldownBars = 200;
					return;
				}
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

			var diff = _currentPrice - entry.Price;
			_buyProfit += CalculateProfit(diff, entry.Volume);
			_buyPips += CalculatePips(diff);
		}

		foreach (var entry in _shortEntries)
		{
			_totalShortVolume += entry.Volume;
			_lastSellPrice = entry.Price;

			var diff = entry.Price - _currentPrice;
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
		var entries = _buyOrders + _sellOrders;
		if (entries <= 0)
			return false;

		return CloseTarget switch
		{
			CloseTargetModes.Pips => (_buyPips + _sellPips) / entries >= PipsCloseInProfit,
			CloseTargetModes.Currency => (_buyProfit + _sellProfit) >= CurrencyCloseInProfit,
			_ => false,
		};
	}

	private bool ShouldCloseInLoss()
	{
		var entries = _buyOrders + _sellOrders;
		if (entries <= 0)
			return false;

		return (_buyPips + _sellPips) / entries <= -PipsForCloseInLoss;
	}

	private bool CloseAllPositions()
	{
		var closed = false;

		if (_buyOrders > 0)
		{
			var volume = _totalLongVolume;
			_longEntries.Clear();
			if (volume > 0m)
			{
				SellMarket(volume);
				closed = true;
			}
		}

		if (_sellOrders > 0)
		{
			var volume = _totalShortVolume;
			_shortEntries.Clear();
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
			_longEntries.RemoveAt(0);
			if (volume > 0m)
			{
				SellMarket(volume);
				closed = true;
			}
		}

		if (_sellOrders > 0)
		{
			var volume = _shortEntries[0].Volume;
			_shortEntries.RemoveAt(0);
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

			_longEntries.Add(new PositionEntry(_currentPrice, volume));
			BuyMarket(volume);
			return true;
		}

		var sellVolume = NormalizeVolume(imbalance * HedgeVolumeMultiplier);
		if (sellVolume <= 0m)
			return false;

		_shortEntries.Add(new PositionEntry(_currentPrice, sellVolume));
		SellMarket(sellVolume);
		return true;
	}

	private bool TryOpenInitialOrders()
	{
		if (!CanOpenMoreOrders())
			return false;

		if (_buyOrders == 0 && (OrderPlacement == OrderPlacementModes.Both || OrderPlacement == OrderPlacementModes.LongOnly))
		{
			var volume = GetOrderVolume(Sides.Buy);
			if (SendMarketOrder(Sides.Buy, volume))
				return true;
		}

		if (_sellOrders == 0 && (OrderPlacement == OrderPlacementModes.Both || OrderPlacement == OrderPlacementModes.ShortOnly))
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

		var allowBuy = OrderPlacement != OrderPlacementModes.ShortOnly;
		var allowSell = OrderPlacement != OrderPlacementModes.LongOnly;

		if (!allowBuy && !allowSell)
			return;

		if ((_buyOrders > 0 || OrderPlacement == OrderPlacementModes.ShortOnly)
			&& (_sellOrders > 0 || OrderPlacement == OrderPlacementModes.LongOnly))
		{
			var buyDistance = allowBuy ? GetNextDistance(Sides.Buy) : 0m;
			var sellDistance = allowSell ? GetNextDistance(Sides.Sell) : 0m;

			if (GridDirection == GridDirectionModes.WithTrend)
			{
				if (allowBuy && _lastBuyPrice.HasValue && buyDistance > 0m)
				{
					var trigger = _lastBuyPrice.Value + buyDistance;
					if (_currentPrice >= trigger)
					{
						var volume = GetOrderVolume(Sides.Buy);
						if (SendMarketOrder(Sides.Buy, volume))
							return;
					}
				}

				if (allowSell && _lastSellPrice.HasValue && sellDistance > 0m)
				{
					var trigger = _lastSellPrice.Value - sellDistance;
					if (_currentPrice <= trigger)
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
					if (_currentPrice <= trigger)
					{
						var volume = GetOrderVolume(Sides.Buy);
						if (SendMarketOrder(Sides.Buy, volume))
							return;
					}
				}

				if (allowSell && _lastSellPrice.HasValue && sellDistance > 0m)
				{
					var trigger = _lastSellPrice.Value + sellDistance;
					if (_currentPrice >= trigger)
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
			StepProgressionModes.Static => 1m,
			StepProgressionModes.Geometric => Math.Max(1, count),
			StepProgressionModes.Exponential => count <= 0 ? 1m : (decimal)Math.Max(1, Math.Pow(2, count - 1)),
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
			case LotProgressionModes.Static:
				result = orders == 0 ? baseVolume : (firstVolume > 0m ? firstVolume : baseVolume);
				break;
			case LotProgressionModes.Geometric:
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
			case LotProgressionModes.Exponential:
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
		{
			_longEntries.Add(new PositionEntry(_currentPrice, volume));
			BuyMarket(volume);
		}
		else
		{
			_shortEntries.Add(new PositionEntry(_currentPrice, volume));
			SellMarket(volume);
		}

		return true;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var min = Security?.MinVolume ?? 0m;
		var max = Security?.MaxVolume ?? 0m;
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

}

