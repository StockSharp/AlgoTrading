using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Rejection Candle strategy.
/// A bullish rejection probes below the previous candle's low, closes up, and has a lower wick longer than WickRatio bodies;
/// a bearish rejection mirrors it above the previous high. While flat the strategy trades against the wick.
/// The stop lies StopLossPercent beyond the rejected low or high, and a close beyond it closes the position.
/// </summary>
public class RejectionCandleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _wickRatio;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal _stopPrice;

	/// <summary>
	/// How many bodies long the rejecting wick must be.
	/// </summary>
	public decimal WickRatio
	{
		get => _wickRatio.Value;
		set => _wickRatio.Value = value;
	}

	/// <summary>
	/// Distance of the stop beyond the rejected extreme, in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public RejectionCandleStrategy()
	{
		_wickRatio = Param(nameof(WickRatio), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Wick Ratio", "How many bodies long the rejecting wick must be", "Pattern");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Distance of the stop beyond the rejected extreme, in percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;
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
		_prevCandle = candle;

		if (previous == null || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close <= _stopPrice)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (close >= _stopPrice)
				BuyMarket(-Position);

			return;
		}

		var body = Math.Abs(close - candle.OpenPrice);
		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, close);
		var lowerWick = Math.Min(candle.OpenPrice, close) - candle.LowPrice;

		if (candle.LowPrice < previous.LowPrice && close > candle.OpenPrice && lowerWick > body * WickRatio)
		{
			BuyMarket(Volume);
			_stopPrice = candle.LowPrice * (1 - StopLossPercent / 100m);
		}
		else if (candle.HighPrice > previous.HighPrice && close < candle.OpenPrice && upperWick > body * WickRatio)
		{
			SellMarket(Volume);
			_stopPrice = candle.HighPrice * (1 + StopLossPercent / 100m);
		}
	}
}
