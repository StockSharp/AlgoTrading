using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average channel breakout strategy.
/// Buys when price crosses above the upper channel (MA of highs + offset).
/// Sells when price crosses below the lower channel (MA of lows - offset).
/// </summary>
public class MaChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _offset;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _maHigh;
	private ExponentialMovingAverage _maLow;
	private int _trend;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Offset { get => _offset.Value; set => _offset.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaChannelStrategy()
	{
		_length = Param(nameof(Length), 8)
			.SetDisplay("Length", "Moving average period", "Parameters")
			.SetOptimize(5, 20, 1);

		_offset = Param(nameof(Offset), 100m)
			.SetDisplay("Offset", "Price offset from the average", "Parameters")
			.SetOptimize(50m, 500m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
		_maHigh = null;
		_maLow = null;
		_trend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trend = 0;
		_maHigh = new ExponentialMovingAverage { Length = Length };
		_maLow = new ExponentialMovingAverage { Length = Length };

		Indicators.Add(_maHigh);
		Indicators.Add(_maLow);

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

		var t = candle.ServerTime;

		var highResult = _maHigh.Process(new DecimalIndicatorValue(_maHigh, candle.HighPrice, t) { IsFinal = true });
		var lowResult = _maLow.Process(new DecimalIndicatorValue(_maLow, candle.LowPrice, t) { IsFinal = true });

		if (!_maHigh.IsFormed || !_maLow.IsFormed)
			return;

		var upper = highResult.GetValue<decimal>() + Offset;
		var lower = lowResult.GetValue<decimal>() - Offset;

		var prevTrend = _trend;

		if (candle.HighPrice > upper)
			_trend = 1;
		else if (candle.LowPrice < lower)
			_trend = -1;

		if (prevTrend <= 0 && _trend > 0 && Position <= 0)
			BuyMarket();
		else if (prevTrend >= 0 && _trend < 0 && Position >= 0)
			SellMarket();
	}
}
