namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// TTM Squeeze Strategy.
/// Detects volatility squeeze using BB width narrowing, then trades breakouts.
/// Uses RSI for momentum confirmation.
/// Buys when BB width expands from narrow and RSI > 50.
/// Sells when BB width expands from narrow and RSI less than 50.
/// </summary>
public class TtmSqueezeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bb;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema;

	private decimal _prevBbWidth;
	private decimal _minBbWidth;
	private int _narrowBars;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public TtmSqueezeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bb = null;
		_rsi = null;
		_ema = null;
		_prevBbWidth = 0;
		_minBbWidth = decimal.MaxValue;
		_narrowBars = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bb = new BollingerBands { Length = BbLength, Width = 2.0m };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_ema = new ExponentialMovingAverage { Length = BbLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bb, _rsi, _ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bb);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue rsiValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bb.IsFormed || !_rsi.IsFormed || !_ema.IsFormed)
			return;

		if (bbValue.IsEmpty || rsiValue.IsEmpty || emaValue.IsEmpty)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal mid)
			return;

		var rsiVal = rsiValue.ToDecimal();
		var emaVal = emaValue.ToDecimal();

		// Calculate BB width as percentage
		var bbWidth = mid > 0 ? (upper - lower) / mid * 100 : 0;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevBbWidth = bbWidth;
			_minBbWidth = Math.Min(_minBbWidth, bbWidth);
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevBbWidth = bbWidth;
			_minBbWidth = Math.Min(_minBbWidth, bbWidth);
			return;
		}

		if (_prevBbWidth == 0)
		{
			_prevBbWidth = bbWidth;
			_minBbWidth = bbWidth;
			return;
		}

		// Track narrow BB (squeeze)
		if (bbWidth <= _minBbWidth * 1.1m)
		{
			_narrowBars++;
			_minBbWidth = Math.Min(_minBbWidth, bbWidth);
		}
		else if (bbWidth > _prevBbWidth && _narrowBars >= 3)
		{
			// BB is expanding after squeeze - breakout
			if (rsiVal > 50 && candle.ClosePrice > emaVal && Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_cooldownRemaining = CooldownBars;
				_narrowBars = 0;
				_minBbWidth = bbWidth;
			}
			else if (rsiVal < 50 && candle.ClosePrice < emaVal && Position >= 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
				_narrowBars = 0;
				_minBbWidth = bbWidth;
			}
			else
			{
				_narrowBars = 0;
				_minBbWidth = bbWidth;
			}
		}
		else
		{
			_narrowBars = 0;
			_minBbWidth = bbWidth;
		}

		// Exit long: price falls below lower BB
		if (Position > 0 && candle.ClosePrice < lower)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price rises above upper BB
		else if (Position < 0 && candle.ClosePrice > upper)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevBbWidth = bbWidth;
	}
}
