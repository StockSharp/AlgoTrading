using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on True Strength Index crossover filtered by Williams %R.
/// Buys when TSI crosses above its signal line and WPR is in oversold,
/// sells when TSI crosses below its signal line and WPR is in overbought.
/// </summary>
public class TsiWprCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevTsi;
	private decimal _prevSignal;
	private bool _initialized;
	private int _cooldownRemaining;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candles type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TsiWprCrossStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a signal", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevTsi = 0m;
		_prevSignal = 0m;
		_initialized = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var tsi = new TrueStrengthIndex();
		var wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tsi, wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue tsiValue, IIndicatorValue wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!tsiValue.IsFinal || !wprValue.IsFinal)
			return;

		var tv = (ITrueStrengthIndexValue)tsiValue;
		if (tv.Tsi is not decimal tsi || tv.Signal is not decimal signal)
			return;

		var wpr = wprValue.ToDecimal();

		if (!_initialized)
		{
			_prevTsi = tsi;
			_prevSignal = signal;
			_initialized = true;
			return;
		}

		var crossedUp = _prevTsi <= _prevSignal && tsi > signal;
		var crossedDown = _prevTsi >= _prevSignal && tsi < signal;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		// WPR range: -100 to 0. Oversold < -80, Overbought > -20
		if (crossedUp && wpr < -55 && _cooldownRemaining == 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (crossedDown && wpr > -45 && _cooldownRemaining == 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}

		_prevTsi = tsi;
		_prevSignal = signal;
	}
}
