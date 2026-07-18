using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending grid strategy converted from the MetaTrader 4 expert advisor "Pending_tread".
/// Maintains two independent ladders of limit orders above and below the market with configurable direction and spacing.
/// When price reaches a grid level, a market order is placed in the configured direction.
/// </summary>
public class PendingTreadStrategy : Strategy
{
	private readonly StrategyParam<decimal> _pipStep;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _ordersPerSide;
	private readonly StrategyParam<Sides> _aboveMarketSide;
	private readonly StrategyParam<Sides> _belowMarketSide;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _anchorPrice;
	private bool _initialized;
	private readonly List<decimal> _triggeredLevelsAbove = new();
	private readonly List<decimal> _triggeredLevelsBelow = new();
	private decimal _entryPrice;

	public PendingTreadStrategy()
	{
		_pipStep = Param(nameof(PipStep), 200000m)
			.SetGreaterThanZero()
			.SetDisplay("Grid step (pips)", "Distance between adjacent pending orders expressed in pips", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150000m)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (pips)", "Individual take-profit distance assigned to every pending order", "Trading");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Volume sent with each pending order", "Trading");

		_ordersPerSide = Param(nameof(OrdersPerSide), 2)
			.SetGreaterThanZero()
			.SetDisplay("Orders per side", "Maximum number of grid levels maintained above and below the anchor", "Trading");

		_aboveMarketSide = Param(nameof(AboveMarketSide), Sides.Buy)
			.SetDisplay("Above market side", "Type of orders triggered above the current price", "Orders");

		_belowMarketSide = Param(nameof(BelowMarketSide), Sides.Sell)
			.SetDisplay("Below market side", "Type of orders triggered below the current price", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle timeframe", "General");
	}

	public decimal PipStep
	{
		get => _pipStep.Value;
		set => _pipStep.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public int OrdersPerSide
	{
		get => _ordersPerSide.Value;
		set => _ordersPerSide.Value = value;
	}

	public Sides AboveMarketSide
	{
		get => _aboveMarketSide.Value;
		set => _aboveMarketSide.Value = value;
	}

	public Sides BelowMarketSide
	{
		get => _belowMarketSide.Value;
		set => _belowMarketSide.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_anchorPrice = 0m;
		_initialized = false;
		_triggeredLevelsAbove.Clear();
		_triggeredLevelsBelow.Clear();
		_entryPrice = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = GetPipSize();

		this
			.SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_initialized)
		{
			_anchorPrice = close;
			_initialized = true;
			return;
		}

		var distance = PipStep * _pipSize;
		if (distance <= 0m)
			return;

		var tpOffset = TakeProfitPips * _pipSize;

		// Check above-market grid levels
		for (var i = 1; i <= OrdersPerSide; i++)
		{
			var level = _anchorPrice + distance * i;

			if (_triggeredLevelsAbove.Contains(level))
				continue;

			if (close >= level)
			{
				_triggeredLevelsAbove.Add(level);
				ExecuteGridOrder(AboveMarketSide, close, tpOffset);
				return; // one order per candle
			}
		}

		// Check below-market grid levels
		for (var i = 1; i <= OrdersPerSide; i++)
		{
			var level = _anchorPrice - distance * i;

			if (_triggeredLevelsBelow.Contains(level))
				continue;

			if (close <= level)
			{
				_triggeredLevelsBelow.Add(level);
				ExecuteGridOrder(BelowMarketSide, close, tpOffset);
				return; // one order per candle
			}
		}

		// Check take-profit for existing position
		CheckTakeProfit(close, tpOffset);
	}

	private void ExecuteGridOrder(Sides side, decimal price, decimal tpOffset)
	{
		// Close existing opposite position first
		if (Position != 0)
		{
			if ((Position > 0 && side == Sides.Sell) || (Position < 0 && side == Sides.Buy))
			{
				ClosePosition(side);
			}
		}

		var vol = OrderVolume;

		if (side == Sides.Buy)
		{
			BuyMarket(vol);
			_entryPrice = price;
		}
		else
		{
			SellMarket(vol);
			_entryPrice = price;
		}
	}

	private void ClosePosition(Sides newSide)
	{
		var absPos = Position.Abs();
		if (absPos <= 0)
			return;

		if (Position > 0)
			SellMarket(absPos);
		else
			BuyMarket(absPos);
	}

	private void CheckTakeProfit(decimal close, decimal tpOffset)
	{
		if (Position == 0 || _entryPrice == 0 || tpOffset <= 0)
			return;

		if (Position > 0 && close >= _entryPrice + tpOffset)
		{
			SellMarket(Position.Abs());
			_entryPrice = 0;

			// Reset grid to re-establish levels around current price
			ResetGrid(close);
		}
		else if (Position < 0 && close <= _entryPrice - tpOffset)
		{
			BuyMarket(Position.Abs());
			_entryPrice = 0;

			ResetGrid(close);
		}
	}

	private void ResetGrid(decimal newAnchor)
	{
		_anchorPrice = newAnchor;
		_triggeredLevelsAbove.Clear();
		_triggeredLevelsBelow.Clear();
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
			return 0.01m;

		var step = security.PriceStep ?? 0.01m;
		return step > 0m ? step : 0.01m;
	}
}
