using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Overnight Gap strategy.
/// The market trades around the clock, so the session is the UTC day.
/// The gap is the distance between the previous UTC day's last close and the new day's first open. A gap of at least MinGapPercent
/// is faded at the close of the day's first candle; the stop lies StopLossPercent beyond the first candle's extreme in the gap's direction,
/// and the position closes at the day's last candle.
/// </summary>
public class OvernightGapStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minGapPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal? _prevClose;
	private decimal _stopPrice;

	/// <summary>
	/// Smallest gap to fade, in percent.
	/// </summary>
	public decimal MinGapPercent
	{
		get => _minGapPercent.Value;
		set => _minGapPercent.Value = value;
	}

	/// <summary>
	/// Distance of the stop beyond the first candle's extreme, in percent.
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
	public OvernightGapStrategy()
	{
		_minGapPercent = Param(nameof(MinGapPercent), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Min Gap %", "Smallest gap to fade, in percent", "Session");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Distance of the stop beyond the first candle's extreme, in percent", "Risk");

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
		_day = null;
		_prevClose = null;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_prevClose = null;
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

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		var firstOfDay = _day != day;

		if (firstOfDay)
		{
			_day = day;
		}

		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;
		var lastOfDay = (candle.OpenTime + frame).Date != day;

		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position != 0)
		{
			if (lastOfDay || (Position > 0 ? close <= _stopPrice : close >= _stopPrice))
				ClosePosition();

			return;
		}

		if (!firstOfDay || prevClose is not decimal last || last <= 0)
			return;

		var gap = (candle.OpenPrice - last) / last * 100m;

		if (gap >= MinGapPercent)
		{
			SellMarket(Volume);
			_stopPrice = candle.HighPrice * (1 + StopLossPercent / 100m);
		}
		else if (-gap >= MinGapPercent)
		{
			BuyMarket(Volume);
			_stopPrice = candle.LowPrice * (1 - StopLossPercent / 100m);
		}
	}
}
