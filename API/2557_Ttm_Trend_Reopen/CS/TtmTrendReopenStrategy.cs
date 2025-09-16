using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TTM Trend strategy with re-entry logic inspired by the MetaTrader expert.
/// Opens positions when the TTM Trend color flips and pyramids after price moves far enough.
/// </summary>
public class TtmTrendReopenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _compBars;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _priceStepPoints;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private TtmTrendIndicator? _ttmIndicator;
	private readonly List<int> _colorHistory = new();
	private int _longEntries;
	private int _shortEntries;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of Heikin-Ashi comparison bars maintained by the indicator.
	/// </summary>
	public int CompBars
	{
		get => _compBars.Value;
		set => _compBars.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Offset of the bar used for signal detection (0 = latest closed candle).
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Minimum favorable move in points before adding to an existing position.
	/// </summary>
	public decimal PriceStepPoints
	{
		get => _priceStepPoints.Value;
		set => _priceStepPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Maximum number of entries per direction (including the first one).
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Allow opening new long positions on bullish colors.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Allow opening new short positions on bearish colors.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Allow closing long positions when a bearish color appears.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Allow closing short positions when a bullish color appears.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TtmTrendReopenStrategy"/>.
	/// </summary>
	public TtmTrendReopenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for the TTM Trend calculation", "General");

		_compBars = Param(nameof(CompBars), 6)
		.SetGreaterThanZero()
		.SetDisplay("Comparison Bars", "Heikin-Ashi bars stored for the color smoothing", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(3, 12, 1);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Offset of the bar used for trading decisions", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);

		_priceStepPoints = Param(nameof(PriceStepPoints), 300m)
		.SetNotNegative()
		.SetDisplay("Re-entry Step", "Minimum favorable move (in points) before pyramiding", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(100m, 600m, 100m);

		_maxPositions = Param(nameof(MaxPositions), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Entries", "Maximum number of stacked entries per direction", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable Long Entries", "Allow buying when the TTM Trend turns bullish", "Trading Rules");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable Short Entries", "Allow selling when the TTM Trend turns bearish", "Trading Rules");

		_enableLongExits = Param(nameof(EnableLongExits), true)
		.SetDisplay("Enable Long Exits", "Close longs on bearish colors", "Trading Rules");

		_enableShortExits = Param(nameof(EnableShortExits), true)
		.SetDisplay("Enable Short Exits", "Close shorts on bullish colors", "Trading Rules");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Protective stop distance in price points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(200m, 2000m, 200m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Profit target distance in price points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(500m, 4000m, 500m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_colorHistory.Clear();
		_longEntries = 0;
		_shortEntries = 0;

		_ttmIndicator = new TtmTrendIndicator
		{
			CompBars = CompBars
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_ttmIndicator, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ttmIndicator);
			DrawOwnTrades(area);
		}

		var step = Security?.PriceStep ?? 1m;
		Unit? stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Absolute) : null;
		Unit? takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Absolute) : null;

		if (stopLossUnit != null || takeProfitUnit != null)
		StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ttmValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!ttmValue.IsFinal)
		return;

		var colorDecimal = ttmValue.GetValue<decimal>();
		var color = (int)Math.Round(colorDecimal);
		_colorHistory.Add(color);

		var offset = Math.Max(0, SignalBar - 1);
		var signalIndex = _colorHistory.Count - 1 - offset;
		if (signalIndex < 0)
		return;

		var currentColor = _colorHistory[signalIndex];
		int? previousColor = signalIndex > 0 ? _colorHistory[signalIndex - 1] : null;

		var isBullishColor = currentColor is 1 or 4;
		var isBearishColor = currentColor is 0 or 3;

		var wasBullish = previousColor.HasValue && (previousColor.Value == 1 || previousColor.Value == 4);
		var wasBearish = previousColor.HasValue && (previousColor.Value == 0 || previousColor.Value == 3);

		// Close existing positions before opening new ones.
		if (EnableLongExits && isBearishColor && Position > 0)
		{
			SellMarket(Position);
			_longEntries = 0;
		}

		if (EnableShortExits && isBullishColor && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_shortEntries = 0;
		}

		// Open a fresh long when the color flips to bullish.
		if (EnableLongEntries && isBullishColor && previousColor.HasValue && !wasBullish && Position <= 0)
		{
			var quantity = Volume + Math.Max(0m, -Position);
			BuyMarket(quantity);
			_longEntries = 1;
			_shortEntries = 0;
		}
		// Open a fresh short when the color flips to bearish.
		else if (EnableShortEntries && isBearishColor && previousColor.HasValue && !wasBearish && Position >= 0)
		{
			var quantity = Volume + Math.Max(0m, Position);
			SellMarket(quantity);
			_shortEntries = 1;
			_longEntries = 0;
		}

		var step = Security?.PriceStep ?? 1m;
		var reentryStep = PriceStepPoints * step;

		// Add to an existing long once price moves in favor.
		if (EnableLongEntries && Position > 0 && reentryStep > 0m && _longEntries > 0 && _longEntries < MaxPositions)
		{
			var distance = candle.ClosePrice - PositionPrice;
			if (distance >= reentryStep)
			{
				BuyMarket(Volume);
				_longEntries++;
			}
		}
		// Add to an existing short once price moves in favor.
		else if (EnableShortEntries && Position < 0 && reentryStep > 0m && _shortEntries > 0 && _shortEntries < MaxPositions)
		{
			var distance = PositionPrice - candle.ClosePrice;
			if (distance >= reentryStep)
			{
				SellMarket(Volume);
				_shortEntries++;
			}
		}

		var keep = Math.Max(offset + 2, 3);
		if (_colorHistory.Count > keep)
		_colorHistory.RemoveRange(0, _colorHistory.Count - keep);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_longEntries = 0;
			_shortEntries = 0;
		}
	}

	/// <summary>
	/// Internal indicator reproducing the MetaTrader TTM Trend color output.
	/// </summary>
	private sealed class TtmTrendIndicator : BaseIndicator<decimal>
	{
		private readonly LinkedList<TtmEntry> _history = new();

		public int CompBars { get; set; } = 6;

		private decimal? _prevHaOpen;
		private decimal? _prevHaClose;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time, isFinal: false);

			var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
			decimal haOpen;

			if (_prevHaOpen is null || _prevHaClose is null)
			{
				haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			}
			else
			{
				haOpen = (_prevHaOpen.Value + _prevHaClose.Value) / 2m;
			}

			_prevHaOpen = haOpen;
			_prevHaClose = haClose;

			var color = CalculateBaseColor(candle, haOpen, haClose);

			foreach (var entry in _history)
			{
				var high = Math.Max(entry.HaOpen, entry.HaClose);
				var low = Math.Min(entry.HaOpen, entry.HaClose);

				if (haOpen <= high && haOpen >= low && haClose <= high && haClose >= low)
				{
					color = entry.Color;
					break;
				}
			}

			_history.AddFirst(new TtmEntry(haOpen, haClose, color));

			while (_history.Count > Math.Max(1, CompBars))
			_history.RemoveLast();

			return new DecimalIndicatorValue(this, color, input.Time);
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();
			_history.Clear();
			_prevHaOpen = null;
			_prevHaClose = null;
		}

		private static int CalculateBaseColor(ICandleMessage candle, decimal haOpen, decimal haClose)
		{
			const int neutral = 2;

			if (haClose > haOpen)
			return candle.OpenPrice <= candle.ClosePrice ? 4 : 3;

			if (haClose < haOpen)
			return candle.OpenPrice > candle.ClosePrice ? 0 : 1;

			return neutral;
		}

		private readonly record struct TtmEntry(decimal HaOpen, decimal HaClose, int Color);
	}
}
