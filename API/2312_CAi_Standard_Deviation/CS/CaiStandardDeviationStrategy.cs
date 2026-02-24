using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on moving average and standard deviation bands.
/// Opens positions when price breaks outside a wide band
/// and closes them when price returns inside a narrower band.
/// </summary>
public class CaiStandardDeviationStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<decimal> _openMultiplier;
	private readonly StrategyParam<decimal> _closeMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int StdDevPeriod { get => _stdDevPeriod.Value; set => _stdDevPeriod.Value = value; }
	public decimal OpenMultiplier { get => _openMultiplier.Value; set => _openMultiplier.Value = value; }
	public decimal CloseMultiplier { get => _closeMultiplier.Value; set => _closeMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CaiStandardDeviationStrategy()
	{
		_maLength = Param(nameof(MaLength), 12)
			.SetDisplay("MA Length", "Moving average length", "Parameters")
			.SetOptimize(5, 50, 5);

		_stdDevPeriod = Param(nameof(StdDevPeriod), 9)
			.SetDisplay("StdDev Period", "Standard deviation period", "Parameters")
			.SetOptimize(5, 50, 5);

		_openMultiplier = Param(nameof(OpenMultiplier), 2.5m)
			.SetDisplay("Open Multiplier", "StdDev multiplier for entries", "Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_closeMultiplier = Param(nameof(CloseMultiplier), 1.5m)
			.SetDisplay("Close Multiplier", "StdDev multiplier for exits", "Parameters")
			.SetOptimize(0.5m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "Parameters");
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

		var sma = new SimpleMovingAverage { Length = MaLength };
		var stdDev = new StandardDeviation { Length = StdDevPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, stdDev);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upperOpen = smaValue + OpenMultiplier * stdDevValue;
		var lowerOpen = smaValue - OpenMultiplier * stdDevValue;
		var upperClose = smaValue + CloseMultiplier * stdDevValue;
		var lowerClose = smaValue - CloseMultiplier * stdDevValue;

		if (Position <= 0 && candle.ClosePrice > upperOpen)
			BuyMarket();

		if (Position >= 0 && candle.ClosePrice < lowerOpen)
			SellMarket();

		if (Position > 0 && candle.ClosePrice < upperClose)
			SellMarket();

		if (Position < 0 && candle.ClosePrice > lowerClose)
			BuyMarket();
	}
}
