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
/// Strategy based on Stochastic extremes.
/// Opens positions when Stochastic reaches overbought/oversold levels.
/// Closes on reversal signal.
/// </summary>
public class ElliottTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;

	public int StochLength { get => _stochLength.Value; set => _stochLength.Value = value; }
	public decimal OverboughtLevel { get => _overbought.Value; set => _overbought.Value = value; }
	public decimal OversoldLevel { get => _oversold.Value; set => _oversold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ElliottTraderStrategy()
	{
		_stochLength = Param(nameof(StochLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Length for %K", "Indicator");

		_overbought = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay("Overbought", "Level to start selling", "Indicator");

		_oversold = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Oversold", "Level to start buying", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculation", "General");
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

		var stochastic = new StochasticOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sv = (StochasticOscillatorValue)stochValue;
		if (sv.K is not decimal kValue)
			return;

		if (Position > 0)
		{
			if (kValue >= 90m)
			{
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			if (kValue <= 10m)
			{
				BuyMarket();
				return;
			}
		}

		if (Position == 0)
		{
			if (kValue >= OverboughtLevel)
			{
				SellMarket();
			}
			else if (kValue <= OversoldLevel)
			{
				BuyMarket();
			}
		}
	}
}
