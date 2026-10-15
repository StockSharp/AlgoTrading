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
/// Strategy based on True Strength Index crossover filtered by Commodity Channel Index.
/// Opens long when TSI crosses above its signal line and CCI is positive,
/// opens short when TSI crosses below its signal line and CCI is negative.
/// </summary>
public class ExpTsiCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _minTsiSpread;
	private readonly StrategyParam<decimal> _minCciMagnitude;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevTsi;
	private decimal _prevSignal;
	private bool _initialized;
	private int _cooldownRemaining;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute spread between TSI and signal required for a valid crossover.
	/// </summary>
	public decimal MinTsiSpread
	{
		get => _minTsiSpread.Value;
		set => _minTsiSpread.Value = value;
	}

	/// <summary>
	/// Minimum absolute CCI value required for confirmation.
	/// </summary>
	public decimal MinCciMagnitude
	{
		get => _minCciMagnitude.Value;
		set => _minCciMagnitude.Value = value;
	}

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ExpTsiCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation period", "CCI");

		_minTsiSpread = Param(nameof(MinTsiSpread), 2m)
			.SetDisplay("Min TSI Spread", "Minimum TSI-signal spread", "Signal");

		_minCciMagnitude = Param(nameof(MinCciMagnitude), 50m)
			.SetDisplay("Min CCI", "Minimum absolute CCI confirmation", "Signal");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a signal", "Signal");
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
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tsi, cci, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue tsiValue, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!tsiValue.IsFinal || !cciValue.IsFinal)
			return;

		var tv = (ITrueStrengthIndexValue)tsiValue;
		if (tv.Tsi is not decimal tsi || tv.Signal is not decimal signal)
			return;

		var cci = cciValue.ToDecimal();

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

		if (crossUp && spread >= MinTsiSpread && cci >= MinCciMagnitude && _cooldownRemaining == 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (crossDown && spread >= MinTsiSpread && cci <= -MinCciMagnitude && _cooldownRemaining == 0 && Position >= 0)
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
