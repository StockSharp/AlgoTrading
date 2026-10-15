using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot breakout strategy with StdDev volatility filter.
/// Enters when price breaks previous high/low + volatility band.
/// </summary>
public class EmVolStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _stdevPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevStdev;
	private bool _hasPrev;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int StdevPeriod { get => _stdevPeriod.Value; set => _stdevPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmVolStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 1000m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_stdevPeriod = Param(nameof(StdevPeriod), 14)
			.SetDisplay("StdDev Period", "Volatility period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0;
		_prevLow = 0;
		_prevStdev = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_stopPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdev = new StandardDeviation { Length = StdevPeriod };

		SubscribeCandles(CandleType)
			.Bind(stdev, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdevValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevStdev = stdevValue;
			_hasPrev = true;
			return;
		}

		var price = candle.ClosePrice;
		var res1 = _prevHigh + _prevStdev;
		var sup1 = _prevLow - _prevStdev;

		if (Position == 0)
		{
			if (price > res1)
			{
				BuyMarket();
				_entryPrice = price;
				_stopPrice = price - StopLoss;
			}
			else if (price < sup1)
			{
				SellMarket();
				_entryPrice = price;
				_stopPrice = price + StopLoss;
			}
		}
		else if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || price <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price >= _stopPrice)
				BuyMarket();
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevStdev = stdevValue;
	}
}
