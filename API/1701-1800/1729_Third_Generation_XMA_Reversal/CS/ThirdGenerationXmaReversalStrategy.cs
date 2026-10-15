using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 3rd Generation XMA reversal strategy using double-smoothed EMA turning points.
/// </summary>
public class ThirdGenerationXmaReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev1;
	private decimal _prev2;
	private int _barCount;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ThirdGenerationXmaReversalStrategy()
	{
		_maLength = Param(nameof(MaLength), 50)
			.SetDisplay("MA Length", "Base length for the moving average", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = 0;
		_prev2 = 0;
		_barCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema1 = new ExponentialMovingAverage { Length = MaLength };
		var ema2 = new ExponentialMovingAverage { Length = MaLength / 2 > 0 ? MaLength / 2 : 10 };

		SubscribeCandles(CandleType).Bind(ema1, ema2, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema1Value, decimal ema2Value)
	{
		if (candle.State != CandleStates.Finished) return;

		// XMA = 2*ema1 - ema2 (third generation concept)
		var xma = 2m * ema1Value - ema2Value;
		_barCount++;

		if (_barCount >= 3)
		{
			// Local minimum => buy
			if (_prev1 < _prev2 && xma > _prev1 && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Local maximum => sell
			else if (_prev1 > _prev2 && xma < _prev1 && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prev2 = _prev1;
		_prev1 = xma;
	}
}
