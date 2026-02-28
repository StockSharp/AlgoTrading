namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Stochastic Oscillator with Dynamic Overbought/Oversold Zones.
/// </summary>
public class StochasticWithDynamicZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stdDevFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevStochK;
	private decimal _stochSum;
	private decimal _stochSqSum;
	private int _stochCount;
	private readonly Queue<decimal> _stochQueue = new();

	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	public decimal StdDevFactor
	{
		get => _stdDevFactor.Value;
		set => _stdDevFactor.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public StochasticWithDynamicZonesStrategy()
	{
		_stochKPeriod = Param(nameof(StochKPeriod), 3)
			.SetDisplay("Stoch %K Period", "Smoothing period for %K", "Indicators");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetDisplay("Stoch %D Period", "Smoothing period for %D", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetDisplay("Lookback Period", "Period for dynamic zones", "Indicators");

		_stdDevFactor = Param(nameof(StdDevFactor), 1.5m)
			.SetDisplay("StdDev Factor", "Factor for dynamic zones", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevStochK = 50;
		_stochSum = 0;
		_stochSqSum = 0;
		_stochCount = 0;
		_stochQueue.Clear();

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;

		if (stochTyped.K is not decimal stochK)
			return;

		// Maintain running stats for dynamic zones
		_stochQueue.Enqueue(stochK);
		_stochSum += stochK;
		_stochSqSum += stochK * stochK;
		_stochCount++;

		if (_stochCount > LookbackPeriod)
		{
			var removed = _stochQueue.Dequeue();
			_stochSum -= removed;
			_stochSqSum -= removed * removed;
			_stochCount = LookbackPeriod;
		}

		if (_stochCount < LookbackPeriod)
		{
			_prevStochK = stochK;
			return;
		}

		var avg = _stochSum / _stochCount;
		var variance = (_stochSqSum / _stochCount) - (avg * avg);
		var stdDev = variance <= 0 ? 0m : (decimal)Math.Sqrt((double)variance);

		var dynamicOversold = avg - StdDevFactor * stdDev;
		var dynamicOverbought = avg + StdDevFactor * stdDev;

		var isReversingUp = stochK > _prevStochK;
		var isReversingDown = stochK < _prevStochK;

		if (stochK < dynamicOversold && isReversingUp && Position <= 0)
		{
			BuyMarket();
		}
		else if (stochK > dynamicOverbought && isReversingDown && Position >= 0)
		{
			SellMarket();
		}
		else if (Position > 0 && stochK > 50)
		{
			SellMarket();
		}
		else if (Position < 0 && stochK < 50)
		{
			BuyMarket();
		}

		_prevStochK = stochK;
	}
}
