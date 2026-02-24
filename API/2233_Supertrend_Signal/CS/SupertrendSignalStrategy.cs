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
/// Simple SuperTrend crossover strategy.
/// </summary>
public class SupertrendSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevIsUpTrend;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SupertrendSignalStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 5)
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "Parameters");

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevIsUpTrend = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var st = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(st, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stValue.IsFormed)
			return;

		var stv = stValue as SuperTrendIndicatorValue;
		if (stv == null)
			return;

		var isUpTrend = stv.IsUpTrend;

		if (IsFormedAndOnlineAndAllowTrading() && _prevIsUpTrend.HasValue)
		{
			if (isUpTrend && !_prevIsUpTrend.Value && Position <= 0)
				BuyMarket();
			else if (!isUpTrend && _prevIsUpTrend.Value && Position >= 0)
				SellMarket();
		}

		_prevIsUpTrend = isUpTrend;
	}
}
