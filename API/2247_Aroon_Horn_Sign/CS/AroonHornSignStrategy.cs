using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aroon Horn Sign trend reversal strategy.
/// Opens long when Aroon Up crosses above Aroon Down above 50.
/// Opens short when the opposite occurs.
/// </summary>
public class AroonHornSignStrategy : Strategy
{
	private readonly StrategyParam<int> _aroonPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevTrend;

	public int AroonPeriod { get => _aroonPeriod.Value; set => _aroonPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AroonHornSignStrategy()
	{
		_aroonPeriod = Param(nameof(AroonPeriod), 9)
			.SetDisplay("Aroon Period", "Aroon indicator period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for processing", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevTrend = 0;

		var aroon = new Aroon { Length = AroonPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(aroon, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, aroon);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aroonValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var value = (IAroonValue)aroonValue;

		var up = value.Up;
		var down = value.Down;

		if (up is null || down is null)
			return;

		var trend = _prevTrend;

		if (up > down && up >= 50m)
			trend = 1;
		else if (down > up && down >= 50m)
			trend = -1;

		if (_prevTrend <= 0 && trend > 0 && Position <= 0)
			BuyMarket();
		else if (_prevTrend >= 0 && trend < 0 && Position >= 0)
			SellMarket();

		_prevTrend = trend;
	}
}
