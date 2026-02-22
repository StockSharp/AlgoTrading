using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MacdVolumeXauusdStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _shortVolumeEma;
	private ExponentialMovingAverage _longVolumeEma;
	private MovingAverageConvergenceDivergence _macd;

	private decimal _prevMacd;
	private bool _prevMacdSet;

	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdVolumeXauusdStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 5);
		_longLength = Param(nameof(LongLength), 10);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_shortVolumeEma = new ExponentialMovingAverage { Length = ShortLength };
		_longVolumeEma = new ExponentialMovingAverage { Length = LongLength };
		_macd = new MovingAverageConvergenceDivergence();

		_prevMacd = 0;
		_prevMacdSet = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.ServerTime;

		_shortVolumeEma.Process(new DecimalIndicatorValue(_shortVolumeEma, candle.TotalVolume, t));
		_longVolumeEma.Process(new DecimalIndicatorValue(_longVolumeEma, candle.TotalVolume, t));

		if (!_macd.IsFormed)
		{
			_prevMacd = macd;
			_prevMacdSet = true;
			return;
		}

		if (!_prevMacdSet)
		{
			_prevMacd = macd;
			_prevMacdSet = true;
			return;
		}

		// MACD cross above zero = buy
		var longSignal = _prevMacd <= 0 && macd > 0;
		// MACD cross below zero = sell
		var shortSignal = _prevMacd >= 0 && macd < 0;

		if (longSignal && Position <= 0)
		{
			BuyMarket();
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket();
		}

		_prevMacd = macd;
	}
}
