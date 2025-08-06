namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid Bot Strategy
/// </summary>
public class GridBotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<decimal> _upperLimit;
	private readonly StrategyParam<decimal> _lowerLimit;
	private readonly StrategyParam<int> _gridCount;
	private readonly StrategyParam<int> _marketDirection;
	private readonly StrategyParam<bool> _useExtremes;

	private decimal _gridInterval;
	private decimal[] _gridLevels;
	private int _lastSignal;
	private int _lastSignalIndex;
	private decimal _signalLine;
	private ICandleMessage _previousCandle;

	public GridBotStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_upperLimit = Param(nameof(UpperLimit), 48000m)
			.SetDisplay("Upper Limit", "Grid upper boundary", "Grid Settings");

		_lowerLimit = Param(nameof(LowerLimit), 45000m)
			.SetDisplay("Lower Limit", "Grid lower boundary", "Grid Settings");

		_gridCount = Param(nameof(GridCount), 10)
			.SetDisplay("Grid Count", "Number of grid levels", "Grid Settings");

		_marketDirection = Param(nameof(MarketDirection), 0)
			.SetDisplay("Market Direction", "1=Up, 0=Neutral, -1=Down", "Strategy");

		_useExtremes = Param(nameof(UseExtremes), true)
			.SetDisplay("Use Extremes", "Use High/Low for signals", "Strategy");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public decimal UpperLimit
	{
		get => _upperLimit.Value;
		set => _upperLimit.Value = value;
	}

	public decimal LowerLimit
	{
		get => _lowerLimit.Value;
		set => _lowerLimit.Value = value;
	}

	public int GridCount
	{
		get => _gridCount.Value;
		set => _gridCount.Value = value;
	}

	public int MarketDirection
	{
		get => _marketDirection.Value;
		set => _marketDirection.Value = value;
	}

	public bool UseExtremes
	{
		get => _useExtremes.Value;
		set => _useExtremes.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, CandleType) };

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Calculate grid parameters
		var gridRange = UpperLimit - LowerLimit;
		_gridInterval = gridRange / GridCount;

		// Initialize grid levels
		_gridLevels = new decimal[GridCount + 1];
		for (var i = 0; i <= GridCount; i++)
		{
			_gridLevels[i] = LowerLimit + _gridInterval * i;
		}

		// Subscribe to candles
		var subscription = this.SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Skip if strategy is not ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Skip non-finished candles
		if (candle.State != CandleStates.Finished)
			return;

		var buyIndex = GetBuyLineIndex(candle);
		var sellIndex = GetSellLineIndex(candle);

		var buy = false;
		var sell = false;

		// Check for buy signal
		if (buyIndex > 0)
		{
			// No repeat trades at current level
			if (UseExtremes)
			{
				if (candle.LowPrice < _signalLine - _gridInterval)
					buy = true;
			}
			else
			{
				if (candle.ClosePrice < _signalLine - _gridInterval)
					buy = true;
			}

			// No trades outside of grid limits
			if (candle.ClosePrice >= UpperLimit || candle.ClosePrice < LowerLimit)
				buy = false;

			// Direction Filter (skip one signal if against market direction)
			if (MarketDirection == -1 && candle.LowPrice >= _signalLine - _gridInterval * 2)
				buy = false;
		}

		// Check for sell signal
		if (sellIndex > 0)
		{
			// No repeat trades at current level
			if (UseExtremes)
			{
				if (candle.HighPrice > _signalLine + _gridInterval)
					sell = true;
			}
			else
			{
				if (candle.ClosePrice > _signalLine + _gridInterval)
					sell = true;
			}

			// No trades outside of grid limits
			if (candle.ClosePrice <= LowerLimit || candle.ClosePrice > UpperLimit)
				sell = false;

			// Direction Filter (skip one signal if against market direction)
			if (MarketDirection == 1 && candle.HighPrice <= _signalLine + _gridInterval * 2)
				sell = false;
		}

		// Update trackers
		if (buy)
		{
			_lastSignal = 1;
			_lastSignalIndex = buyIndex;
			_signalLine = LowerLimit + _gridInterval * _lastSignalIndex;

			// Execute buy order
			if (Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
		}
		else if (sell)
		{
			_lastSignal = -1;
			_lastSignalIndex = sellIndex;
			_signalLine = LowerLimit + _gridInterval * _lastSignalIndex;

			// Execute sell order
			if (Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}

		// Store previous candle for next iteration
		_previousCandle = candle;
	}

	private int GetBuyLineIndex(ICandleMessage candle)
	{
		var index = 0;

		for (var i = 0; i <= GridCount; i++)
		{
			var buyValue = _gridLevels[i];

			if (UseExtremes)
			{
				if (_previousCandle?.HighPrice > buyValue && candle.LowPrice <= buyValue)
					index = i;
			}
			else
			{
				if (_previousCandle?.ClosePrice > buyValue && candle.ClosePrice <= buyValue)
					index = i;
			}
		}

		return index;
	}

	private int GetSellLineIndex(ICandleMessage candle)
	{
		var index = 0;

		for (var i = 0; i <= GridCount; i++)
		{
			var sellValue = _gridLevels[i];

			if (UseExtremes)
			{
				if (_previousCandle?.LowPrice < sellValue && candle.HighPrice >= sellValue)
					index = i;
			}
			else
			{
				if (_previousCandle?.ClosePrice < sellValue && candle.ClosePrice >= sellValue)
					index = i;
			}
		}

		return index;
	}
}