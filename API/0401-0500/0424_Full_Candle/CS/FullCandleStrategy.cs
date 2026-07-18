namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Full Candle Strategy.
/// Trades on "full body" candles (small shadows) with EMA trend filter.
/// Buys on bullish full candle above EMA. Sells on bearish full candle below EMA.
/// </summary>
public class FullCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _shadowPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;
	private decimal? _entryPrice;
	private int _cooldownRemaining;

	public FullCandleStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Moving Averages");

		_shadowPercent = Param(nameof(ShadowPercent), 10m)
			.SetDisplay("Shadow Percent", "Maximum shadow percentage of candle range", "Strategy");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public decimal ShadowPercent
	{
		get => _shadowPercent.Value;
		set => _shadowPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_entryPrice = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var candleSize = high - low;
		if (candleSize <= 0)
			return;

		var bodySize = Math.Abs(close - open);

		// Calculate shadow sizes
		decimal upperShadow, lowerShadow;
		if (close > open)
		{
			upperShadow = high - close;
			lowerShadow = open - low;
		}
		else
		{
			upperShadow = high - open;
			lowerShadow = close - low;
		}

		var totalShadowPercent = ((upperShadow + lowerShadow) * 100) / candleSize;

		// Full candle = small shadows (body fills most of the range)
		var isFullCandle = totalShadowPercent <= ShadowPercent && bodySize > 0;

		// Exit conditions
		if (Position > 0 && _entryPrice.HasValue && close > _entryPrice.Value * 1.003m)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = null;
			_cooldownRemaining = CooldownBars;
			return;
		}
		else if (Position < 0 && _entryPrice.HasValue && close < _entryPrice.Value * 0.997m)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = null;
			_cooldownRemaining = CooldownBars;
			return;
		}

		// Entry: full bullish candle above EMA
		if (isFullCandle && close > open && close > emaValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = close;
			_cooldownRemaining = CooldownBars;
		}
		// Entry: full bearish candle below EMA
		else if (isFullCandle && close < open && close < emaValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = close;
			_cooldownRemaining = CooldownBars;
		}
	}
}
