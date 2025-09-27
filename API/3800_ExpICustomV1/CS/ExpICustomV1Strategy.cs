using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the exp_iCustom_v1 MetaTrader expert.
/// The strategy reads signals from a configurable indicator and opens positions on arrow buffers.
/// </summary>
public class ExpICustomV1Strategy : Strategy
{
	private sealed class IndicatorState
	{
		private readonly List<decimal?[]> _history = new();
		private readonly IIndicator _indicator;

		public IndicatorState(IIndicator indicator)
		{
			_indicator = indicator;
		}

		public IIndicator Indicator => _indicator;

		public void Reset()
		{
			_history.Clear();
			_indicator?.Reset();
		}

		public void Process(ICandleMessage candle, Func<ICandleMessage, decimal> priceSelector)
		{
			if (_indicator == null)
				return;

			var price = priceSelector(candle);
			var value = _indicator.Process(new CandleIndicatorValue(candle, price));

			if (!value.IsFinal)
				return;
		}

		public decimal? GetValue(int bufferIndex, int shift)
		{
			if (bufferIndex < 0 || shift < 0)
				return null;

			var index = _history.Count - 1 - shift;
			if (index < 0 || index >= _history.Count)
				return null;

			var values = _history[index];
			if (bufferIndex >= values.Length)
				return null;

			return values[bufferIndex];
		}

		public int Count => _history.Count;
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _indicatorName;
	private readonly StrategyParam<string> _indicatorParameters;
	private readonly StrategyParam<int> _buyBufferIndex;
	private readonly StrategyParam<int> _sellBufferIndex;
	private readonly StrategyParam<int> _indicatorShift;
	private readonly StrategyParam<bool> _override1Use;
	private readonly StrategyParam<int> _override1Index;
	private readonly StrategyParam<decimal> _override1Value;
	private readonly StrategyParam<bool> _override2Use;
	private readonly StrategyParam<int> _override2Index;
	private readonly StrategyParam<decimal> _override2Value;
	private readonly StrategyParam<bool> _override3Use;
	private readonly StrategyParam<int> _override3Index;
	private readonly StrategyParam<decimal> _override3Value;
	private readonly StrategyParam<bool> _override4Use;
	private readonly StrategyParam<int> _override4Index;
	private readonly StrategyParam<decimal> _override4Value;
	private readonly StrategyParam<bool> _override5Use;
	private readonly StrategyParam<int> _override5Index;
	private readonly StrategyParam<decimal> _override5Value;
	private readonly StrategyParam<int> _sleepBars;
	private readonly StrategyParam<bool> _cancelSleeping;
	private readonly StrategyParam<bool> _closeOnReverse;
	private readonly StrategyParam<int> _maxOrdersCount;
	private readonly StrategyParam<int> _maxBuyCount;
	private readonly StrategyParam<int> _maxSellCount;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _trailingStopEnabled;
	private readonly StrategyParam<int> _trailingStartPoints;
	private readonly StrategyParam<int> _trailingDistancePoints;
	private readonly StrategyParam<bool> _breakEvenEnabled;
	private readonly StrategyParam<int> _breakEvenStartPoints;
	private readonly StrategyParam<int> _breakEvenLockPoints;
	private readonly StrategyParam<decimal> _baseOrderVolume;

	private IndicatorState _indicatorState;
	private DateTimeOffset? _lastBuyBarTime;
	private DateTimeOffset? _lastSellBarTime;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="ExpICustomV1Strategy"/>.
	/// </summary>
	public ExpICustomV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used to read indicator buffers", "General");

		_indicatorName = Param(nameof(IndicatorName), "SMA")
		.SetDisplay("Indicator Name", "Type name of the indicator used for signals", "Indicator");

		_indicatorParameters = Param(nameof(IndicatorParameters), "Length=14")
		.SetDisplay("Indicator Parameters", "Slash separated parameters passed to the indicator", "Indicator");

		_buyBufferIndex = Param(nameof(BuyBufferIndex), 0)
		.SetDisplay("Buy Buffer", "Buffer index that marks long entries", "Indicator");

		_sellBufferIndex = Param(nameof(SellBufferIndex), 1)
		.SetDisplay("Sell Buffer", "Buffer index that marks short entries", "Indicator");

		_indicatorShift = Param(nameof(IndicatorShift), 1)
		.SetDisplay("Indicator Shift", "Historical shift when reading buffer values", "Indicator");

