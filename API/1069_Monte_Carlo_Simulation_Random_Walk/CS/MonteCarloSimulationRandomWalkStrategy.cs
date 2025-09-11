using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MonteCarloSimulationRandomWalkStrategy : Strategy
{
	private readonly StrategyParam<int> _numberOfBarsToPredict;
	private readonly StrategyParam<int> _numberOfSimulations;
	private readonly StrategyParam<int> _dataLength;
	private readonly StrategyParam<bool> _keepPastMinMaxLevels;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _returns = new();
	private decimal? _prevClose;
	private decimal? _maxLevel;
	private decimal? _minLevel;
	private readonly Random _random = new();

	public int NumberOfBarsToPredict
	{
		get => _numberOfBarsToPredict.Value;
		set => _numberOfBarsToPredict.Value = value;
	}

	public int NumberOfSimulations
	{
		get => _numberOfSimulations.Value;
		set => _numberOfSimulations.Value = value;
	}

	public int DataLength
	{
		get => _dataLength.Value;
		set => _dataLength.Value = value;
	}

	public bool KeepPastMinMaxLevels
	{
		get => _keepPastMinMaxLevels.Value;
		set => _keepPastMinMaxLevels.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MonteCarloSimulationRandomWalkStrategy()
	{
		_numberOfBarsToPredict = Param(nameof(NumberOfBarsToPredict), 50)
		.SetGreaterThanZero()
		.SetDisplay("Bars to Predict", "Prediction horizon", "Simulation")
		.SetCanOptimize(true)
		.SetOptimize(10, 100, 10);

		_numberOfSimulations = Param(nameof(NumberOfSimulations), 500)
		.SetGreaterThanZero()
		.SetDisplay("Simulations", "Number of random paths", "Simulation")
		.SetCanOptimize(true)
		.SetOptimize(100, 1000, 100);

		_dataLength = Param(nameof(DataLength), 2000)
		.SetGreaterThanZero()
		.SetDisplay("Data Length", "Number of history bars", "Simulation");

		_keepPastMinMaxLevels = Param(nameof(KeepPastMinMaxLevels), false)
		.SetDisplay("Keep Past Levels", "Keep previous min-max values", "Simulation");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_returns.Clear();
		_prevClose = null;
		_maxLevel = null;
		_minLevel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_prevClose is decimal prevClose)
		{
			var ret = (decimal)Math.Log((double)(candle.ClosePrice / prevClose));
			_returns.Insert(0, ret);
			if (_returns.Count > DataLength)
			_returns.RemoveAt(_returns.Count - 1);
		}

		_prevClose = candle.ClosePrice;

		if (_returns.Count == 0)
		return;

		var avg = _returns.Average();
		var variance = _returns.Select(r => (r - avg) * (r - avg)).Average();
		var drift = avg - variance / 2m;

		var maxCls = Enumerable.Repeat(decimal.MinValue, NumberOfBarsToPredict).ToArray();
		var minCls = Enumerable.Repeat(decimal.MaxValue, NumberOfBarsToPredict).ToArray();

		for (var sim = 0; sim < NumberOfSimulations; sim++)
		{
			var lastClose = candle.ClosePrice;

			for (var step = 0; step < NumberOfBarsToPredict; step++)
			{
				var index = _random.Next(_returns.Count);
				var nextClose = Math.Max(0m, lastClose * (decimal)Math.Exp((double)(_returns[index] + drift)));
				lastClose = nextClose;

				if (nextClose > maxCls[step])
				maxCls[step] = nextClose;

				if (nextClose < minCls[step])
				minCls[step] = nextClose;
			}
		}

		var newMax = maxCls[0];
		var newMin = minCls[0];

		_maxLevel = KeepPastMinMaxLevels && _maxLevel != null ? Math.Max(_maxLevel.Value, newMax) : newMax;
		_minLevel = KeepPastMinMaxLevels && _minLevel != null ? Math.Min(_minLevel.Value, newMin) : newMin;

		LogInfo($"Max level: {_maxLevel:0.####}, Min level: {_minLevel:0.####}");
	}
}
