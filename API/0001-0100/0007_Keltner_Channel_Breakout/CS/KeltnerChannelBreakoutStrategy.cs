using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Keltner Channel breakout.
/// The channel is an EMA with bands AtrMultiplier ATRs away. A close that breaks above the upper band opens a long position,
/// one that breaks below the lower band a short one. The position closes when the close crosses back through the EMA
/// or reaches the stop set AtrMultiplier ATRs from the entry close.
/// </summary>
public class KeltnerChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	// Close and bands of the previous candle.
	private decimal? _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _stopPrice;

	/// <summary>
	/// EMA period of the channel center.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period of the channel width.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Band and stop distance in ATR multiples.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Initializes a new instance of the <see cref="KeltnerChannelBreakoutStrategy"/>.
	/// </summary>
	public KeltnerChannelBreakoutStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for Exponential Moving Average", "Indicators")
			.SetOptimize(10, 50, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for Average True Range", "Indicators")
			.SetOptimize(10, 30, 2);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Band and stop distance in ATR multiples", "Indicators")
			.SetOptimize(1, 3, 0.5m);

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
		_prevClose = null;
		_prevUpper = default;
		_prevLower = default;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !emaValue.IsFormed || !atrValue.IsFormed)
			return;

		var center = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;

		_prevClose = close;
		_prevUpper = center + AtrMultiplier * atr;
		_prevLower = center - AtrMultiplier * atr;

		if (prevClose is not decimal lastClose || !IsFormedAndOnlineAndAllowTrading())
			return;

		// A breakout is the first close beyond the band of the previous candle.
		var upperBreakout = close > prevUpper && lastClose <= prevUpper;
		var lowerBreakout = close < prevLower && lastClose >= prevLower;

		if (upperBreakout && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - AtrMultiplier * atr;
		}
		else if (lowerBreakout && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + AtrMultiplier * atr;
		}
		else if (Position > 0 && (close < center || close <= _stopPrice))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close > center || close >= _stopPrice))
		{
			BuyMarket(-Position);
		}
	}
}
