using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grover Llorens Activator strategy.
/// The activator line restarts Multiplier ATRs away from price whenever the close crosses it: below price after an upward cross,
/// above price after a downward one. Between crosses it moves towards price by ATR / Length times the number of candles since the
/// cross, so it accelerates the longer the trend lasts. The strategy buys when close minus the line crosses above zero and sells
/// when it crosses below zero, reversing an opposite position.
/// </summary>
public class GroverLlorensActivatorStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _ts;
	private decimal _prevDiff;
	private decimal _step;
	private int _barsSinceCross;
	private int _direction;

	/// <summary>
	/// ATR period, also the divisor of the per-candle step.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the restart distance.
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
	public GroverLlorensActivatorStrategy()
	{
		_length = Param(nameof(Length), 480)
			.SetGreaterThanZero()
			.SetDisplay("Length", "ATR period and step divisor", "Indicators");

		_multiplier = Param(nameof(Multiplier), 14m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier for the restart distance", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_ts = null;
		_prevDiff = 0m;
		_step = 0m;
		_barsSinceCross = 0;
		_direction = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!atrValue.IsFormed)
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var prevTs = _ts ?? close;
		var diff = close - prevTs;

		var crossUp = diff > 0 && _prevDiff <= 0;
		var crossDown = diff < 0 && _prevDiff >= 0;

		if (crossUp || crossDown)
		{
			_direction = crossUp ? 1 : -1;
			_step = atr / Length;
			_barsSinceCross = 0;
			_ts = crossUp ? close - atr * Multiplier : close + atr * Multiplier;
		}
		else
		{
			_barsSinceCross++;
			_ts = prevTs + _direction * _step * _barsSinceCross;
		}

		_prevDiff = diff;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
