namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Demonstrates multiple linear regression using two SMA inputs.
/// </summary>
public class FunctionMatrixLibraryStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<DataType> _candleType;
	
	private Sma _sma1 = null!;
	private Sma _sma2 = null!;
	
	private readonly List<decimal> _yValues = new();
	private readonly List<decimal> _x1Values = new();
	private readonly List<decimal> _x2Values = new();
	
	/// <summary>
	/// Lookback period for regression.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}
	
	/// <summary>
	/// SMA length for first explanatory variable.
	/// </summary>
	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}
	
	/// <summary>
	/// SMA length for second explanatory variable.
	/// </summary>
	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FunctionMatrixLibraryStrategy"/> class.
	/// </summary>
	public FunctionMatrixLibraryStrategy()
	{
		_lookback = Param(nameof(Lookback), 20)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Regression lookback length", "General");
		
		_length1 = Param(nameof(Length1), 10)
		.SetGreaterThanZero()
		.SetDisplay("Length1", "First SMA length", "General");
		
		_length2 = Param(nameof(Length2), 20)
		.SetGreaterThanZero()
		.SetDisplay("Length2", "Second SMA length", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_sma1 = new Sma { Length = Length1 };
		_sma2 = new Sma { Length = Length2 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_sma1, _sma2, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma1);
			DrawIndicator(area, _sma2);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal sma1Value, decimal sma2Value)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		AddToBuffer(_yValues, candle.ClosePrice);
		AddToBuffer(_x1Values, sma1Value);
		AddToBuffer(_x2Values, sma2Value);
		
		if (_yValues.Count < Lookback)
		return;
		
		var coeffs = CalculateCoefficients(_yValues, _x1Values, _x2Values);
		var estimate = coeffs[0] + coeffs[1] * sma1Value + coeffs[2] * sma2Value;
		this.AddInfoLog($"Estimate={estimate}");
	}
	
	private void AddToBuffer(List<decimal> buffer, decimal value)
	{
		if (buffer.Count == Lookback)
		buffer.RemoveAt(0);
		buffer.Add(value);
	}
	
	private decimal[] CalculateCoefficients(List<decimal> y, List<decimal> x1, List<decimal> x2)
	{
		var n = y.Count;
		decimal sumY = 0m, sumX1 = 0m, sumX2 = 0m;
		for (var i = 0; i < n; i++)
		{
			sumY += y[i];
			sumX1 += x1[i];
			sumX2 += x2[i];
		}
		var meanY = sumY / n;
		var meanX1 = sumX1 / n;
		var meanX2 = sumX2 / n;
		
		decimal s11 = 0m, s22 = 0m, s12 = 0m, s1y = 0m, s2y = 0m;
		for (var i = 0; i < n; i++)
		{
			var dx1 = x1[i] - meanX1;
			var dx2 = x2[i] - meanX2;
			var dy = y[i] - meanY;
			s11 += dx1 * dx1;
			s22 += dx2 * dx2;
			s12 += dx1 * dx2;
			s1y += dx1 * dy;
			s2y += dx2 * dy;
		}
		
		var denom = s11 * s22 - s12 * s12;
		if (denom == 0)
		return new decimal[3];
		
		var b1 = (s1y * s22 - s2y * s12) / denom;
		var b2 = (s2y * s11 - s1y * s12) / denom;
		var b0 = meanY - b1 * meanX1 - b2 * meanX2;
		return new[] { b0, b1, b2 };
	}
}
