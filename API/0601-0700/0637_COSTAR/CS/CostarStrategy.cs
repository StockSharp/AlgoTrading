using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// COSTAR strategy.
/// A least-squares line is fitted to the last Length closes and the standard deviation of its residuals, times Multiplier,
/// builds bands around the line's current value. A close crossing back above the lower band buys and a close crossing back
/// below the upper band sells short, reversing an opposite position. A long closes when the close crosses above the
/// regression line and a short when it crosses below it.
/// </summary>
public class CostarStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closes = new();
	private decimal? _prevClose;
	private decimal? _prevLine;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	/// <summary>
	/// Regression length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Residual deviation multiplier for the bands.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// Constructor.
	/// </summary>
	public CostarStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetRange(2, 10000)
			.SetDisplay("Length", "Regression length", "Indicators");

		_multiplier = Param(nameof(Multiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Residual deviation multiplier for the bands", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_closes.Clear();
		_prevClose = null;
		_prevLine = null;
		_prevUpper = null;
		_prevLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		var close = candle.ClosePrice;

		_closes.Enqueue(close);
		while (_closes.Count > Length)
			_closes.Dequeue();

		if (_closes.Count < Length)
		{
			_prevClose = close;
			return;
		}

		var (line, deviation) = CalculateRegression(_closes.ToArray());
		var upper = line + deviation * Multiplier;
		var lower = line - deviation * Multiplier;

		var prevClose = _prevClose;
		var prevLine = _prevLine;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;

		_prevClose = close;
		_prevLine = line;
		_prevUpper = upper;
		_prevLower = lower;

		if (prevClose is not decimal pc || prevLine is not decimal pl || prevUpper is not decimal pu || prevLower is not decimal plo)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossAboveLower = pc <= plo && close > lower;
		var crossBelowUpper = pc >= pu && close < upper;

		if (crossAboveLower && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossBelowUpper && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && pc <= pl && close > line)
			SellMarket(Position);
		else if (Position < 0 && pc >= pl && close < line)
			BuyMarket(-Position);
	}

	private static (decimal line, decimal deviation) CalculateRegression(decimal[] values)
	{
		var n = values.Length;
		double sumX = 0, sumY = 0, sumXy = 0, sumXx = 0;

		for (var i = 0; i < n; i++)
		{
			var y = (double)values[i];
			sumX += i;
			sumY += y;
			sumXy += i * y;
			sumXx += (double)i * i;
		}

		var denominator = n * sumXx - sumX * sumX;
		var slope = denominator == 0 ? 0 : (n * sumXy - sumX * sumY) / denominator;
		var intercept = (sumY - slope * sumX) / n;

		var sumSquares = 0.0;
		for (var i = 0; i < n; i++)
		{
			var residual = (double)values[i] - (intercept + slope * i);
			sumSquares += residual * residual;
		}

		var line = intercept + slope * (n - 1);
		return ((decimal)line, (decimal)Math.Sqrt(sumSquares / n));
	}
}
