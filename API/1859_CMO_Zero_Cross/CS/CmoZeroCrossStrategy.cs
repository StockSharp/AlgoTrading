using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters on Chande Momentum Oscillator zero cross.
/// Buys when CMO crosses below zero and sells when it crosses above.
/// Optional stop loss and take profit are applied in points.
/// </summary>
public class CmoZeroCrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _cmoPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<DataType> _candleType;

	private ChandeMomentumOscillator _cmo;
	private decimal? _prevCmo;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Period for the Chande Momentum Oscillator.
	/// </summary>
	public int CmoPeriod
	{
		get => _cmoPeriod.Value;
		set => _cmoPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Enable closing long positions.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Enable closing short positions.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CmoZeroCrossStrategy"/>.
	/// </summary>
	public CmoZeroCrossStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

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

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_cmo = null;
		_prevCmo = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cmo = new ChandeMomentumOscillator { Length = CmoPeriod };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(_cmo, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Point),
			stopLoss: new Unit(StopLoss, UnitTypes.Point));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, _cmo);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cmoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cmo == null || !_cmo.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var prev = _prevCmo;
		_prevCmo = cmoValue;
		if (prev == null)
			return;

		var crossDown = prev > 0m && cmoValue < 0m;
		var crossUp = prev < 0m && cmoValue > 0m;

		if (crossDown)
		{
			if (AllowLongEntry && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (AllowShortExit && Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		else if (crossUp)
		{
			if (AllowShortEntry && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (AllowLongExit && Position > 0)
				SellMarket(Math.Abs(Position));
		}
	}
}
