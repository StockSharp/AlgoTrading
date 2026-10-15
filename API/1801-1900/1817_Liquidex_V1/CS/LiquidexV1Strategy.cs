using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// WMA crossover strategy with range filter.
/// </summary>
public class LiquidexV1Strategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevWma;
	private bool _hasPrev;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidexV1Strategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "WMA period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevWma = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var wma = new WeightedMovingAverage { Length = MaPeriod };

		SubscribeCandles(CandleType)
			.Bind(wma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaVal)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevClose = candle.ClosePrice;
			_prevWma = wmaVal;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevClose <= _prevWma && candle.ClosePrice > wmaVal;
		var crossDown = _prevClose >= _prevWma && candle.ClosePrice < wmaVal;

		if (crossUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevWma = wmaVal;
	}
}
