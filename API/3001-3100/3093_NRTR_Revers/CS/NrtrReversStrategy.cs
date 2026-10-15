using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NRTR reversal strategy using ATR channel around EMA.
/// Goes long when price breaks above EMA + ATR*multiplier.
/// Goes short when price breaks below EMA - ATR*multiplier.
/// </summary>
public class NrtrReversStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _volatilityMultiplier;
	private readonly StrategyParam<int> _emaPeriod;

	private AverageTrueRange _atr;
	private ExponentialMovingAverage _ema;

	private decimal _entryPrice;
	private int _cooldown;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for band calculation.
	/// </summary>
	public decimal VolatilityMultiplier
	{
		get => _volatilityMultiplier.Value;
		set => _volatilityMultiplier.Value = value;
	}

	/// <summary>
	/// EMA period for trend center.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public NrtrReversStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR averaging period", "Indicator");

		_volatilityMultiplier = Param(nameof(VolatilityMultiplier), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier for bands", "Indicator");

		_emaPeriod = Param(nameof(EmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend center period", "Indicator");
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

		_atr = null;
		_ema = null;
		_entryPrice = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_atr, _ema, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed || !_ema.IsFormed)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var close = candle.ClosePrice;
		var bandOffset = atrValue * VolatilityMultiplier;

		var upperBand = emaValue + bandOffset;
		var lowerBand = emaValue - bandOffset;

		// Buy: price breaks above upper band
		if (close > upperBand && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_entryPrice = close;
			_cooldown = 50;
		}
		// Sell: price breaks below lower band
		else if (close < lowerBand && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_entryPrice = close;
			_cooldown = 50;
		}
		// Exit long: price returns to EMA
		else if (Position > 0 && close < emaValue)
		{
			SellMarket();
			_entryPrice = 0;
			_cooldown = 50;
		}
		// Exit short: price returns to EMA
		else if (Position < 0 && close > emaValue)
		{
			BuyMarket();
			_entryPrice = 0;
			_cooldown = 50;
		}
	}
}
