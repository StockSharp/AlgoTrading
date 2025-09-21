using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-layer adaptation of the XWAMI indicator strategy with money management recounter.
/// Each layer emulates a dedicated MagicNumber instance from the MQL version.
/// </summary>
public class XwamiMultiLayerMmrecStrategy : Strategy
{
	private readonly LayerSettings _layerA;
	private readonly LayerSettings _layerB;
	private readonly LayerSettings _layerC;

	private readonly List<XwamiLayer> _layers = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="XwamiMultiLayerMmrecStrategy"/> class.
	/// </summary>
	public XwamiMultiLayerMmrecStrategy()
	{
		_layerA = new LayerSettings(this, "A", TimeSpan.FromHours(8), 0.1m, 0.01m, 5, 3, 5, 3, 3000, 10000, "Layer A");
		_layerB = new LayerSettings(this, "B", TimeSpan.FromHours(4), 0.1m, 0.01m, 5, 3, 5, 3, 2000, 6000, "Layer B");
		_layerC = new LayerSettings(this, "C", TimeSpan.FromHours(1), 0.1m, 0.01m, 5, 3, 5, 3, 1000, 3000, "Layer C");
	}

	/// <summary>
	/// Candle type for layer A.
	/// </summary>
	public DataType LayerACandleType
	{
		get => _layerA.CandleType.Value;
		set => _layerA.CandleType.Value = value;
	}

	/// <summary>
	/// Candle type for layer B.
	/// </summary>
	public DataType LayerBCandleType
	{
		get => _layerB.CandleType.Value;
		set => _layerB.CandleType.Value = value;
	}

	/// <summary>
	/// Candle type for layer C.
	/// </summary>
	public DataType LayerCCandleType
	{
		get => _layerC.CandleType.Value;
		set => _layerC.CandleType.Value = value;
	}

	/// <summary>
	/// Period shift for the momentum leg of layer A.
	/// </summary>
	public int LayerAPeriod
	{
		get => _layerA.Period.Value;
		set => _layerA.Period.Value = value;
	}

	/// <summary>
	/// Period shift for the momentum leg of layer B.
	/// </summary>
	public int LayerBPeriod
	{
		get => _layerB.Period.Value;
		set => _layerB.Period.Value = value;
	}

	/// <summary>
	/// Period shift for the momentum leg of layer C.
	/// </summary>
	public int LayerCPeriod
	{
		get => _layerC.Period.Value;
		set => _layerC.Period.Value = value;
	}

	/// <summary>
	/// Applied price used by layer A.
	/// </summary>
	public XwamiAppliedPrice LayerAAppliedPrice
	{
		get => _layerA.AppliedPrice.Value;
		set => _layerA.AppliedPrice.Value = value;
	}

	/// <summary>
	/// Applied price used by layer B.
	/// </summary>
	public XwamiAppliedPrice LayerBAppliedPrice
	{
		get => _layerB.AppliedPrice.Value;
		set => _layerB.AppliedPrice.Value = value;
	}

	/// <summary>
	/// Applied price used by layer C.
	/// </summary>
	public XwamiAppliedPrice LayerCAppliedPrice
	{
		get => _layerC.AppliedPrice.Value;
		set => _layerC.AppliedPrice.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, _layerA.CandleType.Value), (Security, _layerB.CandleType.Value), (Security, _layerC.CandleType.Value)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_layers.Clear();

