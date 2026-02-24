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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI strategy that trades only inside a selected session.
/// </summary>
public class TutorialAddingSessionsToStrategiesStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _upper;
	private readonly StrategyParam<decimal> _lower;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal Upper { get => _upper.Value; set => _upper.Value = value; }
	public decimal Lower { get => _lower.Value; set => _lower.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TutorialAddingSessionsToStrategiesStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "RSI");

		_upper = Param(nameof(Upper), 70m)
			.SetDisplay("Upper Level", "Overbought threshold", "RSI");

		_lower = Param(nameof(Lower), 30m)
			.SetDisplay("Lower Level", "Oversold threshold", "RSI");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRsi == 0)
		{
			_prevRsi = rsi;
			return;
		}

		// RSI crossover signals
		if (_prevRsi <= Lower && rsi > Lower && Position <= 0)
		{
			BuyMarket();
		}
		else if (_prevRsi >= Upper && rsi < Upper && Position >= 0)
		{
			SellMarket();
		}

		_prevRsi = rsi;
	}
}
