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
/// Trend-following strategy that activates Parabolic SAR signals only when ATR expands above its recent regime.
/// </summary>
public class ParabolicSarWithVolatilityExpansionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarAf;
	private readonly StrategyParam<decimal> _sarMaxAf;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _volatilityExpansionFactor;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ParabolicSar _parabolicSar;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrSma;
	private StandardDeviation _atrStdDev;
	private int _cooldown;

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarAf
	{
		get => _sarAf.Value;
		set => _sarAf.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaxAf
	{
		get => _sarMaxAf.Value;
		set => _sarMaxAf.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for volatility expansion detection.
	/// </summary>
	public decimal VolatilityExpansionFactor
	{
		get => _volatilityExpansionFactor.Value;
		set => _volatilityExpansionFactor.Value = value;
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
	public ParabolicSarWithVolatilityExpansionStrategy()
	{
		_sarAf = Param(nameof(SarAf), 0.02m)
			.SetRange(0.001m, 1m)
			.SetDisplay("SAR AF", "Parabolic SAR acceleration factor", "Indicators");

		_sarMaxAf = Param(nameof(SarMaxAf), 0.2m)
			.SetRange(0.01m, 2m)
			.SetDisplay("SAR Max AF", "Parabolic SAR maximum acceleration factor", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

		_volatilityExpansionFactor = Param(nameof(VolatilityExpansionFactor), 1.6m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Volatility Expansion Factor", "Factor for volatility expansion detection", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 84)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

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

		_parabolicSar = null;
		_atr = null;
		_atrSma = null;
		_atrStdDev = null;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarAf,
			AccelerationMax = SarMaxAf,
		};
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrSma = new SimpleMovingAverage { Length = AtrPeriod };
		_atrStdDev = new StandardDeviation { Length = AtrPeriod };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrValue = _atr.Process(candle).ToDecimal();
		var atrSmaValue = _atrSma.Process(atrValue, candle.OpenTime, true).ToDecimal();
		var atrStdDevValue = _atrStdDev.Process(atrValue, candle.OpenTime, true).ToDecimal();

		if (!_parabolicSar.IsFormed || !_atr.IsFormed || !_atrSma.IsFormed || !_atrStdDev.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var volatilityThreshold = atrSmaValue + VolatilityExpansionFactor * atrStdDevValue;
		var isVolatilityExpanding = atrValue >= volatilityThreshold;
		var isAboveSar = candle.ClosePrice > sarValue;
		var isBelowSar = candle.ClosePrice < sarValue;

		if (Position == 0)
		{
			if (isVolatilityExpanding && isAboveSar)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (isVolatilityExpanding && isBelowSar)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		if (Position > 0 && (!isAboveSar || !isVolatilityExpanding))
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (!isBelowSar || !isVolatilityExpanding))
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
