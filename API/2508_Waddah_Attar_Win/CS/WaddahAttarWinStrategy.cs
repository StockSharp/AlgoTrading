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
/// Grid strategy that places symmetric market orders around grid levels and takes profit when equity target is reached.
/// Based on Waddah Attar grid concept: define a grid step, enter in the direction of price movement
/// when price crosses a grid level, and close all when profit target is met.
/// </summary>
public class WaddahAttarWinStrategy : Strategy
{
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<decimal> _firstVolume;
	private readonly StrategyParam<decimal> _incrementVolume;
	private readonly StrategyParam<decimal> _minProfit;

	private decimal _gridOrigin;
	private int _lastGridIndex;
	private decimal _currentVolume;
	private decimal _entryPrice;
	private bool _initialized;
	private int _totalOrders;

	/// <summary>
	/// Distance in points between grid levels.
	/// </summary>
	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Initial volume for orders.
	/// </summary>
	public decimal FirstVolume
	{
		get => _firstVolume.Value;
		set => _firstVolume.Value = value;
	}

	/// <summary>
	/// Volume increment added on each new grid level entry.
	/// </summary>
	public decimal IncrementVolume
	{
		get => _incrementVolume.Value;
		set => _incrementVolume.Value = value;
	}

	/// <summary>
	/// Profit target in price points to close the position.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public WaddahAttarWinStrategy()
	{
		_stepPoints = Param(nameof(StepPoints), 500)
			.SetGreaterThanZero()
			.SetDisplay("Step (Points)", "Distance between grid levels in price steps", "General")
			.SetOptimize(100, 2000, 100);

		_firstVolume = Param(nameof(FirstVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("First Volume", "Volume for grid orders", "General");

		_incrementVolume = Param(nameof(IncrementVolume), 0m)
			.SetDisplay("Increment Volume", "Additional volume on subsequent grid entries", "General");

		_minProfit = Param(nameof(MinProfit), 200m)
			.SetNotNegative()
			.SetDisplay("Min Profit", "Price movement profit target to close position", "Risk");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_gridOrigin = 0m;
		_lastGridIndex = 0;
		_currentVolume = 0m;
		_entryPrice = 0m;
		_initialized = false;
		_totalOrders = 0;
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

		if (!IsFormed)
			return;

		// cap total orders to avoid exceeding limits
		if (_totalOrders >= 80)
			return;

		var close = candle.ClosePrice;
		var priceStep = Security?.PriceStep ?? 0.01m;
		if (priceStep <= 0m)
			priceStep = 0.01m;

		var stepOffset = StepPoints * priceStep;
		if (stepOffset <= 0m)
			return;

		// initialize grid origin on first candle
		if (!_initialized)
		{
			_gridOrigin = close;
			_lastGridIndex = 0;
			_currentVolume = FirstVolume;
			_initialized = true;
			return;
		}

		// calculate which grid index the price is at
		var gridIndex = (int)Math.Floor((close - _gridOrigin) / stepOffset);

		// check profit target: close position if in profit
		if (Position != 0 && _entryPrice > 0m)
		{
			var pnl = Position > 0
				? close - _entryPrice
				: _entryPrice - close;

			if (pnl >= MinProfit * priceStep)
			{
				if (Position > 0)
				{
					SellMarket();
					_totalOrders++;
				}
				else
				{
					BuyMarket();
					_totalOrders++;
				}

				// reset grid around current price
				_gridOrigin = close;
				_lastGridIndex = 0;
				_currentVolume = FirstVolume;
				_entryPrice = 0m;
				return;
			}
		}

		// price crossed to a new grid level
		if (gridIndex != _lastGridIndex)
		{
			if (gridIndex > _lastGridIndex)
			{
				// price moved up - buy (or add to long / close short)
				if (Position < 0)
				{
					// close short first
					BuyMarket();
					_totalOrders++;
					_entryPrice = close;
					_gridOrigin = close;
					_lastGridIndex = 0;
					_currentVolume = FirstVolume;
				}
				else
				{
					BuyMarket();
					_totalOrders++;
					if (Position <= 0)
						_entryPrice = close;
					_currentVolume += IncrementVolume;
				}
			}
			else
			{
				// price moved down - sell (or add to short / close long)
				if (Position > 0)
				{
					// close long first
					SellMarket();
					_totalOrders++;
					_entryPrice = close;
					_gridOrigin = close;
					_lastGridIndex = 0;
					_currentVolume = FirstVolume;
				}
				else
				{
					SellMarket();
					_totalOrders++;
					if (Position >= 0)
						_entryPrice = close;
					_currentVolume += IncrementVolume;
				}
			}

			_lastGridIndex = gridIndex;
		}
	}
}
