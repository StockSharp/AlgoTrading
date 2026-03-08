using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Xbug Free v4 strategy based on SMA crossing median price.
/// </summary>
public class XbugFreeV4Strategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSma;
	private decimal _prevPrice;
	private decimal _prev2Sma;
	private decimal _prev2Price;
	private int _barCount;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XbugFreeV4Strategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSma = 0; _prevPrice = 0;
		_prev2Sma = 0; _prev2Price = 0;
		_barCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var stdev = new StandardDeviation { Length = 14 };

		SubscribeCandles(CandleType).Bind(sma, stdev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdevValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		_barCount++;

		if (_barCount >= 3)
		{
			var buySignal = smaValue > median && _prevSma > _prevPrice && _prev2Sma < _prev2Price;
			var sellSignal = smaValue < median && _prevSma < _prevPrice && _prev2Sma > _prev2Price;

			if (buySignal && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (sellSignal && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prev2Sma = _prevSma;
		_prev2Price = _prevPrice;
		_prevSma = smaValue;
		_prevPrice = median;
	}
}
