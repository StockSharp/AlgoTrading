using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dekidaka-Ashi strategy combining candles and volume.
/// Buys on bullish signals and sells on bearish signals.
/// </summary>
public class DekidakaAshiCandlesVolumeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _bodySize;
	private readonly StrategyParam<int> _volumeSmooth;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ExponentialMovingAverage _volumeEma = new();
	private decimal _prevVolumeEma;

	/// <summary>
	/// Body size multiplier.
	/// </summary>
	public decimal BodySize
	{
		get => _bodySize.Value;
		set => _bodySize.Value = value;
	}

	/// <summary>
	/// Volume smoothing period.
	/// </summary>
	public int VolumeSmooth
	{
		get => _volumeSmooth.Value;
		set => _volumeSmooth.Value = value;
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
	/// Initialize <see cref="DekidakaAshiCandlesVolumeStrategy"/>.
	/// </summary>
	public DekidakaAshiCandlesVolumeStrategy()
	{
		_bodySize = Param(nameof(BodySize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Body Size", "Body size multiplier", "General");

		_volumeSmooth = Param(nameof(VolumeSmooth), 1)
			.SetGreaterThanZero()
			.SetDisplay("Volume Smooth", "Volume smoothing period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_volumeEma.Length = VolumeSmooth;
		_volumeEma.Reset();
		_prevVolumeEma = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_volumeEma.Length = VolumeSmooth;

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var value = _volumeEma.Process(new DecimalIndicatorValue(_volumeEma, candle.TotalVolume));
		if (!value.IsFinal)
			return;

		var v = value.ToDecimal();

		if (_prevVolumeEma == 0)
		{
			_prevVolumeEma = v;
			return;
		}

		var k = v / (v + _prevVolumeEma) * BodySize;

		var max = Math.Max(candle.ClosePrice, candle.OpenPrice);
		var min = Math.Min(candle.ClosePrice, candle.OpenPrice);
		var range = max - min;
		var upper = max + k * range;
		var lower = min - k * range;

		var strongBull = candle.HighPrice > upper && candle.LowPrice > lower && candle.ClosePrice > candle.OpenPrice;
		var strongBear = candle.HighPrice < upper && candle.LowPrice < lower && candle.ClosePrice < candle.OpenPrice;
		var weakBull = candle.HighPrice > upper && candle.LowPrice > lower && candle.ClosePrice < candle.OpenPrice;
		var weakBear = candle.HighPrice < upper && candle.LowPrice < lower && candle.ClosePrice > candle.OpenPrice;
		var uncertainty = candle.HighPrice > upper && candle.LowPrice < lower;

		if (strongBull || weakBull)
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (strongBear || weakBear)
		{
			if (Position >= 0)
				SellMarket();
		}
		else if (uncertainty)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
		}

		_prevVolumeEma = v;
	}
}

