using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Visual rule builder inspired by the original FAT Panel for MetaTrader.
/// The strategy evaluates JSON-defined conditions and executes the mapped orders.
/// </summary>
public class FatPanelVisualBuilderStrategy : Strategy
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true) }
	};

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _configuration;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private Dictionary<FatPanelSignalDefinition, SignalContext> _signalContexts = new();
	private List<FatPanelRuleContext> _ruleContexts = new();

	private decimal? _bestBid;
	private decimal _priceStep;

	/// <summary>
	/// Primary candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// JSON configuration string describing rules, conditions and actions.
	/// </summary>
	public string Configuration
	{
		get => _configuration.Value;
		set => _configuration.Value = value;
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
	/// Initializes strategy parameters with defaults matching the original panel.
	/// </summary>
	public FatPanelVisualBuilderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");

		_configuration = Param(nameof(Configuration), DefaultConfiguration)
			.SetDisplay("Configuration", "JSON definition of rules", "Logic");


		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Protection");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetDisplay("Take Profit (pts)", "Take profit distance in points", "Protection");
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

		_signalContexts = new();
		_ruleContexts = new();
		_bestBid = null;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;

		var configuration = LoadConfiguration();
		(_signalContexts, _ruleContexts) = BuildContexts(configuration);

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(ProcessCandle)
			.Start();

		if (NeedsBestBidUpdates(_signalContexts))
		{
			SubscribeLevel1()
				.Bind(ProcessLevel1)
				.Start();
		}

		StartProtectionIfNeeded();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}
	}

	private void StartProtectionIfNeeded()
	{
		var hasStop = StopLossPoints > 0 && _priceStep > 0;
		var hasTake = TakeProfitPoints > 0 && _priceStep > 0;

		if (!hasStop && !hasTake)
			return;

		var take = hasTake ? new Unit(TakeProfitPoints * _priceStep, UnitTypes.Absolute) : new Unit();
		var stop = hasStop ? new Unit(StopLossPoints * _priceStep, UnitTypes.Absolute) : new Unit();

		StartProtection(take, stop);
	}

	private static bool NeedsBestBidUpdates(Dictionary<FatPanelSignalDefinition, SignalContext> contexts)
	{
		foreach (var pair in contexts)
		{
			if (pair.Key.Type == SignalType.Bid)
				return true;
		}

		return false;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bestBid) && ConvertToDecimal(bestBid) is decimal bid)
		{
			_bestBid = bid;
		}
		else if (level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last) && ConvertToDecimal(last) is decimal trade)
		{
			_bestBid = trade;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		foreach (var context in _signalContexts.Values)
			context.Update(candle, _bestBid);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		foreach (var rule in _ruleContexts)
			rule.TryExecute(this, candle);
	}

	private FatPanelConfiguration LoadConfiguration()
	{
		var text = string.IsNullOrWhiteSpace(Configuration) ? DefaultConfiguration : Configuration;

		try
		{
			var config = JsonSerializer.Deserialize<FatPanelConfiguration>(text, _jsonOptions);
			if (config == null || config.Rules.Count == 0)
				throw new InvalidOperationException("Configuration must contain at least one rule.");

			return config;
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			LogError($"Failed to parse configuration: {ex.Message}");
			throw;
		}
	}

	private (Dictionary<FatPanelSignalDefinition, SignalContext>, List<FatPanelRuleContext>) BuildContexts(FatPanelConfiguration configuration)
	{
		var signals = new Dictionary<FatPanelSignalDefinition, SignalContext>();

		for (var i = 0; i < configuration.Rules.Count; i++)
		{
			var rule = configuration.Rules[i];
			AddSignals(rule.All, signals);
			AddSignals(rule.Any, signals);
			AddSignals(rule.None, signals);
		}

		var rules = new List<FatPanelRuleContext>(configuration.Rules.Count);
		for (var i = 0; i < configuration.Rules.Count; i++)
		{
			var rule = configuration.Rules[i];
			var all = BuildConditionList(rule.All, signals);
			var any = BuildConditionList(rule.Any, signals);
			var none = BuildConditionList(rule.None, signals);
			var action = new FatPanelActionContext(rule.Action ?? new FatPanelAction());

			rules.Add(new FatPanelRuleContext(rule.Name, all, any, none, action));
		}

		return (signals, rules);
	}

	private static void AddSignals(List<FatPanelCondition> conditions, Dictionary<FatPanelSignalDefinition, SignalContext> signals)
	{
		if (conditions == null)
			return;

		for (var i = 0; i < conditions.Count; i++)
		{
			var condition = conditions[i];
			if (condition.Type != ConditionType.Comparison)
				continue;

			if (condition.Left == null || condition.Right == null)
				throw new InvalidOperationException("Comparison condition requires both left and right signals.");

			if (!signals.ContainsKey(condition.Left))
				signals[condition.Left] = new SignalContext(condition.Left);

			if (!signals.ContainsKey(condition.Right))
				signals[condition.Right] = new SignalContext(condition.Right);
		}
	}

	private List<FatPanelConditionContext> BuildConditionList(List<FatPanelCondition> source, Dictionary<FatPanelSignalDefinition, SignalContext> signals)
	{
		var result = new List<FatPanelConditionContext>();

		if (source == null)
			return result;

		for (var i = 0; i < source.Count; i++)
			result.Add(BuildCondition(source[i], signals));

		return result;
	}

	private FatPanelConditionContext BuildCondition(FatPanelCondition condition, Dictionary<FatPanelSignalDefinition, SignalContext> signals)
	{
		return condition.Type switch
		{
			ConditionType.Comparison => BuildComparisonCondition(condition, signals),
			ConditionType.Position => BuildPositionCondition(condition),
			ConditionType.Time => BuildTimeCondition(condition),
			ConditionType.DayOfWeek => BuildDayOfWeekCondition(condition),
			_ => throw new InvalidOperationException($"Unsupported condition type: {condition.Type}.")
		};
	}

	private FatPanelConditionContext BuildComparisonCondition(FatPanelCondition condition, Dictionary<FatPanelSignalDefinition, SignalContext> signals)
	{
		if (condition.Operator == null || condition.Left == null || condition.Right == null)
			throw new InvalidOperationException("Comparison condition requires operator and both signals.");

		if (!signals.TryGetValue(condition.Left, out var left))
			throw new InvalidOperationException("Left signal context was not created.");

		if (!signals.TryGetValue(condition.Right, out var right))
			throw new InvalidOperationException("Right signal context was not created.");

		var comparison = new FatPanelComparisonContext(left, right, condition.Operator.Value, condition.Threshold ?? 0m);
		return new FatPanelConditionContext(_ => comparison.Evaluate());
	}

	private FatPanelConditionContext BuildPositionCondition(FatPanelCondition condition)
	{
		var requirement = condition.Required ?? PositionRequirement.Any;

		return new FatPanelConditionContext(_ => requirement switch
		{
			PositionRequirement.Any => true,
			PositionRequirement.FlatOnly => Position == 0m,
			PositionRequirement.FlatOrShort => Position <= 0m,
			PositionRequirement.FlatOrLong => Position >= 0m,
			PositionRequirement.LongOnly => Position > 0m,
			PositionRequirement.ShortOnly => Position < 0m,
			_ => true
		});
	}

	private FatPanelConditionContext BuildTimeCondition(FatPanelCondition condition)
	{
		var startText = string.IsNullOrWhiteSpace(condition.Start) ? "00:00" : condition.Start!;
		var endText = string.IsNullOrWhiteSpace(condition.End) ? "00:00" : condition.End!;

		if (!TryParseTime(startText, out var start) || !TryParseTime(endText, out var end))
			throw new InvalidOperationException("Time condition requires parsable start and end values.");

		return new FatPanelConditionContext(time =>
		{
			var targetTime = time.TimeOfDay;

			if (start <= end)
				return targetTime >= start && targetTime < end;

			return targetTime >= start || targetTime < end;
		});
	}

	private FatPanelConditionContext BuildDayOfWeekCondition(FatPanelCondition condition)
	{
		var days = condition.Days;
		if (days == null || days.Count == 0)
		{
			days = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
		}

		var parsed = new HashSet<DayOfWeek>();
		for (var i = 0; i < days.Count; i++)
		{
			var day = days[i];
			if (!Enum.TryParse(day, true, out DayOfWeek parsedDay))
				throw new InvalidOperationException($"Unknown day of week: {day}.");

			parsed.Add(parsedDay);
		}

		return new FatPanelConditionContext(time => parsed.Contains(time.DayOfWeek));
	}

	private static bool TryParseTime(string text, out TimeSpan time)
	{
		return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out time);
	}

	private static decimal? ConvertToDecimal(object value)
	{
		return value switch
		{
			null => null,
			decimal decimalValue => decimalValue,
			double doubleValue => (decimal)doubleValue,
			float floatValue => (decimal)floatValue,
			int intValue => intValue,
			long longValue => longValue,
			_ => null
		};
	}

	private static decimal GetPrice(ICandleMessage candle, PriceSource source)
	{
		return source switch
		{
			PriceSource.Open => candle.OpenPrice,
			PriceSource.High => candle.HighPrice,
			PriceSource.Low => candle.LowPrice,
			_ => candle.ClosePrice
		};
	}

	private sealed class SignalContext
	{
		private readonly FatPanelSignalDefinition _definition;
		private readonly IIndicator _indicator;

		public decimal? Current { get; private set; }
		public decimal? Previous { get; private set; }

		public SignalContext(FatPanelSignalDefinition definition)
		{
			_definition = definition;

			if (definition.Type == SignalType.MovingAverage)
			{
				var period = definition.Period ?? 0;
				if (period <= 0)
					throw new InvalidOperationException("Moving average signal requires a positive period.");

				_indicator = definition.Method switch
				{
					MovingAverageMethod.Simple => new SMA { Length = period },
					MovingAverageMethod.Exponential => new EMA { Length = period },
					MovingAverageMethod.Smoothed => new SMMA { Length = period },
					MovingAverageMethod.LinearWeighted => new WMA { Length = period },
					_ => new SMA { Length = period }
				};
			}
			else if (definition.Type == SignalType.Constant)
			{
				var level = definition.Level ?? 0m;
				Current = level;
				Previous = level;
			}
		}

		public void Update(ICandleMessage candle, decimal? bestBid)
		{
			switch (_definition.Type)
			{
				case SignalType.MovingAverage:
				{
					if (_indicator == null)
						return;

					var price = GetPrice(candle, _definition.Price);
					var value = _indicator.Process(price, candle.OpenTime, true).ToNullableDecimal();

					if (_indicator.IsFormed && value is decimal formed)
					{
						Previous = Current;
						Current = formed;
					}

					break;
				}
				case SignalType.Bid:
				{
					var bid = bestBid ?? candle.ClosePrice;
					if (bid > 0)
					{
						Previous = Current;
						Current = bid;
					}

					break;
				}
				case SignalType.Constant:
				{
					// Constant signal does not change over time.
					break;
				}
			}
		}
	}

	private sealed class FatPanelComparisonContext
	{
		private readonly SignalContext _left;
		private readonly SignalContext _right;
		private readonly OperatorType _operator;
		private readonly decimal _threshold;

		public FatPanelComparisonContext(SignalContext left, SignalContext right, OperatorType @operator, decimal threshold)
		{
			_left = left;
			_right = right;
			_operator = @operator;
			_threshold = Math.Abs(threshold);
		}

		public bool Evaluate()
		{
			var left = _left.Current;
			var right = _right.Current;

			if (left is null || right is null)
				return false;

			return _operator switch
			{
				OperatorType.Greater => left.Value - right.Value > _threshold,
				OperatorType.Less => right.Value - left.Value > _threshold,
				OperatorType.Equal => Math.Abs(left.Value - right.Value) <= _threshold,
				OperatorType.CrossAbove => IsCrossAbove(),
				OperatorType.CrossBelow => IsCrossBelow(),
				_ => false
			};
		}

		private bool IsCrossAbove()
		{
			var prevLeft = _left.Previous;
			var prevRight = _right.Previous;
			if (prevLeft is null || prevRight is null)
				return false;

			var previousDiff = prevLeft.Value - prevRight.Value;
			var currentDiff = _left.Current!.Value - _right.Current!.Value;

			if (previousDiff > 0m)
				return false;

			return currentDiff > _threshold;
		}

		private bool IsCrossBelow()
		{
			var prevLeft = _left.Previous;
			var prevRight = _right.Previous;
			if (prevLeft is null || prevRight is null)
				return false;

			var previousDiff = prevLeft.Value - prevRight.Value;
			var currentDiff = _left.Current!.Value - _right.Current!.Value;

			if (previousDiff < 0m)
				return false;

			return currentDiff < -_threshold;
		}
	}

	private sealed class FatPanelConditionContext
	{
		private readonly Func<DateTimeOffset, bool> _predicate;

		public FatPanelConditionContext(Func<DateTimeOffset, bool> predicate)
		{
			_predicate = predicate;
		}

		public bool Evaluate(DateTimeOffset time)
		{
			return _predicate(time);
		}
	}

	private sealed class FatPanelRuleContext
	{
		private readonly string _name;
		private readonly List<FatPanelConditionContext> _all;
		private readonly List<FatPanelConditionContext> _any;
		private readonly List<FatPanelConditionContext> _none;
		private readonly FatPanelActionContext _action;

		public FatPanelRuleContext(string name, List<FatPanelConditionContext> all, List<FatPanelConditionContext> any, List<FatPanelConditionContext> none, FatPanelActionContext action)
		{
			_name = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
			_all = all;
			_any = any;
			_none = none;
			_action = action;
		}

		public void TryExecute(FatPanelVisualBuilderStrategy strategy, ICandleMessage candle)
		{
			var time = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

			for (var i = 0; i < _all.Count; i++)
			{
				if (!_all[i].Evaluate(time))
					return;
			}

			if (_any.Count > 0)
			{
				var anyPassed = false;
				for (var i = 0; i < _any.Count; i++)
				{
					if (_any[i].Evaluate(time))
					{
						anyPassed = true;
						break;
					}
				}

				if (!anyPassed)
					return;
			}

			for (var i = 0; i < _none.Count; i++)
			{
				if (_none[i].Evaluate(time))
					return;
			}

			_action.Execute(strategy, candle, _name);
		}
	}

	private sealed class FatPanelActionContext
	{
		private readonly ActionType _type;
		private readonly decimal? _volume;

		public FatPanelActionContext(FatPanelAction action)
		{
			_type = action?.Type ?? ActionType.Buy;
			_volume = action?.Volume;
		}

		public void Execute(FatPanelVisualBuilderStrategy strategy, ICandleMessage candle, string ruleName)
		{
			var volume = _volume ?? strategy.Volume;
			if (volume <= 0)
				return;

			switch (_type)
			{
				case ActionType.Buy:
				{
					if (strategy.Position <= 0)
					{
						strategy.BuyMarket(volume);
						strategy.LogInfo($"Rule '{ruleName}' executed Buy at {candle.ClosePrice}.");
					}

					break;
				}
				case ActionType.SellShort:
				{
					if (strategy.Position >= 0)
					{
						strategy.SellMarket(volume);
						strategy.LogInfo($"Rule '{ruleName}' executed SellShort at {candle.ClosePrice}.");
					}

					break;
				}
				case ActionType.Close:
				{
					if (strategy.Position != 0)
					{
						strategy.ClosePosition();
						strategy.LogInfo($"Rule '{ruleName}' closed position at {candle.ClosePrice}.");
					}

					break;
				}
			}
		}
	}

	private sealed class FatPanelConfiguration
	{
		[JsonPropertyName("rules")]
		public List<FatPanelRule> Rules { get; init; } = new();
	}

	private sealed class FatPanelRule
	{
		[JsonPropertyName("name")]
		public string Name { get; init; }

		[JsonPropertyName("all")]
		public List<FatPanelCondition> All { get; init; }

		[JsonPropertyName("any")]
		public List<FatPanelCondition> Any { get; init; }

		[JsonPropertyName("none")]
		public List<FatPanelCondition> None { get; init; }

		[JsonPropertyName("action")]
		public FatPanelAction Action { get; init; }
	}

	private sealed class FatPanelAction
	{
		[JsonPropertyName("type")]
		public ActionType Type { get; init; } = ActionType.Buy;

		[JsonPropertyName("volume")]
		public decimal? Volume { get; init; }
	}

	private sealed class FatPanelCondition
	{
		[JsonPropertyName("type")]
		public ConditionType Type { get; init; }

		[JsonPropertyName("operator")]
		public OperatorType? Operator { get; init; }

		[JsonPropertyName("left")]
		public FatPanelSignalDefinition Left { get; init; }

		[JsonPropertyName("right")]
		public FatPanelSignalDefinition Right { get; init; }

		[JsonPropertyName("threshold")]
		public decimal? Threshold { get; init; }

		[JsonPropertyName("required")]
		public PositionRequirement? Required { get; init; }

		[JsonPropertyName("start")]
		public string Start { get; init; }

		[JsonPropertyName("end")]
		public string End { get; init; }

		[JsonPropertyName("days")]
		public List<string> Days { get; init; }
	}

	private sealed record class FatPanelSignalDefinition
	{
		[JsonPropertyName("type")]
		public SignalType Type { get; init; }

		[JsonPropertyName("period")]
		public int? Period { get; init; }

		[JsonPropertyName("method")]
		public MovingAverageMethod Method { get; init; } = MovingAverageMethod.Simple;

		[JsonPropertyName("price")]
		public PriceSource Price { get; init; } = PriceSource.Close;

		[JsonPropertyName("level")]
		public decimal? Level { get; init; }
	}

	private enum SignalType
	{
		MovingAverage,
		Bid,
		Constant
	}

	private enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	private enum PriceSource
	{
		Close,
		Open,
		High,
		Low
	}

	private enum OperatorType
	{
		CrossAbove,
		CrossBelow,
		Greater,
		Less,
		Equal
	}

	private enum ConditionType
	{
		Comparison,
		Position,
		Time,
		DayOfWeek
	}

	private enum ActionType
	{
		Buy,
		SellShort,
		Close
	}

	private enum PositionRequirement
	{
		Any,
		FlatOnly,
		FlatOrShort,
		FlatOrLong,
		LongOnly,
		ShortOnly
	}

	private const string DefaultConfiguration = """
{
  "rules": [
	{
	  "name": "EMA crosses above SMA",
	  "all": [
		{
		  "type": "comparison",
		  "operator": "CrossAbove",
		  "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
		  "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
		},
		{ "type": "dayOfWeek", "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"] },
		{ "type": "time", "start": "09:00", "end": "17:00" },
		{ "type": "position", "required": "FlatOrShort" }
	  ],
	  "action": { "type": "Buy" }
	},
	{
	  "name": "EMA crosses below SMA",
	  "all": [
		{
		  "type": "comparison",
		  "operator": "CrossBelow",
		  "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
		  "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
		},
		{ "type": "position", "required": "LongOnly" }
	  ],
	  "action": { "type": "Close" }
	}
  ]
}
""";
}
