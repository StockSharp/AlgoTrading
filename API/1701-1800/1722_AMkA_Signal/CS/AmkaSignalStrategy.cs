using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AMkA based strategy using KAMA derivative and standard deviation filter.
/// Buys when KAMA rises above volatility threshold and sells when it falls below.
/// </summary>
public class AmkaSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevKama;
	private bool _hasPrev;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal DeviationMultiplier { get => _deviationMultiplier.Value; set => _deviationMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AmkaSignalStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("KAMA Length", "Lookback period for the adaptive moving average", "Indicator");

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Multiplier for standard deviation filter", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevKama = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var kama = new KaufmanAdaptiveMovingAverage { Length = Length };
		var stdev = new StandardDeviation { Length = Length };

		SubscribeCandles(CandleType).Bind(kama, stdev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal kamaValue, decimal stdevValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevKama = kamaValue;
			_hasPrev = true;
			return;
		}

		var delta = kamaValue - _prevKama;
		_prevKama = kamaValue;

		if (stdevValue <= 0) return;

		var threshold = stdevValue * DeviationMultiplier;

		if (delta > threshold && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (delta < -threshold && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
