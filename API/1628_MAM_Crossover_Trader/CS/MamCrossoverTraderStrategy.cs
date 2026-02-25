using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MAM crossover using SMA of close and open prices.
/// </summary>
public class MamCrossoverTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _openSma;

	private decimal? _prevDiff1;
	private decimal? _prevDiff2;

	public MamCrossoverTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Simple moving average period", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDiff1 = null;
		_prevDiff2 = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var closeSma = new SimpleMovingAverage { Length = MaPeriod };
		_openSma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(closeSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, closeSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openInput = new DecimalIndicatorValue(_openSma, candle.OpenPrice, candle.OpenTime) { IsFinal = true };
		var openSmaValue = _openSma.Process(openInput);
		if (!openSmaValue.IsFormed || !openSmaValue.IsFinal)
			return;

		var openSma = openSmaValue.GetValue<decimal>();

		var diff = closeSma - openSma;

		if (_prevDiff1.HasValue && _prevDiff2.HasValue)
		{
			var crossUp = _prevDiff2.Value < 0 && _prevDiff1.Value > 0 && diff > 0;
			var crossDown = _prevDiff2.Value > 0 && _prevDiff1.Value < 0 && diff < 0;

			if (crossUp && Position <= 0)
				BuyMarket();
			else if (crossDown && Position >= 0)
				SellMarket();
		}

		_prevDiff2 = _prevDiff1;
		_prevDiff1 = diff;
	}
}
