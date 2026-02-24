using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Night scalping strategy using Bollinger Bands.
/// </summary>
public class NightScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _rangeThreshold;
	private readonly StrategyParam<DataType> _candleType;

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public decimal RangeThreshold
	{
		get => _rangeThreshold.Value;
		set => _rangeThreshold.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NightScalperStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 40)
			.SetDisplay("BB Period", "Bollinger period", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1m)
			.SetDisplay("BB Deviation", "Bollinger deviation", "Indicators");

		_rangeThreshold = Param(nameof(RangeThreshold), 450m)
			.SetDisplay("Range Threshold", "Maximum band width", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
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

		if (bbValue is not IBollingerBandsValue bb)
			return;

		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var width = upper - lower;

		if (Position == 0 && width < RangeThreshold)
		{
			if (candle.ClosePrice < lower)
				BuyMarket();
			else if (candle.ClosePrice > upper)
				SellMarket();
		}
		else if (Position > 0 && candle.ClosePrice > upper)
		{
			SellMarket();
		}
		else if (Position < 0 && candle.ClosePrice < lower)
		{
			BuyMarket();
		}
	}
}
