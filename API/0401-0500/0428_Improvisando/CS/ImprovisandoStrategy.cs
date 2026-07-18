namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Improvisando Strategy.
/// Uses EMA trend + RSI filter + candle pattern (engulfing) for entries.
/// Exits via take profit or EMA crossback.
/// </summary>
public class ImprovisandoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;

	private decimal _prevClose;
	private decimal _prevOpen;
	private int _cooldownRemaining;
	private decimal? _entryPrice;

	public ImprovisandoStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Moving Averages");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

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

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_rsi = null;
		_prevClose = 0;
		_prevOpen = 0;
		_cooldownRemaining = 0;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _rsi, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal ema, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_rsi.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevOpen = candle.OpenPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			_prevOpen = candle.OpenPrice;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = candle.ClosePrice;
			_prevOpen = candle.OpenPrice;
			return;
		}

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		// Engulfing patterns
		var prevBearish = _prevClose < _prevOpen && _prevClose > 0;
		var prevBullish = _prevClose > _prevOpen && _prevClose > 0;

		// Bullish engulfing: previous red, current green, close above prev open
		var buyPattern = prevBearish && close > open && close > _prevOpen;
		// Bearish engulfing: previous green, current red, close below prev open
		var sellPattern = prevBullish && close < open && close < _prevOpen;

		// Exit long: price crosses below EMA or take profit
		if (Position > 0)
		{
			var tp = _entryPrice.HasValue && close > _entryPrice.Value * 1.02m;
			if (close < ema || tp)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = null;
				_cooldownRemaining = CooldownBars;
				_prevClose = close;
				_prevOpen = open;
				return;
			}
		}
		// Exit short: price crosses above EMA or take profit
		else if (Position < 0)
		{
			var tp = _entryPrice.HasValue && close < _entryPrice.Value * 0.98m;
			if (close > ema || tp)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
				_cooldownRemaining = CooldownBars;
				_prevClose = close;
				_prevOpen = open;
				return;
			}
		}

		// Buy: engulfing + above EMA + RSI not overbought
		if (buyPattern && close > ema && rsi < 65 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = close;
			_cooldownRemaining = CooldownBars;
		}
		// Sell: engulfing + below EMA + RSI not oversold
		else if (sellPattern && close < ema && rsi > 35 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = close;
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = close;
		_prevOpen = open;
	}
}
