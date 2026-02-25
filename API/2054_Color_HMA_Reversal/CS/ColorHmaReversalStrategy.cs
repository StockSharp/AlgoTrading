using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Hull Moving Average slope reversals.
/// Buys when HMA turns up, sells when HMA turns down.
/// </summary>
public class ColorHmaReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevValue1;
	private decimal _prevValue2;
	private int _count;

	public int HmaPeriod { get => _hmaPeriod.Value; set => _hmaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorHmaReversalStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Hull Moving Average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_count = 0;

		var hma = new HullMovingAverage { Length = HmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count <= 2)
		{
			_prevValue2 = _prevValue1;
			_prevValue1 = hmaValue;
			return;
		}

		var wasFalling = _prevValue1 < _prevValue2;
		var wasRising = _prevValue1 > _prevValue2;
		var nowRising = hmaValue > _prevValue1;
		var nowFalling = hmaValue < _prevValue1;

		// HMA slope reversal from falling to rising -> buy
		if (wasFalling && nowRising && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// HMA slope reversal from rising to falling -> sell
		else if (wasRising && nowFalling && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevValue2 = _prevValue1;
		_prevValue1 = hmaValue;
	}
}
