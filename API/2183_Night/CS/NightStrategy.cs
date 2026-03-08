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
/// Night trading strategy based on the Stochastic Oscillator.
/// Trades only during night hours when the market is quiet.
/// </summary>
public class NightStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<DataType> _candleType;

	public decimal StochOversold { get => _stochOversold.Value; set => _stochOversold.Value = value; }
	public decimal StochOverbought { get => _stochOverbought.Value; set => _stochOverbought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NightStrategy()
	{
		_stochOversold = Param(nameof(StochOversold), 30m)
			.SetDisplay("Stochastic Oversold", "Oversold level for %K", "Indicators");

		_stochOverbought = Param(nameof(StochOverbought), 70m)
			.SetDisplay("Stochastic Overbought", "Overbought level for %K", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = 14;
		stochastic.D.Length = 3;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (IStochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal kValue)
			return;

		// Trade only during night hours 21:00-06:00
		var hour = candle.OpenTime.Hour;
		var isNight = hour >= 21 || hour < 6;

		if (!isNight)
			return;

		if (kValue < StochOversold && Position <= 0)
			BuyMarket();
		else if (kValue > StochOverbought && Position >= 0)
			SellMarket();
	}
}
