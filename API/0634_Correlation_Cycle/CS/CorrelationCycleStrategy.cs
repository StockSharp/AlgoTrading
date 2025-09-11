using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// John Ehlers' Correlation Cycle based strategy.
/// </summary>
public class CorrelationCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _thresholdDegrees;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _buffer = [];
	private double _previousAngle;
	private int _previousState;

	/// <summary>
	/// Correlation cycle period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Market state threshold in degrees.
	/// </summary>
	public int ThresholdDegrees
	{
		get => _thresholdDegrees.Value;
		set => _thresholdDegrees.Value = value;
	}

	/// <summary>
	/// The type of candles to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CorrelationCycleStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Correlation cycle period", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_thresholdDegrees = Param(nameof(ThresholdDegrees), 9)
			.SetGreaterThanZero()
			.SetDisplay("Market State Threshold", "Angle difference threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_buffer.Clear();
		_previousAngle = 0;
		_previousState = 0;
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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_buffer.Add(candle.ClosePrice);
		if (_buffer.Count > Period)
			_buffer.RemoveAt(0);

		if (_buffer.Count < Period)
			return;

		var (realPart, imaginaryPart) = ComputeCorrelation(_buffer);
		var angle = ComputeAngle(realPart, imaginaryPart);
		var state = ComputeMarketState(angle, _previousAngle);
		_previousAngle = angle;

		if (state != 0 && state != _previousState)
		{
			if (state > 0 && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (state < 0 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			_previousState = state;
		}
	}

	private (double real, double imaginary) ComputeCorrelation(IReadOnlyList<decimal> series)
	{
		var period = Period;
		const double pi2 = Math.PI * 2;

		double Rx = 0, Rxx = 0, Rxy = 0, Ryy = 0, Ry = 0;
		double Ix = 0, Ixx = 0, Ixy = 0, Iyy = 0, Iy = 0;

		for (var i = 0; i < period; i++)
		{
			var x = (double)series[^1 - i];
			var temp = pi2 * i / period;
			var yc = Math.Cos(temp);
			var ys = -Math.Sin(temp);

			Rx += x;
			Ix += x;
			Rxx += x * x;
			Ixx += x * x;
			Rxy += x * yc;
			Ixy += x * ys;
			Ryy += yc * yc;
			Iyy += ys * ys;
			Ry += yc;
			Iy += ys;
		}

		var real = 0.0;
		var t1 = period * Rxx - Rx * Rx;
		var t2 = period * Ryy - Ry * Ry;

		if (t1 > 0 && t2 > 0)
			real = (period * Rxy - Rx * Ry) / Math.Sqrt(t1 * t2);

		var imag = 0.0;
		t1 = period * Ixx - Ix * Ix;
		t2 = period * Iyy - Iy * Iy;

		if (t1 > 0 && t2 > 0)
			imag = (period * Ixy - Ix * Iy) / Math.Sqrt(t1 * t2);

		return (real, imag);
	}

	private double ComputeAngle(double realPart, double imaginaryPart)
	{
		const double halfPi = Math.PI / 2;
		var angle = imaginaryPart == 0 ? 0 : (Math.Atan(realPart / imaginaryPart) + halfPi) * 180 / Math.PI;

		if (imaginaryPart > 0)
			angle -= 180;

		if (_previousAngle > angle && _previousAngle - angle < 270)
			angle = _previousAngle;

		return angle;
	}

	private int ComputeMarketState(double angle, double prevAngle)
	{
		var threshold = ThresholdDegrees;
		var stable = Math.Abs(angle - prevAngle) < threshold;

		if (angle >= 0 && stable)
			return 1;
		if (angle < 0 && stable)
			return -1;

		return 0;
	}
}
