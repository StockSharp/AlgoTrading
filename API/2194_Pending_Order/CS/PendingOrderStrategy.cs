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
/// Strategy that buys or sells based on price breaking above/below
/// a previous candle's range by a configurable offset.
/// </summary>
public class PendingOrderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _distance;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHigh;
	private decimal? _prevLow;

	public decimal Distance { get => _distance.Value; set => _distance.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PendingOrderStrategy()
	{
		_distance = Param(nameof(Distance), 50m)
			.SetDisplay("Distance", "Offset from prev candle range for entry", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = _prevLow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new ExponentialMovingAverage { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		if (_prevHigh is decimal ph && _prevLow is decimal pl)
		{
			var breakUp = ph + Distance;
			var breakDown = pl - Distance;

			if (candle.ClosePrice > breakUp && Position <= 0)
				BuyMarket();
			else if (candle.ClosePrice < breakDown && Position >= 0)
				SellMarket();
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
