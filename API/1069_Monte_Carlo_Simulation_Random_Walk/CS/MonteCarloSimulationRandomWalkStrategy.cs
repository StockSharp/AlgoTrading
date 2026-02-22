using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monte Carlo simulation random walk strategy.
/// Uses MC simulation to estimate expected price range and trades based on mean forecast.
/// </summary>
public class MonteCarloSimulationRandomWalkStrategy : Strategy
{
	private readonly StrategyParam<int> _forecastBars;
	private readonly StrategyParam<int> _simulations;
	private readonly StrategyParam<int> _dataLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _returns = new();
	private decimal? _prevClose;
	private readonly Random _random = new(42);

	public int ForecastBars { get => _forecastBars.Value; set => _forecastBars.Value = value; }
	public int Simulations { get => _simulations.Value; set => _simulations.Value = value; }
	public int DataLength { get => _dataLength.Value; set => _dataLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MonteCarloSimulationRandomWalkStrategy()
	{
		_forecastBars = Param(nameof(ForecastBars), 10).SetGreaterThanZero();
		_simulations = Param(nameof(Simulations), 100).SetGreaterThanZero();
		_dataLength = Param(nameof(DataLength), 100).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_returns.Clear();
		_prevClose = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose is decimal prevClose && prevClose > 0)
		{
			var ret = (decimal)Math.Log((double)(candle.ClosePrice / prevClose));
			_returns.Add(ret);
			if (_returns.Count > DataLength)
				_returns.RemoveAt(0);
		}

		_prevClose = candle.ClosePrice;

		if (_returns.Count < 20)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var avg = _returns.Average();
		var variance = _returns.Select(r => (r - avg) * (r - avg)).Average();
		var drift = avg - variance / 2m;

		double sum = 0;
		for (var sim = 0; sim < Simulations; sim++)
		{
			var price = (double)candle.ClosePrice;
			for (var step = 0; step < ForecastBars; step++)
			{
				var idx = _random.Next(_returns.Count);
				price *= Math.Exp((double)(_returns[idx] + drift));
			}
			sum += price;
		}

		var meanForecast = (decimal)(sum / Simulations);
		var current = candle.ClosePrice;

		if (meanForecast > current * 1.001m && Position <= 0)
			BuyMarket();
		else if (meanForecast < current * 0.999m && Position >= 0)
			SellMarket();
	}
}
