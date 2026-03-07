// ValueMomentumAcrossAssetsStrategy.cs
// -----------------------------------------------------------------------------
// Value and momentum combination strategy.
// Uses RSI as a value (mean-reversion) signal and EMA crossover as momentum.
// Buys when RSI indicates oversold AND momentum is positive.
// Sells when RSI indicates overbought AND momentum is negative.
// Cooldown prevents excessive trading.
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
/// Strategy combining value (RSI) and momentum (EMA crossover) signals.
/// </summary>
public class ValueMomentumAcrossAssetsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
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

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private int _cooldownRemaining;

	public ValueMomentumAcrossAssetsStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Parameters");

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "RSI oversold threshold", "Parameters");

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "RSI overbought threshold", "Parameters");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 10)
			.SetDisplay("Fast EMA Period", "Fast EMA period for momentum", "Parameters");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
			.SetDisplay("Slow EMA Period", "Slow EMA period for momentum", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

		_rsi = null;
		_fastEma = null;
		_slowEma = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(_rsi, _fastEma, _slowEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal fastEmaValue, decimal slowEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var momentumBullish = fastEmaValue > slowEmaValue;
		var momentumBearish = fastEmaValue < slowEmaValue;

		// Value + Momentum buy: RSI oversold AND momentum turning positive
		if (rsiValue < RsiOversold && momentumBullish && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Value + Momentum sell: RSI overbought AND momentum turning negative
		else if (rsiValue > RsiOverbought && momentumBearish && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
