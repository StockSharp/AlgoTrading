using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Line order strategy using SMA as dynamic support/resistance levels.
/// </summary>
public class MyLineOrderStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevSma;
	private bool _hasPrev;

	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MyLineOrderStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("SMA", "SMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevSma = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevSma = sma;
			_hasPrev = true;
			return;
		}

		// Cross above SMA
		if (_prevClose <= _prevSma && close > sma)
		{
			if (Position < 0) BuyMarket();
			if (Position <= 0) BuyMarket();
		}
		// Cross below SMA
		else if (_prevClose >= _prevSma && close < sma)
		{
			if (Position > 0) SellMarket();
			if (Position >= 0) SellMarket();
		}

		_prevClose = close;
		_prevSma = sma;
	}
}
