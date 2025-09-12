using System;
using System.Collections.Generic;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new EMA { Length = EmaLength };
		var st1 = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier1 };
		var st2 = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier2 };
		var st3 = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier3 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, st1, st2, st3, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, st1);
			DrawIndicator(area, st2);
			DrawIndicator(area, st3);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, IIndicatorValue st1Val, IIndicatorValue st2Val, IIndicatorValue st3Val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var st1 = (SuperTrendIndicatorValue)st1Val;
		var st2 = (SuperTrendIndicatorValue)st2Val;
		var st3 = (SuperTrendIndicatorValue)st3Val;

		var longCond = st1.IsUpTrend && st2.IsUpTrend && st3.IsUpTrend && candle.ClosePrice > emaVal;
		var shortCond = !st1.IsUpTrend && !st2.IsUpTrend && !st3.IsUpTrend && candle.ClosePrice < emaVal;

		if (longCond && Position <= 0)
			BuyMarket();
		else if (shortCond && Position >= 0)
			SellMarket();

		if (Position > 0 && !st3.IsUpTrend)
			SellMarket(Position);
		else if (Position < 0 && st3.IsUpTrend)
			BuyMarket(-Position);
	}
}
