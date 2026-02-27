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
/// Hedging grid strategy converted from the Frank Ud MetaTrader expert.
/// Opens a position and adds martingale entries when price moves against the trade.
/// Closes all on take profit from average price.
/// </summary>
public class FrankUdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _stepDistance;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastEntryPrice;
	private decimal _avgPrice;
	private int _entryCount;
	private bool _isLong;
	private bool _initialized;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal StepDistance { get => _stepDistance.Value; set => _stepDistance.Value = value; }
	public int MaxEntries { get => _maxEntries.Value; set => _maxEntries.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FrankUdStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance from avg price", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance from avg price", "Risk");

		_stepDistance = Param(nameof(StepDistance), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Step Distance", "Price distance for adding martingale entries", "Grid");

		_maxEntries = Param(nameof(MaxEntries), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Entries", "Maximum martingale entries", "Grid");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastEntryPrice = 0m;
		_avgPrice = 0m;
		_entryCount = 0;
		_isLong = false;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Initial entry based on candle direction
		if (Position == 0 && !_initialized)
		{
			if (candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket();
				_isLong = true;
			}
			else
			{
				SellMarket();
				_isLong = false;
			}
			_lastEntryPrice = price;
			_avgPrice = price;
			_entryCount = 1;
			_initialized = true;
			return;
		}

		if (Position == 0)
		{
			// Position was closed, reset for new cycle
			_initialized = false;
			_entryCount = 0;
			return;
		}

		// Check take profit
		if (_isLong && price >= _avgPrice + TakeProfit)
		{
			SellMarket();
			_initialized = false;
			_entryCount = 0;
			return;
		}
		if (!_isLong && price <= _avgPrice - TakeProfit)
		{
			BuyMarket();
			_initialized = false;
			_entryCount = 0;
			return;
		}

		// Check stop loss
		if (_isLong && price <= _avgPrice - StopLoss)
		{
			SellMarket();
			_initialized = false;
			_entryCount = 0;
			return;
		}
		if (!_isLong && price >= _avgPrice + StopLoss)
		{
			BuyMarket();
			_initialized = false;
			_entryCount = 0;
			return;
		}

		// Martingale: add entries when price moves against us
		if (_entryCount < MaxEntries)
		{
			if (_isLong && _lastEntryPrice - price >= StepDistance)
			{
				BuyMarket();
				_avgPrice = (_avgPrice * _entryCount + price) / (_entryCount + 1);
				_lastEntryPrice = price;
				_entryCount++;
			}
			else if (!_isLong && price - _lastEntryPrice >= StepDistance)
			{
				SellMarket();
				_avgPrice = (_avgPrice * _entryCount + price) / (_entryCount + 1);
				_lastEntryPrice = price;
				_entryCount++;
			}
		}
	}
}
