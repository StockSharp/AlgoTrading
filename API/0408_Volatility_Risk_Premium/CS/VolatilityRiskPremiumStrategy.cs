// VolatilityRiskPremiumStrategy.cs
// -----------------------------------------------------------------------------
// Volatility risk premium strategy.
// Compares realized volatility (StdDev) to ATR as a proxy for vol premium.
// When realized vol is low relative to ATR (vol premium is high), sells vol
// by going long. When realized vol exceeds ATR, exits to flat.
// Uses Bollinger Bands width as an alternative volatility measure.
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
/// Volatility risk premium strategy using realized vs implied volatility proxy.
/// </summary>
public class VolatilityRiskPremiumStrategy : Strategy
{
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _volRatioThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Standard deviation period for realized volatility.
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for implied movement proxy.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Ratio threshold for vol premium signal.
	/// </summary>
	public decimal VolRatioThreshold
	{
		get => _volRatioThreshold.Value;
		set => _volRatioThreshold.Value = value;
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

	private StandardDeviation _stdDev;
	private AverageTrueRange _atr;
	private int _cooldownRemaining;

	public VolatilityRiskPremiumStrategy()
	{
		_stdDevPeriod = Param(nameof(StdDevPeriod), 20)
			.SetDisplay("StdDev Period", "Period for realized volatility", "Parameters");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Parameters");

		_volRatioThreshold = Param(nameof(VolRatioThreshold), 1.0m)
			.SetDisplay("Vol Ratio Threshold", "StdDev/ATR ratio threshold", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 20)
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

		_stdDev = null;
		_atr = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stdDev = new StandardDeviation { Length = StdDevPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		SubscribeCandles(CandleType)
			.Bind(_stdDev, _atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdDevValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_stdDev.IsFormed || !_atr.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		if (atrValue <= 0)
			return;

		var volRatio = stdDevValue / atrValue;

		// Low realized vol relative to ATR -> vol premium is high -> sell vol (go long)
		if (volRatio < VolRatioThreshold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// High realized vol relative to ATR -> vol premium collapsed -> exit or go short
		else if (volRatio > VolRatioThreshold * 1.5m && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
