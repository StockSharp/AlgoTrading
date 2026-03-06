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
	private readonly StrategyParam<decimal> _minForecastEdgePercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private int _barsFromSignal;

	public int ForecastPeriod { get => _forecastPeriod.Value; set => _forecastPeriod.Value = value; }
	public int Simulations { get => _simulations.Value; set => _simulations.Value = value; }
	public decimal MinForecastEdgePercent { get => _minForecastEdgePercent.Value; set => _minForecastEdgePercent.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MonteCarloRangeForecastStrategy()
	{
		_forecastPeriod = Param(nameof(ForecastPeriod), 20).SetGreaterThanZero();
		_simulations = Param(nameof(Simulations), 100).SetGreaterThanZero();
		_minForecastEdgePercent = Param(nameof(MinForecastEdgePercent), 0.25m).SetGreaterThanZero();
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 10).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);
		_barsFromSignal = SignalCooldownBars;

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

		_barsFromSignal++;

		var stepVol = atr / current;
		double sum = 0.0;
		var random = new Random(unchecked((int)candle.OpenTime.Ticks));

		for (var i = 0; i < Simulations; i++)
		{
			var price = current;
			for (var j = 0; j < ForecastPeriod; j++)
			{
				var rnd = NextGaussian(random);
				price += price * stepVol * rnd;
			}
			sum += (double)price;
		}

		var mean = sum / Simulations;
		var edgePercent = (decimal)((mean - (double)current) / (double)current * 100d);

		if (_barsFromSignal >= SignalCooldownBars && edgePercent >= MinForecastEdgePercent && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (_barsFromSignal >= SignalCooldownBars && edgePercent <= -MinForecastEdgePercent && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}
	}

	private static decimal NextGaussian(Random random)
	{
		var u1 = 1.0 - random.NextDouble();
		var u2 = 1.0 - random.NextDouble();
		return (decimal)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
	}
}
