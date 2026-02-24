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
/// Strategy that buys when the stochastic oscillator crosses up in oversold zone
/// and sells when it crosses down in overbought zone.
/// </summary>
public class UniversalEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;

	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UniversalEaStrategy()
	{
		_oversold = Param(nameof(Oversold), 20m)
			.SetDisplay("Oversold", "%K oversold threshold", "Stochastic");

		_overbought = Param(nameof(Overbought), 80m)
			.SetDisplay("Overbought", "%K overbought threshold", "Stochastic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (_prevK != 0)
		{
			// Buy when %K crosses above %D in the oversold zone
			if (_prevK < _prevD && k > d && k < Oversold && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			// Sell when %K crosses below %D in the overbought zone
			else if (_prevK > _prevD && k < d && k > Overbought && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}

		_prevK = k;
		_prevD = d;
	}
}
