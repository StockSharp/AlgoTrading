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
/// True Strength Index MACD crossover strategy.
/// Generates buy when TSI crosses above its signal line and sell on opposite cross.
/// </summary>
public class TsiMacdCrossoverStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minSpread;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevTsi;
	private decimal _prevSignal;
	private bool _initialized;
	private int _cooldownRemaining;

	/// <summary>
	/// Minimum absolute spread between TSI and signal required for a valid crossover.
	/// </summary>
	public decimal MinSpread
	{
		get => _minSpread.Value;
		set => _minSpread.Value = value;
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
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TsiMacdCrossoverStrategy()
	{
		_minSpread = Param(nameof(MinSpread), 2m)
			.SetDisplay("Min Spread", "Minimum TSI-signal spread", "Signal");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a signal", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var tsi = new TrueStrengthIndex();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tsi);
			DrawOwnTrades(area);
		}
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue tsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!tsiValue.IsFinal)
			return;

		var tv = (ITrueStrengthIndexValue)tsiValue;
		if (tv.Tsi is not decimal tsi || tv.Signal is not decimal signal)
			return;

		if (!_initialized)
		{
			_prevTsi = tsi;
			_prevSignal = signal;
			_initialized = true;
			return;
		}

		var crossUp = _prevTsi <= _prevSignal && tsi > signal;
		var crossDown = _prevTsi >= _prevSignal && tsi < signal;
		var spread = Math.Abs(tsi - signal);

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (crossUp && spread >= MinSpread && _cooldownRemaining == 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (crossDown && spread >= MinSpread && _cooldownRemaining == 0 && Position >= 0)
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
