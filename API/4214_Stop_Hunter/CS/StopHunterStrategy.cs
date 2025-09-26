using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "Stop Hunter" expert advisor using StockSharp high-level API.
/// </summary>
public class StopHunterStrategy : Strategy
{
	private readonly StrategyParam<int> _zeroes;
	private readonly StrategyParam<int> _distancePoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<bool> _enableLongOrders;
	private readonly StrategyParam<bool> _enableShortOrders;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<int> _maxLongPositions;
	private readonly StrategyParam<int> _maxShortPositions;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _exitOrder;
	private decimal? _buyRoundLevel;
	private decimal? _sellRoundLevel;
	private bool _secondTrade;
	private int _takeProfitExtension;
	private int _stopLossExtension;
	private decimal _lastPosition;
	private decimal? _entryPrice;
	private decimal _entryVolume;
	private ExitAction _pendingExitAction;

	private enum ExitAction
	{
		None,
		PartialLong,
		FullLong,
		PartialShort,
		FullShort
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StopHunterStrategy"/> class.
	/// </summary>
	public StopHunterStrategy()
	{
		_zeroes = Param(nameof(Zeroes), 2)
			.SetRange(1, 5)
			.SetDisplay("Zero digits", "Number of decimal digits considered when searching for round levels", "Levels")
			.SetCanOptimize(true);

		_distancePoints = Param(nameof(DistancePoints), 15)
			.SetGreaterThanZero()
			.SetDisplay("Distance (points)", "Offset in points from the round price used for pending orders", "Levels")
			.SetCanOptimize(true)
			.SetOptimize(5, 150, 5);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 15)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (points)", "Hidden take-profit distance expressed in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 200, 5);

		_stopLossPoints = Param(nameof(StopLossPoints), 15)
			.SetGreaterThanZero()
			.SetDisplay("Stop loss (points)", "Hidden stop-loss distance expressed in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 200, 5);

		_enableLongOrders = Param(nameof(EnableLongOrders), true)
			.SetDisplay("Enable long side", "Allow the strategy to place buy-stop orders", "Trading");

		_enableShortOrders = Param(nameof(EnableShortOrders), true)
			.SetDisplay("Enable short side", "Allow the strategy to place sell-stop orders", "Trading");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Risk percent", "Percentage of capital converted into volume: volume = balance / 100000 * percent", "Money management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 15m, 1m);

