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
/// Linear Regression (All Data) strategy.
/// Calculates linear regression using all available bars and trades based on deviation from regression line.
/// </summary>
public class LinearRegressionAllDataStrategy : Strategy
{
	private readonly StrategyParam<int> _maxBarsBack;
	private readonly StrategyParam<decimal> _deviationThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private long _index;
	private decimal _sumX;
	private decimal _sumY;
	private decimal _sumX2;
	private decimal _sumXY;

	public int MaxBarsBack { get => _maxBarsBack.Value; set => _maxBarsBack.Value = value; }
	public decimal DeviationThreshold { get => _deviationThreshold.Value; set => _deviationThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LinearRegressionAllDataStrategy()
	{
		_maxBarsBack = Param(nameof(MaxBarsBack), 5000)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars Back", "Maximum number of bars for drawing", "General");

		_deviationThreshold = Param(nameof(DeviationThreshold), 0.002m)
			.SetDisplay("Deviation Threshold", "Deviation from regression to trigger trade", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_index = 0;
		_sumX = 0m;
		_sumY = 0m;
		_sumX2 = 0m;
		_sumXY = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
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

		// Mean reversion: buy when price below regression, sell when above
		if (deviation < -DeviationThreshold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (deviation > DeviationThreshold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
