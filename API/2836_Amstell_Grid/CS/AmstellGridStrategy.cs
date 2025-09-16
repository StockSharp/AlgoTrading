using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that alternates buy and sell entries with a virtual take profit.
/// </summary>
public class AmstellGridStrategy : Strategy
{
	private sealed class PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; set; }

		public decimal Volume { get; set; }

		public bool IsClosing { get; set; }
	}

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stepPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal? _lastBuyPrice;
	private decimal? _lastSellPrice;
	private bool _hasInitialOrder;
	private decimal _pipSize;

	/// <summary>
	/// Order volume per grid action.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Virtual take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance between consecutive entries in pips.
	/// </summary>
	public int StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Candle type used to generate trade decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AmstellGridStrategy"/> class.
	/// </summary>
	public AmstellGridStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Virtual take profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_stepPips = Param(nameof(StepPips), 15)
			.SetGreaterThanZero()
			.SetDisplay("Step (pips)", "Distance between grid entries", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal candles", "General");
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

		_longEntries.Clear();
		_shortEntries.Clear();
		_lastBuyPrice = null;
		_lastSellPrice = null;
		_hasInitialOrder = false;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only react to completed candles to emulate stable tick processing.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var stepDistance = GetStepDistance();
		var takeProfitDistance = GetTakeProfitDistance();

		// Bootstrap the grid exactly like the MQL version.
		if (!_hasInitialOrder && _lastBuyPrice is null && _lastSellPrice is null)
		{
			BuyMarket(Volume);
			_hasInitialOrder = true;
			return;
		}

		// Check whether the grid should add a new long layer.
		if (CanOpenBuy(price, stepDistance))
		{
			BuyMarket(Volume);
			return;
		}

		// Mirror logic for the short side of the grid.
		if (CanOpenSell(price, stepDistance))
		{
			SellMarket(Volume);
			return;
		}

		// No new entries were placed, so check for virtual take-profit exits.
		if (TryClosePositions(price, takeProfitDistance))
			return;
	}

	private bool CanOpenBuy(decimal price, decimal stepDistance)
	{
		if (Volume <= 0)
			return false;

		return !_lastBuyPrice.HasValue || _lastBuyPrice.Value - price >= stepDistance;
	}

	private bool CanOpenSell(decimal price, decimal stepDistance)
	{
		if (Volume <= 0)
			return false;

		return !_lastSellPrice.HasValue || price - _lastSellPrice.Value >= stepDistance;
	}

	private bool TryClosePositions(decimal price, decimal takeProfitDistance)
	{
		if (takeProfitDistance <= 0)
			return false;

		// Evaluate longs first because the original EA does the same.
		foreach (var entry in _longEntries)
		{
			if (entry.IsClosing)
				continue;

			if (price - entry.Price >= takeProfitDistance)
			{
				// Prevent duplicate closing requests until the trade is processed.
				entry.IsClosing = true;
				SellMarket(entry.Volume);
				return true;
			}
		}

		// Short entries use the symmetrical distance check.
		foreach (var entry in _shortEntries)
		{
			if (entry.IsClosing)
				continue;

			if (entry.Price - price >= takeProfitDistance)
			{
				entry.IsClosing = true;
				BuyMarket(entry.Volume);
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null || trade.Order.Security != Security)
			return;

		var volume = trade.Trade.Volume;

		// Feed the executed trade into the synthetic short stack first.
		if (trade.Order.Side == Sides.Buy)
		{
			var remainder = ReduceEntries(_shortEntries, volume);

			if (remainder > 0)
			{
				// Remaining volume becomes a new long layer.
				_longEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
				_lastBuyPrice = trade.Trade.Price;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var remainder = ReduceEntries(_longEntries, volume);

			if (remainder > 0)
			{
				// Remaining volume becomes a new short layer.
				_shortEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
				_lastSellPrice = trade.Trade.Price;
			}
		}

		// Recalculate helper state after rebuilding the stacks.
		UpdateLastPrices();
	}

	private decimal ReduceEntries(List<PositionEntry> entries, decimal volume)
	{
		var remaining = volume;

		// Consume volume using a FIFO approach just like MT5 positions.
		while (remaining > 0 && entries.Count > 0)
		{
			var entry = entries[0];
			var used = Math.Min(entry.Volume, remaining);
			entry.Volume -= used;
			remaining -= used;

			if (entry.Volume <= 0)
			{
				// Entry fully closed, remove it from the stack.
				entries.RemoveAt(0);
			}
			else
			{
				// Partial reduction keeps the entry alive; clear closing flag.
				entry.IsClosing = false;
			}
		}

		return remaining;
	}

	private void UpdateLastPrices()
	{
		// If only shorts remain, unlock the buy grid for immediate reuse.
		if (_longEntries.Count == 0 && _shortEntries.Count > 0)
		{
			_lastBuyPrice = null;
		}

		// If only longs remain, clear the last sell price to mimic MT5 logic.
		if (_shortEntries.Count == 0 && _longEntries.Count > 0)
		{
			_lastSellPrice = null;
		}

		// Any surviving entries should be marked as active again.
		for (var i = 0; i < _longEntries.Count; i++)
		{
			_longEntries[i].IsClosing = false;
		}

		for (var i = 0; i < _shortEntries.Count; i++)
		{
			_shortEntries[i].IsClosing = false;
		}
	}

	private decimal GetStepDistance()
	{
		var pip = _pipSize;
		if (pip <= 0)
		{
			// Fallback to the raw price step if the pip size has not been initialized yet.
			pip = Security?.PriceStep ?? 1m;
		}

		return StepPips * pip;
	}

	private decimal GetTakeProfitDistance()
	{
		var pip = _pipSize;
		if (pip <= 0)
		{
			// Same fallback logic as the step distance.
			pip = Security?.PriceStep ?? 1m;
		}

		return TakeProfitPips * pip;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0)
			step = 1m;

		var decimals = Security?.Decimals ?? 0;

		// Replicate MT5 digit adjustment so that 1 pip equals 0.0001 on five-digit symbols.
		return (decimals == 3 || decimals == 5) ? step * 10m : step;
	}
}
