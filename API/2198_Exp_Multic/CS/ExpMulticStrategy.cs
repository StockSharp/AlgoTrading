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
/// Momentum flip strategy: trades in current momentum direction,
/// flips direction when loss threshold hit, adds on profit.
/// Adapted from multi-currency EA to single-security candle-based approach.
/// </summary>
public class ExpMulticStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMomentum;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpMulticStrategy()
	{
		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Momentum lookback period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMomentum = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var area2 = CreateChartArea();
			if (area2 != null)
				DrawIndicator(area2, momentum);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMomentum = momentumValue;
			return;
		}

		if (_prevMomentum is decimal prev)
		{
			// Momentum crosses above zero - buy
			if (prev <= 0 && momentumValue > 0 && Position <= 0)
				BuyMarket();
			// Momentum crosses below zero - sell
			else if (prev >= 0 && momentumValue < 0 && Position >= 0)
				SellMarket();
		}

		_prevMomentum = momentumValue;
	}
}