		_layers.Add(CreateLayer(_layerA, "A"));
		_layers.Add(CreateLayer(_layerB, "B"));
		_layers.Add(CreateLayer(_layerC, "C"));
	}

	private XwamiLayer CreateLayer(LayerSettings settings, string name)
	{
		var layer = new XwamiLayer(this, settings, name);
		var subscription = SubscribeCandles(settings.CandleType.Value);
		subscription.Bind(layer.ProcessCandle).Start();
		return layer;
	}

	internal bool IsTradingAllowed()
	{
		return IsFormedAndOnlineAndAllowTrading();
	}

	internal void RequestCloseLong(XwamiLayer layer, decimal price)
	{
		if (!layer.CloseLong(price))
		return;

		UpdateTargetPosition();
	}

	internal void RequestCloseShort(XwamiLayer layer, decimal price)
	{
		if (!layer.CloseShort(price))
		return;

		UpdateTargetPosition();
	}

	internal void RequestOpenLong(XwamiLayer layer, decimal price)
	{
		var changed = false;

		foreach (var other in _layers)
		{
			if (other == layer)
			continue;

			if (other.HasShortPosition && other.CloseShort(price))
			changed = true;
		}

		if (layer.HasShortPosition && layer.CloseShort(price))
		changed = true;

		if (layer.OpenLong(price))
		changed = true;

		if (changed)
		UpdateTargetPosition();
	}

	internal void RequestOpenShort(XwamiLayer layer, decimal price)
	{
		var changed = false;

		foreach (var other in _layers)
		{
			if (other == layer)
			continue;

			if (other.HasLongPosition && other.CloseLong(price))
			changed = true;
		}

		if (layer.HasLongPosition && layer.CloseLong(price))
		changed = true;

		if (layer.OpenShort(price))
		changed = true;

		if (changed)
		UpdateTargetPosition();
	}

	private void UpdateTargetPosition()
	{
		decimal target = 0m;

		foreach (var layer in _layers)
		target += layer.CurrentPosition;

		var diff = target - Position;

		if (diff > 0)
		BuyMarket(diff);
		else if (diff < 0)
		SellMarket(-diff);
	}

	internal decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 0m;
	}

	private sealed class LayerSettings
	{
		public LayerSettings(
		XwamiMultiLayerMmrecStrategy strategy,
		string key,
		TimeSpan defaultFrame,
		decimal normalVolume,
		decimal smallVolume,
		int buyTotalTrigger,
		int buyLossTrigger,
		int sellTotalTrigger,
		int sellLossTrigger,
		int stopLossPoints,
		int takeProfitPoints,
		string displayName)
		{
			CandleType = strategy.Param($"Layer{key}CandleType", defaultFrame.TimeFrame())
			.SetDisplay($"{displayName} Candle", $"Candle type for {displayName}", displayName);

			Period = strategy.Param($"Layer{key}Period", 1)
			.SetNotNegative()
			.SetDisplay($"{displayName} Period", $"Momentum lookback for {displayName}", displayName);

			Method1 = strategy.Param($"Layer{key}Method1", XwamiSmoothMethod.T3)
			.SetDisplay($"{displayName} Method 1", $"Primary smoothing method for {displayName}", displayName);
			Length1 = strategy.Param($"Layer{key}Length1", 4)
			.SetGreaterThanZero()
			.SetDisplay($"{displayName} Length 1", $"Primary smoothing length for {displayName}", displayName);
			Phase1 = strategy.Param($"Layer{key}Phase1", 15)
			.SetDisplay($"{displayName} Phase 1", $"Primary smoothing phase for {displayName}", displayName);

			Method2 = strategy.Param($"Layer{key}Method2", XwamiSmoothMethod.Jurik)
			.SetDisplay($"{displayName} Method 2", $"Second smoothing method for {displayName}", displayName);
			Length2 = strategy.Param($"Layer{key}Length2", 13)
			.SetGreaterThanZero()
			.SetDisplay($"{displayName} Length 2", $"Second smoothing length for {displayName}", displayName);
			Phase2 = strategy.Param($"Layer{key}Phase2", 15)
			.SetDisplay($"{displayName} Phase 2", $"Second smoothing phase for {displayName}", displayName);

			Method3 = strategy.Param($"Layer{key}Method3", XwamiSmoothMethod.Jurik)
			.SetDisplay($"{displayName} Method 3", $"Third smoothing method for {displayName}", displayName);
			Length3 = strategy.Param($"Layer{key}Length3", 13)
			.SetGreaterThanZero()
			.SetDisplay($"{displayName} Length 3", $"Third smoothing length for {displayName}", displayName);
			Phase3 = strategy.Param($"Layer{key}Phase3", 15)
			.SetDisplay($"{displayName} Phase 3", $"Third smoothing phase for {displayName}", displayName);

			Method4 = strategy.Param($"Layer{key}Method4", XwamiSmoothMethod.Jurik)
			.SetDisplay($"{displayName} Method 4", $"Fourth smoothing method for {displayName}", displayName);
			Length4 = strategy.Param($"Layer{key}Length4", 4)
			.SetGreaterThanZero()
			.SetDisplay($"{displayName} Length 4", $"Fourth smoothing length for {displayName}", displayName);
			Phase4 = strategy.Param($"Layer{key}Phase4", 15)
			.SetDisplay($"{displayName} Phase 4", $"Fourth smoothing phase for {displayName}", displayName);

			AppliedPrice = strategy.Param($"Layer{key}AppliedPrice", XwamiAppliedPrice.Close)
			.SetDisplay($"{displayName} Price", $"Applied price for {displayName}", displayName);

			SignalBar = strategy.Param($"Layer{key}SignalBar", 1)
			.SetNotNegative()
			.SetDisplay($"{displayName} Signal Bar", $"Shift for signal extraction in {displayName}", displayName);

			AllowBuyOpen = strategy.Param($"Layer{key}AllowBuyOpen", true)
			.SetDisplay($"{displayName} Allow Buy Open", $"Enable long entries for {displayName}", displayName);
			AllowSellOpen = strategy.Param($"Layer{key}AllowSellOpen", true)
			.SetDisplay($"{displayName} Allow Sell Open", $"Enable short entries for {displayName}", displayName);
			AllowBuyClose = strategy.Param($"Layer{key}AllowBuyClose", true)
			.SetDisplay($"{displayName} Allow Buy Close", $"Enable long exits for {displayName}", displayName);
			AllowSellClose = strategy.Param($"Layer{key}AllowSellClose", true)
			.SetDisplay($"{displayName} Allow Sell Close", $"Enable short exits for {displayName}", displayName);

			NormalVolume = strategy.Param($"Layer{key}NormalVolume", normalVolume)
			.SetGreaterThanZero()
			.SetDisplay($"{displayName} Normal Volume", $"Default volume for {displayName}", displayName);
			SmallVolume = strategy.Param($"Layer{key}SmallVolume", smallVolume)
			.SetGreaterThanZero()
			.SetDisplay($"{displayName} Reduced Volume", $"Reduced volume after losses for {displayName}", displayName);

			BuyTotalTrigger = strategy.Param($"Layer{key}BuyTotalTrigger", buyTotalTrigger)
			.SetGreaterThanOrEqual(1)
			.SetDisplay($"{displayName} Buy Total Trigger", $"Number of historical buy trades evaluated for {displayName}", displayName);
			BuyLossTrigger = strategy.Param($"Layer{key}BuyLossTrigger", buyLossTrigger)
			.SetNotNegative()
			.SetDisplay($"{displayName} Buy Loss Trigger", $"Loss threshold that activates reduced volume for {displayName}", displayName);

			SellTotalTrigger = strategy.Param($"Layer{key}SellTotalTrigger", sellTotalTrigger)
			.SetGreaterThanOrEqual(1)
			.SetDisplay($"{displayName} Sell Total Trigger", $"Number of historical sell trades evaluated for {displayName}", displayName);
			SellLossTrigger = strategy.Param($"Layer{key}SellLossTrigger", sellLossTrigger)
			.SetNotNegative()
			.SetDisplay($"{displayName} Sell Loss Trigger", $"Loss threshold that activates reduced volume for {displayName}", displayName);

			StopLossPoints = strategy.Param($"Layer{key}StopLossPoints", stopLossPoints)
			.SetNotNegative()
			.SetDisplay($"{displayName} Stop Loss", $"Stop loss in points for {displayName}", displayName);
			TakeProfitPoints = strategy.Param($"Layer{key}TakeProfitPoints", takeProfitPoints)
			.SetNotNegative()
			.SetDisplay($"{displayName} Take Profit", $"Take profit in points for {displayName}", displayName);
		}

		public StrategyParam<DataType> CandleType { get; }
		public StrategyParam<int> Period { get; }
		public StrategyParam<XwamiSmoothMethod> Method1 { get; }
		public StrategyParam<int> Length1 { get; }
		public StrategyParam<int> Phase1 { get; }
		public StrategyParam<XwamiSmoothMethod> Method2 { get; }
		public StrategyParam<int> Length2 { get; }
		public StrategyParam<int> Phase2 { get; }
		public StrategyParam<XwamiSmoothMethod> Method3 { get; }
		public StrategyParam<int> Length3 { get; }
		public StrategyParam<int> Phase3 { get; }
		public StrategyParam<XwamiSmoothMethod> Method4 { get; }
		public StrategyParam<int> Length4 { get; }
		public StrategyParam<int> Phase4 { get; }
		public StrategyParam<XwamiAppliedPrice> AppliedPrice { get; }
		public StrategyParam<int> SignalBar { get; }
		public StrategyParam<bool> AllowBuyOpen { get; }
		public StrategyParam<bool> AllowSellOpen { get; }
		public StrategyParam<bool> AllowBuyClose { get; }
		public StrategyParam<bool> AllowSellClose { get; }
		public StrategyParam<decimal> NormalVolume { get; }
		public StrategyParam<decimal> SmallVolume { get; }
		public StrategyParam<int> BuyTotalTrigger { get; }
		public StrategyParam<int> BuyLossTrigger { get; }
		public StrategyParam<int> SellTotalTrigger { get; }
		public StrategyParam<int> SellLossTrigger { get; }
		public StrategyParam<int> StopLossPoints { get; }
		public StrategyParam<int> TakeProfitPoints { get; }
	}

	private sealed class XwamiLayer
	{
		private readonly XwamiMultiLayerMmrecStrategy _strategy;
		private readonly LayerSettings _settings;
		private readonly DifferenceCalculator _difference;
		private readonly IXwamiFilter _filter1;
		private readonly IXwamiFilter _filter2;
		private readonly IXwamiFilter _filter3;
		private readonly IXwamiFilter _filter4;
		private readonly List<(decimal up, decimal down)> _history = new();
		private readonly List<bool> _buyLosses = new();
		private readonly List<bool> _sellLosses = new();

		private decimal _position;
		private decimal? _entryPrice;

		public XwamiLayer(XwamiMultiLayerMmrecStrategy strategy, LayerSettings settings, string name)
		{
			_strategy = strategy;
			_settings = settings;
			_difference = new DifferenceCalculator(settings.Period.Value);
			_filter1 = CreateFilter(settings.Method1.Value, settings.Length1.Value, settings.Phase1.Value);
			_filter2 = CreateFilter(settings.Method2.Value, settings.Length2.Value, settings.Phase2.Value);
			_filter3 = CreateFilter(settings.Method3.Value, settings.Length3.Value, settings.Phase3.Value);
			_filter4 = CreateFilter(settings.Method4.Value, settings.Length4.Value, settings.Phase4.Value);
		}

		public decimal CurrentPosition => _position;
		public bool HasLongPosition => _position > 0;
		public bool HasShortPosition => _position < 0;

		public void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (!_strategy.IsTradingAllowed())
			return;

			var price = GetAppliedPrice(candle, _settings.AppliedPrice.Value);
			var diff = _difference.Process(price);

			if (diff is null)
			return;

			var time = candle.CloseTime;
			var value1 = ProcessFilter(_filter1, diff.Value, time);
			if (value1 is null)
			return;

			var value2 = ProcessFilter(_filter2, value1.Value, time);
			if (value2 is null)
			return;

			var value3 = ProcessFilter(_filter3, value2.Value, time);
			if (value3 is null)
			return;

			var value4 = ProcessFilter(_filter4, value3.Value, time);
			if (value4 is null)
			return;

			_history.Insert(0, (value3.Value, value4.Value));
			var need = _settings.SignalBar.Value + 2;
			if (_history.Count > need)
			_history.RemoveAt(_history.Count - 1);

			if (_history.Count < need)
			return;

			CheckStops(candle);

			var current = _history[_settings.SignalBar.Value];
			var previous = _history[_settings.SignalBar.Value + 1];

			if (previous.up > previous.down)
			{
				if (_settings.AllowSellClose.Value)
				_strategy.RequestCloseShort(this, candle.ClosePrice);

				if (_settings.AllowBuyOpen.Value && current.up <= current.down)
				_strategy.RequestOpenLong(this, candle.ClosePrice);
			}

			if (previous.up < previous.down)
			{
				if (_settings.AllowBuyClose.Value)
				_strategy.RequestCloseLong(this, candle.ClosePrice);

				if (_settings.AllowSellOpen.Value && current.up >= current.down)
				_strategy.RequestOpenShort(this, candle.ClosePrice);
			}
		}

		public bool OpenLong(decimal price)
		{
			if (HasLongPosition)
			return false;

			var volume = GetNextVolume(true);
			if (volume <= 0)
			return false;

			_position = volume;
			_entryPrice = price;
			return true;
		}

		public bool OpenShort(decimal price)
		{
			if (HasShortPosition)
			return false;

			var volume = GetNextVolume(false);
			if (volume <= 0)
			return false;

			_position = -volume;
			_entryPrice = price;
			return true;
		}

		public bool CloseLong(decimal price)
		{
			if (!HasLongPosition)
			return false;

			var volume = _position;
			var entry = _entryPrice ?? price;
			var pnl = (price - entry) * volume;
			RecordTrade(true, pnl < 0m);
			_position = 0m;
			_entryPrice = null;
			return true;
		}

		public bool CloseShort(decimal price)
		{
			if (!HasShortPosition)
			return false;

			var volume = Math.Abs(_position);
			var entry = _entryPrice ?? price;
			var pnl = (entry - price) * volume;
			RecordTrade(false, pnl < 0m);
			_position = 0m;
			_entryPrice = null;
			return true;
		}

		private void CheckStops(ICandleMessage candle)
		{
			var step = _strategy.GetPriceStep();
			if (step <= 0m)
			return;

			if (HasLongPosition && _entryPrice is decimal entryLong)
			{
				var stop = _settings.StopLossPoints.Value * step;
				if (stop > 0m && candle.LowPrice <= entryLong - stop)
				{
					_strategy.RequestCloseLong(this, entryLong - stop);
					return;
				}

				var take = _settings.TakeProfitPoints.Value * step;
				if (take > 0m && candle.HighPrice >= entryLong + take)
				{
					_strategy.RequestCloseLong(this, entryLong + take);
				}
			}
			else if (HasShortPosition && _entryPrice is decimal entryShort)
			{
				var stop = _settings.StopLossPoints.Value * step;
				if (stop > 0m && candle.HighPrice >= entryShort + stop)
				{
					_strategy.RequestCloseShort(this, entryShort + stop);
					return;
				}

				var take = _settings.TakeProfitPoints.Value * step;
				if (take > 0m && candle.LowPrice <= entryShort - take)
				{
					_strategy.RequestCloseShort(this, entryShort - take);
				}
			}
		}

		private decimal GetNextVolume(bool isBuy)
		{
			var totalTrigger = isBuy ? _settings.BuyTotalTrigger.Value : _settings.SellTotalTrigger.Value;
			var lossTrigger = isBuy ? _settings.BuyLossTrigger.Value : _settings.SellLossTrigger.Value;
			var history = isBuy ? _buyLosses : _sellLosses;
			var losses = 0;
			var count = 0;

			foreach (var loss in history)
			{
				count++;
				if (loss)
				losses++;

				if (count >= totalTrigger)
				break;
			}

			if (lossTrigger > 0 && losses >= lossTrigger)
			return _settings.SmallVolume.Value;

			return _settings.NormalVolume.Value;
		}

		private void RecordTrade(bool isBuy, bool isLoss)
		{
			var history = isBuy ? _buyLosses : _sellLosses;
			history.Insert(0, isLoss);
			var limit = isBuy ? _settings.BuyTotalTrigger.Value : _settings.SellTotalTrigger.Value;
			if (history.Count > limit)
			history.RemoveAt(history.Count - 1);
		}

		private static decimal? ProcessFilter(IXwamiFilter filter, decimal value, DateTimeOffset time)
		{
			var result = filter.Process(value, time);
			return filter.IsFormed ? result : null;
		}

		private static decimal GetAppliedPrice(ICandleMessage candle, XwamiAppliedPrice type)
		{
			return type switch
			{
				XwamiAppliedPrice.Close => candle.ClosePrice,
				XwamiAppliedPrice.Open => candle.OpenPrice,
				XwamiAppliedPrice.High => candle.HighPrice,
				XwamiAppliedPrice.Low => candle.LowPrice,
				XwamiAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
				XwamiAppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
				XwamiAppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				XwamiAppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
				XwamiAppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				XwamiAppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
				XwamiAppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
				XwamiAppliedPrice.Demark => CalculateDemarkPrice(candle),
				_ => candle.ClosePrice,
			};
		}

		private static decimal CalculateDemarkPrice(ICandleMessage candle)
		{
			var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

			if (candle.ClosePrice < candle.OpenPrice)
			res = (res + candle.LowPrice) / 2m;
			else if (candle.ClosePrice > candle.OpenPrice)
			res = (res + candle.HighPrice) / 2m;
			else
			res = (res + candle.ClosePrice) / 2m;

			return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
		}

		private IXwamiFilter CreateFilter(XwamiSmoothMethod method, int length, int phase)
		{
			return method switch
			{
				XwamiSmoothMethod.Sma => new IndicatorWrapper(new SimpleMovingAverage { Length = length }),
				XwamiSmoothMethod.Ema => new IndicatorWrapper(new ExponentialMovingAverage { Length = length }),
				XwamiSmoothMethod.Smma => new IndicatorWrapper(new SmoothedMovingAverage { Length = length }),
				XwamiSmoothMethod.Lwma => new IndicatorWrapper(new WeightedMovingAverage { Length = length }),
				XwamiSmoothMethod.Jurik => new IndicatorWrapper(new JurikMovingAverage { Length = length }),
				XwamiSmoothMethod.T3 => new TillsonT3Filter(length, phase),
				_ => new IndicatorWrapper(new ExponentialMovingAverage { Length = length }),
			};
		}
	}

	private interface IXwamiFilter
	{
		decimal Process(decimal value, DateTimeOffset time);
		bool IsFormed { get; }
	}

	private sealed class IndicatorWrapper : IXwamiFilter
	{
		private readonly IIndicator _indicator;

		public IndicatorWrapper(IIndicator indicator)
		{
			_indicator = indicator;
		}

		public bool IsFormed => _indicator.IsFormed;

		public decimal Process(decimal value, DateTimeOffset time)
		{
			return _indicator.Process(value, time, true).ToDecimal();
		}
	}

	private sealed class TillsonT3Filter : IXwamiFilter
	{
		private readonly decimal _w1;
		private readonly decimal _w2;
		private readonly decimal _c1;
		private readonly decimal _c2;
		private readonly decimal _c3;
		private readonly decimal _c4;
		private decimal _e1;
		private decimal _e2;
		private decimal _e3;
		private decimal _e4;
		private decimal _e5;
		private decimal _e6;
		private bool _initialized;

		public TillsonT3Filter(int length, int curvature)
		{
			var b = curvature / 100m;
			var b2 = b * b;
			var b3 = b2 * b;
			_c1 = -b3;
			_c2 = 3m * (b2 + b3);
			_c3 = -3m * (2m * b2 + b + b3);
			_c4 = 1m + 3m * b + b3 + 3m * b2;
			var n = 1m + 0.5m * (length - 1m);
			_w1 = 2m / (n + 1m);
			_w2 = 1m - _w1;
		}

		public bool IsFormed => _initialized;

		public decimal Process(decimal value, DateTimeOffset time)
		{
			if (!_initialized)
			{
				_e1 = _e2 = _e3 = _e4 = _e5 = _e6 = value;
				_initialized = true;
			}

			_e1 = _w1 * value + _w2 * _e1;
			_e2 = _w1 * _e1 + _w2 * _e2;
			_e3 = _w1 * _e2 + _w2 * _e3;
			_e4 = _w1 * _e3 + _w2 * _e4;
			_e5 = _w1 * _e4 + _w2 * _e5;
			_e6 = _w1 * _e5 + _w2 * _e6;

			return _c1 * _e6 + _c2 * _e5 + _c3 * _e4 + _c4 * _e3;
		}
	}

	private sealed class DifferenceCalculator
	{
		private readonly int _period;
		private readonly Queue<decimal> _buffer = new();

		public DifferenceCalculator(int period)
		{
			_period = Math.Max(0, period);
		}

		public decimal? Process(decimal value)
		{
			if (_period == 0)
			return 0m;

			_buffer.Enqueue(value);

			if (_buffer.Count <= _period)
			return null;

			if (_buffer.Count > _period + 1)
			_buffer.Dequeue();

			var previous = _buffer.Peek();
			return value - previous;
		}
	}
}

