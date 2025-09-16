using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale grid that alternates long and short entries while doubling volume.
/// </summary>
public class MartinMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<int> _entryOffsetPoints;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private readonly HashSet<long> _processedOrders = new();

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _reversalOrder;

	private decimal _stepSize;
	private decimal _entryOffset;
	private decimal _lastTradePrice;
	private decimal _lastTradeVolume;
	private int _martingaleLevel;
	private Sides? _lastTradeSide;
	private bool _isClosing;

	/// <summary>
	/// Distance in points that defines when the next reversal is triggered.
	/// </summary>
	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Offset in points for the initial stop orders around the current price.
	/// </summary>
	public int EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
	}

	/// <summary>
	/// Aggregated profit required to close the entire martingale cycle.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor the price.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MartinMartingaleStrategy"/>.
	/// </summary>
	public MartinMartingaleStrategy()
	{
		_stepPoints = Param(nameof(StepPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Step (points)", "Distance multiplier for reversals", "General")
			.SetCanOptimize(true);

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Entry Offset (points)", "Offset for initial stop orders", "General")
			.SetCanOptimize(true);

		_profitTarget = Param(nameof(ProfitTarget), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Total profit to close all positions", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for price monitoring", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		CancelActiveOrders();
		ResetCycle();
		_isClosing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateStepSettings();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.State != OrderStates.Done &&
			order.State != OrderStates.Failed &&
			order.State != OrderStates.Cancelled)
		{
			return;
		}

		if (order == _buyStopOrder)
		{
			_buyStopOrder = null;
		}
		else if (order == _sellStopOrder)
		{
			_sellStopOrder = null;
		}
		else if (order == _reversalOrder)
		{
			_reversalOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Trade is null)
			return;

		var order = trade.Order;
		if (order is null)
			return;

		if (order.TransactionId == 0)
			return;

		if (!_processedOrders.Add(order.TransactionId))
			return;

		if (order != _buyStopOrder && order != _sellStopOrder && order != _reversalOrder)
			return;

		_lastTradePrice = trade.Trade.Price;

		var volume = order.Volume;
		if (volume <= 0m)
		{
			volume = trade.Trade.Volume;
		}

		if (volume > 0m)
		{
			_lastTradeVolume = volume;
		}

		_lastTradeSide = order.Direction;

		if (_martingaleLevel == 0)
		{
			_martingaleLevel = 1;
		}
		else
		{
			_martingaleLevel++;
		}

		if (order == _buyStopOrder && _sellStopOrder != null)
		{
			CancelOrderIfActive(ref _sellStopOrder);
		}
		else if (order == _sellStopOrder && _buyStopOrder != null)
		{
			CancelOrderIfActive(ref _buyStopOrder);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateStepSettings();

		if (_stepSize <= 0m || Volume <= 0m)
			return;

		var price = candle.ClosePrice;

		if (_isClosing)
		{
			if (Position == 0)
			{
				_isClosing = false;
				ResetCycle();
			}
			else
			{
				return;
			}
		}

		if (!_isClosing && Position == 0 && _martingaleLevel > 0)
		{
			ResetCycle();
		}

		var totalProfit = CalculateTotalProfit(price);
		if (ProfitTarget > 0m && totalProfit >= ProfitTarget && (Position != 0 || _martingaleLevel > 0))
		{
			_isClosing = true;
			CloseAll();
			CancelActiveOrders();
			return;
		}

		if (_martingaleLevel == 0 && Position == 0)
		{
			EnsureInitialOrders(price);
			return;
		}

		if (_lastTradeSide is null || _martingaleLevel == 0)
			return;

		if (IsOrderActive(_reversalOrder))
			return;

		var threshold = _martingaleLevel * _stepSize;

		if (_lastTradeSide == Sides.Buy)
		{
			if (price <= _lastTradePrice - threshold)
			{
				PlaceReversal(Sides.Sell);
			}
		}
		else
		{
			if (price >= _lastTradePrice + threshold)
			{
				PlaceReversal(Sides.Buy);
			}
		}
	}

	private void EnsureInitialOrders(decimal price)
	{
		if (_entryOffset <= 0m || price <= 0m)
			return;

		if (!IsOrderActive(_buyStopOrder))
		{
			_buyStopOrder = BuyStop(Volume, price + _entryOffset);
		}

		if (!IsOrderActive(_sellStopOrder))
		{
			_sellStopOrder = SellStop(Volume, price - _entryOffset);
		}
	}

	private void PlaceReversal(Sides side)
	{
		var nextVolume = GetNextVolume();
		if (nextVolume <= 0m)
			return;

		_reversalOrder = side == Sides.Buy
			? BuyMarket(nextVolume)
			: SellMarket(nextVolume);
	}

	private decimal GetNextVolume()
	{
		if (_martingaleLevel == 0 || _lastTradeVolume <= 0m)
		{
			return Volume;
		}

		return _lastTradeVolume * 2m;
	}

	private decimal CalculateTotalProfit(decimal price)
	{
		var unrealized = Position != 0 ? Position * (price - PositionPrice) : 0m;
		return PnL + unrealized;
	}

	private void UpdateStepSettings()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			priceStep = 1m;
		}

		_stepSize = StepPoints * priceStep;
		_entryOffset = EntryOffsetPoints * priceStep;
	}

	private void CancelActiveOrders()
	{
		CancelOrderIfActive(ref _buyStopOrder);
		CancelOrderIfActive(ref _sellStopOrder);
		CancelOrderIfActive(ref _reversalOrder);
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (IsOrderActive(order))
		{
			CancelOrder(order);
		}

		order = null;
	}

	private static bool IsOrderActive(Order order)
	{
		if (order == null)
		{
			return false;
		}

		return order.State == OrderStates.Active ||
			order.State == OrderStates.Pending ||
			order.State == OrderStates.PartiallyFilled ||
			order.State == OrderStates.None;
	}

	private void ResetCycle()
	{
		_martingaleLevel = 0;
		_lastTradePrice = 0m;
		_lastTradeVolume = 0m;
		_lastTradeSide = null;
		_processedOrders.Clear();
		_reversalOrder = null;
	}
}
