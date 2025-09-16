using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<OpenedMode> _openedMode;
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

		_openedMode = Param(nameof(Mode), OpenedMode.Any)
			.SetDisplay("Opened Mode", "Position handling mode", "Trading");

		_equalityPips = Param(nameof(EqualityPips), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Equality Pips", "Maximum pip distance to treat prices as equal", "Detection");

		_enableGroup1 = Param(nameof(EnableOneBarPatterns), true)
			.SetDisplay("Enable One-Bar Patterns", "Toggle detection of one-bar formations", "Groups");

		_enableGroup2 = Param(nameof(EnableTwoBarPatterns), true)
			.SetDisplay("Enable Two-Bar Patterns", "Toggle detection of two-bar formations", "Groups");

		_enableGroup3 = Param(nameof(EnableThreeBarPatterns), true)
			.SetDisplay("Enable Three-Bar Patterns", "Toggle detection of three-bar formations", "Groups");

		_patternDefinitions = new[]
		{
			new PatternDefinition(PatternType.DoubleInside, "Double Inside", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.Inside, "Inside Bar", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.Outside, "Outside Bar", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.PinUp, "Pin Up", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.PinDown, "Pin Down", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.PivotPointReversalUp, "Pivot Point Reversal Up", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.PivotPointReversalDown, "Pivot Point Reversal Down", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.DoubleBarLowHigherClose, "Double Bar Low With A Higher Close", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.DoubleBarHighLowerClose, "Double Bar High With A Lower Close", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.ClosePriceReversalUp, "Close Price Reversal Up", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.ClosePriceReversalDown, "Close Price Reversal Down", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.NeutralBar, "Neutral Bar", PatternGroup.OneBar),
			new PatternDefinition(PatternType.ForceBarUp, "Force Bar Up", PatternGroup.OneBar),
			new PatternDefinition(PatternType.ForceBarDown, "Force Bar Down", PatternGroup.OneBar),
			new PatternDefinition(PatternType.MirrorBar, "Mirror Bar", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.Hammer, "Hammer", PatternGroup.OneBar),
			new PatternDefinition(PatternType.ShootingStar, "Shooting Star", PatternGroup.OneBar),
			new PatternDefinition(PatternType.EveningStar, "Evening Star", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.MorningStar, "Morning Star", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.BearishHarami, "Bearish Harami", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.BearishHaramiCross, "Bearish Harami Cross", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.BullishHarami, "Bullish Harami", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.BullishHaramiCross, "Bullish Harami Cross", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.DarkCloudCover, "Dark Cloud Cover", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.DojiStar, "Doji Star", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.EngulfingBearishLine, "Engulfing Bearish Line", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.EngulfingBullishLine, "Engulfing Bullish Line", PatternGroup.TwoBars),
			new PatternDefinition(PatternType.EveningDojiStar, "Evening Doji Star", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.MorningDojiStar, "Morning Doji Star", PatternGroup.ThreeBars),
			new PatternDefinition(PatternType.TwoNeutralBars, "Two Neutral Bars", PatternGroup.TwoBars),
		};

		_patternEnabled = new StrategyParam<bool>[_patternDefinitions.Length];
		_patternSides = new StrategyParam<Sides>[_patternDefinitions.Length];

		for (var i = 0; i < _patternDefinitions.Length; i++)
		{
			var definition = _patternDefinitions[i];
			var groupName = definition.Group switch
			{
				PatternGroup.OneBar => "Group 1 - One Bar",
				PatternGroup.TwoBars => "Group 2 - Two Bars",
				PatternGroup.ThreeBars => "Group 3 - Three Bars",
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

	public OpenedMode Mode
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
				TriggerPattern(PatternType.NeutralBar);

			if (Compare(candle0.Close, candle0.High, equality) == 0)
				TriggerPattern(PatternType.ForceBarUp);

			if (Compare(candle0.Close, candle0.Low, equality) == 0)
				TriggerPattern(PatternType.ForceBarDown);

			if (candle0.UpperShadow <= minDeviation && candle0.LowerShadow > 2m * candle0.BodySize)
				TriggerPattern(PatternType.Hammer);

			if (candle0.LowerShadow <= minDeviation && candle0.UpperShadow > 2m * candle0.BodySize)
				TriggerPattern(PatternType.ShootingStar);
		}

		if (!hasPrevious)
			return;

		// Two-bar patterns evaluate the current bar together with the previous one.
		if (EnableTwoBarPatterns)
		{
			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0)
				TriggerPattern(PatternType.Inside);

			if (Compare(candle0.High, candle1.High, equality) > 0 && Compare(candle0.Low, candle1.Low, equality) < 0)
				TriggerPattern(PatternType.Outside);

			if (Compare(candle0.High, candle1.High, equality) == 0 && Compare(candle0.Close, candle1.Close, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) <= 0)
				TriggerPattern(PatternType.DoubleBarHighLowerClose);

			if (Compare(candle0.Low, candle1.Low, equality) == 0 && Compare(candle0.Close, candle1.Close, equality) > 0 && Compare(candle0.High, candle1.High, equality) >= 0)
				TriggerPattern(PatternType.DoubleBarLowHigherClose);

			if (Compare(candle1.BodySize, candle0.BodySize, equality) == 0 && Compare(candle1.Open, candle0.Close, equality) == 0)
				TriggerPattern(PatternType.MirrorBar);

			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle1.BodySize, candle0.BodySize, equality) > 0 && Compare(candle1.Close, candle0.Open, equality) > 0 && Compare(candle1.Open, candle0.Close, equality) < 0)
				TriggerPattern(PatternType.BearishHarami);

			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Open, candle0.Close, equality) == 0 && Compare(candle1.BodySize, candle0.BodySize, equality) > 0 && Compare(candle1.Close, candle0.Open, equality) > 0 && Compare(candle1.Open, candle0.Close, equality) < 0)
				TriggerPattern(PatternType.BearishHaramiCross);

			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) < 0 && Compare(candle0.Close, candle0.Open, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) > 0 && Compare(candle1.Close, candle0.Open, equality) < 0 && Compare(candle1.Open, candle0.Close, equality) > 0)
				TriggerPattern(PatternType.BullishHarami);

			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) < 0 && Compare(candle0.Open, candle0.Close, equality) == 0 && Compare(candle1.BodySize, candle0.BodySize, equality) > 0 && Compare(candle1.Close, candle0.Open, equality) < 0 && Compare(candle1.Open, candle0.Close, equality) > 0)
				TriggerPattern(PatternType.BullishHaramiCross);

			if (Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle1.High, candle0.Open, equality) < 0 && Compare(candle0.Close, candle1.Close, equality) < 0 && Compare(candle0.Close, candle1.Open, equality) > 0)
				TriggerPattern(PatternType.DarkCloudCover);

			if (Compare(candle0.Open, candle0.Close, equality) == 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Open, candle1.High, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) < 0 && Compare(candle0.Open, candle1.Low, equality) < 0)
				TriggerPattern(PatternType.DojiStar);

			if (Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle0.Open, candle1.Close, equality) > 0 && Compare(candle0.Close, candle1.Open, equality) < 0)
				TriggerPattern(PatternType.EngulfingBearishLine);

			if (Compare(candle1.Close, candle1.Open, equality) < 0 && Compare(candle0.Close, candle0.Open, equality) > 0 && Compare(candle0.Open, candle1.Close, equality) < 0 && Compare(candle0.Close, candle1.Open, equality) > 0)
				TriggerPattern(PatternType.EngulfingBullishLine);

			if (Compare(candle0.Open, candle0.Close, equality) == 0 && candle0.UpperShadow > minDeviation4 && candle0.LowerShadow > minDeviation4 && Compare(candle1.Open, candle1.Close, equality) == 0 && candle1.UpperShadow > minDeviation4 && candle1.LowerShadow > minDeviation4)
				TriggerPattern(PatternType.TwoNeutralBars);
		}

		if (!hasPrevious2)
			return;

		// Three-bar patterns combine the last three candles.
		if (EnableThreeBarPatterns)
		{
			if (Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle1.High, candle2.High, equality) < 0 && Compare(candle1.Low, candle2.Low, equality) > 0)
				TriggerPattern(PatternType.DoubleInside);

			if (Compare(candle1.High, candle2.High, equality) > 0 && Compare(candle1.High, candle0.High, equality) > 0 && Compare(candle1.Low, candle2.Low, equality) > 0 && Compare(candle1.Low, candle0.Low, equality) > 0 && candle1.BodySize * 2m < candle1.UpperShadow)
				TriggerPattern(PatternType.PinUp);

			if (Compare(candle1.High, candle2.High, equality) < 0 && Compare(candle1.High, candle0.High, equality) < 0 && Compare(candle1.Low, candle2.Low, equality) < 0 && Compare(candle1.Low, candle0.Low, equality) < 0 && candle1.BodySize * 2m < candle1.LowerShadow)
				TriggerPattern(PatternType.PinDown);

			if (Compare(candle1.High, candle2.High, equality) > 0 && Compare(candle1.High, candle0.High, equality) > 0 && Compare(candle1.Low, candle2.Low, equality) > 0 && Compare(candle1.Low, candle0.Low, equality) > 0 && Compare(candle0.Close, candle1.Low, equality) < 0)
				TriggerPattern(PatternType.PivotPointReversalDown);

			if (Compare(candle1.High, candle2.High, equality) < 0 && Compare(candle1.High, candle0.High, equality) < 0 && Compare(candle1.Low, candle2.Low, equality) < 0 && Compare(candle1.Low, candle0.Low, equality) < 0 && Compare(candle0.Close, candle1.High, equality) > 0)
				TriggerPattern(PatternType.PivotPointReversalUp);

			if (Compare(candle1.High, candle2.High, equality) < 0 && Compare(candle1.Low, candle2.Low, equality) < 0 && Compare(candle0.High, candle1.High, equality) < 0 && Compare(candle0.Low, candle1.Low, equality) < 0 && Compare(candle0.Close, candle1.Close, equality) > 0 && Compare(candle0.Open, candle0.Close, equality) < 0)
				TriggerPattern(PatternType.ClosePriceReversalUp);

			if (Compare(candle1.High, candle2.High, equality) > 0 && Compare(candle1.Low, candle2.Low, equality) > 0 && Compare(candle0.High, candle1.High, equality) > 0 && Compare(candle0.Low, candle1.Low, equality) > 0 && Compare(candle0.Close, candle1.Close, equality) < 0 && Compare(candle0.Open, candle0.Close, equality) > 0)
				TriggerPattern(PatternType.ClosePriceReversalDown);

			if (Compare(candle2.Close, candle2.Open, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle2.Close, candle1.Open, equality) < 0 && Compare(candle2.BodySize, candle1.BodySize, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) < 0 && Compare(candle0.Close, candle2.Open, equality) > 0 && Compare(candle0.Close, candle2.Close, equality) < 0)
				TriggerPattern(PatternType.EveningStar);

			if (Compare(candle2.Close, candle2.Open, equality) < 0 && Compare(candle1.Close, candle1.Open, equality) > 0 && Compare(candle0.Close, candle0.Open, equality) > 0 && Compare(candle2.Close, candle1.Open, equality) > 0 && Compare(candle2.BodySize, candle1.BodySize, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) < 0 && Compare(candle0.Close, candle2.Close, equality) > 0 && Compare(candle0.Close, candle2.Open, equality) < 0)
				TriggerPattern(PatternType.MorningStar);

			if (Compare(candle2.Close, candle2.Open, equality) > 0 && Compare(candle1.Close, candle1.Open, equality) == 0 && Compare(candle0.Close, candle0.Open, equality) < 0 && Compare(candle2.Close, candle1.Open, equality) < 0 && Compare(candle2.BodySize, candle1.BodySize, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) < 0 && Compare(candle0.Close, candle2.Open, equality) > 0 && Compare(candle0.Close, candle2.Close, equality) < 0)
				TriggerPattern(PatternType.EveningDojiStar);

			if (Compare(candle2.Close, candle2.Open, equality) < 0 && Compare(candle1.Close, candle1.Open, equality) == 0 && Compare(candle0.Close, candle0.Open, equality) > 0 && Compare(candle2.Close, candle1.Open, equality) > 0 && Compare(candle2.BodySize, candle1.BodySize, equality) > 0 && Compare(candle1.BodySize, candle0.BodySize, equality) < 0 && Compare(candle0.Close, candle2.Close, equality) > 0 && Compare(candle0.Close, candle2.Open, equality) < 0)
				TriggerPattern(PatternType.MorningDojiStar);
		}
	}

	private int Compare(decimal price1, decimal price2, decimal tolerance)
	{
		var diff = price1 - price2;

		if (Math.Abs(diff) < tolerance)
			return 0;

		return diff > 0 ? 1 : -1;
	}

	private bool IsGroupEnabled(PatternGroup group)
	{
		return group switch
		{
			PatternGroup.OneBar => EnableOneBarPatterns,
			PatternGroup.TwoBars => EnableTwoBarPatterns,
			PatternGroup.ThreeBars => EnableThreeBarPatterns,
			_ => true,
		};
	}

	private void TriggerPattern(PatternType type)
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

		if (Mode == OpenedMode.Swing)
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
			OpenedMode.Any => true,
			OpenedMode.Swing => true,
			OpenedMode.BuyOne => side == Sides.Buy && Position <= 0,
			OpenedMode.BuyMany => side == Sides.Buy,
			OpenedMode.SellOne => side == Sides.Sell && Position >= 0,
			OpenedMode.SellMany => side == Sides.Sell,
			_ => false,
		};
	}

	private enum PatternGroup
	{
		OneBar = 1,
		TwoBars = 2,
		ThreeBars = 3,
	}

	private enum PatternType
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

	public enum OpenedMode
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
		public PatternDefinition(PatternType type, string displayName, PatternGroup group)
		{
			Type = type;
			DisplayName = displayName;
			Group = group;
		}

		public PatternType Type { get; }

		public string DisplayName { get; }

		public PatternGroup Group { get; }
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
