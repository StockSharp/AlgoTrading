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
/// Momentum strategy that allows longs or shorts only when the current month historically supports that seasonal bias.
/// </summary>
public class SeasonalityAdjustedMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _seasonalityThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<int, decimal> _seasonalStrengthByMonth = [];
	private Momentum _momentum;
	private SimpleMovingAverage _momentumAverage;
	private int _cooldown;

	/// <summary>
	/// Period for the momentum indicator.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute seasonality strength required to allow directional entries.
	/// </summary>
	public decimal SeasonalityThreshold
	{
		get => _seasonalityThreshold.Value;
		set => _seasonalityThreshold.Value = value;
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
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	public SeasonalityAdjustedMomentumStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetRange(3, 100)
			.SetDisplay("Momentum Period", "Period for the momentum indicator", "Indicators");

		_seasonalityThreshold = Param(nameof(SeasonalityThreshold), 0.2m)
			.SetRange(0m, 1m)
			.SetDisplay("Seasonality Threshold", "Minimum absolute seasonality strength required for entries", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 120)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");

		InitializeSeasonalityData();
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
		_momentumAverage = null;
		_cooldown = 0;
		_seasonalStrengthByMonth.Clear();
		InitializeSeasonalityData();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_momentum = new Momentum { Length = MomentumPeriod };
		_momentumAverage = new SimpleMovingAverage { Length = MomentumPeriod };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _momentum);
			DrawIndicator(area, _momentumAverage);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentumAvgValue = _momentumAverage.Process(momentumValue, candle.OpenTime, true).ToDecimal();

		if (!_momentum.IsFormed || !_momentumAverage.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var seasonalStrength = GetSeasonalStrength(candle.OpenTime.Month);
		var allowLong = seasonalStrength >= SeasonalityThreshold;
		var allowShort = seasonalStrength <= -SeasonalityThreshold;
		var bullishMomentum = momentumValue > momentumAvgValue;
		var bearishMomentum = momentumValue < momentumAvgValue;

		if (Position > 0)
		{
			if (!allowLong || bearishMomentum)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}

			return;
		}

		if (Position < 0)
		{
			if (!allowShort || bullishMomentum)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}

			return;
		}

		if (allowLong && bullishMomentum)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (allowShort && bearishMomentum)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
	}

	private decimal GetSeasonalStrength(int month)
		=> _seasonalStrengthByMonth.TryGetValue(month, out var strength) ? strength : 0m;

	private void InitializeSeasonalityData()
	{
		_seasonalStrengthByMonth[1] = 0.8m;
		_seasonalStrengthByMonth[2] = 0.2m;
		_seasonalStrengthByMonth[3] = 0.5m;
		_seasonalStrengthByMonth[4] = 0.7m;
		_seasonalStrengthByMonth[5] = 0.3m;
		_seasonalStrengthByMonth[6] = -0.2m;
		_seasonalStrengthByMonth[7] = -0.3m;
		_seasonalStrengthByMonth[8] = -0.4m;
		_seasonalStrengthByMonth[9] = -0.7m;
		_seasonalStrengthByMonth[10] = 0.4m;
		_seasonalStrengthByMonth[11] = 0.6m;
		_seasonalStrengthByMonth[12] = 0.9m;
	}
}
