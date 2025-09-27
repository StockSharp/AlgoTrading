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
/// Price action strategy that trades configurable candlestick patterns converted from the MQL5 Patterns EA.
/// </summary>
public class PatternsEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<OpenedModes> _openedMode;
	private readonly StrategyParam<decimal> _equalityPips;
	private readonly StrategyParam<bool> _enableGroup1;
	private readonly StrategyParam<bool> _enableGroup2;
	private readonly StrategyParam<bool> _enableGroup3;
	private readonly PatternDefinition[] _patternDefinitions;
	private readonly StrategyParam<bool>[] _patternEnabled;
	private readonly StrategyParam<Sides>[] _patternSides;

	private CandleInfo? _current;
	private CandleInfo? _previous;
	private CandleInfo? _previous2;

	public PatternsEaStrategy()
	{
		Volume = 1;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for pattern search", "General");

		_openedMode = Param(nameof(Mode), OpenedModes.Any)
			.SetDisplay("Opened Mode", "Position handling mode", "Trading");

		_equalityPips = Param(nameof(EqualityPips), 1m)
			.SetNotNegative()
			.SetDisplay("Equality Pips", "Maximum pip distance to treat prices as equal", "Detection");

		_enableGroup1 = Param(nameof(EnableOneBarPatterns), true)
			.SetDisplay("Enable One-Bar Patterns", "Toggle detection of one-bar formations", "Groups");

		_enableGroup2 = Param(nameof(EnableTwoBarPatterns), true)
			.SetDisplay("Enable Two-Bar Patterns", "Toggle detection of two-bar formations", "Groups");

		_enableGroup3 = Param(nameof(EnableThreeBarPatterns), true)
			.SetDisplay("Enable Three-Bar Patterns", "Toggle detection of three-bar formations", "Groups");

		_patternDefinitions = new[]
		{
			new PatternDefinition(PatternTypes.DoubleInside, "Double Inside", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.Inside, "Inside Bar", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.Outside, "Outside Bar", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.PinUp, "Pin Up", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.PinDown, "Pin Down", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.PivotPointReversalUp, "Pivot Point Reversal Up", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.PivotPointReversalDown, "Pivot Point Reversal Down", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.DoubleBarLowHigherClose, "Double Bar Low With A Higher Close", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.DoubleBarHighLowerClose, "Double Bar High With A Lower Close", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.ClosePriceReversalUp, "Close Price Reversal Up", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.ClosePriceReversalDown, "Close Price Reversal Down", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.NeutralBar, "Neutral Bar", PatternGroups.OneBar),
			new PatternDefinition(PatternTypes.ForceBarUp, "Force Bar Up", PatternGroups.OneBar),
			new PatternDefinition(PatternTypes.ForceBarDown, "Force Bar Down", PatternGroups.OneBar),
			new PatternDefinition(PatternTypes.MirrorBar, "Mirror Bar", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.Hammer, "Hammer", PatternGroups.OneBar),
			new PatternDefinition(PatternTypes.ShootingStar, "Shooting Star", PatternGroups.OneBar),
			new PatternDefinition(PatternTypes.EveningStar, "Evening Star", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.MorningStar, "Morning Star", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.BearishHarami, "Bearish Harami", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.BearishHaramiCross, "Bearish Harami Cross", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.BullishHarami, "Bullish Harami", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.BullishHaramiCross, "Bullish Harami Cross", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.DarkCloudCover, "Dark Cloud Cover", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.DojiStar, "Doji Star", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.EngulfingBearishLine, "Engulfing Bearish Line", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.EngulfingBullishLine, "Engulfing Bullish Line", PatternGroups.TwoBars),
			new PatternDefinition(PatternTypes.EveningDojiStar, "Evening Doji Star", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.MorningDojiStar, "Morning Doji Star", PatternGroups.ThreeBars),
			new PatternDefinition(PatternTypes.TwoNeutralBars, "Two Neutral Bars", PatternGroups.TwoBars),
		};

		_patternEnabled = new StrategyParam<bool>[_patternDefinitions.Length];
		_patternSides = new StrategyParam<Sides>[_patternDefinitions.Length];

		for (var i = 0; i < _patternDefinitions.Length; i++)
		{
			var definition = _patternDefinitions[i];
			var groupName = definition.Group switch
			{
				PatternGroups.OneBar => "Group 1 - One Bar",
				PatternGroups.TwoBars => "Group 2 - Two Bars",
				PatternGroups.ThreeBars => "Group 3 - Three Bars",
				_ => "Patterns",
			};

			_patternEnabled[i] = Param($"Enable{definition.Type}", true)
				.SetDisplay($"Enable {definition.DisplayName}", $"Use {definition.DisplayName} pattern", groupName);

			_patternSides[i] = Param($"Order{definition.Type}", Sides.Buy)
				.SetDisplay($"{definition.DisplayName} Order", "Market order direction for the pattern", groupName);
		}
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public OpenedModes Mode
	{
		get => _openedMode.Value;
		set => _openedMode.Value = value;
	}

	public decimal EqualityPips
	{
		get => _equalityPips.Value;
		set => _equalityPips.Value = value;
	}

	public bool EnableOneBarPatterns
	{
		get => _enableGroup1.Value;
		set => _enableGroup1.Value = value;
	}

	public bool EnableTwoBarPatterns
	{
		get => _enableGroup2.Value;
		set => _enableGroup2.Value = value;
	}

	public bool EnableThreeBarPatterns
	{
		get => _enableGroup3.Value;
		set => _enableGroup3.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_current = null;
		_previous = null;
		_previous2 = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previous2 = _previous;
		_previous = _current;
		_current = new CandleInfo(candle);

		EvaluatePatterns();
	}

	private void EvaluatePatterns()
	{
		if (_current is null)
			return;

		var point = Security?.PriceStep ?? 0.0001m;
		var equality = EqualityPips * point;
		var minDeviation = Math.Max(equality, point);
		var minDeviation4 = Math.Max(equality * 4m, point * 4m);

		var candle0 = _current.Value;
		var hasPrevious = _previous.HasValue;
		var hasPrevious2 = _previous2.HasValue;
		var candle1 = hasPrevious ? _previous.Value : default;
		var candle2 = hasPrevious2 ? _previous2.Value : default;

		// One-bar patterns require only the current bar.
		if (EnableOneBarPatterns)
		{
			if (Compare(candle0.Open, candle0.Close, equality) == 0 && candle0.UpperShadow > minDeviation4 && candle0.LowerShadow > minDeviation4)
				TriggerPattern(PatternTypes.NeutralBar);

			if (Compare(candle0.Close, candle0.High, equality) == 0)
				TriggerPattern(PatternTypes.ForceBarUp);

			if (Compare(candle0.Close, candle0.Low, equality) == 0)
				TriggerPattern(PatternTypes.ForceBarDown);

			if (candle0.UpperShadow <= minDeviation && candle0.LowerShadow > 2m * candle0.BodySize)
				TriggerPattern(PatternTypes.Hammer);

			if (candle0.LowerShadow <= minDeviation && candle0.UpperShadow > 2m * candle0.BodySize)
				TriggerPattern(PatternTypes.ShootingStar);
		}

		if (!hasPrevious)
			return;

		// Two-bar patterns evaluate the current bar together with the previous one.
		if (EnableTwoBarPatterns)
		{
			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0)
				TriggerPattern(PatternTypes.Inside);

			if (Compare(candle0.High, candle1.High, equality) > 0 && Compare(candle0.Low, candle1.Low, equality) < 0)
				TriggerPattern(PatternTypes.Outside);

			if (Compare(candle0.High, candle1.High, equality) == 0 && Compare(candle0.Close, candle1.Close, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) <= 0)
				TriggerPattern(PatternTypes.DoubleBarHighLowerClose);

			if (Compare(candle0.Low, candle1.Low, equality) == 0 && Compare(candle0.Close, candle1.Close, equality) > 0 && Compare(candle0.High, candle1.High, equality) >= 0)
				TriggerPattern(PatternTypes.DoubleBarLowHigherClose);

			if (Compare(candle1.BodySize, candle0.BodySize, equality) == 0 && Compare(candle1.Open, candle0.Close, equality) == 0)
				TriggerPattern(PatternTypes.MirrorBar);

			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle1.BodySize, candle0.BodySize, equality) > 0 && Compare(candle1.Close, candle0.Open, equality) > 0 && Compare(candle1.Open, candle0.Close, equality) < 0)
				TriggerPattern(PatternTypes.BearishHarami);

			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Open, candle0.Close, equality) == 0 && Compare(candle1.BodySize, candle0.BodySize, equality) > 0 && Compare(candle1.Close, candle0.Open, equality) > 0 && Compare(candle1.Open, candle0.Close, equality) < 0)
				TriggerPattern(PatternTypes.BearishHaramiCross);

			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) < 0 && Compare(candle0.Close, candle0.Open, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) > 0 && Compare(candle1.Close, candle0.Open, equality) < 0 && Compare(candle1.Open, candle0.Close, equality) > 0)
				TriggerPattern(PatternTypes.BullishHarami);

			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) < 0 && Compare(candle0.Open, candle0.Close, equality) == 0 && Compare(candle1.BodySize, candle0.BodySize, equality) > 0 && Compare(candle1.Close, candle0.Open, equality) < 0 && Compare(candle1.Open, candle0.Close, equality) > 0)
				TriggerPattern(PatternTypes.BullishHaramiCross);

			if (Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle1.High, candle0.Open, equality) < 0 && Compare(candle0.Close, candle1.Close, equality) < 0 && Compare(candle0.Close, candle1.Open, equality) > 0)
				TriggerPattern(PatternTypes.DarkCloudCover);

			if (Compare(candle0.Open, candle0.Close, equality) == 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Open, candle1.High, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) < 0 && Compare(candle0.Open, candle1.Low, equality) < 0)
				TriggerPattern(PatternTypes.DojiStar);

			if (Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle0.Open, candle1.Close, equality) > 0 && Compare(candle0.Close, candle1.Open, equality) < 0)
				TriggerPattern(PatternTypes.EngulfingBearishLine);

			if (Compare(candle1.Close, candle1.Open, equality) < 0 && Compare(candle0.Close, candle0.Open, equality) > 0 && Compare(candle0.Open, candle1.Close, equality) < 0 && Compare(candle0.Close, candle1.Open, equality) > 0)
				TriggerPattern(PatternTypes.EngulfingBullishLine);

			if (Compare(candle0.Open, candle0.Close, equality) == 0 && candle0.UpperShadow > minDeviation4 && candle0.LowerShadow > minDeviation4 && Compare(candle1.Open, candle1.Close, equality) == 0 && candle1.UpperShadow > minDeviation4 && candle1.LowerShadow > minDeviation4)
				TriggerPattern(PatternTypes.TwoNeutralBars);
		}

		if (!hasPrevious2)
			return;

		// Three-bar patterns combine the last three candles.
		if (EnableThreeBarPatterns)
		{
			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.High, candle2.High, equality) < 0 && Compare(candle1.Low, candle2.Low, equality) > 0)
				TriggerPattern(PatternTypes.DoubleInside);

			if (Compare(candle1.High, candle2.High, equality) > 0 && Compare(candle1.High, candle0.High, equality) > 0 && Compare(candle1.Low, candle2.Low, equality) > 0 && Compare(candle1.Low, candle0.Low, equality) > 0 && candle1.BodySize * 2m < candle1.UpperShadow)
				TriggerPattern(PatternTypes.PinUp);

			if (Compare(candle1.High, candle2.High, equality) < 0 && Compare(candle1.High, candle0.High, equality) < 0 && Compare(candle1.Low, candle2.Low, equality) < 0 && Compare(candle1.Low, candle0.Low, equality) < 0 && candle1.BodySize * 2m < candle1.LowerShadow)
				TriggerPattern(PatternTypes.PinDown);

			if (Compare(candle1.High, candle2.High, equality) > 0 && Compare(candle1.High, candle0.High, equality) > 0 && Compare(candle1.Low, candle2.Low, equality) > 0 && Compare(candle1.Low, candle0.Low, equality) > 0 && Compare(candle0.Close, candle1.Low, equality) < 0)
				TriggerPattern(PatternTypes.PivotPointReversalDown);

			if (Compare(candle1.High, candle2.High, equality) < 0 && Compare(candle1.High, candle0.High, equality) < 0 && Compare(candle1.Low, candle2.Low, equality) < 0 && Compare(candle1.Low, candle0.Low, equality) < 0 && Compare(candle0.Close, candle1.High, equality) > 0)
				TriggerPattern(PatternTypes.PivotPointReversalUp);

			if (Compare(candle1.High, candle2.High, equality) < 0 && Compare(candle1.Low, candle2.Low, equality) < 0 && Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) < 0 && Compare(candle0.Close, candle1.Close, equality) > 0 && Compare(candle0.Open, candle0.Close, equality) < 0)
				TriggerPattern(PatternTypes.ClosePriceReversalUp);

			if (Compare(candle1.High, candle2.High, equality) > 0 && Compare(candle1.Low, candle2.Low, equality) > 0 && Compare(candle0.High, candle1.High, equality) > 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle0.Close, candle1.Close, equality) < 0 && Compare(candle0.Open, candle0.Close, equality) > 0)
				TriggerPattern(PatternTypes.ClosePriceReversalDown);

			if (Compare(candle2.Close, candle2.Open, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle2.Close, candle1.Open, equality) < 0 && Compare(candle2.BodySize, candle1.BodySize, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) < 0 && Compare(candle0.Close, candle2.Open, equality) > 0 && Compare(candle0.Close, candle2.Close, equality) < 0)
				TriggerPattern(PatternTypes.EveningStar);

			if (Compare(candle2.Close, candle2.Open, equality) < 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) > 0 && Compare(candle2.Close, candle1.Open, equality) > 0 && Compare(candle2.BodySize, candle1.BodySize, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) < 0 && Compare(candle0.Close, candle2.Close, equality) > 0 && Compare(candle0.Close, candle2.Open, equality) < 0)
				TriggerPattern(PatternTypes.MorningStar);

			if (Compare(candle2.Close, candle2.Open, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) == 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle2.Close, candle1.Open, equality) < 0 && Compare(candle2.BodySize, candle1.BodySize, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) < 0 && Compare(candle0.Close, candle2.Open, equality) > 0 && Compare(candle0.Close, candle2.Close, equality) < 0)
				TriggerPattern(PatternTypes.EveningDojiStar);

			if (Compare(candle2.Close, candle2.Open, equality) < 0 && Compare(candle1.Close, candle1.Open, equality) == 0 && Compare(candle0.Close, candle0.Open, equality) > 0 && Compare(candle2.Close, candle1.Open, equality) > 0 && Compare(candle2.BodySize, candle1.BodySize, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) < 0 && Compare(candle0.Close, candle2.Close, equality) > 0 && Compare(candle0.Close, candle2.Open, equality) < 0)
				TriggerPattern(PatternTypes.MorningDojiStar);
		}
	}

	private int Compare(decimal price1, decimal price2, decimal tolerance)
	{
		var diff = price1 - price2;

		if (Math.Abs(diff) < tolerance)
			return 0;

		return diff > 0 ? 1 : -1;
	}

	private bool IsGroupEnabled(PatternGroups group)
	{
		return group switch
		{
			PatternGroups.OneBar => EnableOneBarPatterns,
			PatternGroups.TwoBars => EnableTwoBarPatterns,
			PatternGroups.ThreeBars => EnableThreeBarPatterns,
			_ => true,
		};
	}

	private void TriggerPattern(PatternTypes type)
	{
		var index = (int)type;
		var definition = _patternDefinitions[index];

		if (!IsGroupEnabled(definition.Group))
			return;

		if (!_patternEnabled[index].Value)
			return;

		var side = _patternSides[index].Value;

		if (!CanExecute(side))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Mode == OpenedModes.Swing)
		{
			if (side == Sides.Buy && Position < 0)
				BuyMarket(Math.Abs(Position));
			else if (side == Sides.Sell && Position > 0)
				SellMarket(Math.Abs(Position));
		}

		if (side == Sides.Buy)
			BuyMarket();
		else
			SellMarket();
	}

	private bool CanExecute(Sides side)
	{
		return Mode switch
		{
			OpenedModes.Any => true,
			OpenedModes.Swing => true,
			OpenedModes.BuyOne => side == Sides.Buy && Position <= 0,
			OpenedModes.BuyMany => side == Sides.Buy,
			OpenedModes.SellOne => side == Sides.Sell && Position >= 0,
			OpenedModes.SellMany => side == Sides.Sell,
			_ => false,
		};
	}

	private enum PatternGroups
	{
		OneBar = 1,
		TwoBars = 2,
		ThreeBars = 3,
	}

	private enum PatternTypes
	{
		DoubleInside,
		Inside,
		Outside,
		PinUp,
		PinDown,
		PivotPointReversalUp,
		PivotPointReversalDown,
		DoubleBarLowHigherClose,
		DoubleBarHighLowerClose,
		ClosePriceReversalUp,
		ClosePriceReversalDown,
		NeutralBar,
		ForceBarUp,
		ForceBarDown,
		MirrorBar,
		Hammer,
		ShootingStar,
		EveningStar,
		MorningStar,
		BearishHarami,
		BearishHaramiCross,
		BullishHarami,
		BullishHaramiCross,
		DarkCloudCover,
		DojiStar,
		EngulfingBearishLine,
		EngulfingBullishLine,
		EveningDojiStar,
		MorningDojiStar,
		TwoNeutralBars,
	}

	public enum OpenedModes
	{
		Any,
		Swing,
		BuyOne,
		BuyMany,
		SellOne,
		SellMany,
	}

	private readonly struct PatternDefinition
	{
		public PatternDefinition(PatternTypes type, string displayName, PatternGroups group)
		{
			Type = type;
			DisplayName = displayName;
			Group = group;
		}

		public PatternTypes Type { get; }

		public string DisplayName { get; }

		public PatternGroups Group { get; }
	}

	private readonly struct CandleInfo
	{
		public CandleInfo(ICandleMessage candle)
		{
			Open = candle.OpenPrice;
			Close = candle.ClosePrice;
			High = candle.HighPrice;
			Low = candle.LowPrice;
			BodyTop = Math.Max(Open, Close);
			BodyBottom = Math.Min(Open, Close);
			BodySize = BodyTop - BodyBottom;
			UpperShadow = High - BodyTop;
			LowerShadow = BodyBottom - Low;
		}

		public decimal Open { get; }

		public decimal Close { get; }

		public decimal High { get; }

		public decimal Low { get; }

		public decimal BodyTop { get; }

		public decimal BodyBottom { get; }

		public decimal BodySize { get; }

		public decimal UpperShadow { get; }

		public decimal LowerShadow { get; }
	}
}