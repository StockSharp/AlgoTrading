using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified Reflex and Trendflex crossover strategy.
/// </summary>
public class ReflexTrendflexStrategy : Strategy
{
	private readonly StrategyParam<int> _reflexLen;
	private readonly StrategyParam<int> _trendflexLen;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevReflex;
	private decimal _prevTrend;

	public int ReflexLength { get => _reflexLen.Value; set => _reflexLen.Value = value; }
	public int TrendflexLength { get => _trendflexLen.Value; set => _trendflexLen.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ReflexTrendflexStrategy()
	{
		_reflexLen = Param(nameof(ReflexLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Reflex Length", "Reflex EMA length", "General");
		_trendflexLen = Param(nameof(TrendflexLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Trendflex Length", "Trendflex EMA length", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevReflex = 0m;
		_prevTrend = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var reflex = new ExponentialMovingAverage { Length = ReflexLength };
		var trend = new ExponentialMovingAverage { Length = TrendflexLength };

		_prevReflex = 0m;
		_prevTrend = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(reflex, trend, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, reflex);
			DrawIndicator(area, trend);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal reflexVal, decimal trendVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevReflex == 0)
		{
			_prevReflex = reflexVal;
			_prevTrend = trendVal;
			return;
		}

		var prevDiff = _prevReflex - _prevTrend;
		var currDiff = reflexVal - trendVal;

		if (prevDiff <= 0 && currDiff > 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (prevDiff >= 0 && currDiff < 0 && Position >= 0)
		{
			SellMarket();
		}

		_prevReflex = reflexVal;
		_prevTrend = trendVal;
	}
}
