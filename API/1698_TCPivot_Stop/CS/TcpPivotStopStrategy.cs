using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot-based breakout strategy.
/// Buys when close crosses above calculated pivot, sells when it crosses below.
/// Uses support/resistance levels for exits.
/// </summary>
public class TcpPivotStopStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pivot;
	private decimal _res1, _sup1;
	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int _barCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TcpPivotStopStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_pivot = _res1 = _sup1 = 0;
		_prevClose = _prevHigh = _prevLow = 0;
		_barCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished) return;

		_barCount++;

		// Recalculate pivot every 12 bars (~1 hour for 5min candles)
		if (_barCount % 12 == 0 && _prevHigh > 0)
		{
			_pivot = (_prevHigh + _prevLow + _prevClose) / 3m;
			_res1 = 2m * _pivot - _prevLow;
			_sup1 = 2m * _pivot - _prevHigh;
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		var close = candle.ClosePrice;

		if (_pivot > 0 && _prevClose > 0)
		{
			// Cross above pivot => long
			if (_prevClose <= _pivot && close > _pivot && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Cross below pivot => short
			else if (_prevClose >= _pivot && close < _pivot && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
			// Exit long at resistance or support
			else if (Position > 0 && (close >= _res1 || close <= _sup1))
			{
				SellMarket();
			}
			// Exit short at support or resistance
			else if (Position < 0 && (close <= _sup1 || close >= _res1))
			{
				BuyMarket();
			}
		}

		_prevClose = close;
	}
}
