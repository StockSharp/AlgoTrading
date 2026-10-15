using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual Keltner Channels strategy.
/// Both channels are EMA(EmaPeriod) plus and minus a multiple of ATR(EmaPeriod): InnerMultiplier for the inner channel and
/// OuterMultiplier for the outer one. A low below the lower outer band arms a long, which is taken when the close then crosses back
/// above the lower inner band; a high above the upper outer band arms a short, taken when the close crosses back below the upper inner
/// band. An opposite signal reverses the position. The stop is MaxStopPercent from the entry and the take profit SlTpRatio times that.
/// </summary>
public class DualKeltnerChannelsStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _innerMultiplier;
	private readonly StrategyParam<decimal> _outerMultiplier;
	private readonly StrategyParam<decimal> _maxStopPercent;
	private readonly StrategyParam<decimal> _slTpRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevInnerUpper;
	private decimal? _prevInnerLower;
	private bool _longArmed;
	private bool _shortArmed;

	/// <summary>
	/// EMA and ATR period of the channels.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the inner channel.
	/// </summary>
	public decimal InnerMultiplier
	{
		get => _innerMultiplier.Value;
		set => _innerMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the outer channel.
	/// </summary>
	public decimal OuterMultiplier
	{
		get => _outerMultiplier.Value;
		set => _outerMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal MaxStopPercent
	{
		get => _maxStopPercent.Value;
		set => _maxStopPercent.Value = value;
	}

	/// <summary>
	/// Take profit as a multiple of the stop distance.
	/// </summary>
	public decimal SlTpRatio
	{
		get => _slTpRatio.Value;
		set => _slTpRatio.Value = value;
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
	public DualKeltnerChannelsStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA and ATR period of the channels", "Indicators");

		_innerMultiplier = Param(nameof(InnerMultiplier), 2.75m)
			.SetGreaterThanZero()
			.SetDisplay("Inner Multiplier", "ATR multiplier of the inner channel", "Indicators");

		_outerMultiplier = Param(nameof(OuterMultiplier), 3.75m)
			.SetGreaterThanZero()
			.SetDisplay("Outer Multiplier", "ATR multiplier of the outer channel", "Indicators");

		_maxStopPercent = Param(nameof(MaxStopPercent), 10m)
			.SetNotNegative()
			.SetDisplay("Max Stop %", "Stop loss percentage from entry price", "Risk");

		_slTpRatio = Param(nameof(SlTpRatio), 1m)
			.SetNotNegative()
			.SetDisplay("SL/TP Ratio", "Take profit as a multiple of the stop distance", "Risk");

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
		_prevClose = null;
		_prevInnerUpper = null;
		_prevInnerLower = null;
		_longArmed = false;
		_shortArmed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, atr, ProcessCandle)
			.Start();

		var stop = MaxStopPercent > 0 ? new Unit(MaxStopPercent, UnitTypes.Percent) : new Unit();
		var takePercent = MaxStopPercent * SlTpRatio;
		var take = takePercent > 0 ? new Unit(takePercent, UnitTypes.Percent) : new Unit();
		StartProtection(take, stop, useMarketOrders: true);

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
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaValue.IsFormed || !atrValue.IsFormed)
			return;

		var ema = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var innerUpper = ema + atr * InnerMultiplier;
		var innerLower = ema - atr * InnerMultiplier;
		var outerUpper = ema + atr * OuterMultiplier;
		var outerLower = ema - atr * OuterMultiplier;

		if (candle.LowPrice < outerLower)
			_longArmed = true;

		if (candle.HighPrice > outerUpper)
			_shortArmed = true;

		var prevClose = _prevClose;
		var prevInnerUpper = _prevInnerUpper;
		var prevInnerLower = _prevInnerLower;

		_prevClose = close;
		_prevInnerUpper = innerUpper;
		_prevInnerLower = innerLower;

		if (prevClose is not decimal pc || prevInnerUpper is not decimal piu || prevInnerLower is not decimal pil)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longSignal = _longArmed && pc <= pil && close > innerLower;
		var shortSignal = _shortArmed && pc >= piu && close < innerUpper;

		if (longSignal)
		{
			_longArmed = false;

			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortSignal)
		{
			_shortArmed = false;

			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
	}
}
