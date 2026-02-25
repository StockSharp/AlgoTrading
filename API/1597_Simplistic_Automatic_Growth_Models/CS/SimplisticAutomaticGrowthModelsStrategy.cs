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
/// Simplistic Automatic Growth Models Strategy - trades when price crosses averaged growth bands.
/// </summary>
public class SimplisticAutomaticGrowthModelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private decimal _cumHigh;
	private decimal _cumLow;
	private int _count;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }

	public SimplisticAutomaticGrowthModelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");

		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback length for bands", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cumHigh = 0;
		_cumLow = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = Length };
		var lowest = new Lowest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hi, decimal lo)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_cumHigh += hi;
		_cumLow += lo;
		_count++;

		var avgHi = _cumHigh / _count;
		var avgLo = _cumLow / _count;

		if (candle.ClosePrice > avgHi && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < avgLo && Position >= 0)
			SellMarket();
	}
}
