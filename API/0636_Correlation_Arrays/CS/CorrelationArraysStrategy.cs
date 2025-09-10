using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Calculates correlation matrix for up to six securities.
/// </summary>
public class CorrelationArraysStrategy : Strategy
{
	private readonly StrategyParam<Security> _security1Param;
	private readonly StrategyParam<Security> _security2Param;
	private readonly StrategyParam<Security> _security3Param;
	private readonly StrategyParam<Security> _security4Param;
	private readonly StrategyParam<Security> _security5Param;
	private readonly StrategyParam<Security> _security6Param;
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _lookbackParam;
	private readonly StrategyParam<decimal> _positiveWeakParam;
	private readonly StrategyParam<decimal> _positiveMediumParam;
	private readonly StrategyParam<decimal> _positiveStrongParam;
	private readonly StrategyParam<decimal> _negativeWeakParam;
	private readonly StrategyParam<decimal> _negativeMediumParam;
	private readonly StrategyParam<decimal> _negativeStrongParam;

	private Security[] _securities;
	private readonly List<Queue<decimal>> _priceSeries = new();

	/// <summary>
	/// First security.
	/// </summary>
	public Security Security1
	{
		get => _security1Param.Value;
		set => _security1Param.Value = value;
	}

	/// <summary>
	/// Second security.
	/// </summary>
	public Security Security2
	{
		get => _security2Param.Value;
		set => _security2Param.Value = value;
	}

	/// <summary>
	/// Third security.
	/// </summary>
	public Security Security3
	{
		get => _security3Param.Value;
		set => _security3Param.Value = value;
	}

	/// <summary>
	/// Fourth security.
	/// </summary>
	public Security Security4
	{
		get => _security4Param.Value;
		set => _security4Param.Value = value;
	}

	/// <summary>
	/// Fifth security.
	/// </summary>
	public Security Security5
	{
		get => _security5Param.Value;
		set => _security5Param.Value = value;
	}

	/// <summary>
	/// Sixth security.
	/// </summary>
	public Security Security6
	{
		get => _security6Param.Value;
		set => _security6Param.Value = value;
	}

