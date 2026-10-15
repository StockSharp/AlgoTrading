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
/// Grid averaging strategy based on the Ilan 1.6 Dynamic expert advisor.
/// Adds positions when price moves against the current one and closes the
/// whole basket on a take profit.
/// Each grid level trades 1 unit; closing flattens via multiple market orders.
/// </summary>
public class Ilan16DynamicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _pipStep;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _startLong;

	private int _tradeCount;
	private decimal _lastEntryPrice;
	private decimal _avgPrice;
	private bool _isLong;

	/// <summary>
	/// Distance in price steps between grid levels.
	/// </summary>
	public decimal PipStep { get => _pipStep.Value; set => _pipStep.Value = value; }

	/// <summary>
	/// Profit target from average price in price steps.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Maximum number of averaging entries.
	/// </summary>
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Open first trade as long if true.
	/// </summary>
	public bool StartLong { get => _startLong.Value; set => _startLong.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public Ilan16DynamicStrategy()
	{
		_pipStep = Param(nameof(PipStep), 50000m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Step", "Distance in price steps between grid levels", "Trading")
			.SetOptimize(10000m, 100000m, 10000m);

		_takeProfit = Param(nameof(TakeProfit), 30000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target from average price in price steps", "Trading")
			.SetOptimize(10000m, 100000m, 10000m);

		_maxTrades = Param(nameof(MaxTrades), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of averaging entries", "Trading")
			.SetOptimize(2, 10, 1);

		_startLong = Param(nameof(StartLong), true)
			.SetDisplay("Start Long", "Open first trade as long", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isLong = StartLong;

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

		var step = Security.PriceStep ?? 1m;
		var price = candle.ClosePrice;

		// No position - open initial entry
		if (Position == 0)
		{
			if (_isLong)
				BuyMarket();
			else
				SellMarket();

			_tradeCount = 1;
			_lastEntryPrice = price;
			_avgPrice = price;
			return;
		}

		// Check take profit: close entire basket
		if (_isLong && price >= _avgPrice + TakeProfit * step)
		{
			CloseAll();
			return;
		}
		else if (!_isLong && price <= _avgPrice - TakeProfit * step)
		{
			CloseAll();
			return;
		}

		// Check for grid averaging entry (price moved against us)
		if (_isLong && _tradeCount < MaxTrades && _lastEntryPrice - price >= PipStep * step)
		{
			BuyMarket();
			_tradeCount++;
			_avgPrice = (_avgPrice * (_tradeCount - 1) + price) / _tradeCount;
			_lastEntryPrice = price;
		}
		else if (!_isLong && _tradeCount < MaxTrades && price - _lastEntryPrice >= PipStep * step)
		{
			SellMarket();
			_tradeCount++;
			_avgPrice = (_avgPrice * (_tradeCount - 1) + price) / _tradeCount;
			_lastEntryPrice = price;
		}
	}

	private void CloseAll()
	{
		var pos = Position;

		if (pos > 0)
		{
			// Close long: sell abs(pos) times
			for (var i = 0; i < (int)Math.Abs(pos); i++)
				SellMarket();
		}
		else if (pos < 0)
		{
			// Close short: buy abs(pos) times
			for (var i = 0; i < (int)Math.Abs(pos); i++)
				BuyMarket();
		}

		ResetState();
	}

	private void ResetState()
	{
		_tradeCount = 0;
		_lastEntryPrice = 0m;
		_avgPrice = 0m;
		_isLong = StartLong;
	}
}

