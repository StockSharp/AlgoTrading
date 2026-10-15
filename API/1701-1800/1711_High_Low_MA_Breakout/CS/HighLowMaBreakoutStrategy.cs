using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on price crossing moving averages of highs and lows.
/// Buys when price closes above the high-based moving average and sells when closing below the low-based moving average.
/// </summary>
public class HighLowMaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maHighPeriod;
	private readonly StrategyParam<int> _maLowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMaHigh;
	private decimal _prevMaLow;
	private decimal _prevClose;
	private bool _hasPrev;

	public int MaHighPeriod { get => _maHighPeriod.Value; set => _maHighPeriod.Value = value; }
	public int MaLowPeriod { get => _maLowPeriod.Value; set => _maLowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HighLowMaBreakoutStrategy()
	{
		_maHighPeriod = Param(nameof(MaHighPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("High MA Period", "Period of high price MA", "Parameters");

		_maLowPeriod = Param(nameof(MaLowPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Low MA Period", "Period of low price MA", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMaHigh = 0;
		_prevMaLow = 0;
		_prevClose = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var maHigh = new SimpleMovingAverage { Length = MaHighPeriod };
		var maLow = new SimpleMovingAverage { Length = MaLowPeriod };

		SubscribeCandles(CandleType).Bind(maHigh, maLow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maHigh, decimal maLow)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevMaHigh = maHigh;
			_prevMaLow = maLow;
			_hasPrev = true;
			return;
		}

		// Cross above high MA => buy
		if (_prevClose <= _prevMaHigh && close > maHigh && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Cross below low MA => sell
		else if (_prevClose >= _prevMaLow && close < maLow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevClose = close;
		_prevMaHigh = maHigh;
		_prevMaLow = maLow;
	}
}
