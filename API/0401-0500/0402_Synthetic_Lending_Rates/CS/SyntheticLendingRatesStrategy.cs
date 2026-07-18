// SyntheticLendingRatesStrategy.cs
// -----------------------------------------------------------------------------
// Uses change in synthetic lending-rate intensity derived from price momentum
// to take directional positions.
// Compares short-term and long-term moving averages as a proxy for
// synthetic lending-rate shifts. Buys when short-term momentum increases,
// sells when it decreases, with cooldown management.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades based on changes in synthetic lending-rate intensity derived from price momentum.
/// </summary>
public class SyntheticLendingRatesStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Short-term momentum period.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}

	/// <summary>
	/// Long-term momentum period.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private ExponentialMovingAverage _shortEma;
	private ExponentialMovingAverage _longEma;
	private decimal _prevShortValue;
	private decimal _prevLongValue;
	private int _cooldownRemaining;

	public SyntheticLendingRatesStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 5)
			.SetDisplay("Short Period", "Short-term momentum period", "Parameters");

		_longPeriod = Param(nameof(LongPeriod), 20)
			.SetDisplay("Long Period", "Long-term momentum period", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 30)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_shortEma = null;
		_longEma = null;
		_prevShortValue = 0;
		_prevLongValue = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_shortEma = new ExponentialMovingAverage { Length = ShortPeriod };
		_longEma = new ExponentialMovingAverage { Length = LongPeriod };

		SubscribeCandles(CandleType)
			.Bind(_shortEma, _longEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_shortEma.IsFormed || !_longEma.IsFormed)
		{
			_prevShortValue = shortValue;
			_prevLongValue = longValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevShortValue = shortValue;
			_prevLongValue = longValue;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevShortValue = shortValue;
			_prevLongValue = longValue;
			return;
		}

		// Synthetic intensity: short-term momentum relative to long-term
		var currentIntensity = shortValue - longValue;
		var prevIntensity = _prevShortValue - _prevLongValue;

		// Intensity increasing (short EMA rising faster) -> buy signal
		if (currentIntensity > 0 && prevIntensity <= 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Intensity decreasing (short EMA falling relative to long) -> sell signal
		else if (currentIntensity < 0 && prevIntensity >= 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevShortValue = shortValue;
		_prevLongValue = longValue;
	}
}