		_override1Use = Param(nameof(Override1Use), false)
		.SetDisplay("Override 1", "Enable replacement of parameter by index", "Overrides");
		_override1Index = Param(nameof(Override1Index), 0)
		.SetDisplay("Override 1 Index", "Parameter index replaced by Override 1", "Overrides");
		_override1Value = Param(nameof(Override1Value), 0m)
		.SetDisplay("Override 1 Value", "Value inserted into the parameter list", "Overrides");

		_override2Use = Param(nameof(Override2Use), false)
		.SetDisplay("Override 2", "Enable replacement of parameter by index", "Overrides");
		_override2Index = Param(nameof(Override2Index), 1)
		.SetDisplay("Override 2 Index", "Parameter index replaced by Override 2", "Overrides");
		_override2Value = Param(nameof(Override2Value), 0m)
		.SetDisplay("Override 2 Value", "Value inserted into the parameter list", "Overrides");

		_override3Use = Param(nameof(Override3Use), false)
		.SetDisplay("Override 3", "Enable replacement of parameter by index", "Overrides");
		_override3Index = Param(nameof(Override3Index), 2)
		.SetDisplay("Override 3 Index", "Parameter index replaced by Override 3", "Overrides");
		_override3Value = Param(nameof(Override3Value), 0m)
		.SetDisplay("Override 3 Value", "Value inserted into the parameter list", "Overrides");

		_override4Use = Param(nameof(Override4Use), false)
		.SetDisplay("Override 4", "Enable replacement of parameter by index", "Overrides");
		_override4Index = Param(nameof(Override4Index), 3)
		.SetDisplay("Override 4 Index", "Parameter index replaced by Override 4", "Overrides");
		_override4Value = Param(nameof(Override4Value), 0m)
		.SetDisplay("Override 4 Value", "Value inserted into the parameter list", "Overrides");

		_override5Use = Param(nameof(Override5Use), false)
		.SetDisplay("Override 5", "Enable replacement of parameter by index", "Overrides");
		_override5Index = Param(nameof(Override5Index), 4)
		.SetDisplay("Override 5 Index", "Parameter index replaced by Override 5", "Overrides");
		_override5Value = Param(nameof(Override5Value), 0m)
		.SetDisplay("Override 5 Value", "Value inserted into the parameter list", "Overrides");

		_sleepBars = Param(nameof(SleepBars), 1)
		.SetDisplay("Sleep Bars", "Minimal number of bars between new entries of the same direction", "Trading");

		_cancelSleeping = Param(nameof(CancelSleeping), true)
		.SetDisplay("Cancel Sleeping", "Reset sleep timer after the opposite signal", "Trading");

		_closeOnReverse = Param(nameof(CloseOnReverse), false)
		.SetDisplay("Close On Reverse", "Close current position when opposite signal appears", "Trading");

		_maxOrdersCount = Param(nameof(MaxOrdersCount), -1)
		.SetDisplay("Max Orders", "Maximum simultaneous orders (-1 disables the check)", "Trading");

		_maxBuyCount = Param(nameof(MaxBuyCount), -1)
		.SetDisplay("Max Buy Orders", "Maximum stacked long entries (-1 disables the check)", "Trading");

		_maxSellCount = Param(nameof(MaxSellCount), -1)
		.SetDisplay("Max Sell Orders", "Maximum stacked short entries (-1 disables the check)", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 25)
		.SetDisplay("Stop Loss", "Distance in points for protective stop", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 25)
		.SetDisplay("Take Profit", "Distance in points for profit target", "Risk");

