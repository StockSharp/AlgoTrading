using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Displays correlation matrix between selected securities.
/// </summary>
public class CorrelationMatrixHeatmapStrategy : Strategy
{
	private readonly StrategyParam<Security> _security2;
	private readonly StrategyParam<Security> _security3;
	private readonly StrategyParam<Security> _security4;
	private readonly StrategyParam<Security> _security5;
	private readonly StrategyParam<Security> _security6;
	private readonly StrategyParam<Security> _security7;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _smooth;

	private readonly List<Security> _securities = [];
	private readonly List<SMA> _smas = [];
	private readonly List<Queue<decimal>> _series = [];
	private readonly Queue<decimal> _index = new();
	private int _barIndex;

	/// <summary>
	/// First security.
	/// </summary>
	public Security Security1
	{
		get => Security;
		set => Security = value;
	}

	/// <summary>
	/// Second security.
	/// </summary>
	public Security Security2
	{
		get => _security2.Value;
		set => _security2.Value = value;
	}

	/// <summary>
	/// Third security.
	/// </summary>
	public Security Security3
	{
		get => _security3.Value;
		set => _security3.Value = value;
	}

	/// <summary>
	/// Fourth security.
	/// </summary>
	public Security Security4
	{
		get => _security4.Value;
		set => _security4.Value = value;
	}

	/// <summary>
	/// Fifth security.
	/// </summary>
	public Security Security5
	{
		get => _security5.Value;
		set => _security5.Value = value;
	}

	/// <summary>
	/// Sixth security.
	/// </summary>
	public Security Security6
	{
		get => _security6.Value;
		set => _security6.Value = value;
	}

	/// <summary>
	/// Seventh security.
	/// </summary>
	public Security Security7
	{
		get => _security7.Value;
		set => _security7.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Correlation length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Smoothing period for source data.
	/// </summary>
	public int Smooth
	{
		get => _smooth.Value;
		set => _smooth.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CorrelationMatrixHeatmapStrategy()
	{
		_security2 = Param<Security>(nameof(Security2))
		.SetDisplay("Security 2", "Second security", "Securities");
		_security3 = Param<Security>(nameof(Security3))
		.SetDisplay("Security 3", "Third security", "Securities");
		_security4 = Param<Security>(nameof(Security4))
		.SetDisplay("Security 4", "Fourth security", "Securities");
		_security5 = Param<Security>(nameof(Security5))
		.SetDisplay("Security 5", "Fifth security", "Securities");
		_security6 = Param<Security>(nameof(Security6))
		.SetDisplay("Security 6", "Sixth security", "Securities");
		_security7 = Param<Security>(nameof(Security7))
		.SetDisplay("Security 7", "Seventh security", "Securities");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_length = Param(nameof(Length), 10)
		.SetGreaterThanZero()
		.SetDisplay("Length", "Correlation length", "Parameters");

		_smooth = Param(nameof(Smooth), 5)
		.SetGreaterThanZero()
		.SetDisplay("Smooth", "SMA smoothing period", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var dt = CandleType;
		if (Security1 != null && dt != null)
		yield return (Security1, dt);
		if (Security2 != null && dt != null)
		yield return (Security2, dt);
		if (Security3 != null && dt != null)
		yield return (Security3, dt);
		if (Security4 != null && dt != null)
		yield return (Security4, dt);
		if (Security5 != null && dt != null)
		yield return (Security5, dt);
		if (Security6 != null && dt != null)
		yield return (Security6, dt);
		if (Security7 != null && dt != null)
		yield return (Security7, dt);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_securities.Clear();
		_smas.Clear();
		_series.Clear();
		_index.Clear();
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		AddSecurity(Security1);
		AddSecurity(Security2);
		AddSecurity(Security3);
		AddSecurity(Security4);
		AddSecurity(Security5);
		AddSecurity(Security6);
		AddSecurity(Security7);
	}

	private void AddSecurity(Security security)
	{
		if (security == null)
		return;

		var sma = new SMA { Length = Smooth };
		var queue = new Queue<decimal>();
		var index = _securities.Count;

		_securities.Add(security);
		_smas.Add(sma);
		_series.Add(queue);

		var subscription = SubscribeCandles(CandleType, security: security);
		subscription
		.Bind(candle => ProcessCandle(index, candle))
		.Start();
	}

	private void ProcessCandle(int index, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var sma = _smas[index];
		var value = sma.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!sma.IsFormed)
		return;

		var series = _series[index];
		var price = value.ToDecimal();
		if (series.Count == Length)
		series.Dequeue();
		series.Enqueue(price);

		if (index == 0)
		{
		_barIndex++;
		if (_index.Count == Length)
		_index.Dequeue();
		_index.Enqueue(_barIndex);
		}

		TryCalculateCorrelations();
	}

	private void TryCalculateCorrelations()
	{
		if (_index.Count < Length)
		return;

		for (var i = 0; i < _series.Count; i++)
		{
		if (_series[i].Count < Length)
		return;
		}

		var indexArr = _index.ToArray();
		for (var i = 0; i < _series.Count; i++)
		{
		var corrTime = CalculateCorrelation(indexArr, _series[i].ToArray());
		LogInfo($"Index vs {_securities[i].Id}: {corrTime:P2}");
		}

		for (var i = 0; i < _series.Count; i++)
		{
		var arrI = _series[i].ToArray();
		for (var j = i + 1; j < _series.Count; j++)
		{
		var corr = CalculateCorrelation(arrI, _series[j].ToArray());
		LogInfo($"{_securities[i].Id} vs {_securities[j].Id}: {corr:P2}");
		}
		}
	}

	private decimal CalculateCorrelation(decimal[] x, decimal[] y)
	{
		var n = x.Length;
		decimal sumX = 0, sumY = 0, sumXY = 0;
		decimal sumX2 = 0, sumY2 = 0;

		for (var i = 0; i < n; i++)
		{
		sumX += x[i];
		sumY += y[i];
		sumXY += x[i] * y[i];
		sumX2 += x[i] * x[i];
		sumY2 += y[i] * y[i];
		}

		var denominator = (decimal)Math.Sqrt((double)(n * sumX2 - sumX * sumX) * (double)(n * sumY2 - sumY * sumY));
		if (denominator == 0)
		return 0;

		return (n * sumXY - sumX * sumY) / denominator;
	}
}
