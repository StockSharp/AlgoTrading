using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Fisher Transform indicator.
/// Replicates the logic of the MQL5 expert Exp_ColorFisher_m11 with configurable entries and exits.
/// </summary>
public class ColorFisherM11Strategy : Strategy
{
	private readonly StrategyParam<int> _rangePeriods;
	private readonly StrategyParam<decimal> _priceSmoothing;
	private readonly StrategyParam<decimal> _indexSmoothing;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyEntry;
	private readonly StrategyParam<bool> _enableSellEntry;
	private readonly StrategyParam<bool> _enableBuyExit;
	private readonly StrategyParam<bool> _enableSellExit;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ColorFisherM11Indicator _colorFisher;
	private readonly List<int> _colorHistory = new();
	private DateTimeOffset? _nextLongTime;
	private DateTimeOffset? _nextShortTime;

	/// <summary>
	/// Range length used to determine the Fisher Transform input window.
	/// </summary>
	public int RangePeriods
	{
		get => _rangePeriods.Value;
		set => _rangePeriods.Value = value;
	}

	/// <summary>
	/// Price smoothing factor (0..1) applied before the Fisher Transform.
	/// </summary>
	public decimal PriceSmoothing
	{
		get => _priceSmoothing.Value;
		set => _priceSmoothing.Value = value;
	}

	/// <summary>
	/// Fisher index smoothing factor (0..1) applied after the transform.
	/// </summary>
	public decimal IndexSmoothing
	{
		get => _indexSmoothing.Value;
		set => _indexSmoothing.Value = value;
	}

