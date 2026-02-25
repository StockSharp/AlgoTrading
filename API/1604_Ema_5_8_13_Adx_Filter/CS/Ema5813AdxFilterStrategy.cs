using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA 5/8/13 crossover strategy.
/// </summary>
public class Ema5813AdxFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDiff;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Ema5813AdxFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDiff = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema5 = new ExponentialMovingAverage { Length = 5 };
		var ema8 = new ExponentialMovingAverage { Length = 8 };
		var ema13 = new ExponentialMovingAverage { Length = 13 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema5, ema8, ema13, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema5);
			DrawIndicator(area, ema8);
			DrawIndicator(area, ema13);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal e5, decimal e8, decimal e13)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var diff = e5 - e8;
		var crossUp = _prevDiff <= 0m && diff > 0m;
		var crossDown = _prevDiff >= 0m && diff < 0m;
		_prevDiff = diff;

		if (crossUp && Position <= 0)
			BuyMarket();
		else if (crossDown && Position >= 0)
			SellMarket();

		// Exit on EMA13 cross
		if (Position > 0 && candle.ClosePrice < e13)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice > e13)
			BuyMarket();
	}
}
