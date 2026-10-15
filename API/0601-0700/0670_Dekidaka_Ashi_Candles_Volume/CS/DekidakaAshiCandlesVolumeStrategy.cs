using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dekidaka-Ashi candles volume strategy.
/// The range is the body of the previous candle, scaled around its middle by BodySize and by the ratio of that candle's volume
/// to its VolumeSmooth EMA, so heavy candles give a wider range.
/// A candle with its high above the upper bound and its low above the lower bound is bullish and goes long; one with its high below
/// the upper bound and its low below the lower bound is bearish and goes short, reversing an opposite position.
/// A candle spanning both bounds closes any position.
/// </summary>
public class DekidakaAshiCandlesVolumeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _bodySize;
	private readonly StrategyParam<int> _volumeSmooth;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _volumeEma;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	/// <summary>
	/// Multiplier of the body range.
	/// </summary>
	public decimal BodySize
	{
		get => _bodySize.Value;
		set => _bodySize.Value = value;
	}

	/// <summary>
	/// EMA length of the volume smoothing.
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
	/// Constructor.
	/// </summary>
	public DekidakaAshiCandlesVolumeStrategy()
	{
		_bodySize = Param(nameof(BodySize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Body Size", "Multiplier of the body range", "Range");

		_volumeSmooth = Param(nameof(VolumeSmooth), 1)
			.SetGreaterThanZero()
			.SetDisplay("Volume Smooth", "EMA length of the volume smoothing", "Range");

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
		_prevUpper = null;
		_prevLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpper = null;
		_prevLower = null;
		_volumeEma = new ExponentialMovingAverage { Length = VolumeSmooth };

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

		var upper = _prevUpper;
		var lower = _prevLower;

		var smoothed = _volumeEma.Process(candle.TotalVolume, candle.ServerTime, true);
		if (smoothed.IsFormed)
		{
			var avgVolume = smoothed.GetValue<decimal>();
			var ratio = avgVolume > 0 ? candle.TotalVolume / avgVolume : 1m;
			var middle = (candle.OpenPrice + candle.ClosePrice) / 2;
			var halfBody = Math.Abs(candle.ClosePrice - candle.OpenPrice) / 2 * BodySize * ratio;
			_prevUpper = middle + halfBody;
			_prevLower = middle - halfBody;
		}

		if (upper is not decimal up || lower is not decimal low)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullish = candle.HighPrice > up && candle.LowPrice > low;
		var bearish = candle.HighPrice < up && candle.LowPrice < low;
		var spansBoth = candle.HighPrice >= up && candle.LowPrice <= low;

		if (bullish && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bearish && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (spansBoth && Position > 0)
			SellMarket(Position);
		else if (spansBoth && Position < 0)
			BuyMarket(-Position);
	}
}
