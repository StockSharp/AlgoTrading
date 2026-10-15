namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 50 EMA crossover strategy.
/// Buys when price crosses above EMA 50.
/// Sells when price crosses below EMA 50.
/// Uses RSI as momentum filter.
/// </summary>
public class Ema50CrossoverMonthlyDcaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;
	private decimal _prevClose;
	private decimal _prevEma;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public Ema50CrossoverMonthlyDcaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
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
		_prevEma = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_rsi.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevEma = emaValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			_prevEma = emaValue;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = candle.ClosePrice;
			_prevEma = emaValue;
			return;
		}

		var close = candle.ClosePrice;

		// Buy: price crosses above EMA + RSI not overbought
		var bullCross = _prevClose > 0 && _prevClose <= _prevEma && close > emaValue;
		if (bullCross && rsiValue < 70 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: price crosses below EMA + RSI not oversold
		else if (_prevClose > 0 && _prevClose >= _prevEma && close < emaValue && rsiValue > 30 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: deep below EMA
		else if (Position > 0 && close < emaValue * 0.98m)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: deep above EMA
		else if (Position < 0 && close > emaValue * 1.02m)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = close;
		_prevEma = emaValue;
	}
}
