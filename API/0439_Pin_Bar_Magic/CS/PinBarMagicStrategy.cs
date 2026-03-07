namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pin Bar Magic Strategy.
/// Detects pin bar candlestick patterns at EMA/SMA levels in trending markets.
/// Buys on bullish pin bars piercing moving averages in uptrend.
/// Sells on bearish pin bars piercing moving averages in downtrend.
/// </summary>
public class PinBarMagicStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _mediumEmaLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _slowSma;
	private ExponentialMovingAverage _mediumEma;
	private ExponentialMovingAverage _fastEma;

	private int _cooldownRemaining;

	public PinBarMagicStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_slowSmaLength = Param(nameof(SlowSmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Period", "Slow SMA period", "Indicators");

		_mediumEmaLength = Param(nameof(MediumEmaLength), 18)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA Period", "Medium EMA period", "Indicators");

		_fastEmaLength = Param(nameof(FastEmaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Fast EMA period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int SlowSmaLength
	{
		get => _slowSmaLength.Value;
		set => _slowSmaLength.Value = value;
	}

	public int MediumEmaLength
	{
		get => _mediumEmaLength.Value;
		set => _mediumEmaLength.Value = value;
	}

	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
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

		_slowSma = null;
		_mediumEma = null;
		_fastEma = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_slowSma = new SimpleMovingAverage { Length = SlowSmaLength };
		_mediumEma = new ExponentialMovingAverage { Length = MediumEmaLength };
		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slowSma, _mediumEma, _fastEma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowSma);
			DrawIndicator(area, _mediumEma);
			DrawIndicator(area, _fastEma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal slowSma, decimal mediumEma, decimal fastEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_slowSma.IsFormed || !_mediumEma.IsFormed || !_fastEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Check pin bar patterns
		var candleRange = candle.HighPrice - candle.LowPrice;
		if (candleRange == 0)
			return;

		var bullishPinBar = false;
		var bearishPinBar = false;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			var lowerWick = candle.OpenPrice - candle.LowPrice;
			bullishPinBar = lowerWick > 0.60m * candleRange;

			var upperWick = candle.HighPrice - candle.ClosePrice;
			bearishPinBar = upperWick > 0.60m * candleRange;
		}
		else
		{
			var lowerWick = candle.ClosePrice - candle.LowPrice;
			bullishPinBar = lowerWick > 0.60m * candleRange;

			var upperWick = candle.HighPrice - candle.OpenPrice;
			bearishPinBar = upperWick > 0.60m * candleRange;
		}

		// Trend conditions - EMA fan
		var fanUpTrend = fastEma > mediumEma && mediumEma > slowSma;
		var fanDnTrend = fastEma < mediumEma && mediumEma < slowSma;

		// Piercing conditions - candle wick pierces through an MA level
		var bullPierce = (candle.LowPrice < fastEma && candle.ClosePrice > fastEma) ||
						 (candle.LowPrice < mediumEma && candle.ClosePrice > mediumEma) ||
						 (candle.LowPrice < slowSma && candle.ClosePrice > slowSma);

		var bearPierce = (candle.HighPrice > fastEma && candle.ClosePrice < fastEma) ||
						 (candle.HighPrice > mediumEma && candle.ClosePrice < mediumEma) ||
						 (candle.HighPrice > slowSma && candle.ClosePrice < slowSma);

		// Buy: uptrend + bullish pin bar + pierce
		if (fanUpTrend && bullishPinBar && bullPierce && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: downtrend + bearish pin bar + pierce
		else if (fanDnTrend && bearishPinBar && bearPierce && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: trend reversal (fast crosses below medium)
		else if (Position > 0 && fastEma < mediumEma)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: trend reversal (fast crosses above medium)
		else if (Position < 0 && fastEma > mediumEma)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
