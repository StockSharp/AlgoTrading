using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 5 expert advisor "FitFul 13".
/// Generates entries around weekly pivot levels confirmed by lower timeframe candles and applies a trailing stop.
/// </summary>
public class FitFul13Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _confirmationCandleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _indentPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private CandleSnapshot? _previousPrimary;
	private CandleSnapshot? _confirmationLast1;
	private CandleSnapshot? _confirmationLast2;
	private CandleSnapshot? _confirmationLast3;
	private CandleSnapshot? _weeklyLast;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="FitFul13Strategy"/> class.
	/// </summary>
	public FitFul13Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Trading timeframe", "Main timeframe used for pivot evaluation.", "General");

		_confirmationCandleType = Param(nameof(ConfirmationCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Confirmation timeframe", "Lower timeframe confirming pivot reactions.", "General");

		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Order volume", "Net volume used for each market order.", "Trading")
			.SetGreaterThanZero();

		_maxPositions = Param(nameof(MaxPositions), 3)
			.SetDisplay("Max positions", "Maximum net exposure measured in Volume multiples.", "Trading")
			.SetGreaterThanZero();

		_indentPips = Param(nameof(IndentPips), 3m)
			.SetDisplay("Indent (pips)", "Offset applied to pivot levels when computing stops and targets.", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 150m)
			.SetDisplay("Trailing stop (pips)", "Distance between price and trailing stop.", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing step (pips)", "Minimum price progress before tightening the trailing stop.", "Risk")
			.SetGreaterOrEqualZero();
	}

	/// <summary>
	/// Main trading timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lower timeframe used to confirm pivot reactions.
	/// </summary>
	public DataType ConfirmationCandleType
	{
		get => _confirmationCandleType.Value;
		set => _confirmationCandleType.Value = value;
	}

	/// <summary>
	/// Net market order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Maximum net exposure expressed in <see cref="Volume"/> multiples.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Offset in pips applied to pivot-based stop and take levels.
	/// </summary>
	public decimal IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price progress in pips required before tightening the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, CandleType),
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

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(ProcessPrimaryCandle).Start();

		var confirmationSubscription = SubscribeCandles(ConfirmationCandleType);
		confirmationSubscription.Bind(ProcessConfirmationCandle).Start();

		var weeklySubscription = SubscribeCandles(TimeSpan.FromDays(7).TimeFrame());
		weeklySubscription.Bind(ProcessWeeklyCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageActivePosition(candle);

		if (_pipSize <= 0m)
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

		var weekly = _weeklyLast.Value;
		var levels = PivotLevels.FromWeekly(weekly, IndentPips * _pipSize);

		var volume = Volume;
		if (volume <= 0m || MaxPositions <= 0)
			return;

		var limitVolume = volume * MaxPositions;

		if (Position < limitVolume)
		{
			var longSignal = TryBuildLongSignal(_previousPrimary.Value, previous.Value, levels);
			if (longSignal is SignalParameters buyParameters)
			{
				TryEnterLong(buyParameters);
				return;
			}
		}

		if (-Position < limitVolume)
		{
			var shortSignal = TryBuildShortSignal(_previousPrimary.Value, previous.Value, levels);
			if (shortSignal is SignalParameters sellParameters)
				TryEnterShort(sellParameters);
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
		}
	}

	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m)
			return;

		if (PositionPrice is not decimal entry || Position <= 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var profit = candle.ClosePrice - entry;

		if (profit <= trailingDistance + trailingStep)
			return;

		var candidate = candle.ClosePrice - trailingDistance;

		if (_longStopPrice is decimal current && candidate <= current + trailingStep)
			return;

		_longStopPrice = candidate;
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m)
			return;

		if (PositionPrice is not decimal entry || Position >= 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var profit = entry - candle.ClosePrice;

		if (profit <= trailingDistance + trailingStep)
			return;

		var candidate = candle.ClosePrice + trailingDistance;

		if (_shortStopPrice is decimal current && candidate >= current - trailingStep)
			return;

		_shortStopPrice = candidate;
	}

	private void TryEnterLong(SignalParameters parameters)
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
	}

	private void TryEnterShort(SignalParameters parameters)
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

	private void ResetTargets()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private decimal CalculatePipSize()
	{
		if (Security is null)
			return 0.0001m;

		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals ?? GetDecimalsFromStep(step);
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
		return step * factor;
	}

	private static int GetDecimalsFromStep(decimal step)
	{
		if (step <= 0m)
			return 0;

		var value = Math.Abs(Math.Log10((double)step));
		return (int)Math.Round(value);
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
