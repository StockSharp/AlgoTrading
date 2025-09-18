using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FitFul 13 strategy with explicit time filters and trailing management converted from MetaTrader 4.
/// Trades around weekly pivot levels using a primary and a confirmation timeframe while respecting intraday windows.
/// </summary>
public class FitFul13TimeGatedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _confirmationCandleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _maxNetPositions;
	private readonly StrategyParam<decimal> _offsetPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<TimeSpan> _closeAfter;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _closeMinuteFrom;
	private readonly StrategyParam<int> _closeMinuteTo;
	private readonly StrategyParam<int> _entryMinute0;
	private readonly StrategyParam<int> _entryMinute1;
	private readonly StrategyParam<int> _entryMinute2;
	private readonly StrategyParam<int> _entryMinute3;

	private CandleSnapshot? _previousPrimary;
	private CandleSnapshot? _confirmationLast1;
	private CandleSnapshot? _confirmationLast2;
	private CandleSnapshot? _confirmationLast3;
	private CandleSnapshot? _weeklyLast;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;

	private decimal _pointValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="FitFul13TimeGatedStrategy"/> class.
	/// </summary>
	public FitFul13TimeGatedStrategy()
	{
		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Primary timeframe", "Main timeframe used for pivot comparison.", "General");

		_confirmationCandleType = Param(nameof(ConfirmationCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Confirmation timeframe", "Lower timeframe confirming level reactions.", "General");

		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Net volume for market entries.", "Trading")
			.SetGreaterThanZero();

		_maxNetPositions = Param(nameof(MaxNetPositions), 2)
			.SetDisplay("Max net positions", "Maximum number of net position multiples allowed.", "Trading")
			.SetGreaterThanZero();

		_offsetPoints = Param(nameof(OffsetPoints), 15.5m)
			.SetDisplay("Offset (points)", "Distance from pivot levels used for stops and targets.", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
			.SetDisplay("Trailing stop (points)", "Trailing stop distance expressed in price points.", "Risk")
			.SetGreaterOrEqualZero();

		_closeAfter = Param(nameof(CloseAfter), TimeSpan.FromHours(48))
			.SetDisplay("Close after", "Maximum holding duration for profitable trades.", "Risk");

		_closeHour = Param(nameof(CloseHour), 21)
			.SetDisplay("Friday close hour", "Hour of day to force closing on Friday.", "Time filters");

		_closeMinuteFrom = Param(nameof(CloseMinuteFrom), 50)
			.SetDisplay("Friday close from", "Start minute for the Friday close window.", "Time filters")
			.SetRange(0, 59);

		_closeMinuteTo = Param(nameof(CloseMinuteTo), 59)
			.SetDisplay("Friday close to", "End minute for the Friday close window.", "Time filters")
			.SetRange(0, 59);

		_entryMinute0 = Param(nameof(EntryMinute0), 0)
			.SetDisplay("Entry minute #1", "First minute of the hour eligible for new positions.", "Time filters")
			.SetRange(0, 59);

		_entryMinute1 = Param(nameof(EntryMinute1), 15)
			.SetDisplay("Entry minute #2", "Second minute of the hour eligible for new positions.", "Time filters")
			.SetRange(0, 59);

		_entryMinute2 = Param(nameof(EntryMinute2), 30)
			.SetDisplay("Entry minute #3", "Third minute of the hour eligible for new positions.", "Time filters")
			.SetRange(0, 59);

		_entryMinute3 = Param(nameof(EntryMinute3), 45)
			.SetDisplay("Entry minute #4", "Fourth minute of the hour eligible for new positions.", "Time filters")
			.SetRange(0, 59);
	}

	/// <summary>
	/// Primary timeframe used for pivot analysis.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Lower timeframe confirming the pivot interaction.
	/// </summary>
	public DataType ConfirmationCandleType
	{
		get => _confirmationCandleType.Value;
		set => _confirmationCandleType.Value = value;
	}

	/// <summary>
	/// Net volume used for each market order.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Maximum amount of net exposure measured in <see cref="Volume"/> multiples.
	/// </summary>
	public int MaxNetPositions
	{
		get => _maxNetPositions.Value;
		set => _maxNetPositions.Value = value;
	}

	/// <summary>
	/// Offset in price points applied around pivot levels.
	/// </summary>
	public decimal OffsetPoints
	{
		get => _offsetPoints.Value;
		set => _offsetPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Maximum holding duration for profitable positions.
	/// </summary>
	public TimeSpan CloseAfter
	{
		get => _closeAfter.Value;
		set => _closeAfter.Value = value;
	}

	/// <summary>
	/// Friday hour when the strategy forces closing positions.
	/// </summary>
	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <summary>
	/// Start minute for the Friday close window.
	/// </summary>
	public int CloseMinuteFrom
	{
		get => _closeMinuteFrom.Value;
		set => _closeMinuteFrom.Value = value;
	}

	/// <summary>
	/// End minute for the Friday close window.
	/// </summary>
	public int CloseMinuteTo
	{
		get => _closeMinuteTo.Value;
		set => _closeMinuteTo.Value = value;
	}

	/// <summary>
	/// First allowed minute of the hour for entries.
	/// </summary>
	public int EntryMinute0
	{
		get => _entryMinute0.Value;
		set => _entryMinute0.Value = value;
	}

	/// <summary>
	/// Second allowed minute of the hour for entries.
	/// </summary>
	public int EntryMinute1
	{
		get => _entryMinute1.Value;
		set => _entryMinute1.Value = value;
	}

	/// <summary>
	/// Third allowed minute of the hour for entries.
	/// </summary>
	public int EntryMinute2
	{
		get => _entryMinute2.Value;
		set => _entryMinute2.Value = value;
	}

	/// <summary>
	/// Fourth allowed minute of the hour for entries.
	/// </summary>
	public int EntryMinute3
	{
		get => _entryMinute3.Value;
		set => _entryMinute3.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, PrimaryCandleType),
			(Security, ConfirmationCandleType),
			(Security, TimeSpan.FromDays(7).TimeFrame())
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousPrimary = null;
		_confirmationLast1 = null;
		_confirmationLast2 = null;
		_confirmationLast3 = null;
		_weeklyLast = null;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		_longEntryTime = null;
		_shortEntryTime = null;

		_pointValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 0.0001m;

		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		primarySubscription.Bind(ProcessPrimaryCandle).Start();

		var confirmationSubscription = SubscribeCandles(ConfirmationCandleType);
		confirmationSubscription.Bind(ProcessConfirmationCandle).Start();

		var weeklySubscription = SubscribeCandles(TimeSpan.FromDays(7).TimeFrame());
		weeklySubscription.Bind(ProcessWeeklyCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageActivePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pointValue <= 0m)
			return;

		if (_weeklyLast is null || _confirmationLast1 is null || _confirmationLast2 is null || _confirmationLast3 is null)
		{
			_previousPrimary = CandleSnapshot.From(candle);
			return;
		}

		var previous = _previousPrimary;
		_previousPrimary = CandleSnapshot.From(candle);

		if (previous is null)
			return;

		if (!IsEntryMinuteAllowed(candle.OpenTime))
			return;

		var limitVolume = Volume * MaxNetPositions;
		if (limitVolume <= 0m)
			return;

		var indent = OffsetPoints * _pointValue;
		var levels = PivotLevels.FromWeekly(_weeklyLast.Value, indent);

		if (Position < limitVolume)
		{
			var longSignal = TryBuildLongSignal(_previousPrimary.Value, previous.Value, levels);
			if (longSignal is SignalParameters buyParameters)
			{
				TryEnterLong(buyParameters, candle);
				return;
			}
		}

		if (-Position < limitVolume)
		{
			var shortSignal = TryBuildShortSignal(_previousPrimary.Value, previous.Value, levels);
			if (shortSignal is SignalParameters sellParameters)
				TryEnterShort(sellParameters, candle);
		}
	}

	private void ProcessConfirmationCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_confirmationLast3 = _confirmationLast2;
		_confirmationLast2 = _confirmationLast1;
		_confirmationLast1 = CandleSnapshot.From(candle);
	}

	private void ProcessWeeklyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_weeklyLast = CandleSnapshot.From(candle);
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetTargets();
				return;
			}

			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetTargets();
				return;
			}

			ApplyTrailingForLong(candle);
			if (CheckLongTimeExit(candle))
				return;
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
				return;
			}

			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
				return;
			}

			ApplyTrailingForShort(candle);
			if (CheckShortTimeExit(candle))
				return;
		}
	}

	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m)
			return;

		if (PositionPrice is not decimal entryPrice)
			return;

		var distance = TrailingStopPoints * _pointValue;
		if (distance <= 0m)
			return;

		if (candle.ClosePrice - entryPrice < distance)
			return;

		var candidate = candle.ClosePrice - distance;
		if (_longStopPrice is decimal current && candidate <= current)
			return;

		_longStopPrice = candidate;
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m)
			return;

		if (PositionPrice is not decimal entryPrice)
			return;

		var distance = TrailingStopPoints * _pointValue;
		if (distance <= 0m)
			return;

		if (entryPrice - candle.ClosePrice < distance)
			return;

		var candidate = candle.ClosePrice + distance;
		if (_shortStopPrice is decimal current && candidate >= current)
			return;

		_shortStopPrice = candidate;
	}

	private bool CheckLongTimeExit(ICandleMessage candle)
	{
		if (_longEntryTime is not DateTimeOffset entry)
			return false;

		var now = candle.CloseTime;
		if (CloseAfter > TimeSpan.Zero && now - entry >= CloseAfter && PositionPrice is decimal price)
		{
			var profit = candle.ClosePrice - price;
			if (profit >= 0m)
			{
				SellMarket(Position);
				ResetTargets();
				return true;
			}
		}

		if (now.DayOfWeek == DayOfWeek.Friday && now.Hour == CloseHour)
		{
			if (now.Minute >= CloseMinuteFrom && now.Minute <= CloseMinuteTo)
			{
				SellMarket(Position);
				ResetTargets();
				return true;
			}
		}

		return false;
	}

	private bool CheckShortTimeExit(ICandleMessage candle)
	{
		if (_shortEntryTime is not DateTimeOffset entry)
			return false;

		var now = candle.CloseTime;
		if (CloseAfter > TimeSpan.Zero && now - entry >= CloseAfter && PositionPrice is decimal price)
		{
			var profit = price - candle.ClosePrice;
			if (profit >= 0m)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
				return true;
			}
		}

		if (now.DayOfWeek == DayOfWeek.Friday && now.Hour == CloseHour)
		{
			if (now.Minute >= CloseMinuteFrom && now.Minute <= CloseMinuteTo)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
				return true;
			}
		}

		return false;
	}

	private void TryEnterLong(SignalParameters parameters, ICandleMessage candle)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		var orderVolume = volume;
		if (Position < 0m)
			orderVolume += Math.Abs(Position);

		BuyMarket(orderVolume);

		_longStopPrice = parameters.StopPrice;
		_longTakePrice = parameters.TakePrice;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longEntryTime = candle.CloseTime;
		_shortEntryTime = null;
	}

	private void TryEnterShort(SignalParameters parameters, ICandleMessage candle)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		var orderVolume = volume;
		if (Position > 0m)
			orderVolume += Position;

		SellMarket(orderVolume);

		_shortStopPrice = parameters.StopPrice;
		_shortTakePrice = parameters.TakePrice;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortEntryTime = candle.CloseTime;
		_longEntryTime = null;
	}

	private SignalParameters? TryBuildLongSignal(CandleSnapshot lastBar, CandleSnapshot olderBar, PivotLevels levels)
	{
		if (lastBar.IsBullish)
		{
			if (BodyCrossesUp(olderBar, levels.PriceTypical))
				return new SignalParameters(levels.S1BelowPivot, levels.R1AbovePivot);
			if (BodyCrossesUp(olderBar, levels.R05))
				return new SignalParameters(levels.S05, levels.R15);
			if (BodyCrossesUp(olderBar, levels.R1))
				return new SignalParameters(levels.PriceTypicalMinusIndent, levels.R2);
			if (BodyCrossesUp(olderBar, levels.R15))
				return new SignalParameters(levels.R05, levels.R25);
			if (BodyCrossesUp(olderBar, levels.R2))
				return new SignalParameters(levels.R1, levels.R3);
			if (BodyCrossesUp(olderBar, levels.R25))
				return new SignalParameters(levels.R15, levels.R3);
			if (BodyCrossesUp(olderBar, levels.S1))
				return new SignalParameters(levels.S2, levels.PriceTypicalPlusIndent);
			if (BodyCrossesUp(olderBar, levels.S05))
				return new SignalParameters(levels.S15, levels.R05);
		}

		var confirm = _confirmationLast1;
		if (confirm is null || _confirmationLast2 is null || _confirmationLast3 is null)
			return null;

		if (confirm.Value.IsBullish)
		{
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.PriceTypical))
				return new SignalParameters(levels.S1BelowPivot, levels.R1AbovePivot);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R05))
				return new SignalParameters(levels.S05, levels.R15);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R1))
				return new SignalParameters(levels.PriceTypicalMinusIndent, levels.R2);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R15))
				return new SignalParameters(levels.R05, levels.R25);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R2))
				return new SignalParameters(levels.R1, levels.R3);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R25))
				return new SignalParameters(levels.R15, levels.R3);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.S1))
				return new SignalParameters(levels.S2, levels.PriceTypicalPlusIndent);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.S05))
				return new SignalParameters(levels.S15, levels.R05);
		}

		if (confirm.Value.IsBearish)
		{
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.PriceTypical))
				return new SignalParameters(levels.R1AbovePivot, levels.S1BelowPivot);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S05))
				return new SignalParameters(levels.R05, levels.S15);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S1))
				return new SignalParameters(levels.PriceTypicalPlusIndent, levels.S2);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S15))
				return new SignalParameters(levels.S05, levels.S25);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S2))
				return new SignalParameters(levels.S1, levels.S3);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S25))
				return new SignalParameters(levels.S15, levels.S3);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.R1))
				return new SignalParameters(levels.R2, levels.PriceTypicalMinusIndent);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.R05))
				return new SignalParameters(levels.R15, levels.S05);
		}

		return null;
	}

	private SignalParameters? TryBuildShortSignal(CandleSnapshot lastBar, CandleSnapshot olderBar, PivotLevels levels)
	{
		if (lastBar.IsBearish)
		{
			if (BodyCrossesDown(olderBar, levels.PriceTypical))
				return new SignalParameters(levels.R1AbovePivot, levels.S1BelowPivot);
			if (BodyCrossesDown(olderBar, levels.S05))
				return new SignalParameters(levels.R05, levels.S15);
			if (BodyCrossesDown(olderBar, levels.S1))
				return new SignalParameters(levels.PriceTypicalPlusIndent, levels.S2);
			if (BodyCrossesDown(olderBar, levels.S15))
				return new SignalParameters(levels.S05, levels.S25);
			if (BodyCrossesDown(olderBar, levels.S2))
				return new SignalParameters(levels.S1, levels.S3);
			if (BodyCrossesDown(olderBar, levels.S25))
				return new SignalParameters(levels.S15, levels.S3);
			if (BodyCrossesDown(olderBar, levels.R1))
				return new SignalParameters(levels.R2, levels.PriceTypicalMinusIndent);
			if (BodyCrossesDown(olderBar, levels.R05))
				return new SignalParameters(levels.R15, levels.S05);
		}

		var confirm = _confirmationLast1;
		if (confirm is null || _confirmationLast2 is null || _confirmationLast3 is null)
			return null;

		if (confirm.Value.IsBullish)
		{
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.PriceTypical))
				return new SignalParameters(levels.S1BelowPivot, levels.R1AbovePivot);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R05))
				return new SignalParameters(levels.S05, levels.R15);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R1))
				return new SignalParameters(levels.PriceTypicalMinusIndent, levels.R2);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R15))
				return new SignalParameters(levels.R05, levels.R25);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R2))
				return new SignalParameters(levels.R1, levels.R3);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.R25))
				return new SignalParameters(levels.R15, levels.R3);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.S1))
				return new SignalParameters(levels.S2, levels.PriceTypicalPlusIndent);
			if (LowsCrossUp(_confirmationLast3.Value, _confirmationLast2.Value, levels.S05))
				return new SignalParameters(levels.S15, levels.R05);
		}

		if (confirm.Value.IsBearish)
		{
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.PriceTypical))
				return new SignalParameters(levels.R1AbovePivot, levels.S1BelowPivot);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S05))
				return new SignalParameters(levels.R05, levels.S15);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S1))
				return new SignalParameters(levels.PriceTypicalPlusIndent, levels.S2);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S15))
				return new SignalParameters(levels.S05, levels.S25);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S2))
				return new SignalParameters(levels.S1, levels.S3);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.S25))
				return new SignalParameters(levels.S15, levels.S3);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.R1))
				return new SignalParameters(levels.R2, levels.PriceTypicalMinusIndent);
			if (HighsCrossDown(_confirmationLast3.Value, _confirmationLast2.Value, levels.R05))
				return new SignalParameters(levels.R15, levels.S05);
		}

		return null;
	}

	private static bool BodyCrossesUp(CandleSnapshot bar, decimal level)
	{
		return bar.Open <= level && bar.Close >= level;
	}

	private static bool BodyCrossesDown(CandleSnapshot bar, decimal level)
	{
		return bar.Open >= level && bar.Close <= level;
	}

	private static bool LowsCrossUp(CandleSnapshot older, CandleSnapshot newer, decimal level)
	{
		return older.Low <= level && older.Close >= level && newer.Low <= level && newer.Close >= level;
	}

	private static bool HighsCrossDown(CandleSnapshot older, CandleSnapshot newer, decimal level)
	{
		return older.High >= level && older.Close <= level && newer.High >= level && newer.Close <= level;
	}

	private bool IsEntryMinuteAllowed(DateTimeOffset time)
	{
		var minute = time.Minute;
		return minute == EntryMinute0 || minute == EntryMinute1 || minute == EntryMinute2 || minute == EntryMinute3;
	}

	private void ResetTargets()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longEntryTime = null;
		_shortEntryTime = null;
	}

	private readonly record struct CandleSnapshot(decimal Open, decimal High, decimal Low, decimal Close)
	{
		public static CandleSnapshot From(ICandleMessage candle)
		{
			return new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		}

		public bool IsBullish => Close >= Open;
		public bool IsBearish => Close <= Open;
	}

	private readonly record struct PivotLevels(decimal PriceTypical, decimal R05, decimal R1, decimal R15, decimal R2, decimal R25, decimal R3, decimal S05, decimal S1, decimal S15, decimal S2, decimal S25, decimal S3, decimal PriceTypicalMinusIndent, decimal PriceTypicalPlusIndent, decimal R1AbovePivot, decimal S1BelowPivot)
	{
		public static PivotLevels FromWeekly(CandleSnapshot weekly, decimal indent)
		{
			var priceTypical = (weekly.High + weekly.Low + weekly.Close) / 3m;
			var r1 = 2m * priceTypical - weekly.Low;
			var s1 = 2m * priceTypical - weekly.High;
			var r05 = (priceTypical + r1) / 2m;
			var s05 = (priceTypical + s1) / 2m;
			var range = weekly.High - weekly.Low;
			var r2 = priceTypical + range;
			var s2 = priceTypical - range;
			var r15 = (r1 + r2) / 2m;
			var s15 = (s1 + s2) / 2m;
			var r3 = 2m * priceTypical + (weekly.High - 2m * weekly.Low);
			var s3 = 2m * priceTypical - (2m * weekly.High - weekly.Low);
			var r25 = (r2 + r3) / 2m;
			var s25 = (s2 + s3) / 2m;

			return new PivotLevels(
				priceTypical,
				r05,
				r1,
				r15,
				r2,
				r25,
				r3,
				s05,
				s1,
				s15,
				s2,
				s25,
				s3,
				priceTypical - indent,
				priceTypical + indent,
				r1 + indent,
				s1 - indent
			);
		}

		public decimal PriceTypical => Item1;
		public decimal R05 => Item2;
		public decimal R1 => Item3;
		public decimal R15 => Item4;
		public decimal R2 => Item5;
		public decimal R25 => Item6;
		public decimal R3 => Item7;
		public decimal S05 => Item8;
		public decimal S1 => Item9;
		public decimal S15 => Item10;
		public decimal S2 => Item11;
		public decimal S25 => Item12;
		public decimal S3 => Item13;
		public decimal PriceTypicalMinusIndent => Item14;
		public decimal PriceTypicalPlusIndent => Item15;
		public decimal R1AbovePivot => Item16;
		public decimal S1BelowPivot => Item17;
	}

	private readonly record struct SignalParameters(decimal StopPrice, decimal TakePrice);
}
