using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy.
/// Enters long when fast EMA crosses above slow EMA, enters short on the opposite crossover.
/// </summary>
public class CrossingMovingAverageStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public CrossingMovingAverageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == null || _prevSlow == null)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		// Crossover detection
		var prevAbove = _prevFast.Value > _prevSlow.Value;
		var currAbove = fast > slow;

		_prevFast = fast;
		_prevSlow = slow;

		// Golden cross: fast crosses above slow
		if (!prevAbove && currAbove)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Death cross: fast crosses below slow
		else if (prevAbove && !currAbove)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}
