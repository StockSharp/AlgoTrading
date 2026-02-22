using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Monte Carlo simulation to forecast price range.
/// </summary>
public class MonteCarloRangeForecastStrategy : Strategy
{
	private readonly StrategyParam<int> _forecastPeriod;
	private readonly StrategyParam<int> _simulations;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Random _random = new(42);

	public int ForecastPeriod { get => _forecastPeriod.Value; set => _forecastPeriod.Value = value; }
	public int Simulations { get => _simulations.Value; set => _simulations.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MonteCarloRangeForecastStrategy()
	{
		_forecastPeriod = Param(nameof(ForecastPeriod), 20).SetGreaterThanZero();
		_simulations = Param(nameof(Simulations), 100).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = 14 };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var current = candle.ClosePrice;
		if (current <= 0m || atr <= 0m)
			return;

		var stepVol = atr / current;
		double sum = 0.0;

		for (var i = 0; i < Simulations; i++)
		{
			var price = current;
			for (var j = 0; j < ForecastPeriod; j++)
			{
				var rnd = NextGaussian();
				price += price * stepVol * rnd;
			}
			sum += (double)price;
		}

		var mean = sum / Simulations;

		if (mean > (double)current && Position <= 0)
			BuyMarket();
		else if (mean < (double)current && Position >= 0)
			SellMarket();
	}

	private decimal NextGaussian()
	{
		var u1 = 1.0 - _random.NextDouble();
		var u2 = 1.0 - _random.NextDouble();
		return (decimal)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
	}
}
