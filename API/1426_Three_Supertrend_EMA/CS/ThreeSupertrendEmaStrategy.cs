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
/// Three Supertrend lines with EMA filter.
/// </summary>
public class ThreeSupertrendEmaStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier1;
	private readonly StrategyParam<decimal> _multiplier2;
	private readonly StrategyParam<decimal> _multiplier3;
	private readonly StrategyParam<DataType> _candleType;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Multiplier1 { get => _multiplier1.Value; set => _multiplier1.Value = value; }
	public decimal Multiplier2 { get => _multiplier2.Value; set => _multiplier2.Value = value; }
	public decimal Multiplier3 { get => _multiplier3.Value; set => _multiplier3.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ThreeSupertrendEmaStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 50);
		_atrPeriod = Param(nameof(AtrPeriod), 10);
		_multiplier1 = Param(nameof(Multiplier1), 3m);
		_multiplier2 = Param(nameof(Multiplier2), 2m);
		_multiplier3 = Param(nameof(Multiplier3), 1m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var st1 = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier1 };
		var st2 = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier2 };
		var st3 = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier3 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, st1, st2, st3, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaIv, IIndicatorValue st1Val, IIndicatorValue st2Val, IIndicatorValue st3Val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (st1Val is not SuperTrendIndicatorValue st1 ||
			st2Val is not SuperTrendIndicatorValue st2 ||
			st3Val is not SuperTrendIndicatorValue st3)
			return;

		var emaVal = emaIv.GetValue<decimal>();

		// Check exits first
		if (Position > 0 && !st3.IsUpTrend)
		{
			SellMarket();
			return;
		}
		else if (Position < 0 && st3.IsUpTrend)
		{
			BuyMarket();
			return;
		}

		// Entries when flat
		if (Position == 0)
		{
			var longCond = st1.IsUpTrend && st2.IsUpTrend && st3.IsUpTrend && candle.ClosePrice > emaVal;
			var shortCond = !st1.IsUpTrend && !st2.IsUpTrend && !st3.IsUpTrend && candle.ClosePrice < emaVal;

			if (longCond)
				BuyMarket();
			else if (shortCond)
				SellMarket();
		}
	}
}
