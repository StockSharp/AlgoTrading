using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA Cross Contest strategy - dual EMA crossover.
/// Buys when short EMA crosses above long EMA.
/// Sells when short EMA crosses below long EMA.
/// </summary>
public class EmaCrossContestHedgedLadderStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShort;
	private decimal _prevLong;
	private bool _hasPrev;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmaCrossContestHedgedLadderStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 9)
			.SetDisplay("Short EMA", "Short EMA period", "Indicators");

		_longPeriod = Param(nameof(LongPeriod), 21)
			.SetDisplay("Long EMA", "Long EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var shortEma = new ExponentialMovingAverage { Length = ShortPeriod };
		var longEma = new ExponentialMovingAverage { Length = LongPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortEma, longEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortEma, decimal longEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevShort = shortEma;
			_prevLong = longEma;
			_hasPrev = true;
			return;
		}

		if (_prevShort <= _prevLong && shortEma > longEma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevShort >= _prevLong && shortEma < longEma && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevShort = shortEma;
		_prevLong = longEma;
	}
}
