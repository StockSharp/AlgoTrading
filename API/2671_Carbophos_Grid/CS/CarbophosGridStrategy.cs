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
/// Grid strategy converted from the Carbophos MetaTrader 5 expert advisor.
/// Simulates symmetric grid levels and manages profit and loss on the aggregated position.
/// </summary>
public class CarbophosGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<int> _stepPips;
	private readonly StrategyParam<int> _ordersPerSide;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _entryPrice;
	private decimal _gridCenterPrice;
	private bool _gridPlaced;

	private readonly List<decimal> _buyLevels = new();
	private readonly List<decimal> _sellLevels = new();

	/// <summary>
	/// Floating profit level (in absolute price * volume) that triggers closing of all positions.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Maximum allowed floating loss before the grid is closed.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// Distance between grid levels expressed in pips.
	/// </summary>
	public int StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Number of limit orders to place above and below the market price.
	/// </summary>
	public int OrdersPerSide
	{
		get => _ordersPerSide.Value;
		set => _ordersPerSide.Value = value;
	}

	/// <summary>
	/// Volume for each grid level order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="CarbophosGridStrategy"/>.
	/// </summary>
	public CarbophosGridStrategy()
	{
		_profitTarget = Param(nameof(ProfitTarget), 50000m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Floating profit target in money", "Risk")
			.SetOptimize(100m, 1000m, 50m);

		_maxLoss = Param(nameof(MaxLoss), 100000m)
			.SetGreaterThanZero()
			.SetDisplay("Max Loss", "Maximum floating loss before closing", "Risk")
			.SetOptimize(50m, 500m, 25m);

		_stepPips = Param(nameof(StepPips), 50000)
			.SetGreaterThanZero()
			.SetDisplay("Step (pips)", "Distance between grid levels in pips", "Grid")
			.SetOptimize(10, 150, 10);

		_ordersPerSide = Param(nameof(OrdersPerSide), 3)
			.SetGreaterThanZero()
			.SetDisplay("Orders Per Side", "Number of pending orders on each side", "Grid")
			.SetOptimize(1, 10, 1);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for each pending order", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_entryPrice = null;
		_gridCenterPrice = 0m;
		_gridPlaced = false;
		_buyLevels.Clear();
		_sellLevels.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentPrice = candle.ClosePrice;

		// Check if any grid levels were hit by this candle
		CheckGridFills(candle);

		// Check profit/loss on position
		if (Position != 0 && _entryPrice is decimal entry)
		{
			var floatingPnL = (currentPrice - entry) * Position;

			if (floatingPnL >= ProfitTarget)
			{
				CloseAll("Profit target reached.");
				return;
			}

			if (floatingPnL <= -MaxLoss)
			{
				CloseAll("Maximum loss reached.");
				return;
			}
		}

		// Place grid if none is active
		if (!_gridPlaced || (Position == 0 && _buyLevels.Count == 0 && _sellLevels.Count == 0))
		{
			PlaceGrid(currentPrice);
		}
	}

	private void PlaceGrid(decimal centerPrice)
	{
		_buyLevels.Clear();
		_sellLevels.Clear();

		var stepSize = GetGridStep();
		if (stepSize <= 0m || centerPrice <= 0m)
			return;

		for (var i = 1; i <= OrdersPerSide; i++)
		{
			var offset = stepSize * i;
			var buyPrice = centerPrice - offset;
			var sellPrice = centerPrice + offset;

			if (buyPrice > 0m)
				_buyLevels.Add(buyPrice);

			_sellLevels.Add(sellPrice);
		}

		_gridCenterPrice = centerPrice;
		_gridPlaced = true;
	}

	private void CheckGridFills(ICandleMessage candle)
	{
		// Check buy levels (price goes down to the level)
		for (var i = _buyLevels.Count - 1; i >= 0; i--)
		{
			if (candle.LowPrice <= _buyLevels[i])
			{
				BuyMarket(OrderVolume);
				UpdateEntryPrice(_buyLevels[i], OrderVolume, true);
				_buyLevels.RemoveAt(i);
			}
		}

		// Check sell levels (price goes up to the level)
		for (var i = _sellLevels.Count - 1; i >= 0; i--)
		{
			if (candle.HighPrice >= _sellLevels[i])
			{
				SellMarket(OrderVolume);
				UpdateEntryPrice(_sellLevels[i], OrderVolume, false);
				_sellLevels.RemoveAt(i);
			}
		}
	}

	private void UpdateEntryPrice(decimal fillPrice, decimal volume, bool isBuy)
	{
		if (_entryPrice is not decimal existingEntry || Position == 0)
		{
			_entryPrice = fillPrice;
			return;
		}

		// Weighted average entry price calculation
		var existingPos = Position;
		var newPos = isBuy ? existingPos + volume : existingPos - volume;

		if (newPos == 0)
		{
			_entryPrice = null;
			return;
		}

		// Only update if adding to position in same direction
		if ((isBuy && existingPos > 0) || (!isBuy && existingPos < 0))
		{
			var totalVolume = Math.Abs(existingPos) + volume;
			_entryPrice = (existingEntry * Math.Abs(existingPos) + fillPrice * volume) / totalVolume;
		}
		else
		{
			// Reducing position - keep same entry price
			if (Math.Abs(newPos) > 0)
				_entryPrice = existingEntry;
			else
				_entryPrice = null;
		}
	}

	private void CloseAll(string reason)
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		_buyLevels.Clear();
		_sellLevels.Clear();
		_gridPlaced = false;
		_entryPrice = null;

		LogInfo(reason);
	}

	private decimal GetGridStep()
	{
		var security = Security;

		var priceStep = security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 0.01m;

		var decimals = security?.Decimals ?? 2;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;
		return StepPips * priceStep * multiplier;
	}
}
