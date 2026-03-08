using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy comparing past open prices with EMA trend filter.
/// </summary>
public class LotScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevPrevOpen;
	private int _barCount;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LotScalpStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle source", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = 0;
		_prevPrevOpen = 0;
		_barCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		SubscribeCandles(CandleType).Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		_barCount++;
		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		if (_barCount >= 3)
		{
			var diff = _prevPrevOpen - _prevOpen;

			// Open prices diverging downward + close above EMA => buy
			if (diff > 0 && close > emaValue && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Open prices diverging upward + close below EMA => sell
			else if (diff < 0 && close < emaValue && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevPrevOpen = _prevOpen;
		_prevOpen = open;
	}
}
