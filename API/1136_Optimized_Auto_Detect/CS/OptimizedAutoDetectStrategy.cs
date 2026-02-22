using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OptimizedAutoDetectStrategy : Strategy
{
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShortSma;
	private decimal _prevLongSma;
	private bool _hasPrev;

	public int ShortMaPeriod { get => _shortMaPeriod.Value; set => _shortMaPeriod.Value = value; }
	public int LongMaPeriod { get => _longMaPeriod.Value; set => _longMaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OptimizedAutoDetectStrategy()
	{
		_shortMaPeriod = Param(nameof(ShortMaPeriod), 9).SetGreaterThanZero();
		_longMaPeriod = Param(nameof(LongMaPeriod), 21).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var shortSma = new SimpleMovingAverage { Length = ShortMaPeriod };
		var longSma = new SimpleMovingAverage { Length = LongMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortSma, longSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortSma);
			DrawIndicator(area, longSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortSma, decimal longSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevShortSma = shortSma;
			_prevLongSma = longSma;
			_hasPrev = true;
			return;
		}

		var maLong = _prevShortSma <= _prevLongSma && shortSma > longSma;
		var maShort = _prevShortSma >= _prevLongSma && shortSma < longSma;

		if (maLong && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		else if (maShort && Position >= 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}

		_prevShortSma = shortSma;
		_prevLongSma = longSma;
	}
}
