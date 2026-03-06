using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that trades SuperTrend direction only when momentum confirms acceleration.
/// </summary>
public class SupertrendWithMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _supertrend;
	private Momentum _momentum;
	private decimal _prevMomentum;
	private bool _isInitialized;
	private int _cooldown;

	/// <summary>
	/// SuperTrend period.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// SuperTrend multiplier.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Bars to wait between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SupertrendWithMomentumStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetRange(2, 50)
			.SetDisplay("Supertrend Period", "Period of the SuperTrend indicator", "Indicators");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Supertrend Multiplier", "Multiplier of the SuperTrend indicator", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("Momentum Period", "Period of the Momentum indicator", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 84)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_supertrend = null;
		_momentum = null;
		_prevMomentum = 0m;
		_isInitialized = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_supertrend = new SuperTrend
		{
			Length = SupertrendPeriod,
			Multiplier = SupertrendMultiplier,
		};
		_momentum = new Momentum { Length = MomentumPeriod };
		_cooldown = 0;
		_isInitialized = false;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_supertrend, _momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawIndicator(area, _momentum);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal supertrendValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_supertrend.IsFormed || !_momentum.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (!_isInitialized)
		{
			_prevMomentum = momentumValue;
			_isInitialized = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMomentum = momentumValue;
			return;
		}

		var price = candle.ClosePrice;
		var isAboveSupertrend = price > supertrendValue;
		var isBelowSupertrend = price < supertrendValue;
		var isMomentumRising = momentumValue > _prevMomentum;
		var isMomentumFalling = momentumValue < _prevMomentum;

		if (Position == 0)
		{
			if (isAboveSupertrend && isMomentumRising && momentumValue >= 100m)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (isBelowSupertrend && isMomentumFalling && momentumValue <= 100m)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (isBelowSupertrend || isMomentumFalling)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (isAboveSupertrend || isMomentumRising)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}

		_prevMomentum = momentumValue;
	}
}
