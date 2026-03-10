using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Linear Regression (All Data) strategy.
/// Calculates linear regression using all available bars and trades based on deviation from regression line.
/// </summary>
public class LinearRegressionAllDataStrategy : Strategy
{
	private readonly StrategyParam<int> _maxBarsBack;
	private readonly StrategyParam<decimal> _deviationThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private long _index;
	private decimal _sumX;
	private decimal _sumY;
	private decimal _sumX2;
	private decimal _sumXY;
	private int _barsFromSignal;

	public int MaxBarsBack { get => _maxBarsBack.Value; set => _maxBarsBack.Value = value; }
	public decimal DeviationThreshold { get => _deviationThreshold.Value; set => _deviationThreshold.Value = value; }
	public decimal ExitThreshold { get => _exitThreshold.Value; set => _exitThreshold.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LinearRegressionAllDataStrategy()
	{
		_maxBarsBack = Param(nameof(MaxBarsBack), 5000)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars Back", "Maximum number of bars for drawing", "General");

		_deviationThreshold = Param(nameof(DeviationThreshold), 0.008m)
			.SetDisplay("Deviation Threshold", "Deviation from regression to trigger trade", "General");

		_exitThreshold = Param(nameof(ExitThreshold), 0.002m)
			.SetDisplay("Exit Threshold", "Deviation level to close a position", "General");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars between signals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_index = 0;
		_sumX = 0m;
		_sumY = 0m;
		_sumX2 = 0m;
		_sumXY = 0m;
		_barsFromSignal = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_index = 0;
		_sumX = 0m;
		_sumY = 0m;
		_sumX2 = 0m;
		_sumXY = 0m;
		_barsFromSignal = int.MaxValue;

		var dummyEma1 = new ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(dummyEma1, dummyEma2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_index++;

		var x = (decimal)_index;
		var y = candle.ClosePrice;

		_sumX += x;
		_sumY += y;
		_sumX2 += x * x;
		_sumXY += x * y;

		if (_index < 20)
			return;

		var n = (decimal)_index;
		var denom = n * _sumX2 - _sumX * _sumX;
		if (denom == 0)
			return;

		var slope = (n * _sumXY - _sumX * _sumY) / denom;
		var intercept = (_sumY - slope * _sumX) / n;

		// Current predicted value from regression
		var predicted = slope * x + intercept;

		if (predicted == 0)
			return;

		// Deviation from regression line
		var deviation = (candle.ClosePrice - predicted) / predicted;
		_barsFromSignal++;

		if (_barsFromSignal < CooldownBars)
			return;

		if (Position == 0)
		{
			if (deviation <= -DeviationThreshold)
			{
				BuyMarket();
				_barsFromSignal = 0;
			}
			else if (deviation >= DeviationThreshold)
			{
				SellMarket();
				_barsFromSignal = 0;
			}

			return;
		}

		if (Position > 0 && deviation >= -ExitThreshold)
		{
			SellMarket();
			_barsFromSignal = 0;
		}
		else if (Position < 0 && deviation <= ExitThreshold)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
	}
}
