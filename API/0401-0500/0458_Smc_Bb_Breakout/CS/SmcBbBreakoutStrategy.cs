namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Smart Money Concepts with Bollinger Bands Breakout Strategy.
/// Uses BB breakout with momentum candle filter and structure shift detection.
/// </summary>
public class SmcBbBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<decimal> _momentumBodyPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bb;

	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;
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

	public decimal BbWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	public decimal MomentumBodyPercent
	{
		get => _momentumBodyPercent.Value;
		set => _momentumBodyPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public SmcBbBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bbLength = Param(nameof(BbLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger");

		_bbWidth = Param(nameof(BbWidth), 2m)
			.SetDisplay("BB Width", "Bollinger width multiplier", "Bollinger");

		_momentumBodyPercent = Param(nameof(MomentumBodyPercent), 0.5m)
			.SetDisplay("Momentum Body %", "Minimum body vs range ratio", "Momentum");

		_cooldownBars = Param(nameof(CooldownBars), 10)
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
		_prevClose = 0;
		_prevHigh = 0;
		_prevLow = 0;
		_hasPrev = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bb = new BollingerBands { Length = BbLength, Width = BbWidth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bb, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bb);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bb.IsFormed)
			return;

		if (bbValue.IsEmpty)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal mid)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = candle.ClosePrice;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_hasPrev = true;
			return;
		}

		var price = candle.ClosePrice;
		var range = candle.HighPrice - candle.LowPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyRatio = range > 0 ? body / range : 0m;

		var isBullishMomentum = bodyRatio >= MomentumBodyPercent && candle.ClosePrice > candle.OpenPrice;
		var isBearishMomentum = bodyRatio >= MomentumBodyPercent && candle.ClosePrice < candle.OpenPrice;

		// Structure shift: new high above previous high
		var breakHigher = _hasPrev && candle.HighPrice > _prevHigh;
		var breakLower = _hasPrev && candle.LowPrice < _prevLow;

		// Buy: close above upper BB + bullish momentum + structure break higher
		if (price > upper && isBullishMomentum && breakHigher && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: close below lower BB + bearish momentum + structure break lower
		else if (price < lower && isBearishMomentum && breakLower && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: price crosses below mid BB
		else if (Position > 0 && price < mid)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price crosses above mid BB
		else if (Position < 0 && price > mid)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_hasPrev = true;
	}
}
