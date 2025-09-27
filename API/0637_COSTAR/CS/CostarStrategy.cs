
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// COSTAR Strategy - trades deviations from a linear regression band.
/// </summary>
public class CostarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;

	private LinearRegression _linearReg;
	private StandardDeviation _stdDev;

	private decimal _previousClose;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Linear regression length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for bands.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CostarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Period for linear regression", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 25);

		_multiplier = Param(nameof(Multiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Band Multiplier", "Standard deviation multiplier for bands", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.25m);
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

		_previousClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_linearReg = new LinearRegression { Length = Length };
		_stdDev = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_linearReg, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _linearReg);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue regValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var regTyped = (LinearRegressionValue)regValue;
		if (regTyped.LinearReg is not decimal regression)
			return;

		var residual = candle.ClosePrice - regression;
		var stdValue = _stdDev.Process(residual, candle.ServerTime, true).ToNullableDecimal();

		if (stdValue is not decimal stdDev || !_linearReg.IsFormed || !_stdDev.IsFormed)
			return;

		var upper = regression + Multiplier * stdDev;
		var lower = regression - Multiplier * stdDev;

		// Entry conditions
		if (Position <= 0 && _previousClose < lower && candle.ClosePrice > lower)
			BuyMarket(Volume + Math.Abs(Position));

		if (Position >= 0 && _previousClose > upper && candle.ClosePrice < upper)
			SellMarket(Volume + Math.Abs(Position));

		// Exit conditions
		if (Position > 0 && _previousClose < regression && candle.ClosePrice > regression)
			SellMarket(Position);

		if (Position < 0 && _previousClose > regression && candle.ClosePrice < regression)
			BuyMarket(Math.Abs(Position));

		_previousClose = candle.ClosePrice;
	}
}
