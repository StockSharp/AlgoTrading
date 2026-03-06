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
	private readonly StrategyParam<decimal> _minForecastEdgePercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _returns = new();
	private decimal? _prevClose;
	private int _barsFromSignal;

	public int ForecastBars { get => _forecastBars.Value; set => _forecastBars.Value = value; }
	public int Simulations { get => _simulations.Value; set => _simulations.Value = value; }
	public int DataLength { get => _dataLength.Value; set => _dataLength.Value = value; }
	public decimal MinForecastEdgePercent { get => _minForecastEdgePercent.Value; set => _minForecastEdgePercent.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MonteCarloSimulationRandomWalkStrategy()
	{
		_forecastBars = Param(nameof(ForecastBars), 10).SetGreaterThanZero();
		_simulations = Param(nameof(Simulations), 100).SetGreaterThanZero();
		_dataLength = Param(nameof(DataLength), 100).SetGreaterThanZero();
		_minForecastEdgePercent = Param(nameof(MinForecastEdgePercent), 0.5m).SetGreaterThanZero();
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_returns.Clear();
		_prevClose = null;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_returns.Clear();
		_prevClose = null;
		_barsFromSignal = SignalCooldownBars;

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

		_barsFromSignal++;

		var history = _returns.ToArray();
		var avg = history.Average();
		var variance = history.Select(r => (r - avg) * (r - avg)).Average();
		var drift = avg - variance / 2m;
		var random = new Random(unchecked((int)candle.OpenTime.Ticks));

		double sum = 0;
		for (var sim = 0; sim < Simulations; sim++)
		{
			var price = (double)candle.ClosePrice;
			for (var step = 0; step < ForecastBars; step++)
			{
				var idx = random.Next(history.Length);
				price *= Math.Exp((double)(history[idx] + drift));
			}
			sum += price;
		}

		var meanForecast = (decimal)(sum / Simulations);
		var current = candle.ClosePrice;
		var edgePercent = (meanForecast - current) / current * 100m;

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
}
