using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bearish Harami strategy.
/// While flat it sells after a bullish candle followed by a candle whose smaller body lies within the first candle's range.
/// The stop lies StopLossPercent above the pattern's highest high, and a close beyond it closes the position.
/// </summary>
public class HaramiBearishStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = [];
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
	public HaramiBearishStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Distance of the stop beyond the pattern, in percent", "Risk");

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
		_candles.Clear();
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candles.Clear();
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

		_candles.Add(candle);

		if (_candles.Count > 2)
			_candles.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position < 0)
		{
			if (candle.ClosePrice >= _stopPrice)
				BuyMarket(-Position);

			return;
		}

		if (Position != 0 || _candles.Count < 2)
			return;

		var c0 = _candles[0];
		var c1 = _candles[1];

		if (!(c0.ClosePrice > c0.OpenPrice && Math.Abs(c1.ClosePrice - c1.OpenPrice) < c0.ClosePrice - c0.OpenPrice && Math.Min(c1.OpenPrice, c1.ClosePrice) >= c0.LowPrice && Math.Max(c1.OpenPrice, c1.ClosePrice) <= c0.HighPrice))
			return;

		SellMarket(Volume);
		_stopPrice = Math.Max(c0.HighPrice, c1.HighPrice) * (1 + StopLossPercent / 100m);
	}
}
