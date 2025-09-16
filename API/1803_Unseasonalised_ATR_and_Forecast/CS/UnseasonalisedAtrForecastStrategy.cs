
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Calculates average trading range and forecasts the next range using linear regression.
/// Does not place trades; outputs statistics only.
/// </summary>
public class UnseasonalisedAtrForecastStrategy : Strategy
{	private readonly StrategyParam<int> _sampleSizeParam;
	private readonly StrategyParam<decimal> _desiredRangeParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private readonly Queue<decimal> _ranges = new();

	private decimal _atr;
	private decimal _std;
	private decimal _forecast;
	private decimal _mape;

	/// <summary>
	/// Number of recent candles used for calculations.
	/// </summary>
	public int SampleSize
	{
		get => _sampleSizeParam.Value;
		set => _sampleSizeParam.Value = value;
	}

	/// <summary>
	/// Desired range for confidence interval calculation.
	/// </summary>
	public decimal DesiredRange
	{
		get => _desiredRangeParam.Value;
		set => _desiredRangeParam.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public UnseasonalisedAtrForecastStrategy()
	{
		_sampleSizeParam = Param(nameof(SampleSize), 36)
			.SetGreaterThanZero()
			.SetDisplay("Sample Size", "Number of candles for calculations", "Parameters");

		_desiredRangeParam = Param(nameof(DesiredRange), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Desired Range", "Target range for confidence interval", "Parameters");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for analysis", "Common");
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

		_ranges.Clear();
		_atr = 0m;
		_std = 0m;
		_forecast = 0m;
		_mape = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to candles of selected type
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}

		// Enable position protection in case of manual trading
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		_ranges.Enqueue(range);
		if (_ranges.Count > SampleSize)
			_ranges.Dequeue();

		if (_ranges.Count < SampleSize)
			return;

		// Calculate average range and standard deviation
		decimal sum = 0m;
		foreach (var r in _ranges)
			sum += r;
		_atr = sum / SampleSize;

		decimal diffSum = 0m;
		foreach (var r in _ranges)
		{
			var diff = r - _atr;
			diffSum += diff * diff;
		}
		_std = (decimal)Math.Sqrt((double)(diffSum / SampleSize));

		// Linear regression for forecast
		int n = SampleSize;
		decimal sumX = 0m;
		decimal sumY = 0m;
		decimal sumXY = 0m;
		decimal sumX2 = 0m;
		int i = 1;
		foreach (var r in _ranges)
		{
			sumX += i;
			sumY += r;
			sumXY += i * r;
			sumX2 += i * i;
			i++;
		}

		var meanX = sumX / n;
		var m = (sumXY - n * meanX * _atr) / (sumX2 - n * meanX * meanX);
		var c = _atr - m * meanX;
		_forecast = m * (n + 1) + c;

		// MAPE calculation
		decimal apeSum = 0m;
		i = 1;
		foreach (var r in _ranges)
		{
			var fitted = m * i + c;
			var ape = r != 0m ? Math.Abs((r - fitted) / r) : 0m;
			apeSum += ape;
			i++;
		}
		_mape = (apeSum / n) * 100m;

		// Confidence interval estimation
		var ci = (_atr - DesiredRange) / _std;
		decimal ciPercent;
		if (ci >= -1m && ci <= 1m)
			ciPercent = 68.26m;
		else if (ci >= -2m && ci <= 2m)
			ciPercent = 95.44m;
		else
			ciPercent = 99.74m;

		LogInfo($"ATR: {_atr:F5}, StdDev: {_std:F5}, CI: {ciPercent}%, Forecast: {_forecast:F5}, MAPE: {_mape:F2}%");
	}
}
