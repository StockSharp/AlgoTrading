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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle-based reversion strategy that reacts to sudden price jumps (high/low shifts)
/// and enforces a configurable loss cap. Adapted from a Level1 quote-reversion approach
/// to work with candle data for backtesting.
/// </summary>
public class LuckyShiftLimitStrategy : Strategy
{
	private readonly StrategyParam<int> _shiftPoints;
	private readonly StrategyParam<int> _limitPoints;

	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal _shiftThreshold;
	private decimal _limitThreshold;
	private decimal _entryPrice;
	private bool _thresholdsReady;
	private int _holdBars;

	/// <summary>
	/// Minimum price shift (as percentage tenths) required to trigger an entry.
	/// </summary>
	public int ShiftPoints
	{
		get => _shiftPoints.Value;
		set => _shiftPoints.Value = value;
	}

	/// <summary>
	/// Maximum adverse excursion (as percentage) tolerated before force-closing losing trades.
	/// </summary>
	public int LimitPoints
	{
		get => _limitPoints.Value;
		set => _limitPoints.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters taken from the original MQ4 expert.
	/// </summary>
	public LuckyShiftLimitStrategy()
	{
		_shiftPoints = Param(nameof(ShiftPoints), 3)
			.SetGreaterThanZero()
			.SetDisplay("Shift points", "Minimum price delta between consecutive candles", "Trading")

			.SetOptimize(1, 20, 1);

		_limitPoints = Param(nameof(LimitPoints), 18)
			.SetGreaterThanZero()
			.SetDisplay("Limit points", "Maximum allowed drawdown in percentage", "Risk management")

			.SetOptimize(5, 80, 5);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousHigh = null;
		_previousLow = null;
		_shiftThreshold = 0m;
		_limitThreshold = 0m;
		_entryPrice = 0m;
		_thresholdsReady = false;
		_holdBars = 0;
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

	private void EnsureThresholds(decimal price)
	{
		if (_thresholdsReady)
			return;

		if (price <= 0m)
			return;

		// ShiftPoints=3 -> 0.9% shift threshold, LimitPoints=18 -> 1.8% limit threshold
		_shiftThreshold = price * ShiftPoints * 0.003m;
		_limitThreshold = price * LimitPoints * 0.01m;
		_thresholdsReady = true;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		EnsureThresholds(close);

		if (!_thresholdsReady)
			return;

		// Count hold bars for position management.
		if (Position != 0)
			_holdBars++;

		// Entry logic: detect sudden shifts in high/low between consecutive candles.
		// Only enter when flat.
		if (Position == 0 && _previousHigh is decimal prevHigh && _previousLow is decimal prevLow)
		{
			// High jumped up sharply -> sell on expected reversion
			if (high - prevHigh >= _shiftThreshold)
			{
				SellMarket();
				_entryPrice = close;
				_holdBars = 0;
				LogInfo($"Sell triggered: high shift {high - prevHigh:0.##} >= {_shiftThreshold:0.##}. Price={close:0.#####}");
			}
			// Low dropped sharply -> buy on expected rebound
			else if (prevLow - low >= _shiftThreshold)
			{
				BuyMarket();
				_entryPrice = close;
				_holdBars = 0;
				LogInfo($"Buy triggered: low shift {prevLow - low:0.##} >= {_shiftThreshold:0.##}. Price={close:0.#####}");
			}
		}

		_previousHigh = high;
		_previousLow = low;

		TryClosePosition(close);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (Position != 0m && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;

		if (Position == 0m)
			_entryPrice = 0m;
	}

	private void TryClosePosition(decimal currentPrice)
	{
		if (Position == 0)
			return;

		var avgPrice = _entryPrice;

		if (avgPrice <= 0m)
			return;

		// Minimum hold of 5 bars before checking exit.
		if (_holdBars < 5)
			return;

		// Use half of shift threshold as profit target.
		var profitTarget = _shiftThreshold * 0.5m;

		if (Position > 0)
		{
			// Close long on profit or loss cap.
			if (currentPrice - avgPrice >= profitTarget)
			{
				SellMarket();
				_holdBars = 0;
				LogInfo($"Closed long in profit. Price={currentPrice:0.#####}");
			}
			else if (_limitThreshold > 0m && avgPrice - currentPrice >= _limitThreshold)
			{
				SellMarket();
				_holdBars = 0;
				LogInfo($"Closed long on loss cap. Price={currentPrice:0.#####}");
			}
		}
		else if (Position < 0)
		{
			// Close short on profit or loss cap.
			if (avgPrice - currentPrice >= profitTarget)
			{
				BuyMarket();
				_holdBars = 0;
				LogInfo($"Closed short in profit. Price={currentPrice:0.#####}");
			}
			else if (_limitThreshold > 0m && currentPrice - avgPrice >= _limitThreshold)
			{
				BuyMarket();
				_holdBars = 0;
				LogInfo($"Closed short on loss cap. Price={currentPrice:0.#####}");
			}
		}
	}
}

