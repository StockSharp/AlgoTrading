using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters on Chande Momentum Oscillator zero cross.
/// </summary>
public class CmoZeroCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _cmoPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minAbsCmo;
	private readonly StrategyParam<int> _cooldownBars;

	private ChandeMomentumOscillator _cmo = null!;
	private decimal? _prevCmo;
	private int _cooldownRemaining;

	public int CmoPeriod
	{
		get => _cmoPeriod.Value;
		set => _cmoPeriod.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal MinAbsCmo
	{
		get => _minAbsCmo.Value;
		set => _minAbsCmo.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public CmoZeroCrossStrategy()
	{
		_cmoPeriod = Param(nameof(CmoPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CMO Period", "Period for Chande Momentum Oscillator", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pt)", "Stop loss in points", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pt)", "Take profit in points", "Risk Management");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Permission to open long positions", "Strategy");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Permission to open short positions", "Strategy");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Permission to close long positions", "Strategy");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Permission to close short positions", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_minAbsCmo = Param(nameof(MinAbsCmo), 5m)
			.SetDisplay("Minimum CMO", "Minimum absolute CMO value required after a zero cross", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_cmo = null!;
		_prevCmo = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cmo = new ChandeMomentumOscillator { Length = CmoPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_cmo, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cmo);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cmoValue)
	{
		if (candle.State != CandleStates.Finished || _cmo == null || !_cmo.IsFormed)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var prev = _prevCmo;
		_prevCmo = cmoValue;
		if (prev == null || _cooldownRemaining > 0)
			return;

		var crossUp = prev < 0m && cmoValue > 0m && Math.Abs(cmoValue) >= MinAbsCmo;
		var crossDown = prev > 0m && cmoValue < 0m && Math.Abs(cmoValue) >= MinAbsCmo;

		if (crossUp)
		{
			if (AllowShortExit && Position < 0)
				BuyMarket();

			if (AllowLongEntry && Position <= 0)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (crossDown)
		{
			if (AllowLongExit && Position > 0)
				SellMarket();

			if (AllowShortEntry && Position >= 0)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
	}
}

