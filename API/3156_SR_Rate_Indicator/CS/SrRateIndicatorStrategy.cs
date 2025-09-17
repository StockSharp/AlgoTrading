using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the SR Rate Indicator expert logic from MetaTrader 5.
/// </summary>
public class SrRateIndicatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _windowSize;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;

	private SrRateIndicator? _indicator;
	private readonly List<decimal> _colorHistory = new();

	/// <summary>
	/// Volume used for every order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Allow opening long trades.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Allow opening short trades.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Allow closing long trades when the indicator reaches the opposite extreme.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Allow closing short trades when the indicator reaches the opposite extreme.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allowed slippage in points, kept for compatibility with the original expert.
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of closed bars to look back when evaluating the signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Window length used inside the SR Rate calculation.
	/// </summary>
	public int WindowSize
	{
		get => _windowSize.Value;
		set => _windowSize.Value = value;
	}

	/// <summary>
	/// Upper level that marks bullish extremes.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower level that marks bearish extremes.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="SrRateIndicatorStrategy"/>.
	/// </summary>
	public SrRateIndicatorStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume applied to every trade", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable Long Entries", "Allow the strategy to open long trades", "Trading");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable Short Entries", "Allow the strategy to open short trades", "Trading");

		_enableLongExits = Param(nameof(EnableLongExits), true)
		.SetDisplay("Enable Long Exits", "Close long trades on opposite signals", "Trading");

		_enableShortExits = Param(nameof(EnableShortExits), true)
		.SetDisplay("Enable Short Exits", "Close short trades on opposite signals", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss", "Stop loss distance in price points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(200, 2000, 200);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit", "Take profit distance in price points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(400, 4000, 200);

		_slippagePoints = Param(nameof(SlippagePoints), 10)
		.SetGreaterOrEqualZero()
		.SetDisplay("Slippage", "Maximum slippage accepted when closing positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for SR Rate calculations", "Data");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Bar", "How many closed bars to offset the signal", "Logic")
		.SetCanOptimize(true)
		.SetOptimize(1, 3, 1);

		_windowSize = Param(nameof(WindowSize), 20)
		.SetGreaterThanZero()
		.SetDisplay("Window Size", "Number of bars used in SR Rate normalization", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 2);

		_highLevel = Param(nameof(HighLevel), 20m)
		.SetDisplay("High Level", "Upper level that triggers long entries", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_lowLevel = Param(nameof(LowLevel), -20m)
		.SetDisplay("Low Level", "Lower level that triggers short entries", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(-40m, -10m, 5m);
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

		Volume = OrderVolume;
		_colorHistory.Clear();

		_indicator = new SrRateIndicator
		{
			WindowSize = WindowSize,
			HighLevel = HighLevel,
			LowLevel = LowLevel
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_indicator, ProcessCandle)
		.Start();

		var step = Security?.PriceStep ?? 0m;
		Unit? stopLossUnit = step > 0m && StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Absolute) : null;
		Unit? takeProfitUnit = step > 0m && TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Absolute) : null;

		if (stopLossUnit != null || takeProfitUnit != null)
		{
			// Apply the same protective levels used in the original expert advisor.
			StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!indicatorValue.IsFinal)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_indicator?.IsFormed != true)
		return;

		var srValue = (SrRateIndicatorValue)indicatorValue;
		var color = srValue.Color;

		_colorHistory.Add(color);

		var historyLimit = Math.Max(WindowSize + SignalBar + 10, 64);
		if (_colorHistory.Count > historyLimit)
		_colorHistory.RemoveRange(0, _colorHistory.Count - historyLimit);

		var offset = Math.Max(0, SignalBar - 1);
		var currentIndex = _colorHistory.Count - 1 - offset;
		if (currentIndex < 0 || currentIndex >= _colorHistory.Count)
		return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
		return;

		var currentColor = _colorHistory[currentIndex];
		var previousColor = _colorHistory[previousIndex];

		var closeShortSignal = currentColor == 4m;
		var closeLongSignal = currentColor == 0m;
		var openLongSignal = currentColor == 4m && previousColor < 4m;
		var openShortSignal = currentColor == 0m && previousColor > 0m;

		if (EnableShortExits && closeShortSignal && Position < 0m)
		{
			// Cover short positions when the oscillator reaches the bullish extreme.
			BuyMarket(Math.Abs(Position));
		}

		if (EnableLongExits && closeLongSignal && Position > 0m)
		{
			// Liquidate long positions when the oscillator reaches the bearish extreme.
			SellMarket(Position);
		}

		if (Position != 0m)
		return;

		if (EnableLongEntries && openLongSignal)
		{
			// Open a new long position after the color switches to strong bullish.
			BuyMarket();
		}
		else if (EnableShortEntries && openShortSignal)
		{
			// Open a new short position after the color switches to strong bearish.
			SellMarket();
		}
	}

	/// <inheritdoc />
	protected override void OnReset()
	{
		base.OnReset();
		_indicator = null;
		_colorHistory.Clear();
	}

	private sealed class SrRateIndicator : BaseIndicator<SrRateIndicatorValue>
	{
		private enum PriceMode
		{
			High,
			Low,
			Weighted
		}

		private readonly List<ICandleMessage> _candles = new();
		private readonly decimal[] _weights = CreateWeights();

		public int WindowSize { get; set; } = 20;
		public decimal HighLevel { get; set; } = 20m;
		public decimal LowLevel { get; set; } = -20m;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			_candles.Add(candle);

			var required = WindowSize + _weights.Length;
			var limit = required + 10;

			if (_candles.Count > limit)
			_candles.RemoveRange(0, _candles.Count - limit);

			if (_candles.Count < required)
			{
				IsFormed = false;
				return new SrRateIndicatorValue(this, input, 0m, 2m);
			}

			var rate = CalculateRate(0);
			var color = CalculateColor(rate);

			IsFormed = true;
			return new SrRateIndicatorValue(this, input, rate, color);
		}

		public override void Reset()
		{
			base.Reset();
			_candles.Clear();
		}

		private decimal CalculateRate(int shift)
		{
			var max = decimal.MinValue;
			var min = decimal.MaxValue;
			var end = shift + WindowSize;

			for (var index = shift; index < end; index++)
			{
				if (!HasEnoughData(index))
				return 0m;

				var low = Smooth(PriceMode.Low, index);
				if (low < min)
				min = low;

				var high = Smooth(PriceMode.High, index);
				if (high > max)
				max = high;
			}

			if (max <= min)
			return 0m;

			var weighted = Smooth(PriceMode.Weighted, shift);
			return 200m * (weighted - min) / (max - min) - 100m;
		}

		private decimal Smooth(PriceMode mode, int shift)
		{
			if (!HasEnoughData(shift))
			return 0m;

			decimal sum = 0m;

			for (var i = 0; i < _weights.Length; i++)
			{
				var candle = GetCandle(shift + i);
				sum += _weights[i] * GetPrice(candle, mode);
			}

			return sum;
		}

		private bool HasEnoughData(int shift)
		{
			return _candles.Count >= shift + _weights.Length;
		}

		private ICandleMessage GetCandle(int shift)
		{
			var index = _candles.Count - 1 - shift;
			return _candles[index];
		}

		private decimal CalculateColor(decimal rate)
		{
			if (rate > 0m)
			return rate > HighLevel ? 4m : 3m;

			if (rate < 0m)
			return rate < LowLevel ? 0m : 1m;

			return 2m;
		}

		private static decimal GetPrice(ICandleMessage candle, PriceMode mode)
		{
			return mode switch
			{
				PriceMode.High => candle.HighPrice,
				PriceMode.Low => candle.LowPrice,
				PriceMode.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				_ => candle.ClosePrice
			};
		}

		private static decimal[] CreateWeights()
		{
			var weights = new decimal[6];

			for (var i = 0; i < weights.Length; i++)
			{
				var value = Math.Exp(-1d * i * i * 9d / Math.Pow(6d, 2d));
				weights[i] = (decimal)value;
			}

			var sum = weights.Sum();

			for (var i = 0; i < weights.Length; i++)
			weights[i] /= sum;

			return weights;
		}
	}

	private sealed class SrRateIndicatorValue : ComplexIndicatorValue
	{
		public SrRateIndicatorValue(IIndicator indicator, IIndicatorValue input, decimal rate, decimal color)
		: base(indicator, input, (nameof(Rate), rate), (nameof(Color), color))
		{
		}

		public decimal Rate => (decimal)GetValue(nameof(Rate));

		public decimal Color => (decimal)GetValue(nameof(Color));
	}
}