	/// <summary>
	/// Candle type for data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Number of bars for correlation calculation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackParam.Value;
		set => _lookbackParam.Value = value;
	}

	/// <summary>
	/// Weak positive correlation limit.
	/// </summary>
	public decimal PositiveWeak
	{
		get => _positiveWeakParam.Value;
		set => _positiveWeakParam.Value = value;
	}

	/// <summary>
	/// Medium positive correlation limit.
	/// </summary>
	public decimal PositiveMedium
	{
		get => _positiveMediumParam.Value;
		set => _positiveMediumParam.Value = value;
	}

	/// <summary>
	/// Strong positive correlation limit.
	/// </summary>
	public decimal PositiveStrong
	{
		get => _positiveStrongParam.Value;
		set => _positiveStrongParam.Value = value;
	}

	/// <summary>
	/// Weak negative correlation limit.
	/// </summary>
	public decimal NegativeWeak
	{
		get => _negativeWeakParam.Value;
		set => _negativeWeakParam.Value = value;
	}

	/// <summary>
	/// Medium negative correlation limit.
	/// </summary>
	public decimal NegativeMedium
	{
		get => _negativeMediumParam.Value;
		set => _negativeMediumParam.Value = value;
	}

	/// <summary>
	/// Strong negative correlation limit.
	/// </summary>
	public decimal NegativeStrong
	{
		get => _negativeStrongParam.Value;
		set => _negativeStrongParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CorrelationArraysStrategy"/> class.
	/// </summary>
	public CorrelationArraysStrategy()
	{
		_security1Param = Param<Security>(nameof(Security1))
			.SetDisplay("Security 1", "First security", "Securities");
		_security2Param = Param<Security>(nameof(Security2))
			.SetDisplay("Security 2", "Second security", "Securities");
		_security3Param = Param<Security>(nameof(Security3))
			.SetDisplay("Security 3", "Third security", "Securities");
		_security4Param = Param<Security>(nameof(Security4))
			.SetDisplay("Security 4", "Fourth security", "Securities");
		_security5Param = Param<Security>(nameof(Security5))
			.SetDisplay("Security 5", "Fifth security", "Securities");
		_security6Param = Param<Security>(nameof(Security6))
			.SetDisplay("Security 6", "Sixth security", "Securities");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_lookbackParam = Param(nameof(LookbackPeriod), 100)
			.SetDisplay("Lookback Period", "Number of bars back", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 25)
			.SetGreaterThanZero();

		_positiveWeakParam = Param(nameof(PositiveWeak), 0.3m)
			.SetDisplay("|+", "Possible positive correlation", "Levels");
		_positiveMediumParam = Param(nameof(PositiveMedium), 0.5m)
			.SetDisplay("+|++", "Medium positive correlation", "Levels");
		_positiveStrongParam = Param(nameof(PositiveStrong), 0.7m)
			.SetDisplay("++|+++", "Strong positive correlation", "Levels");

		_negativeWeakParam = Param(nameof(NegativeWeak), -0.3m)
			.SetDisplay("|-", "Possible negative correlation", "Levels");
		_negativeMediumParam = Param(nameof(NegativeMedium), -0.5m)
			.SetDisplay("-|--", "Medium negative correlation", "Levels");
		_negativeStrongParam = Param(nameof(NegativeStrong), -0.7m)
			.SetDisplay("--|---", "Strong negative correlation", "Levels");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_securities = new[] { Security1, Security2, Security3, Security4, Security5, Security6 };

		for (var i = 0; i < _securities.Length; i++)
		{
			_priceSeries.Add(new Queue<decimal>());
			var sec = _securities[i];
			if (sec == null)
			{
				LogWarning($"Security {i + 1} is not set.");
				continue;
			}

			var index = i;
			var subscription = SubscribeCandles(CandleType, security: sec);
			subscription
				.Bind(c => ProcessCandle(c, index))
				.Start();
		}
	}

	private void ProcessCandle(ICandleMessage candle, int index)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var series = _priceSeries[index];
		series.Enqueue(candle.ClosePrice);
		while (series.Count > LookbackPeriod)
			series.Dequeue();

		if (AllSeriesFormed())
			CalculateMatrix();
	}

	private bool AllSeriesFormed()
	{
		foreach (var series in _priceSeries)
		{
			if (series.Count < LookbackPeriod)
				return false;
		}
		return true;
	}

	private void CalculateMatrix()
	{
		for (var i = 0; i < _securities.Length; i++)
		{
			for (var j = i + 1; j < _securities.Length; j++)
			{
				var corr = CalculateCorrelation([.. _priceSeries[i]], [.. _priceSeries[j]]);
				var classification = Classify(corr);
				LogInfo($"{_securities[i].Code} vs {_securities[j].Code}: {corr:F2} ({classification})");
			}
		}
	}

	private string Classify(decimal correlation)
	{
		if (correlation <= NegativeStrong)
			return "strong negative";
		if (correlation <= NegativeMedium)
			return "medium negative";
		if (correlation <= NegativeWeak)
			return "possible negative";
		if (correlation <= PositiveWeak)
			return "weak";
		if (correlation <= PositiveMedium)
			return "possible positive";
		if (correlation <= PositiveStrong)
			return "medium positive";
		return "strong positive";
	}

	private static decimal CalculateCorrelation(decimal[] x, decimal[] y)
	{
		var n = x.Length;
		decimal sumX = 0, sumY = 0, sumXY = 0;
		decimal sumX2 = 0, sumY2 = 0;

		for (var i = 0; i < n; i++)
		{
			var xi = x[i];
			var yi = y[i];
			sumX += xi;
			sumY += yi;
			sumXY += xi * yi;
			sumX2 += xi * xi;
			sumY2 += yi * yi;
		}

		var denominator = (decimal)Math.Sqrt((double)(n * sumX2 - sumX * sumX) * (double)(n * sumY2 - sumY * sumY));
		if (denominator == 0)
			return 0;

		return (n * sumXY - sumX * sumY) / denominator;
	}
}
