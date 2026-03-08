using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on turning points of double smoothed moving average.
/// Applies EMA followed by another EMA and enters on local extrema.
/// </summary>
public class ExpX2MaStrategy : Strategy
{
	private readonly StrategyParam<int> _firstMaLength;
	private readonly StrategyParam<int> _secondMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPrevValue;
	private decimal _prevValue;
	private int _barCount;

	public int FirstMaLength { get => _firstMaLength.Value; set => _firstMaLength.Value = value; }
	public int SecondMaLength { get => _secondMaLength.Value; set => _secondMaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpX2MaStrategy()
	{
		_firstMaLength = Param(nameof(FirstMaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("First MA Length", "Period for first smoothing", "Indicators");

		_secondMaLength = Param(nameof(SecondMaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Second MA Length", "Period for second smoothing", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPrevValue = 0;
		_prevValue = 0;
		_barCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema1 = new ExponentialMovingAverage { Length = FirstMaLength };
		var ema2 = new ExponentialMovingAverage { Length = SecondMaLength };

		SubscribeCandles(CandleType).Bind(ema1, ema2, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema1Value, decimal ema2Value)
	{
		if (candle.State != CandleStates.Finished) return;

		_barCount++;

		if (_barCount >= 3)
		{
			var isLocalMin = _prevValue < _prevPrevValue && ema2Value > _prevValue;
			var isLocalMax = _prevValue > _prevPrevValue && ema2Value < _prevValue;

			if (isLocalMin && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (isLocalMax && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevPrevValue = _prevValue;
		_prevValue = ema2Value;
	}
}
