using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using the slope of a linear regression with a shifted trigger line.
/// </summary>
public class LinearRegressionSlopeV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _triggerShift;

	private decimal[] _slopeHistory = Array.Empty<decimal>();
	private int _filled;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Number of bars for linear regression slope.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Bars shift for trigger line.
	/// </summary>
	public int TriggerShift { get => _triggerShift.Value; set => _triggerShift.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="LinearRegressionSlopeV1Strategy"/> class.
	/// </summary>
	public LinearRegressionSlopeV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Bars for regression", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_triggerShift = Param(nameof(TriggerShift), 1)
			.SetGreaterThanZero()
			.SetDisplay("Trigger Shift", "Lag for trigger line", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 3, 1);
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
		_slopeHistory = Array.Empty<decimal>();
		_filled = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_slopeHistory = new decimal[TriggerShift + 3];
		_filled = 0;

		var slope = new LinearRegSlope { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(slope, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, slope);
			DrawOwnTrades(area);
		}
	}

	private void Shift(decimal value)
	{
		for (var i = 0; i < _slopeHistory.Length - 1; i++)
			_slopeHistory[i] = _slopeHistory[i + 1];

		_slopeHistory[^1] = value;

		if (_filled < _slopeHistory.Length)
			_filled++;
	}

	private void ProcessCandle(ICandleMessage candle, decimal slope)
	{
		if (candle.State != CandleStates.Finished)
			return;

		Shift(slope);

		if (_filled < _slopeHistory.Length)
			return;

		var s2 = _slopeHistory[_slopeHistory.Length - 3];
		var s1 = _slopeHistory[_slopeHistory.Length - 2];
		var t2 = _slopeHistory[0];
		var t1 = _slopeHistory[1];

		if (s2 > t2)
		{
			if (Position < 0)
				BuyMarket();

			if (s1 <= t1 && Position <= 0)
				BuyMarket();
		}
		else if (t2 > s2)
		{
			if (Position > 0)
				SellMarket();

			if (t1 <= s1 && Position >= 0)
				SellMarket();
		}
	}
}