	/// <summary>
	/// Upper threshold used for bullish color classification.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold used for bearish color classification.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Number of closed bars to wait before acting on a signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool EnableBuyEntry
	{
		get => _enableBuyEntry.Value;
		set => _enableBuyEntry.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool EnableSellEntry
	{
		get => _enableSellEntry.Value;
		set => _enableSellEntry.Value = value;
	}

	/// <summary>
	/// Enable closing of existing long positions based on the indicator.
	/// </summary>
	public bool EnableBuyExit
	{
		get => _enableBuyExit.Value;
		set => _enableBuyExit.Value = value;
	}

	/// <summary>
	/// Enable closing of existing short positions based on the indicator.
	/// </summary>
	public bool EnableSellExit
	{
		get => _enableSellExit.Value;
		set => _enableSellExit.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type and timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ColorFisherM11Strategy"/> class.
	/// </summary>
	public ColorFisherM11Strategy()
	{
		_rangePeriods = Param(nameof(RangePeriods), 10)
			.SetGreaterThanZero()
			.SetDisplay("Range Periods", "Lookback window for highs and lows", "Indicator");

		_priceSmoothing = Param(nameof(PriceSmoothing), 0.3m)
			.SetGreaterOrEqual(0m)
			.SetLessOrEqual(0.99m)
			.SetDisplay("Price Smoothing", "Smoothing factor applied before Fisher transform", "Indicator");

		_indexSmoothing = Param(nameof(IndexSmoothing), 0.3m)
			.SetGreaterOrEqual(0m)
			.SetLessOrEqual(0.99m)
			.SetDisplay("Index Smoothing", "Smoothing factor applied after Fisher transform", "Indicator");

		_highLevel = Param(nameof(HighLevel), 1.01m)
			.SetDisplay("High Level", "Upper level for bullish color", "Indicator");

		_lowLevel = Param(nameof(LowLevel), -1.01m)
			.SetDisplay("Low Level", "Lower level for bearish color", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Bar", "Bars to delay signal execution", "Trading");

		_enableBuyEntry = Param(nameof(EnableBuyEntry), true)
			.SetDisplay("Enable Buy Entry", "Allow opening long positions", "Trading");

		_enableSellEntry = Param(nameof(EnableSellEntry), true)
			.SetDisplay("Enable Sell Entry", "Allow opening short positions", "Trading");

		_enableBuyExit = Param(nameof(EnableBuyExit), true)
			.SetDisplay("Enable Buy Exit", "Allow closing long positions", "Trading");

		_enableSellExit = Param(nameof(EnableSellExit), true)
			.SetDisplay("Enable Sell Exit", "Allow closing short positions", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pts)", "Protective stop distance in price steps", "Protection");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pts)", "Target distance in price steps", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
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
		_colorFisher?.Reset();
		_colorHistory.Clear();
		_nextLongTime = null;
		_nextShortTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_colorFisher = new ColorFisherM11Indicator
		{
			RangePeriods = RangePeriods,
			PriceSmoothing = PriceSmoothing,
			IndexSmoothing = IndexSmoothing,
			HighLevel = HighLevel,
			LowLevel = LowLevel,
			MinRange = Security?.StepPrice ?? 0.0001m
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_colorFisher, ProcessCandle)
			.Start();

		var step = Security?.StepPrice ?? 1m;
		Unit? stopLossUnit = StopLossPoints > 0 ? new Unit(step * StopLossPoints, UnitTypes.Absolute) : null;
		Unit? takeProfitUnit = TakeProfitPoints > 0 ? new Unit(step * TakeProfitPoints, UnitTypes.Absolute) : null;

		if (stopLossUnit != null || takeProfitUnit != null)
			StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _colorFisher);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fisherValue = (ColorFisherM11Value)indicatorValue;
		UpdateHistory(fisherValue.ColorIndex);

		if (!_colorFisher.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signalColor = GetColor(SignalBar);
		var previousColor = GetColor(SignalBar + 1);

		if (signalColor is null || previousColor is null)
			return;

		if (EnableSellExit && signalColor < 2 && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (EnableBuyExit && signalColor > 2 && Position > 0)
		{
			SellMarket(Position);
		}

		var allowLong = !_nextLongTime.HasValue || candle.CloseTime >= _nextLongTime.Value;
		var allowShort = !_nextShortTime.HasValue || candle.CloseTime >= _nextShortTime.Value;

		if (EnableBuyEntry && allowLong && signalColor == 0 && previousColor > 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_nextLongTime = candle.CloseTime;
		}
		else if (EnableSellEntry && allowShort && signalColor == 4 && previousColor < 4 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_nextShortTime = candle.CloseTime;
		}
	}

	private void UpdateHistory(int color)
	{
		_colorHistory.Insert(0, color);
		var max = Math.Max(SignalBar + 2, 5);
		if (_colorHistory.Count > max)
			_colorHistory.RemoveRange(max, _colorHistory.Count - max);
	}

	private int? GetColor(int index)
	{
		if (index < 0 || index >= _colorHistory.Count)
			return null;

		return _colorHistory[index];
	}

	private sealed class ColorFisherM11Indicator : Indicator<ICandleMessage>
	{
		public int RangePeriods { get; set; } = 10;
		public decimal PriceSmoothing { get; set; } = 0.3m;
		public decimal IndexSmoothing { get; set; } = 0.3m;
		public decimal HighLevel { get; set; } = 1.01m;
		public decimal LowLevel { get; set; } = -1.01m;
		public decimal MinRange { get; set; } = 0.0001m;

		private readonly Highest _highest = new();
		private readonly Lowest _lowest = new();
		private decimal _prevFish;
		private decimal _prevIndex;
		private bool _hasPrevIndex;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			if (candle == null)
				return new DecimalIndicatorValue(this, decimal.Zero, input.Time);

			var length = Math.Max(1, RangePeriods);
			_highest.Length = length;
			_lowest.Length = length;

			var highestValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, input.Time));
			var lowestValue = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, input.Time));

			var highest = highestValue.ToDecimal();
			var lowest = lowestValue.ToDecimal();
			var range = highest - lowest;
			var minRange = MinRange <= 0m ? 0.0001m : MinRange;
			if (range < minRange)
				range = minRange;

			var midPrice = (candle.HighPrice + candle.LowPrice) / 2m;
			var priceLocation = range != 0m ? (midPrice - lowest) / range : 0.99m;
			priceLocation = 2m * priceLocation - 1m;

			var prevFish = _hasPrevIndex ? _prevFish : priceLocation;
			var fish = PriceSmoothing * prevFish + (1m - PriceSmoothing) * priceLocation;
			var smoothed = Math.Min(Math.Max(fish, -0.99m), 0.99m);

			decimal fisherRaw;
			var diff = 1m - smoothed;
			if (diff == 0m)
			{
				fisherRaw = 0m;
			}
			else
			{
				var ratio = (1m + smoothed) / diff;
				fisherRaw = (decimal)Math.Log((double)ratio);
			}

			var prevIndex = _hasPrevIndex ? _prevIndex : fisherRaw;
			var value = IndexSmoothing * prevIndex + (1m - IndexSmoothing) * fisherRaw;

			_prevFish = fish;
			_prevIndex = value;
			_hasPrevIndex = true;

			IsFormed = _highest.IsFormed && _lowest.IsFormed && _hasPrevIndex;

			var color = 2;
			if (value > 0m)
				color = value > HighLevel ? 0 : 1;
			else if (value < 0m)
				color = value < LowLevel ? 4 : 3;

			return new ColorFisherM11Value(this, input, value, color);
		}

		public override void Reset()
		{
			base.Reset();
			_highest.Reset();
			_lowest.Reset();
			_prevFish = 0m;
			_prevIndex = 0m;
			_hasPrevIndex = false;
		}
	}

	private sealed class ColorFisherM11Value : ComplexIndicatorValue
	{
		public ColorFisherM11Value(IIndicator indicator, IIndicatorValue input, decimal fisher, int colorIndex)
			: base(indicator, input, (nameof(Fisher), fisher), (nameof(ColorIndex), colorIndex))
		{
		}

		public decimal Fisher => (decimal)GetValue(nameof(Fisher));
		public int ColorIndex => (int)GetValue(nameof(ColorIndex));
	}
}
