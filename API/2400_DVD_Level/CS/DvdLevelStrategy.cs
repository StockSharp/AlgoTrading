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
/// RAVI based level strategy. Opens long when the RAVI crosses below zero and short when above.
/// </summary>
public class DvdLevelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly ExponentialMovingAverage _emaFast = new() { Length = 2 };
	private readonly ExponentialMovingAverage _emaSlow = new() { Length = 24 };
	private decimal _prevRavi;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DvdLevelStrategy()
	{
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

		var sub = SubscribeCandles(CandleType);
		sub.Bind(_emaFast, _emaSlow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished || emaSlow == 0)
			return;

		var ravi = (emaFast - emaSlow) / emaSlow * 100m;

		if (!_hasPrev)
		{
			_prevRavi = ravi;
			_hasPrev = true;
			return;
		}

		var crossAbove = _prevRavi <= 0 && ravi > 0;
		var crossBelow = _prevRavi >= 0 && ravi < 0;

		if (crossBelow && Position <= 0)
			BuyMarket();
		else if (crossAbove && Position >= 0)
			SellMarket();

		_prevRavi = ravi;
	}
}
