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
/// Bollinger Bands mean reversion strategy.
/// Buys when price touches lower band, sells when price touches upper band.
/// Closes at middle band.
/// </summary>
public class BollingerBandsAutomatedStrategy : Strategy
{
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<DataType> _candleType;

	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public decimal BbDeviation { get => _bbDeviation.Value; set => _bbDeviation.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BollingerBandsAutomatedStrategy()
	{
		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
			.SetGreaterThanZero();

		_bbDeviation = Param(nameof(BbDeviation), 2m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BbPeriod,
			Width = BbDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = bbValue as BollingerBandsValue;
		if (bb?.UpBand is not decimal upper || bb?.LowBand is not decimal lower)
			return;

		var middle = (upper + lower) / 2m;
		var close = candle.ClosePrice;

		// Close long at middle band
		if (Position > 0 && close >= middle)
			SellMarket();
		// Close short at middle band
		else if (Position < 0 && close <= middle)
			BuyMarket();

		// Open new positions at band extremes
		if (close <= lower && Position <= 0)
			BuyMarket();
		else if (close >= upper && Position >= 0)
			SellMarket();
	}
}