/// <summary>
/// Smoothing methods supported by the XWAMI adaptation.
/// </summary>
public enum XwamiSmoothMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Sma,
	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Ema,
	/// <summary>
	/// Smoothed moving average (RMA).
	/// </summary>
	Smma,
	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma,
	/// <summary>
	/// Jurik moving average.
	/// </summary>
	Jurik,
	/// <summary>
	/// Tillson T3 filter.
	/// </summary>
	T3
}

/// <summary>
/// Applied price variations supported by the strategy.
/// </summary>
public enum XwamiAppliedPrice
{
	/// <summary>
	/// Close price.
	/// </summary>
	Close,
	/// <summary>
	/// Open price.
	/// </summary>
	Open,
	/// <summary>
	/// High price.
	/// </summary>
	High,
	/// <summary>
	/// Low price.
	/// </summary>
	Low,
	/// <summary>
	/// Median price (H+L)/2.
	/// </summary>
	Median,
	/// <summary>
	/// Typical price (H+L+C)/3.
	/// </summary>
	Typical,
	/// <summary>
	/// Weighted close (2*C+H+L)/4.
	/// </summary>
	Weighted,
	/// <summary>
	/// Simple average of open and close.
	/// </summary>
	Simple,
	/// <summary>
	/// Quarter price (H+L+O+C)/4.
	/// </summary>
	Quarter,
	/// <summary>
	/// Trend following definition #0.
	/// </summary>
	TrendFollow0,
	/// <summary>
	/// Trend following definition #1.
	/// </summary>
	TrendFollow1,
	/// <summary>
	/// Demark pivot based price.
	/// </summary>
	Demark
}
