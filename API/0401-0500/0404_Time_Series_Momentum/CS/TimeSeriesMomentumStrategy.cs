// TimeSeriesMomentumStrategy.cs
// -----------------------------------------------------------------------------
// Time series momentum strategy with volatility scaling.
// Uses rate of change (momentum) and standard deviation (volatility)
// to determine position direction and sizing.
// Long when momentum positive, short when negative.
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
/// Time series momentum strategy with volatility scaling.
/// </summary>
public class TimeSeriesMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _volPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Momentum lookback period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Volatility measurement period.
	/// </summary>
	public int VolPeriod
	{
		get => _volPeriod.Value;
		set => _volPeriod.Value = value;
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

	private RateOfChange _momentum;
	private StandardDeviation _volatility;
	private int _cooldownRemaining;

	public TimeSeriesMomentumStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 20)
			.SetDisplay("Momentum Period", "Lookback for momentum calculation", "Parameters");

		_volPeriod = Param(nameof(VolPeriod), 14)
			.SetDisplay("Volatility Period", "Period for volatility estimation", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 25)
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

		_momentum = null;
		_volatility = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_momentum = new RateOfChange { Length = MomentumPeriod };
		_volatility = new StandardDeviation { Length = VolPeriod };

		SubscribeCandles(CandleType)
			.Bind(_momentum, _volatility, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue, decimal volatilityValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_momentum.IsFormed || !_volatility.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Positive momentum -> long; Negative momentum -> short
		if (momentumValue > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (momentumValue < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
