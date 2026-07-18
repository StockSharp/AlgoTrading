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
/// Berlin Range Index strategy.
/// Uses choppiness index to detect trending vs ranging markets.
/// Enters in trend direction when choppiness is low (strong trend).
/// Exits when choppiness is high (choppy/ranging market).
/// </summary>
public class BerlinRangeIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _chopThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevChop;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal ChopThreshold { get => _chopThreshold.Value; set => _chopThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BerlinRangeIndexStrategy()
	{
		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Choppiness index period", "General")
			.SetOptimize(5, 30, 5);

		_chopThreshold = Param(nameof(ChopThreshold), 55m)
			.SetDisplay("Chop Threshold", "Threshold for trend vs range", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevChop = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var choppiness = new ChoppinessIndex { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(choppiness, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, choppiness);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal chopValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevChop == 0)
		{
			_prevChop = chopValue;
			return;
		}

		// Low choppiness = strong trend; enter in candle direction
		if (chopValue < ChopThreshold && _prevChop >= ChopThreshold)
		{
			if (candle.ClosePrice > candle.OpenPrice && Position <= 0)
				BuyMarket();
			else if (candle.ClosePrice < candle.OpenPrice && Position >= 0)
				SellMarket();
		}
		// High choppiness = choppy market; exit positions
		else if (chopValue > ChopThreshold && _prevChop <= ChopThreshold)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
		}

		_prevChop = chopValue;
	}
}
