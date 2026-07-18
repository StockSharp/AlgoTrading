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
/// Elite eFibo Trader grid strategy converted from MQL5.
/// Builds a Fibonacci-based sequence of market entries at price levels.
/// Buys or sells at progressively worse prices with increasing volume (Fibonacci sequence).
/// Exits when total PnL target is reached or stop loss is hit.
/// </summary>
public class EliteEFiboTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _levelsCount;
	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<decimal> _levelDistance;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _currentLevel;
	private int _activeDirection;
	private bool _cycleActive;

	// Fibonacci volumes for grid levels
	private static readonly decimal[] FibVolumes = { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55 };

	/// <summary>
	/// Enable buy-only mode.
	/// </summary>
	public bool OpenBuy
	{
		get => _openBuy.Value;
		set => _openBuy.Value = value;
	}

	/// <summary>
	/// Enable sell-only mode.
	/// </summary>
	public bool OpenSell
	{
		get => _openSell.Value;
		set => _openSell.Value = value;
	}

	/// <summary>
	/// Number of Fibonacci grid levels.
	/// </summary>
	public int LevelsCount
	{
		get => _levelsCount.Value;
		set => _levelsCount.Value = value;
	}

	/// <summary>
	/// Distance between successive pending levels in price steps.
	/// </summary>
	public decimal LevelDistance
	{
		get => _levelDistance.Value;
		set => _levelDistance.Value = value;
	}

	/// <summary>
	/// Stop-loss size in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit size in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public EliteEFiboTraderStrategy()
	{
		_levelsCount = Param(nameof(LevelsCount), 6)
			.SetGreaterThanZero()
			.SetDisplay("Levels Count", "Number of Fibonacci levels", "Grid");

		_openBuy = Param(nameof(OpenBuy), true)
			.SetDisplay("Open Buy", "Enable buying", "General");

		_openSell = Param(nameof(OpenSell), true)
			.SetDisplay("Open Sell", "Enable selling", "General");

		_levelDistance = Param(nameof(LevelDistance), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Level Distance", "Distance between orders in price steps", "Grid");

		_stopLossPoints = Param(nameof(StopLossPoints), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop-loss size in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit size in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cycleActive = false;
		_currentLevel = 0;
		_activeDirection = 0;
		_entryPrice = 0;

		var sma = new SMA { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var step = Security.PriceStep ?? 1m;

		if (!_cycleActive && Position == 0)
		{
			// Start a new cycle based on trend direction
			if (OpenBuy && close > smaValue)
			{
				_activeDirection = 1;
				_entryPrice = close;
				_currentLevel = 0;
				_cycleActive = true;
				BuyMarket();
			}
			else if (OpenSell && close < smaValue)
			{
				_activeDirection = -1;
				_entryPrice = close;
				_currentLevel = 0;
				_cycleActive = true;
				SellMarket();
			}
		}
		else if (_cycleActive)
		{
			var stopDistance = StopLossPoints * step;
			var tpDistance = TakeProfitPoints * step;
			var levelDist = LevelDistance * step;

			// Check stop loss
			if (_activeDirection == 1 && close <= _entryPrice - stopDistance)
			{
				SellMarket(Math.Abs(Position));
				ResetCycle();
				return;
			}
			else if (_activeDirection == -1 && close >= _entryPrice + stopDistance)
			{
				BuyMarket(Math.Abs(Position));
				ResetCycle();
				return;
			}

			// Check take profit
			if (_activeDirection == 1 && close >= _entryPrice + tpDistance)
			{
				SellMarket(Math.Abs(Position));
				ResetCycle();
				return;
			}
			else if (_activeDirection == -1 && close <= _entryPrice - tpDistance)
			{
				BuyMarket(Math.Abs(Position));
				ResetCycle();
				return;
			}

			// Check for grid level additions (averaging into losing positions)
			var nextLevel = _currentLevel + 1;
			if (nextLevel < LevelsCount && nextLevel < FibVolumes.Length)
			{
				if (_activeDirection == 1 && close <= _entryPrice - levelDist * nextLevel)
				{
					BuyMarket(FibVolumes[nextLevel]);
					_currentLevel = nextLevel;
				}
				else if (_activeDirection == -1 && close >= _entryPrice + levelDist * nextLevel)
				{
					SellMarket(FibVolumes[nextLevel]);
					_currentLevel = nextLevel;
				}
			}
		}
	}

	private void ResetCycle()
	{
		_cycleActive = false;
		_currentLevel = 0;
		_activeDirection = 0;
		_entryPrice = 0;
	}
}
