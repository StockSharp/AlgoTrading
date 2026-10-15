using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending order grid strategy that mirrors the classic AntiFragile EA behavior.
/// Places layered virtual grid levels above and below the initial price.
/// When price reaches a grid level, a market order is executed.
/// Applies take profit and stop loss management based on entry price.
/// </summary>
public class PendingOrderGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gridSpacing;
	private readonly StrategyParam<int> _gridLevels;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _tradeLong;
	private readonly StrategyParam<bool> _tradeShort;

	private decimal _initialPrice;
	private decimal _entryPrice;
	private bool _initialized;
	private HashSet<int> _triggeredBuyLevels;
	private HashSet<int> _triggeredSellLevels;
	private int _tradeCount;

	/// <summary>
	/// Spacing between grid levels as a percentage of price.
	/// </summary>
	public decimal GridSpacing
	{
		get => _gridSpacing.Value;
		set => _gridSpacing.Value = value;
	}

	/// <summary>
	/// Number of grid levels per side.
	/// </summary>
	public int GridLevels
	{
		get => _gridLevels.Value;
		set => _gridLevels.Value = value;
	}

	/// <summary>
	/// Take profit as percentage of entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss as percentage of entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Enables buying on grid levels below price.
	/// </summary>
	public bool TradeLong
	{
		get => _tradeLong.Value;
		set => _tradeLong.Value = value;
	}

	/// <summary>
	/// Enables selling on grid levels above price.
	/// </summary>
	public bool TradeShort
	{
		get => _tradeShort.Value;
		set => _tradeShort.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PendingOrderGridStrategy"/> class.
	/// </summary>
	public PendingOrderGridStrategy()
	{
		_gridSpacing = Param(nameof(GridSpacing), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Spacing %", "Percentage spacing between grid levels", "Grid");

		_gridLevels = Param(nameof(GridLevels), 3)
			.SetGreaterThanZero()
			.SetDisplay("Grid Levels", "Number of grid levels per side", "Grid");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit as percentage of entry", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss as percentage of entry", "Risk");

		_tradeLong = Param(nameof(TradeLong), true)
			.SetDisplay("Enable Long", "Enable buy grid levels", "Grid");

		_tradeShort = Param(nameof(TradeShort), true)
			.SetDisplay("Enable Short", "Enable sell grid levels", "Grid");

		_triggeredBuyLevels = new HashSet<int>();
		_triggeredSellLevels = new HashSet<int>();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_initialPrice = 0m;
		_entryPrice = 0m;
		_initialized = false;
		_triggeredBuyLevels = new HashSet<int>();
		_triggeredSellLevels = new HashSet<int>();
		_tradeCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var tf = TimeSpan.FromMinutes(5).TimeFrame();

		SubscribeCandles(tf)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Initialize grid around the first candle's close price
		if (!_initialized)
		{
			_initialPrice = close;
			_initialized = true;
			_triggeredBuyLevels.Clear();
			_triggeredSellLevels.Clear();
			return;
		}

		// Check if we have a position that needs TP/SL management
		if (Position != 0m && _entryPrice > 0m)
		{
			if (Position > 0m)
			{
				var tpPrice = _entryPrice * (1m + TakeProfitPercent / 100m);
				var slPrice = _entryPrice * (1m - StopLossPercent / 100m);

				if (close >= tpPrice || close <= slPrice)
				{
					SellMarket();
					ResetGrid(close);
					return;
				}
			}
			else if (Position < 0m)
			{
				var tpPrice = _entryPrice * (1m - TakeProfitPercent / 100m);
				var slPrice = _entryPrice * (1m + StopLossPercent / 100m);

				if (close <= tpPrice || close >= slPrice)
				{
					BuyMarket();
					ResetGrid(close);
					return;
				}
			}
		}

		// Check grid levels for new entries
		var spacing = GridSpacing / 100m;

		// Buy levels below initial price
		if (TradeLong)
		{
			for (var i = 1; i <= GridLevels; i++)
			{
				if (_triggeredBuyLevels.Contains(i))
					continue;

				var level = _initialPrice * (1m - i * spacing);

				if (close <= level && Position <= 0m)
				{
					// Close any short first
					if (Position < 0m)
						BuyMarket();

					BuyMarket();
					_triggeredBuyLevels.Add(i);
					_tradeCount++;
					return;
				}
			}
		}

		// Sell levels above initial price
		if (TradeShort)
		{
			for (var i = 1; i <= GridLevels; i++)
			{
				if (_triggeredSellLevels.Contains(i))
					continue;

				var level = _initialPrice * (1m + i * spacing);

				if (close >= level && Position >= 0m)
				{
					// Close any long first
					if (Position > 0m)
						SellMarket();

					SellMarket();
					_triggeredSellLevels.Add(i);
					_tradeCount++;
					return;
				}
			}
		}
	}

	private void ResetGrid(decimal newPrice)
	{
		_initialPrice = newPrice;
		_entryPrice = 0m;
		_triggeredBuyLevels.Clear();
		_triggeredSellLevels.Clear();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Trade != null)
			_entryPrice = trade.Trade.Price;
	}
}
