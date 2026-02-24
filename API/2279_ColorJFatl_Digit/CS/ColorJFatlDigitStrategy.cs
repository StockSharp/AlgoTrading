using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the slope changes of Jurik Moving Average (JMA).
/// Opens long when JMA turns up, opens short when JMA turns down.
/// </summary>
public class ColorJFatlDigitStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevJma;
	private decimal? _prevSlope;

	public int JmaLength { get => _jmaLength.Value; set => _jmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorJFatlDigitStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period for Jurik Moving Average", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of indicator", "Parameters");
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

		_prevJma = null;
		_prevSlope = null;

		var jma = new JurikMovingAverage { Length = JmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jma, Process)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jma);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var slope = _prevJma is decimal prev ? jmaValue - prev : (decimal?)null;

		if (slope is decimal s && _prevSlope is decimal ps)
		{
			// JMA slope turns positive -> buy
			if (ps <= 0m && s > 0m && Position <= 0)
				BuyMarket();
			// JMA slope turns negative -> sell
			else if (ps >= 0m && s < 0m && Position >= 0)
				SellMarket();
		}

		_prevSlope = slope;
		_prevJma = jmaValue;
	}
}
