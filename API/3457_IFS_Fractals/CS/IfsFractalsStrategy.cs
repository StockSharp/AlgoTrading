using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader script IFS_Fractals.
/// Recreates the iterated function system (IFS) point cloud and converts its dynamics into directional trading signals.
/// The strategy iterates the original 28 affine transforms on each finished candle.
/// It smooths the resulting X coordinate with an EMA and trades when the fractal momentum crosses configurable thresholds.
/// </summary>
public class IfsFractalsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _iterationsPerCandle;
	private readonly StrategyParam<decimal> _scale;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _smoothingPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private readonly decimal[] _ifsA =
	{
		0.00m, 0.03m, 0.00m, 0.09m, 0.00m, 0.03m, -0.00m, 0.07m, 0.00m, 0.07m, 0.03m, 0.03m, 0.03m, 0.00m,
		0.04m, 0.04m, -0.00m, 0.09m, 0.03m, 0.03m, 0.03m, 0.03m, 0.03m, 0.00m, 0.05m, -0.00m, 0.05m, 0.00m,
	};

	private readonly decimal[] _ifsB =
	{
		-0.11m, 0.00m, 0.07m, 0.00m, -0.07m, 0.00m, -0.11m, 0.00m, -0.07m, 0.00m, -0.11m, 0.11m, 0.00m, -0.14m,
		-0.12m, 0.12m, -0.11m, 0.00m, -0.11m, 0.11m, 0.00m, -0.11m, 0.11m, -0.11m, 0.00m, -0.07m, 0.00m, -0.07m,
	};

	private readonly decimal[] _ifsC =
	{
		0.12m, 0.00m, 0.08m, -0.00m, 0.08m, 0.00m, 0.12m, 0.00m, 0.04m, 0.00m, 0.12m, -0.12m, 0.00m, 0.12m,
		0.06m, -0.06m, 0.10m, 0.00m, 0.12m, -0.12m, 0.00m, 0.12m, -0.12m, 0.12m, 0.00m, 0.04m, 0.00m, 0.12m,
	};

	private readonly decimal[] _ifsD =
	{
		0.00m, 0.05m, 0.00m, 0.07m, 0.00m, 0.05m, 0.00m, 0.07m, 0.00m, 0.07m, 0.00m, 0.00m, 0.07m, 0.00m,
		0.00m, 0.00m, 0.00m, 0.07m, 0.00m, 0.00m, 0.07m, 0.00m, 0.00m, 0.00m, 0.07m, 0.00m, 0.07m, 0.00m,
	};

	private readonly decimal[] _ifsE =
	{
		-4.58m, -5.06m, -5.16m, -4.70m, -4.09m, -4.35m, -3.73m, -3.26m, -2.76m, -3.26m, -2.22m, -1.86m, -2.04m, -0.98m,
		-0.46m, -0.76m, 0.76m, 0.63m, 1.78m, 2.14m, 1.96m, 3.11m, 3.47m, 4.27m, 4.60m, 4.98m, 4.60m, 5.24m,
	};

	private readonly decimal[] _ifsF =
	{
		1.26m, 0.89m, 1.52m, 2.00m, 1.52m, 0.89m, 1.43m, 1.96m, 1.69m, 1.24m, 1.43m, 1.41m, 1.11m, 1.43m,
		1.79m, 1.05m, 1.32m, 1.96m, 1.43m, 1.41m, 1.11m, 1.43m, 1.41m, 1.43m, 1.42m, 1.16m, 0.71m, 1.43m,
	};

	private readonly decimal[] _ifsProbabilities =
	{
		35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m,
		35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m, 35m,
	};

	private readonly decimal[] _cumulativeProbabilities;

	private readonly Random _random;

	private ExponentialMovingAverage _fractalEma = null!;

	private decimal _totalProbability;
	private decimal _currentX;
	private decimal _currentY;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public IfsFractalsStrategy()
	{

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles driving the fractal iteration", "General");

		_iterationsPerCandle = Param(nameof(IterationsPerCandle), 500)
			.SetRange(100, 5000)
			.SetDisplay("Iterations", "Number of IFS iterations processed per finished candle", "Fractal");

		_scale = Param(nameof(Scale), 50m)
			.SetRange(10m, 200m)
			.SetDisplay("Scale", "Scaling factor matching the original bitmap projection", "Fractal");

		_entryThreshold = Param(nameof(EntryThreshold), 0.20m)
			.SetRange(0.05m, 1.00m)
			.SetDisplay("Entry Threshold", "Normalized EMA value required to open a position", "Trading")
			.SetCanOptimize(true);

		_exitThreshold = Param(nameof(ExitThreshold), 0.05m)
			.SetRange(0.01m, 0.50m)
			.SetDisplay("Exit Threshold", "Normalized EMA value that closes an open trade", "Trading");

		_smoothingPeriod = Param(nameof(SmoothingPeriod), 14)
			.SetRange(5, 60)
			.SetDisplay("EMA Period", "Length of the smoothing EMA applied to the fractal signal", "Fractal")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetRange(0m, 5m)
			.SetDisplay("Take Profit", "Absolute take-profit distance; set to 0 to disable", "Risk Management");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetRange(0m, 5m)
			.SetDisplay("Stop Loss", "Absolute stop-loss distance; set to 0 to disable", "Risk Management");

		_cumulativeProbabilities = new decimal[_ifsProbabilities.Length];
		_random = new Random();
	}


	/// <summary>
	/// Candle type that triggers fractal iterations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of IFS iterations executed every finished candle.
	/// </summary>
	public int IterationsPerCandle
	{
		get => _iterationsPerCandle.Value;
		set => _iterationsPerCandle.Value = value;
	}

	/// <summary>
	/// Scaling constant used to normalize the X coordinate.
	/// </summary>
	public decimal Scale
	{
		get => _scale.Value;
		set => _scale.Value = value;
	}

	/// <summary>
	/// EMA threshold that must be crossed to open a new position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// EMA threshold that forces an exit when the signal mean reverts.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Length of the smoothing EMA applied to the fractal signal.
	/// </summary>
	public int SmoothingPeriod
	{
		get => _smoothingPeriod.Value;
		set => _smoothingPeriod.Value = value;
	}

	/// <summary>
	/// Absolute take-profit distance for protective orders.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Absolute stop-loss distance for protective orders.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentX = 0m;
		_currentY = 0m;
		_fractalEma?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		BuildProbabilityTable();

		_fractalEma = new ExponentialMovingAverage
		{
			Length = SmoothingPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fractalEma);
			DrawOwnTrades(area);
		}

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		IterateFractal(IterationsPerCandle);

		var normalizedX = Scale != 0m ? _currentX / Scale : _currentX;

		var emaValue = _fractalEma.Process(normalizedX, candle.CloseTime, true);
		if (!_fractalEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var smoothed = emaValue.ToDecimal();
		var position = Position;
		var volume = Volume;

		if (smoothed >= EntryThreshold && position <= 0m)
		{
			if (position < 0m)
			{
				BuyMarket(Math.Abs(position));
			}

			BuyMarket(volume);
			LogInfo($"Fractal long entry triggered at {smoothed:F4}");
		}
		else if (smoothed <= -EntryThreshold && position >= 0m)
		{
			if (position > 0m)
			{
				SellMarket(position);
			}

			SellMarket(volume);
			LogInfo($"Fractal short entry triggered at {smoothed:F4}");
		}
		else if (position > 0m && smoothed <= ExitThreshold)
		{
			SellMarket(position);
			LogInfo($"Fractal long exit at {smoothed:F4}");
		}
		else if (position < 0m && smoothed >= -ExitThreshold)
		{
			BuyMarket(Math.Abs(position));
			LogInfo($"Fractal short exit at {smoothed:F4}");
		}
	}

	private void IterateFractal(int iterations)
	{
		for (var i = 0; i < iterations; i++)
		{
			var randomValue = (decimal)_random.NextDouble() * _totalProbability;

			var index = 0;
			for (var k = 0; k < _cumulativeProbabilities.Length; k++)
			{
				if (randomValue <= _cumulativeProbabilities[k])
				{
					index = k;
					break;
				}
			}

			var nextX = _ifsA[index] * _currentX + _ifsB[index] * _currentY + _ifsE[index];
			var nextY = _ifsC[index] * _currentX + _ifsD[index] * _currentY + _ifsF[index];

			_currentX = nextX;
			_currentY = nextY;
		}
	}

	private void BuildProbabilityTable()
	{
		_totalProbability = 0m;
		for (var i = 0; i < _ifsProbabilities.Length; i++)
		{
			_totalProbability += _ifsProbabilities[i];
			_cumulativeProbabilities[i] = _totalProbability;
		}
	}
}
