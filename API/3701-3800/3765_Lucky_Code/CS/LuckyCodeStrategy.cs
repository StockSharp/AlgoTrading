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
/// Momentum strategy that opens trades when candle price jumps reach a configurable distance and manages exits with profit and drawdown filters.
/// </summary>
public class LuckyCodeStrategy : Strategy
{
	private readonly StrategyParam<int> _shiftPoints;
	private readonly StrategyParam<int> _limitPoints;

	private decimal? _previousClose;
	private decimal _shiftThreshold;
	private decimal _limitThreshold;
	private decimal _entryPrice;
	private bool _thresholdsReady;
	private int _holdBars;

	/// <summary>
	/// Minimum price movement in points required before opening a new trade.
	/// </summary>
	public int ShiftPoints
	{
		get => _shiftPoints.Value;
		set => _shiftPoints.Value = value;
	}

	/// <summary>
	/// Maximum adverse excursion in points tolerated before forcing an exit.
	/// </summary>
	public int LimitPoints
	{
		get => _limitPoints.Value;
		set => _limitPoints.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public LuckyCodeStrategy()
	{
		_shiftPoints = Param(nameof(ShiftPoints), 3)
			.SetGreaterThanZero()
			.SetDisplay("Shift points", "Minimum price jump required to trigger entries", "Trading")

			.SetOptimize(1, 20, 1);

		_limitPoints = Param(nameof(LimitPoints), 18)
			.SetGreaterThanZero()
			.SetDisplay("Limit points", "Maximum number of points allowed against the position", "Risk management")

			.SetOptimize(5, 100, 5);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousClose = null;
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

		// Subscribe to candles and process each finished candle.
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

		// Use percentage of price. ShiftPoints=3 means 3% shift, LimitPoints=18 means 18% limit.
		_shiftThreshold = price * ShiftPoints * 0.01m;
		_limitThreshold = price * LimitPoints * 0.01m;
		_thresholdsReady = true;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		EnsureThresholds(close);

		if (!_thresholdsReady)
			return;

		// Count hold bars for position management.
		if (Position != 0)
			_holdBars++;

		if (_previousClose is decimal prevClose)
		{
			var delta = close - prevClose;

			// Only enter if flat.
			if (Position == 0)
			{
				// Price dropped sharply -> buy on expected rebound.
				if (-delta >= _shiftThreshold)
				{
					BuyMarket();
					_entryPrice = close;
					_holdBars = 0;
					LogInfo($"Buy triggered by fast price drop. Price={close:0.#####}");
				}
				// Price rose sharply -> sell on expected reversal.
				else if (delta >= _shiftThreshold)
				{
					SellMarket();
					_entryPrice = close;
					_holdBars = 0;
					LogInfo($"Sell triggered by fast price rise. Price={close:0.#####}");
				}
			}
		}

		_previousClose = close;

		TryClosePosition(close);
	}

	private void TryClosePosition(decimal currentPrice)
	{
		if (Position == 0)
			return;

		var avgPrice = _entryPrice;

		if (avgPrice <= 0m)
			return;

		// Minimum hold of 3 bars before checking exit.
		if (_holdBars < 3)
			return;

		// Use half of shift threshold as profit target.
		var profitTarget = _shiftThreshold * 0.5m;

		if (Position > 0)
		{
			// Close long on profit target or drawdown limit.
			if (currentPrice - avgPrice >= profitTarget)
			{
				SellMarket();
				_holdBars = 0;
				LogInfo($"Closed long on profit. Price={currentPrice:0.#####}");
			}
			else if (_limitThreshold > 0m && avgPrice - currentPrice >= _limitThreshold)
			{
				SellMarket();
				_holdBars = 0;
				LogInfo($"Closed long on drawdown limit. Price={currentPrice:0.#####}");
			}
		}
		else if (Position < 0)
		{
			// Close short on profit target or drawdown limit.
			if (avgPrice - currentPrice >= profitTarget)
			{
				BuyMarket();
				_holdBars = 0;
				LogInfo($"Closed short on profit. Price={currentPrice:0.#####}");
			}
			else if (_limitThreshold > 0m && currentPrice - avgPrice >= _limitThreshold)
			{
				BuyMarket();
				_holdBars = 0;
				LogInfo($"Closed short on drawdown limit. Price={currentPrice:0.#####}");
			}
		}
	}
}
