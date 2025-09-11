using System;
using System.Collections.Generic;

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
	private readonly Random _random = new();

	/// <summary>
	/// Forecast length in candles.
	/// </summary>
	public int ForecastPeriod
	{
		get => _forecastPeriod.Value;
		set => _forecastPeriod.Value = value;
	}

	/// <summary>
	/// Number of Monte Carlo simulations.
	/// </summary>
	public int Simulations
	{
		get => _simulations.Value;
		set => _simulations.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MonteCarloRangeForecastStrategy"/>.
	/// </summary>
	public MonteCarloRangeForecastStrategy()
	{
		_forecastPeriod = Param(nameof(ForecastPeriod), 20)
			.SetDisplay("Forecast Period", "Number of candles to simulate", "Monte Carlo")
			.SetGreaterThanZero();
		_simulations = Param(nameof(Simulations), 100)
			.SetDisplay("Simulations", "Number of Monte Carlo paths", "Monte Carlo")
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = 14 };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}

		StartProtection();
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
		double sum2 = 0.0;

		for (var i = 0; i < Simulations; i++)
		{
			var price = current;

			for (var j = 0; j < ForecastPeriod; j++)
			{
				var rnd = NextGaussian();
				price += price * stepVol * rnd;
			}

			var p = (double)price;
			sum += p;
			sum2 += p * p;
		}

		var mean = sum / Simulations;
		var variance = (sum2 / Simulations) - (mean * mean);
		var std = Math.Sqrt(Math.Max(variance, 0.0));

		if (mean > (double)current && Position <= 0)
			BuyMarket();
		else if (mean < (double)current && Position >= 0)
			SellMarket();
	}

	private decimal NextGaussian()
	{
		var u1 = 1.0 - _random.NextDouble();
		var u2 = 1.0 - _random.NextDouble();
		var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
		return (decimal)randStdNormal;
	}
}
