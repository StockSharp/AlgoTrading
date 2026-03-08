using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// C Factor HLH4 buy-only strategy.
/// Buys when close breaks above previous high, sells on EMA cross.
/// </summary>
public class CFactorHlh4BuyOnlyStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CFactorHlh4BuyOnlyStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend filter", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0;
		_prevLow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		if (close > _prevHigh && Position <= 0 && close > emaVal)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (close < _prevLow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
