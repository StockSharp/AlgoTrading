using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple EMA crossover strategy.
/// Enters long when the fast EMA crosses above the slow EMA and short when the opposite occurs.
/// </summary>
public class SimpleEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _periods;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int Periods
	{
		get => _periods.Value;
		set => _periods.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SimpleEmaCrossoverStrategy()
	{
		_periods = Param(nameof(Periods), 17)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for the fast EMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = Periods };
		var slowEma = new ExponentialMovingAverage { Length = Periods + 10 };

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

		if (_hasPrev)
		{
			var crossUp = _prevFast < _prevSlow && fast > slow;
			var crossDown = _prevFast > _prevSlow && fast < slow;

			if (crossUp && Position <= 0)
				BuyMarket();
			else if (crossDown && Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}
}