		_trailingStopEnabled = Param(nameof(TrailingStopEnabled), false)
		.SetDisplay("Trailing Stop", "Enable price-based trailing stop", "Risk");

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 50)
		.SetDisplay("Trailing Start", "Profit in points that activates trailing", "Risk");

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 15)
		.SetDisplay("Trailing Distance", "Distance maintained by the trailing stop", "Risk");

		_breakEvenEnabled = Param(nameof(BreakEvenEnabled), false)
		.SetDisplay("Break Even", "Enable break-even stop adjustment", "Risk");

		_breakEvenStartPoints = Param(nameof(BreakEvenStartPoints), 30)
		.SetDisplay("Break Even Start", "Profit in points that activates break even", "Risk");

		_breakEvenLockPoints = Param(nameof(BreakEvenLockPoints), 15)
		.SetDisplay("Break Even Lock", "Points locked in once break even triggers", "Risk");

		_baseOrderVolume = Param(nameof(BaseOrderVolume), 1m)
		.SetDisplay("Base Volume", "Volume used for each market entry", "Trading");
	}

	/// <summary>
	/// Type of candles to use for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Indicator type name.
	/// </summary>
	public string IndicatorName
	{
		get => _indicatorName.Value;
		set => _indicatorName.Value = value;
	}

	/// <summary>
	/// Indicator parameters string.
	/// </summary>
	public string IndicatorParameters
	{
		get => _indicatorParameters.Value;
		set => _indicatorParameters.Value = value;
	}

	/// <summary>
	/// Buffer index used to detect buy entries.
	/// </summary>
	public int BuyBufferIndex
	{
		get => _buyBufferIndex.Value;
		set => _buyBufferIndex.Value = value;
	}

	/// <summary>
	/// Buffer index used to detect sell entries.
	/// </summary>
	public int SellBufferIndex
	{
		get => _sellBufferIndex.Value;
		set => _sellBufferIndex.Value = value;
	}

	/// <summary>
	/// Historical shift applied when reading indicator buffers.
	/// </summary>
	public int IndicatorShift
	{
		get => _indicatorShift.Value;
		set => _indicatorShift.Value = value;
	}

	/// <summary>
	/// Minimal number of bars between entries of the same direction.
	/// </summary>
	public int SleepBars
	{
		get => _sleepBars.Value;
		set => _sleepBars.Value = value;
	}

	/// <summary>
	/// Reset the sleep timer after an opposite trade.
	/// </summary>
	public bool CancelSleeping
	{
		get => _cancelSleeping.Value;
		set => _cancelSleeping.Value = value;
	}

	/// <summary>
	/// Close the current position before opening an opposite trade.
	/// </summary>
	public bool CloseOnReverse
	{
		get => _closeOnReverse.Value;
		set => _closeOnReverse.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous orders.
	/// </summary>
	public int MaxOrdersCount
	{
		get => _maxOrdersCount.Value;
		set => _maxOrdersCount.Value = value;
	}

	/// <summary>
	/// Maximum number of stacked buy orders.
	/// </summary>
	public int MaxBuyCount
	{
		get => _maxBuyCount.Value;
		set => _maxBuyCount.Value = value;
	}

	/// <summary>
	/// Maximum number of stacked sell orders.
	/// </summary>
	public int MaxSellCount
	{
		get => _maxSellCount.Value;
		set => _maxSellCount.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable price trailing stop.
	/// </summary>
	public bool TrailingStopEnabled
	{
		get => _trailingStopEnabled.Value;
		set => _trailingStopEnabled.Value = value;
	}

	/// <summary>
	/// Profit in points required before trailing starts.
	/// </summary>
	public int TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Distance in points maintained by the trailing stop.
	/// </summary>
	public int TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	/// <summary>
	/// Enable break-even logic.
	/// </summary>
	public bool BreakEvenEnabled
	{
		get => _breakEvenEnabled.Value;
		set => _breakEvenEnabled.Value = value;
	}

	/// <summary>
	/// Profit in points that activates the break-even adjustment.
	/// </summary>
	public int BreakEvenStartPoints
	{
		get => _breakEvenStartPoints.Value;
		set => _breakEvenStartPoints.Value = value;
	}

	/// <summary>
	/// Points locked in after break-even activates.
	/// </summary>
	public int BreakEvenLockPoints
	{
		get => _breakEvenLockPoints.Value;
		set => _breakEvenLockPoints.Value = value;
	}

	/// <summary>
	/// Base volume for market entries.
	/// </summary>
	public decimal BaseOrderVolume
	{
		get => _baseOrderVolume.Value;
		set => _baseOrderVolume.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_indicatorState?.Reset();
		_lastBuyBarTime = null;
		_lastSellBarTime = null;
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();
		Volume = BaseOrderVolume;

		var processedParameters = BuildParameterString();
		_indicatorState = new IndicatorState(CreateIndicator(IndicatorName, processedParameters));

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_indicatorState.Indicator != null)
			{
				DrawIndicator(area, _indicatorState.Indicator);
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_indicatorState?.Process(candle, GetPriceForIndicator);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buySignal = EvaluateSignal(_indicatorState, BuyBufferIndex, IndicatorShift);
		var sellSignal = EvaluateSignal(_indicatorState, SellBufferIndex, IndicatorShift);

		if (buySignal && sellSignal)
		{
			buySignal = false;
			sellSignal = false;
		}

		if ((buySignal || sellSignal) && CloseOnReverse && Position != 0)
		{
			ClosePosition();
			ResetTradeState();
		}

		if (buySignal)
		{
			TryEnterLong(candle);
		}

		if (sellSignal)
		{
			TryEnterShort(candle);
		}

		if (Position > 0)
		{
			ApplyTrailingForLong(candle);
			if (CheckExitByLevels(candle, true))
				return;
		}
		else if (Position < 0)
		{
			ApplyTrailingForShort(candle);
			if (CheckExitByLevels(candle, false))
				return;
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (Position > 0)
			return;

		if (MaxOrdersCount == 0 || MaxBuyCount == 0)
			return;

		if (!CanEnter(candle.OpenTime, true))
			return;

		BuyMarket(BaseOrderVolume + Math.Max(0m, -Position));
		_entryPrice = candle.ClosePrice;
		_stopPrice = CalculateStopPrice(true, _entryPrice);
		_takeProfitPrice = CalculateTakeProfit(true, _entryPrice);
		_lastBuyBarTime = candle.OpenTime;
		if (CancelSleeping)
			_lastSellBarTime = null;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (Position < 0)
			return;

		if (MaxOrdersCount == 0 || MaxSellCount == 0)
			return;

		if (!CanEnter(candle.OpenTime, false))
			return;

		SellMarket(BaseOrderVolume + Math.Max(0m, Position));
		_entryPrice = candle.ClosePrice;
		_stopPrice = CalculateStopPrice(false, _entryPrice);
		_takeProfitPrice = CalculateTakeProfit(false, _entryPrice);
		_lastSellBarTime = candle.OpenTime;
		if (CancelSleeping)
			_lastBuyBarTime = null;
	}

	private bool CanEnter(DateTimeOffset barTime, bool isBuy)
	{
		if (MaxOrdersCount > 0 && Position != 0)
			return false;

		if (isBuy && MaxBuyCount > 0 && Position > 0)
			return false;

		if (!isBuy && MaxSellCount > 0 && Position < 0)
			return false;

		if (SleepBars <= 0)
			return true;

		var timeFrame = GetTimeFrame();
		if (timeFrame == null)
			return true;

		var lastTime = isBuy ? _lastBuyBarTime : _lastSellBarTime;
		if (lastTime == null)
			return true;

		var required = lastTime.Value + timeFrame.Value * SleepBars;
		return barTime >= required;
	}

	private decimal? CalculateStopPrice(bool isLong, decimal entryPrice)
	{
		if (StopLossPoints <= 0)
			return null;

		var distance = StopLossPoints * GetPriceStep();
		return isLong ? entryPrice - distance : entryPrice + distance;
	}

	private decimal? CalculateTakeProfit(bool isLong, decimal entryPrice)
	{
		if (TakeProfitPoints <= 0)
			return null;

		var distance = TakeProfitPoints * GetPriceStep();
		return isLong ? entryPrice + distance : entryPrice - distance;
	}

	private bool CheckExitByLevels(ICandleMessage candle, bool isLong)
	{
		if (isLong)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}
		else
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}

		return false;
	}

	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		var priceStep = GetPriceStep();
		var currentProfit = candle.HighPrice - _entryPrice;

		if (TrailingStopEnabled && TrailingDistancePoints > 0 && TrailingStartPoints > 0)
		{
			var activation = TrailingStartPoints * priceStep;
			if (currentProfit >= activation)
			{
				var trailDistance = TrailingDistancePoints * priceStep;
				var newStop = candle.HighPrice - trailDistance;
				if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
					_stopPrice = newStop;
			}
		}

		if (BreakEvenEnabled && BreakEvenStartPoints > 0)
		{
			var activation = BreakEvenStartPoints * priceStep;
			if (currentProfit >= activation)
			{
				var lockDistance = BreakEvenLockPoints * priceStep;
				var newStop = _entryPrice + lockDistance;
				if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
					_stopPrice = newStop;
			}
		}
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		var priceStep = GetPriceStep();
		var currentProfit = _entryPrice - candle.LowPrice;

		if (TrailingStopEnabled && TrailingDistancePoints > 0 && TrailingStartPoints > 0)
		{
			var activation = TrailingStartPoints * priceStep;
			if (currentProfit >= activation)
			{
				var trailDistance = TrailingDistancePoints * priceStep;
				var newStop = candle.LowPrice + trailDistance;
				if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
					_stopPrice = newStop;
			}
		}

		if (BreakEvenEnabled && BreakEvenStartPoints > 0)
		{
			var activation = BreakEvenStartPoints * priceStep;
			if (currentProfit >= activation)
			{
				var lockDistance = BreakEvenLockPoints * priceStep;
				var newStop = _entryPrice - lockDistance;
				if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
					_stopPrice = newStop;
			}
		}
	}

	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private static bool EvaluateSignal(IndicatorState state, int bufferIndex, int shift)
	{
		if (state == null || state.Count == 0)
			return false;

		var value = state.GetValue(bufferIndex, shift);
		return value.HasValue && value.Value != 0m;
	}

	private string BuildParameterString()
	{
		var tokens = new List<string>();
		if (!string.IsNullOrWhiteSpace(IndicatorParameters))
		{
			var parts = IndicatorParameters.Split('/');
			foreach (var part in parts)
			{
				var trimmed = part.Trim();
				if (trimmed.Length > 0)
					tokens.Add(trimmed);
			}
		}

		var overrides = GetOverrides();
		foreach (var item in overrides)
		{
			if (!item.use.Value)
				continue;

			if (item.index.Value < 0)
				continue;

			var value = item.value.Value.ToString(CultureInfo.InvariantCulture);
			if (item.index.Value < tokens.Count)
			{
				tokens[item.index.Value] = value;
			}
			else
			{
				while (tokens.Count < item.index.Value)
					tokens.Add("0");
				tokens.Add(value);
			}
		}

		return string.Join("/", tokens);
	}

	private IEnumerable<(StrategyParam<bool> use, StrategyParam<int> index, StrategyParam<decimal> value)> GetOverrides()
	{
		yield return (_override1Use, _override1Index, _override1Value);
		yield return (_override2Use, _override2Index, _override2Value);
		yield return (_override3Use, _override3Index, _override3Value);
		yield return (_override4Use, _override4Index, _override4Value);
		yield return (_override5Use, _override5Index, _override5Value);
	}

	private TimeSpan? GetTimeFrame()
	{
		return CandleType.Arg as TimeSpan?;
	}

	private decimal GetPriceForIndicator(ICandleMessage candle)
	{
		return candle.ClosePrice;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 1m;
	}

	private static IIndicator CreateIndicator(string name, string parameters)
	{
		if (string.IsNullOrWhiteSpace(name))
			return null;

		var type = Type.GetType(name, false, true);
		if (type == null)
			type = Type.GetType($"StockSharp.Algo.Indicators.{name}, StockSharp.Algo", false, true);

		if (type == null)
			throw new InvalidOperationException($"Cannot resolve indicator type '{name}'.");

		if (!typeof(IIndicator).IsAssignableFrom(type))
			throw new InvalidOperationException($"Type '{type.FullName}' does not implement IIndicator.");

		var indicator = (IIndicator)Activator.CreateInstance(type)!;
		ApplyParameters(indicator, parameters);
		return indicator;
	}

	private static void ApplyParameters(IIndicator indicator, string parameters)
	{
		if (string.IsNullOrWhiteSpace(parameters))
			return;

		var namedValues = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		var orderedValues = new List<string>();

		var parts = parameters.Split('/');
		foreach (var part in parts)
		{
			var trimmed = part.Trim();
			if (trimmed.Length == 0)
				continue;

			var eqIndex = trimmed.IndexOf('=');
			if (eqIndex > 0)
			{
				var propertyName = trimmed.Substring(0, eqIndex).Trim();
				var propertyValue = trimmed.Substring(eqIndex + 1).Trim();
				namedValues[propertyName] = propertyValue;
			}
			else
			{
				orderedValues.Add(trimmed);
			}
		}
	}

	private static object ConvertString(string value, Type targetType)
	{
		if (targetType == typeof(string))
			return value;

		if (targetType == typeof(int) || targetType == typeof(int?))
			return int.Parse(value, CultureInfo.InvariantCulture);

		if (targetType == typeof(decimal) || targetType == typeof(decimal?))
			return decimal.Parse(value, CultureInfo.InvariantCulture);

		if (targetType == typeof(double) || targetType == typeof(double?))
			return double.Parse(value, CultureInfo.InvariantCulture);

		if (targetType == typeof(bool) || targetType == typeof(bool?))
		{
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
				return numeric != 0;

			return bool.Parse(value);
		}

		if (targetType.IsEnum)
		{
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
				return Enum.ToObject(targetType, numeric);

			return Enum.Parse(targetType, value, true);
		}

		return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
	}
}