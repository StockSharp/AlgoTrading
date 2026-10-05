using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Inside Bar Breakout strategy.
/// An inside bar's range lies within the previous candle's high and low. While flat, a close above the latest inside bar's
/// high buys and a close below its low sells. The stop lies StopLossPercent percent beyond the opposite side of the pattern,
/// and a close beyond the previous candle's extreme against the position also exits.
/// </summary>
public class InsideBarBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private (decimal High, decimal Low)? _prevCandle;
	private (decimal High, decimal Low)? _insideBar;
	private decimal _stopPrice;

	/// <summary>
	/// Distance of the stop beyond the pattern, in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public InsideBarBreakoutStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Distance of the stop beyond the pattern, in percent", "Risk");

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
		_prevCandle = null;
		_insideBar = null;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;
		_insideBar = null;
		_stopPrice = default;

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

		var previous = _prevCandle;
		var pattern = _insideBar;

		_prevCandle = (candle.HighPrice, candle.LowPrice);

		var isInside = previous is { } prev && candle.HighPrice <= prev.High && candle.LowPrice >= prev.Low;
		var close = candle.ClosePrice;

		// A close outside the pattern uses it up; a new inside bar replaces it.
		if (pattern is { } bar && (close > bar.High || close < bar.Low))
			_insideBar = null;

		if (isInside)
			_insideBar = (candle.HighPrice, candle.LowPrice);

		if (previous is not { } prior || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (close <= _stopPrice || close < prior.Low)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (close >= _stopPrice || close > prior.High)
				BuyMarket(-Position);
		}
		else if (pattern is { } inside)
		{
			if (close > inside.High)
			{
				BuyMarket(Volume);
				_stopPrice = inside.Low * (1 - StopLossPercent / 100m);
			}
			else if (close < inside.Low)
			{
				SellMarket(Volume);
				_stopPrice = inside.High * (1 + StopLossPercent / 100m);
			}
		}
	}
}
