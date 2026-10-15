using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Engulfing with trend strategy.
/// A bullish candle whose body engulfs the body of the preceding bearish candle goes long while SuperTrend is up; a bearish
/// engulfing candle goes short while SuperTrend is down. The engulfing body must be at least EngulfingThreshold percent of its
/// range and the engulfed candle must not be a boring candle (body below BoringThreshold percent of its range). The stop is the
/// pattern extreme offset by one ATR and the target lies StopLevel percent of that risk away from the entry.
/// </summary>
public class EngulfingWithTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _boringThreshold;
	private readonly StrategyParam<decimal> _engulfingThreshold;
	private readonly StrategyParam<decimal> _stopLevel;

	private ICandleMessage _prevCandle;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR period of SuperTrend and the stop offset.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// SuperTrend ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Body percentage of the range below which a candle is boring.
	/// </summary>
	public decimal BoringThreshold
	{
		get => _boringThreshold.Value;
		set => _boringThreshold.Value = value;
	}

	/// <summary>
	/// Minimum body percentage of the range of the engulfing candle.
	/// </summary>
	public decimal EngulfingThreshold
	{
		get => _engulfingThreshold.Value;
		set => _engulfingThreshold.Value = value;
	}

	/// <summary>
	/// Target distance in percent of the risk.
	/// </summary>
	public decimal StopLevel
	{
		get => _stopLevel.Value;
		set => _stopLevel.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EngulfingWithTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period of SuperTrend and the stop offset", "SuperTrend");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "SuperTrend ATR multiplier", "SuperTrend");

		_boringThreshold = Param(nameof(BoringThreshold), 25m)
			.SetNotNegative()
			.SetDisplay("Boring Threshold %", "Body percentage of the range below which a candle is boring", "Pattern");

		_engulfingThreshold = Param(nameof(EngulfingThreshold), 50m)
			.SetNotNegative()
			.SetDisplay("Engulfing Threshold %", "Minimum body percentage of the engulfing candle", "Pattern");

		_stopLevel = Param(nameof(StopLevel), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Level %", "Target distance in percent of the risk", "Risk");
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
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;

		var superTrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrMultiplier };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawOwnTrades(area);
		}
	}

	private static decimal BodyPercent(ICandleMessage candle)
	{
		var range = candle.HighPrice - candle.LowPrice;
		return range > 0m ? Math.Abs(candle.ClosePrice - candle.OpenPrice) / range * 100m : 0m;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue superTrendValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prev = _prevCandle;
		_prevCandle = candle;

		if (!superTrendValue.IsFormed || !atrValue.IsFormed || superTrendValue is not SuperTrendIndicatorValue trend)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);
			return;
		}

		if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);
			return;
		}

		if (prev == null)
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var strongBody = BodyPercent(candle) >= EngulfingThreshold;
		var prevNotBoring = BodyPercent(prev) >= BoringThreshold;

		var bullishEngulfing = prev.ClosePrice < prev.OpenPrice && close > candle.OpenPrice
			&& candle.OpenPrice <= prev.ClosePrice && close >= prev.OpenPrice;
		var bearishEngulfing = prev.ClosePrice > prev.OpenPrice && close < candle.OpenPrice
			&& candle.OpenPrice >= prev.ClosePrice && close <= prev.OpenPrice;

		if (trend.IsUpTrend && bullishEngulfing && strongBody && prevNotBoring)
		{
			var stop = Math.Min(candle.LowPrice, prev.LowPrice) - atr;
			var risk = close - stop;
			if (risk <= 0m)
				return;

			_stopPrice = stop;
			_takePrice = close + risk * StopLevel / 100m;
			BuyMarket(Volume);
		}
		else if (!trend.IsUpTrend && bearishEngulfing && strongBody && prevNotBoring)
		{
			var stop = Math.Max(candle.HighPrice, prev.HighPrice) + atr;
			var risk = stop - close;
			if (risk <= 0m)
				return;

			_stopPrice = stop;
			_takePrice = close - risk * StopLevel / 100m;
			SellMarket(Volume);
		}
	}
}
