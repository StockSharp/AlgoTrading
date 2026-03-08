using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple FX crossover strategy.
/// Uses fast and slow SMA crossover for trend detection.
/// Buys on golden cross, sells on death cross.
/// </summary>
public class SimpleFxCrossoverStrategy : Strategy
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

	public SimpleFxCrossoverStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 10)
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");

		_longPeriod = Param(nameof(LongPeriod), 30)
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevShort = 0m;
		_prevLong = 0m;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var fast = new SimpleMovingAverage { Length = ShortPeriod };
		var slow = new SimpleMovingAverage { Length = LongPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevShort = fast;
			_prevLong = slow;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevShort <= _prevLong && fast > slow;
		var crossDown = _prevShort >= _prevLong && fast < slow;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevShort = fast;
		_prevLong = slow;
	}
}
