namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI-based trend strategy with a simple recursive filter (EMA) as trend confirmation.
/// Buys when RSI crosses above oversold level and EMA confirms uptrend.
/// Sells when RSI crosses below overbought level and EMA confirms downtrend.
/// </summary>
public class RsiRftlStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema;

	private decimal _prevRsi;
	private decimal _entryPrice;
	private int _cooldown;

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// EMA period for trend filter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Overbought RSI level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Oversold RSI level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RsiRftlStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI oscillator", "Indicator");

		_emaPeriod = Param(nameof(EmaPeriod), 44)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the trend EMA filter", "Indicator");

		_overbought = Param(nameof(Overbought), 75m)
			.SetDisplay("Overbought", "RSI overbought level", "Levels");

		_oversold = Param(nameof(Oversold), 25m)
			.SetDisplay("Oversold", "RSI oversold level", "Levels");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
		_ema = null;
		_prevRsi = 0;
		_entryPrice = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_rsi, _ema, OnProcess);
		subscription.Start();
	}

	private void OnProcess(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_ema.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevRsi = rsiValue;
			return;
		}

		var close = candle.ClosePrice;
		var trendUp = close > emaValue;
		var trendDown = close < emaValue;

		// Buy: RSI crosses above oversold + uptrend
		if (_prevRsi < Oversold && rsiValue >= Oversold && trendUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_entryPrice = close;
			_cooldown = 10;
		}
		// Sell: RSI crosses below overbought + downtrend
		else if (_prevRsi > Overbought && rsiValue <= Overbought && trendDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_entryPrice = close;
			_cooldown = 10;
		}

		// Exit long on overbought
		if (Position > 0 && rsiValue > 80m)
		{
			SellMarket();
			_entryPrice = 0;
			_cooldown = 10;
		}
		// Exit short on oversold
		else if (Position < 0 && rsiValue < 20m)
		{
			BuyMarket();
			_entryPrice = 0;
			_cooldown = 10;
		}

		_prevRsi = rsiValue;
	}
}