		_minVolume = Param(nameof(MinimumVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum volume", "Lower bound for generated order volume", "Money management");

		_maxVolume = Param(nameof(MaximumVolume), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum volume", "Upper bound for generated order volume", "Money management");

		_maxLongPositions = Param(nameof(MaxLongPositions), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max long positions", "Maximum number of long entries that may coexist", "Trading");

		_maxShortPositions = Param(nameof(MaxShortPositions), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max short positions", "Maximum number of short entries that may coexist", "Trading");

		ResetState();
	}

	/// <summary>
	/// Number of decimal digits considered when computing round price levels.
	/// </summary>
	public int Zeroes
	{
		get => _zeroes.Value;
		set => _zeroes.Value = value;
	}

	/// <summary>
	/// Distance in points between the round price and the pending order.
	/// </summary>
	public int DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Hidden take-profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Hidden stop-loss distance expressed in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables placement of buy-stop orders.
	/// </summary>
	public bool EnableLongOrders
	{
		get => _enableLongOrders.Value;
		set => _enableLongOrders.Value = value;
	}

	/// <summary>
	/// Enables placement of sell-stop orders.
	/// </summary>
	public bool EnableShortOrders
	{
		get => _enableShortOrders.Value;
		set => _enableShortOrders.Value = value;
	}

	/// <summary>
	/// Percentage of account capital converted to volume when computing order size.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Minimum order volume allowed by the strategy.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Maximum order volume allowed by the strategy.
	/// </summary>
	public decimal MaximumVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open long trades.
	/// </summary>
	public int MaxLongPositions
	{
		get => _maxLongPositions.Value;
		set => _maxLongPositions.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open short trades.
	/// </summary>
	public int MaxShortPositions
	{
		get => _maxShortPositions.Value;
		set => _maxShortPositions.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
			_bestBid = Convert.ToDecimal(bidValue);

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
			_bestAsk = Convert.ToDecimal(askValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
			return;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var roundStep = step * GetRoundMultiplier();
		if (roundStep <= 0m)
			roundStep = step;

		var distanceOffset = DistancePoints * step;
		var buyLevel = Math.Ceiling(bid / roundStep) * roundStep;

		if (buyLevel - distanceOffset <= ask)
			buyLevel += roundStep;

		var sellLevel = buyLevel - roundStep;

		if (sellLevel + distanceOffset >= bid)
			sellLevel -= roundStep;

		ManagePendingOrders(buyLevel, sellLevel, bid, ask, roundStep, distanceOffset);
		ManageOpenPosition(bid, ask, step);
	}

	private void ManagePendingOrders(decimal buyLevel, decimal sellLevel, decimal bid, decimal ask, decimal roundStep, decimal distanceOffset)
	{
		var flushDistance = distanceOffset * 50m;

		if (_buyRoundLevel is decimal previousBuy)
		{
			var shouldCancel = buyLevel > previousBuy || ask < previousBuy - roundStep - flushDistance;

			if (shouldCancel)
			{
				CancelIfActive(ref _buyStopOrder);
				_buyRoundLevel = null;
			}
		}

		if (_sellRoundLevel is decimal previousSell)
		{
			var shouldCancel = sellLevel < previousSell || bid > previousSell + roundStep + flushDistance;

			if (shouldCancel)
			{
				CancelIfActive(ref _sellStopOrder);
				_sellRoundLevel = null;
			}
		}

		var slotsUsed = CountActiveSlots();
		var slotsLimit = MaxLongPositions + MaxShortPositions;

		if (slotsUsed >= slotsLimit)
			return;

		var volume = CalculateEntryVolume();
		if (volume <= 0m)
			return;

		if (EnableLongOrders && MaxLongPositions > 0 && _buyStopOrder == null && Position == 0m)
		{
			var price = buyLevel - distanceOffset;

			if (price > 0m)
			{
				_buyStopOrder = BuyStop(volume, price);
				_buyRoundLevel = buyLevel;
			}
		}

		if (EnableShortOrders && MaxShortPositions > 0 && _sellStopOrder == null && Position == 0m)
		{
			var price = sellLevel + distanceOffset;

			if (price > 0m)
			{
				_sellStopOrder = SellStop(volume, price);
				_sellRoundLevel = sellLevel;
			}
		}
	}

	private void ManageOpenPosition(decimal bid, decimal ask, decimal step)
	{
		if (Position > 0m)
		{
			if (_pendingExitAction != ExitAction.None)
				return;

			var entryPrice = Position.AveragePrice ?? _entryPrice;
			if (entryPrice is not decimal price)
				return;

			var takeProfitDistance = (TakeProfitPoints + _takeProfitExtension) * step;
			var stopLossDistance = (StopLossPoints + _stopLossExtension) * step;

			if (TakeProfitPoints > 0 && bid >= price + takeProfitDistance)
			{
				if (!_secondTrade)
				{
					var volume = CalculatePartialVolume(Position);

					if (volume > 0m)
					{
						_exitOrder = SellMarket(volume);
						_pendingExitAction = ExitAction.PartialLong;
					}
				}
				else
				{
					var volume = Math.Abs(Position);

					if (volume > 0m)
					{
						_exitOrder = SellMarket(volume);
						_pendingExitAction = ExitAction.FullLong;
					}
				}
			}
			else if (StopLossPoints > 0 && ask <= price - stopLossDistance)
			{
				var volume = Math.Abs(Position);

				if (volume > 0m)
				{
					_exitOrder = SellMarket(volume);
					_pendingExitAction = ExitAction.FullLong;
				}
			}
		}
		else if (Position < 0m)
		{
			if (_pendingExitAction != ExitAction.None)
				return;

			var entryPrice = Position.AveragePrice ?? _entryPrice;
			if (entryPrice is not decimal price)
				return;

			var takeProfitDistance = (TakeProfitPoints + _takeProfitExtension) * step;
			var stopLossDistance = (StopLossPoints + _stopLossExtension) * step;

			if (TakeProfitPoints > 0 && ask <= price - takeProfitDistance)
			{
				if (!_secondTrade)
				{
					var volume = CalculatePartialVolume(Position);

					if (volume > 0m)
					{
						_exitOrder = BuyMarket(volume);
						_pendingExitAction = ExitAction.PartialShort;
					}
				}
				else
				{
					var volume = Math.Abs(Position);

					if (volume > 0m)
					{
						_exitOrder = BuyMarket(volume);
						_pendingExitAction = ExitAction.FullShort;
					}
				}
			}
			else if (StopLossPoints > 0 && bid >= price + stopLossDistance)
			{
				var volume = Math.Abs(Position);

				if (volume > 0m)
				{
					_exitOrder = BuyMarket(volume);
					_pendingExitAction = ExitAction.FullShort;
				}
			}
		}
	}

	private decimal CalculateEntryVolume()
	{
		var volume = 0m;
		var capital = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

		if (capital > 0m && RiskPercent > 0m)
			volume = capital / 100000m * RiskPercent;

		if (volume <= 0m)
			volume = MinimumVolume;

		volume = RoundVolume(volume);

		if (volume < MinimumVolume)
			volume = MinimumVolume;

		if (MaximumVolume > 0m && volume > MaximumVolume)
			volume = MaximumVolume;

		return volume;
	}

	private decimal RoundVolume(decimal volume)
	{
		var step = Security?.VolumeStep;

		if (step is > 0m)
		{
			var multiplier = Math.Max(1m, Math.Round(volume / step.Value, MidpointRounding.AwayFromZero));
			volume = multiplier * step.Value;
		}
		else
		{
			var decimals = MinimumVolume < 0.1m ? 2 : MinimumVolume < 1m ? 1 : 0;
			volume = Math.Round(volume, decimals, MidpointRounding.AwayFromZero);
		}

		return Math.Max(volume, 0m);
	}

	private decimal CalculatePartialVolume(decimal currentPosition)
	{
		var absolute = Math.Abs(currentPosition);

		if (absolute <= 0m)
			return 0m;

		var half = absolute / 2m;
		var volume = RoundVolume(half);

		if (volume <= 0m)
			volume = Security?.VolumeStep ?? MinimumVolume;

		if (volume > absolute)
			volume = absolute;

		return Math.Max(volume, 0m);
	}

	private decimal GetRoundMultiplier()
	{
		var multiplier = 1m;

		for (var i = 0; i < Zeroes; i++)
			multiplier *= 10m;

		return multiplier;
	}

	private int CountActiveSlots()
	{
		var total = 0;

		if (Position > 0m || Position < 0m)
			total++;

		if (IsOrderActive(_buyStopOrder))
			total++;

		if (IsOrderActive(_sellStopOrder))
			total++;

		return total;
	}

	private static bool IsOrderActive(Order order)
	{
		if (order == null)
			return false;

		return order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;
	}

	private void CancelIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
			return;

		if (_buyStopOrder != null && order == _buyStopOrder)
		{
			_entryPrice = Position.AveragePrice ?? trade.Trade.Price;
			_entryVolume = Math.Abs(Position);
			_secondTrade = false;
			_takeProfitExtension = 0;
			_stopLossExtension = 0;
			_pendingExitAction = ExitAction.None;
		}
		else if (_sellStopOrder != null && order == _sellStopOrder)
		{
			_entryPrice = Position.AveragePrice ?? trade.Trade.Price;
			_entryVolume = Math.Abs(Position);
			_secondTrade = false;
			_takeProfitExtension = 0;
			_stopLossExtension = 0;
			_pendingExitAction = ExitAction.None;
		}
		else if (_exitOrder != null && order == _exitOrder)
		{
			switch (_pendingExitAction)
			{
				case ExitAction.PartialLong:
				case ExitAction.PartialShort:
					_secondTrade = true;
					_takeProfitExtension += TakeProfitPoints;
					_stopLossExtension += StopLossPoints;
					_entryVolume = Math.Abs(Position);
					break;
				case ExitAction.FullLong:
				case ExitAction.FullShort:
					_secondTrade = false;
					_takeProfitExtension = 0;
					_stopLossExtension = 0;
					if (Position == 0m)
						_entryPrice = null;
					_entryVolume = Math.Abs(Position);
					break;
			}

			_exitOrder = null;
			_pendingExitAction = ExitAction.None;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyStopOrder != null && order == _buyStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_buyStopOrder = null;
			_buyRoundLevel = null;
		}

		if (_sellStopOrder != null && order == _sellStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_sellStopOrder = null;
			_sellRoundLevel = null;
		}

		if (_exitOrder != null && order == _exitOrder && order.State is OrderStates.Failed or OrderStates.Canceled)
		{
			_exitOrder = null;
			_pendingExitAction = ExitAction.None;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var current = Position;

		if (current == 0m)
		{
			_secondTrade = false;
			_takeProfitExtension = 0;
			_stopLossExtension = 0;
			_entryPrice = null;
			_entryVolume = 0m;
			_exitOrder = null;
			_pendingExitAction = ExitAction.None;
		}
		else
		{
			if (_lastPosition == 0m)
			{
				_entryVolume = Math.Abs(current);
				_entryPrice = Position.AveragePrice ?? _entryPrice;
				_secondTrade = false;
				_takeProfitExtension = 0;
				_stopLossExtension = 0;
			}

			if (current > 0m)
			{
				CancelIfActive(ref _sellStopOrder);
				_sellRoundLevel = null;
			}
			else if (current < 0m)
			{
				CancelIfActive(ref _buyStopOrder);
				_buyRoundLevel = null;
			}
		}

		_lastPosition = current;
	}

	private void ResetState()
	{
		_bestBid = null;
		_bestAsk = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_exitOrder = null;
		_buyRoundLevel = null;
		_sellRoundLevel = null;
		_secondTrade = false;
		_takeProfitExtension = 0;
		_stopLossExtension = 0;
		_lastPosition = 0m;
		_entryPrice = null;
		_entryVolume = 0m;
		_pendingExitAction = ExitAction.None;
	}
}
