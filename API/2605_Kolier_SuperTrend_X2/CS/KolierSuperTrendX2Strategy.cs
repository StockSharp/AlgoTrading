using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Kolier SuperTrend X2 strategy that combines higher timeframe trend filtering and lower timeframe entries.
/// </summary>
public class KolierSuperTrendX2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<int> _trendAtrPeriod;
	private readonly StrategyParam<decimal> _trendAtrMultiplier;
	private readonly StrategyParam<int> _entryAtrPeriod;
	private readonly StrategyParam<decimal> _entryAtrMultiplier;
	private readonly StrategyParam<KolierTrendMode> _trendMode;
	private readonly StrategyParam<KolierTrendMode> _entryMode;
	private readonly StrategyParam<int> _trendSignalShift;
	private readonly StrategyParam<int> _entrySignalShift;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _closeBuyOnTrendFlip;
	private readonly StrategyParam<bool> _closeSellOnTrendFlip;
	private readonly StrategyParam<bool> _closeBuyOnEntryFlip;
	private readonly StrategyParam<bool> _closeSellOnEntryFlip;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _slippage;

	private SuperTrend _trendSuperTrend = null!;
	private SuperTrend _entrySuperTrend = null!;

	private readonly List<int> _trendDirections = new();
	private readonly List<int> _entryDirections = new();

	private int _trendDirection;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="KolierSuperTrendX2Strategy"/>.
	/// </summary>
	public KolierSuperTrendX2Strategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Trend Timeframe", "Timeframe for trend SuperTrend", "Data");

		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Entry Timeframe", "Timeframe for entry SuperTrend", "Data");

		_trendAtrPeriod = Param(nameof(TrendAtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Trend ATR Period", "ATR period for trend SuperTrend", "Trend");

		_trendAtrMultiplier = Param(nameof(TrendAtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Trend ATR Multiplier", "ATR multiplier for trend SuperTrend", "Trend");

		_entryAtrPeriod = Param(nameof(EntryAtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Entry ATR Period", "ATR period for entry SuperTrend", "Entry");

		_entryAtrMultiplier = Param(nameof(EntryAtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Entry ATR Multiplier", "ATR multiplier for entry SuperTrend", "Entry");

		_trendMode = Param(nameof(TrendMode), KolierTrendMode.NewWay)
			.SetDisplay("Trend Mode", "Mode used for trend confirmation", "Trend");

		_entryMode = Param(nameof(EntryMode), KolierTrendMode.NewWay)
			.SetDisplay("Entry Mode", "Mode used for entry detection", "Entry");

		_trendSignalShift = Param(nameof(TrendSignalShift), 1)
			.SetRange(0, 10)
			.SetDisplay("Trend Signal Shift", "Bars to delay trend confirmation", "Trend");

		_entrySignalShift = Param(nameof(EntrySignalShift), 1)
			.SetRange(0, 10)
			.SetDisplay("Entry Signal Shift", "Bars to delay entry confirmation", "Entry");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_closeBuyOnTrendFlip = Param(nameof(CloseBuyOnTrendFlip), true)
			.SetDisplay("Close Long On Trend Flip", "Close longs when higher timeframe turns bearish", "Exits");

		_closeSellOnTrendFlip = Param(nameof(CloseSellOnTrendFlip), true)
			.SetDisplay("Close Short On Trend Flip", "Close shorts when higher timeframe turns bullish", "Exits");

		_closeBuyOnEntryFlip = Param(nameof(CloseBuyOnEntryFlip), false)
			.SetDisplay("Close Long On Entry Flip", "Close longs when entry SuperTrend flips down", "Exits");

		_closeSellOnEntryFlip = Param(nameof(CloseSellOnEntryFlip), false)
			.SetDisplay("Close Short On Entry Flip", "Close shorts when entry SuperTrend flips up", "Exits");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss (steps)", "Stop loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit (steps)", "Take profit distance in price steps", "Risk");

		_slippage = Param(nameof(Slippage), 10m)
			.SetDisplay("Slippage", "Reserved parameter for slippage handling", "Trading");
	}

	/// <summary>
	/// Trading volume used for new entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type used for the trend filter.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for entry signals.
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <summary>
	/// ATR period for the trend SuperTrend filter.
	/// </summary>
	public int TrendAtrPeriod
	{
		get => _trendAtrPeriod.Value;
		set => _trendAtrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the trend SuperTrend filter.
	/// </summary>
	public decimal TrendAtrMultiplier
	{
		get => _trendAtrMultiplier.Value;
		set => _trendAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR period for the entry SuperTrend filter.
	/// </summary>
	public int EntryAtrPeriod
	{
		get => _entryAtrPeriod.Value;
		set => _entryAtrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the entry SuperTrend filter.
	/// </summary>
	public decimal EntryAtrMultiplier
	{
		get => _entryAtrMultiplier.Value;
		set => _entryAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Confirmation mode for the trend timeframe.
	/// </summary>
	public KolierTrendMode TrendMode
	{
		get => _trendMode.Value;
		set => _trendMode.Value = value;
	}

	/// <summary>
	/// Confirmation mode for the entry timeframe.
	/// </summary>
	public KolierTrendMode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Bars to shift when confirming trend direction.
	/// </summary>
	public int TrendSignalShift
	{
		get => _trendSignalShift.Value;
		set => _trendSignalShift.Value = value;
	}

	/// <summary>
	/// Bars to shift when confirming entry signals.
	/// </summary>
	public int EntrySignalShift
	{
		get => _entrySignalShift.Value;
		set => _entrySignalShift.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Close long positions when the higher timeframe turns bearish.
	/// </summary>
	public bool CloseBuyOnTrendFlip
	{
		get => _closeBuyOnTrendFlip.Value;
		set => _closeBuyOnTrendFlip.Value = value;
	}

	/// <summary>
	/// Close short positions when the higher timeframe turns bullish.
	/// </summary>
	public bool CloseSellOnTrendFlip
	{
		get => _closeSellOnTrendFlip.Value;
		set => _closeSellOnTrendFlip.Value = value;
	}

	/// <summary>
	/// Close long positions when the entry SuperTrend flips bearish.
	/// </summary>
	public bool CloseBuyOnEntryFlip
	{
		get => _closeBuyOnEntryFlip.Value;
		set => _closeBuyOnEntryFlip.Value = value;
	}

	/// <summary>
	/// Close short positions when the entry SuperTrend flips bullish.
	/// </summary>
	public bool CloseSellOnEntryFlip
	{
		get => _closeSellOnEntryFlip.Value;
		set => _closeSellOnEntryFlip.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Placeholder parameter for slippage configuration.
	/// </summary>
	public decimal Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, EntryCandleType);

		if (!TrendCandleType.Equals(EntryCandleType))
		{
			yield return (Security, TrendCandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendDirections.Clear();
		_entryDirections.Clear();
		_trendDirection = 0;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendSuperTrend = new SuperTrend
		{
			Length = TrendAtrPeriod,
			Multiplier = TrendAtrMultiplier
		};

		_entrySuperTrend = new SuperTrend
		{
			Length = EntryAtrPeriod,
			Multiplier = EntryAtrMultiplier
		};

		var entrySubscription = SubscribeCandles(EntryCandleType);

		if (TrendCandleType.Equals(EntryCandleType))
		{
			entrySubscription
				.BindEx(_trendSuperTrend, ProcessTrendCandle)
				.BindEx(_entrySuperTrend, ProcessEntryCandle)
				.Start();
		}
		else
		{
			entrySubscription
				.BindEx(_entrySuperTrend, ProcessEntryCandle)
				.Start();

			var trendSubscription = SubscribeCandles(TrendCandleType);
			trendSubscription
				.BindEx(_trendSuperTrend, ProcessTrendCandle)
				.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, entrySubscription);
			DrawIndicator(area, _entrySuperTrend);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessTrendCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal || value is not SuperTrendIndicatorValue trendValue)
			return;

		if (!_trendSuperTrend.IsFormed)
			return;

		var direction = trendValue.IsUpTrend ? 1 : trendValue.IsDownTrend ? -1 : 0;
		if (direction == 0)
			return;

		UpdateHistory(_trendDirections, direction, TrendSignalShift + 3);

		var current = GetDirection(_trendDirections, TrendSignalShift);
		var previous = GetDirection(_trendDirections, TrendSignalShift + 1);

		if (current is null || previous is null)
			return;

		switch (TrendMode)
		{
			case KolierTrendMode.NewWay:
				_trendDirection = current.Value;
				break;
			default:
				if (current == previous)
				{
					_trendDirection = current.Value;
				}
				break;
		}
	}

	private void ProcessEntryCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal || value is not SuperTrendIndicatorValue entryValue)
			return;

		if (!_entrySuperTrend.IsFormed)
			return;

		var direction = entryValue.IsUpTrend ? 1 : entryValue.IsDownTrend ? -1 : 0;
		if (direction == 0)
			return;

		UpdateHistory(_entryDirections, direction, EntrySignalShift + 3);

		if (HandleStops(candle))
			return;

		var current = GetDirection(_entryDirections, EntrySignalShift);
		var previous = GetDirection(_entryDirections, EntrySignalShift + 1);

		if (current is null || previous is null)
			return;

		var flipToUp = IsFlipTo(1, current, previous);
		var flipToDown = IsFlipTo(-1, current, previous);

		var closeLong = Position > 0 && ((CloseBuyOnTrendFlip && _trendDirection < 0) || (CloseBuyOnEntryFlip && current < 0));
		var closeShort = Position < 0 && ((CloseSellOnTrendFlip && _trendDirection > 0) || (CloseSellOnEntryFlip && current > 0));

		if (closeLong)
		{
			SellMarket(Math.Abs(Position));
			ResetStops();
		}

		if (closeShort)
		{
			BuyMarket(Math.Abs(Position));
			ResetStops();
		}

		if (EnableBuyEntries && _trendDirection > 0 && flipToUp && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
			BuyMarket(volume);
			UpdateStops(candle.ClosePrice, true);
		}
		else if (EnableSellEntries && _trendDirection < 0 && flipToDown && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Math.Abs(Position) : 0m);
			SellMarket(volume);
			UpdateStops(candle.ClosePrice, false);
		}
	}

	private bool HandleStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopLossPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Math.Abs(Position));
				ResetStops();
				return true;
			}

			if (_takeProfitPrice is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Math.Abs(Position));
				ResetStops();
				return true;
			}
		}
		else if (Position < 0)
		{
			var absPos = Math.Abs(Position);

			if (_stopLossPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(absPos);
				ResetStops();
				return true;
			}

			if (_takeProfitPrice is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(absPos);
				ResetStops();
				return true;
			}
		}

		return false;
	}

	private void UpdateStops(decimal entryPrice, bool isLong)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			_stopLossPrice = null;
			_takeProfitPrice = null;
			return;
		}

		_stopLossPrice = StopLossPoints > 0m
			? isLong ? entryPrice - StopLossPoints * step : entryPrice + StopLossPoints * step
			: null;

		_takeProfitPrice = TakeProfitPoints > 0m
			? isLong ? entryPrice + TakeProfitPoints * step : entryPrice - TakeProfitPoints * step
			: null;
	}

	private void ResetStops()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private static void UpdateHistory(List<int> history, int direction, int maxLength)
	{
		history.Insert(0, direction);

		if (history.Count > maxLength)
		{
			history.RemoveRange(maxLength, history.Count - maxLength);
		}
	}

	private static int? GetDirection(List<int> history, int offset)
	{
		return history.Count > offset ? history[offset] : null;
	}

	private bool IsFlipTo(int targetDirection, int? current, int? previous)
	{
		if (current != targetDirection)
			return false;

		return EntryMode switch
		{
			KolierTrendMode.NewWay => previous != targetDirection,
			_ => previous == -targetDirection,
		};
	}

	/// <summary>
	/// Enumeration mirroring the original MQL SuperTrend modes.
	/// </summary>
	public enum KolierTrendMode
	{
		/// <summary>
		/// Classic SuperTrend confirmation requiring consecutive candles.
		/// </summary>
		SuperTrend,

		/// <summary>
		/// Faster confirmation that reacts after a single candle flip.
		/// </summary>
		NewWay,

		/// <summary>
		/// Visual mode (treated like classic confirmation in this port).
		/// </summary>
		Visual,

		/// <summary>
		/// Expert signal mode (treated like classic confirmation in this port).
		/// </summary>
		ExpertSignal,
	}
}
