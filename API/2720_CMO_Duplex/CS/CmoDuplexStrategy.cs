using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Two-sided strategy built around the Chande Momentum Oscillator zero-line crossings.
/// Long and short legs can use different candle types, periods and signal offsets.
/// </summary>
public class CmoDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _longCmoPeriod;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<int> _longStopLossPoints;
	private readonly StrategyParam<int> _longTakeProfitPoints;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _shortCmoPeriod;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<int> _shortStopLossPoints;
	private readonly StrategyParam<int> _shortTakeProfitPoints;

	private ChandeMomentumOscillator _longCmo;
	private ChandeMomentumOscillator _shortCmo;

	private readonly List<decimal> _longValues = new();
	private readonly List<decimal> _shortValues = new();

	private decimal? _entryPrice;

	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	public int LongCmoPeriod
	{
		get => _longCmoPeriod.Value;
		set => _longCmoPeriod.Value = value;
	}

	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	public int LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	public int LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	public int ShortCmoPeriod
	{
		get => _shortCmoPeriod.Value;
		set => _shortCmoPeriod.Value = value;
	}

	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	public int ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	public int ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	public CmoDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Candle type for the long leg", "Long Leg");

		_longCmoPeriod = Param(nameof(LongCmoPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Long CMO Period", "CMO period for the long leg", "Long Leg");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Long Signal Bar", "Offset in bars for long signals", "Long Leg");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long trades", "Long Leg");

		_enableLongExits = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow closing long trades on signals", "Long Leg");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Long Stop Loss", "Stop loss in price steps for longs", "Risk Management");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Long Take Profit", "Take profit in price steps for longs", "Risk Management");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Candle type for the short leg", "Short Leg");

		_shortCmoPeriod = Param(nameof(ShortCmoPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Short CMO Period", "CMO period for the short leg", "Short Leg");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Short Signal Bar", "Offset in bars for short signals", "Short Leg");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short trades", "Short Leg");

		_enableShortExits = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow closing short trades on signals", "Short Leg");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Short Stop Loss", "Stop loss in price steps for shorts", "Risk Management");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Short Take Profit", "Take profit in price steps for shorts", "Risk Management");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, LongCandleType);

		if (!Equals(LongCandleType, ShortCandleType))
			yield return (Security, ShortCandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_longCmo = null;
		_shortCmo = null;
		_entryPrice = null;
		_longValues.Clear();
		_shortValues.Clear();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longCmo = new ChandeMomentumOscillator { Length = LongCmoPeriod };
		_shortCmo = new ChandeMomentumOscillator { Length = ShortCmoPeriod };

		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription.Bind(_longCmo, ProcessLongCandle);

		if (Equals(LongCandleType, ShortCandleType))
		{
			longSubscription.Bind(_shortCmo, ProcessShortCandle).Start();
		}
		else
		{
			longSubscription.Start();
			var shortSubscription = SubscribeCandles(ShortCandleType);
			shortSubscription.Bind(_shortCmo, ProcessShortCandle).Start();
		}

		StartProtection();
	}

	private void ProcessLongCandle(ICandleMessage candle, decimal cmoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_longCmo == null || !_longCmo.IsFormed)
			return;

		_longValues.Add(cmoValue);
		var shift = Math.Max(1, LongSignalBar);
		TrimBuffer(_longValues, shift + 3);

		if (_longValues.Count < shift + 1)
			return;

		var currentIndex = _longValues.Count - shift;
		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var current = _longValues[currentIndex];
		var previous = _longValues[previousIndex];

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && _entryPrice is decimal entryPrice)
		{
			var step = Security?.PriceStep ?? 1m;
			var stopPrice = LongStopLossPoints > 0 ? entryPrice - LongStopLossPoints * step : (decimal?)null;
			var takePrice = LongTakeProfitPoints > 0 ? entryPrice + LongTakeProfitPoints * step : (decimal?)null;
			var exitBySignal = EnableLongExits && previous < 0m;

			if ((takePrice.HasValue && candle.HighPrice >= takePrice.Value) ||
				(stopPrice.HasValue && candle.LowPrice <= stopPrice.Value) ||
				exitBySignal)
			{
				SellMarket(Position);
				_entryPrice = null;
			}
		}

		var crossDown = previous > 0m && current <= 0m;
		if (EnableLongEntries && crossDown && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			if (volume > 0)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private void ProcessShortCandle(ICandleMessage candle, decimal cmoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_shortCmo == null || !_shortCmo.IsFormed)
			return;

		_shortValues.Add(cmoValue);
		var shift = Math.Max(1, ShortSignalBar);
		TrimBuffer(_shortValues, shift + 3);

		if (_shortValues.Count < shift + 1)
			return;

		var currentIndex = _shortValues.Count - shift;
		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var current = _shortValues[currentIndex];
		var previous = _shortValues[previousIndex];

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position < 0 && _entryPrice is decimal entryPrice)
		{
			var step = Security?.PriceStep ?? 1m;
			var stopPrice = ShortStopLossPoints > 0 ? entryPrice + ShortStopLossPoints * step : (decimal?)null;
			var takePrice = ShortTakeProfitPoints > 0 ? entryPrice - ShortTakeProfitPoints * step : (decimal?)null;
			var exitBySignal = EnableShortExits && previous > 0m;

			if ((takePrice.HasValue && candle.LowPrice <= takePrice.Value) ||
				(stopPrice.HasValue && candle.HighPrice >= stopPrice.Value) ||
				exitBySignal)
			{
				BuyMarket(-Position);
				_entryPrice = null;
			}
		}

		var crossUp = previous < 0m && current >= 0m;
		if (EnableShortEntries && crossUp && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			if (volume > 0)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private static void TrimBuffer(List<decimal> values, int maxCount)
	{
		if (values.Count <= maxCount)
			return;

		values.RemoveRange(0, values.Count - maxCount);
	}
}
