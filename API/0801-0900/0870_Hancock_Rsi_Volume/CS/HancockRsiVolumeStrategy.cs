using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hancock RSI Volume strategy.
/// Each candle's volume is split into bullish and bearish volume: with UseWicks by where the close sits inside the high-low range,
/// otherwise wholly by the candle colour. The RSI is 100 * bullish / (bullish + bearish) of the Wilder averages of both over
/// RsiLength candles. The trend turns up when the RSI rises by more than Threshold from the previous candle and down when it falls
/// by more than Threshold; a switch to up buys and a switch to down sells, reversing an opposite position.
/// </summary>
public class HancockRsiVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _useWicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _bullAvg;
	private decimal _bearAvg;
	private int _samples;
	private decimal? _prevRsi;
	private int _trend;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Minimum RSI change that switches the trend.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Split the volume by the close position inside the candle range including wicks.
	/// </summary>
	public bool UseWicks
	{
		get => _useWicks.Value;
		set => _useWicks.Value = value;
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
	public HancockRsiVolumeStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_threshold = Param(nameof(Threshold), 0.1m)
			.SetNotNegative()
			.SetDisplay("Threshold", "Minimum RSI change that switches the trend", "Indicators");

		_useWicks = Param(nameof(UseWicks), true)
			.SetDisplay("Use Wicks", "Split the volume by the close position inside the candle range", "Indicators");

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
		_bullAvg = 0m;
		_bearAvg = 0m;
		_samples = 0;
		_prevRsi = null;
		_trend = 0;
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

		var volume = candle.TotalVolume;
		decimal bull;
		decimal bear;

		if (UseWicks)
		{
			var range = candle.HighPrice - candle.LowPrice;
			if (range > 0)
			{
				bull = volume * (candle.ClosePrice - candle.LowPrice) / range;
				bear = volume - bull;
			}
			else
			{
				bull = volume / 2;
				bear = volume / 2;
			}
		}
		else if (candle.ClosePrice > candle.OpenPrice)
		{
			bull = volume;
			bear = 0m;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			bull = 0m;
			bear = volume;
		}
		else
		{
			bull = volume / 2;
			bear = volume / 2;
		}

		// Wilder averages seeded with the simple mean of the first RsiLength candles.
		var length = RsiLength;
		if (_samples < length)
		{
			_samples++;
			_bullAvg += (bull - _bullAvg) / _samples;
			_bearAvg += (bear - _bearAvg) / _samples;

			if (_samples < length)
				return;
		}
		else
		{
			_bullAvg = (_bullAvg * (length - 1) + bull) / length;
			_bearAvg = (_bearAvg * (length - 1) + bear) / length;
		}

		var total = _bullAvg + _bearAvg;
		if (total <= 0)
			return;

		var rsi = 100m * _bullAvg / total;
		var prevRsi = _prevRsi;
		_prevRsi = rsi;

		if (prevRsi is not decimal lastRsi)
			return;

		var prevTrend = _trend;
		var change = rsi - lastRsi;

		if (change > Threshold)
			_trend = 1;
		else if (change < -Threshold)
			_trend = -1;

		if (_trend == prevTrend)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_trend > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_trend < 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
