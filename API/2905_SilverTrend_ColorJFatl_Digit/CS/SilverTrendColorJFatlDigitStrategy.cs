using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines the SilverTrend breakout logic with the ColorJFatl Digit Jurik filter.
/// SilverTrend detects breakout direction, while the Jurik moving average slope confirms momentum.
/// Positions are opened only when both subsystems agree and closed when the agreement is lost.
/// </summary>
public class SilverTrendColorJFatlDigitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _silverTrendCandleType;
	private readonly StrategyParam<int> _silverTrendLength;
	private readonly StrategyParam<int> _silverTrendRisk;
	private readonly StrategyParam<int> _silverTrendSignalBar;

	private readonly StrategyParam<DataType> _colorJfatlCandleType;
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _jmaSignalBar;
	private readonly StrategyParam<AppliedPrice> _jmaPriceType;
	private readonly StrategyParam<int> _jmaRoundDigits;

	private Highest _silverTrendHighest = null!;
	private Lowest _silverTrendLowest = null!;
	private JurikMovingAverage _jma = null!;

	private readonly Queue<int> _silverTrendQueue = new();
	private readonly Queue<int> _jmaQueue = new();

	private int _silverTrendLastTrend;
	private int _jmaLastTrend;
	private decimal? _lastJmaValue;
	private int? _silverTrendSignal;
	private int? _jmaSignal;
	private int _currentDirection;

	/// <summary>
	/// Enumeration of price calculation modes that replicate the MQL applied price options.
	/// </summary>
	public enum AppliedPrice
	{
		/// <summary>Use candle close price.</summary>
		Close = 1,
		/// <summary>Use candle open price.</summary>
		Open,
		/// <summary>Use candle high price.</summary>
		High,
		/// <summary>Use candle low price.</summary>
		Low,
		/// <summary>Use median price (high + low) / 2.</summary>
		Median,
		/// <summary>Use typical price (high + low + close) / 3.</summary>
		Typical,
		/// <summary>Use weighted close (2 * close + high + low) / 4.</summary>
		Weighted,
		/// <summary>Use simple average of open and close.</summary>
		Simple,
		/// <summary>Use quarter price (open + high + low + close) / 4.</summary>
		Quarter,
		/// <summary>Trend-follow price #1: pick high, low or close depending on candle direction.</summary>
		TrendFollow0,
		/// <summary>Trend-follow price #2: average of close with high/low depending on direction.</summary>
		TrendFollow1,
		/// <summary>Demark price calculation.</summary>
		Demark
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SilverTrendColorJFatlDigitStrategy"/>.
	/// </summary>
	public SilverTrendColorJFatlDigitStrategy()
	{
		_silverTrendCandleType = Param(nameof(SilverTrendCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("SilverTrend Timeframe", "Candle type for the SilverTrend breakout logic", "SilverTrend");

		_silverTrendLength = Param(nameof(SilverTrendLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("SilverTrend Length", "Lookback window for SilverTrend channel calculation", "SilverTrend")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_silverTrendRisk = Param(nameof(SilverTrendRisk), 3)
		.SetDisplay("SilverTrend Risk", "Risk modifier used inside the SilverTrend thresholds", "SilverTrend")
		.SetCanOptimize(true)
		.SetOptimize(0, 20, 1);

		_silverTrendSignalBar = Param(nameof(SilverTrendSignalBar), 1)
		.SetDisplay("SilverTrend Signal Bar", "Delay in bars before acting on a SilverTrend color change", "SilverTrend")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);

		_colorJfatlCandleType = Param(nameof(ColorJfatlCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("ColorJFatl Timeframe", "Candle type for the ColorJFatl confirmation", "ColorJFatl");

		_jmaLength = Param(nameof(JmaLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("JMA Length", "Length of the Jurik moving average", "ColorJFatl")
		.SetCanOptimize(true)
		.SetOptimize(3, 30, 1);

		_jmaSignalBar = Param(nameof(JmaSignalBar), 1)
		.SetDisplay("JMA Signal Bar", "Delay in bars before acting on Jurik slope flips", "ColorJFatl")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);

		_jmaPriceType = Param(nameof(JmaPriceType), AppliedPrice.Close)
		.SetDisplay("JMA Price", "Applied price used as input for the Jurik filter", "ColorJFatl");

		_jmaRoundDigits = Param(nameof(JmaRoundDigits), 2)
		.SetDisplay("JMA Rounding", "Number of digits used to emulate the original indicator rounding", "ColorJFatl")
		.SetCanOptimize(true)
		.SetOptimize(0, 5, 1);
	}

	/// <summary>Timeframe used for the SilverTrend component.</summary>
	public DataType SilverTrendCandleType
	{
		get => _silverTrendCandleType.Value;
		set => _silverTrendCandleType.Value = value;
	}

	/// <summary>Lookback length of the SilverTrend channel.</summary>
	public int SilverTrendLength
	{
		get => _silverTrendLength.Value;
		set => _silverTrendLength.Value = value;
	}

	/// <summary>Risk modifier inside SilverTrend thresholds (higher values tighten the bands).</summary>
	public int SilverTrendRisk
	{
		get => _silverTrendRisk.Value;
		set => _silverTrendRisk.Value = value;
	}

	/// <summary>Delay in bars before SilverTrend signals are considered valid.</summary>
	public int SilverTrendSignalBar
	{
		get => _silverTrendSignalBar.Value;
		set => _silverTrendSignalBar.Value = value;
	}

	/// <summary>Timeframe used for the ColorJFatl confirmation.</summary>
	public DataType ColorJfatlCandleType
	{
		get => _colorJfatlCandleType.Value;
		set => _colorJfatlCandleType.Value = value;
	}

	/// <summary>Length of the Jurik moving average.</summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>Delay in bars before Jurik slope changes are used.</summary>
	public int JmaSignalBar
	{
		get => _jmaSignalBar.Value;
		set => _jmaSignalBar.Value = value;
	}

	/// <summary>Applied price used as input for the Jurik moving average.</summary>
	public AppliedPrice JmaPriceType
	{
		get => _jmaPriceType.Value;
		set => _jmaPriceType.Value = value;
	}

	/// <summary>Number of digits used when rounding the Jurik value.</summary>
	public int JmaRoundDigits
	{
		get => _jmaRoundDigits.Value;
		set => _jmaRoundDigits.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, SilverTrendCandleType);

		if (!Equals(SilverTrendCandleType, ColorJfatlCandleType))
		yield return (Security, ColorJfatlCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_silverTrendQueue.Clear();
		_jmaQueue.Clear();
		_silverTrendLastTrend = 0;
		_jmaLastTrend = 0;
		_lastJmaValue = null;
		_silverTrendSignal = null;
		_jmaSignal = null;
		_currentDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_silverTrendHighest = new Highest { Length = SilverTrendLength + 1 };
		_silverTrendLowest = new Lowest { Length = SilverTrendLength + 1 };
		_jma = new JurikMovingAverage { Length = JmaLength };

		var silverSubscription = SubscribeCandles(SilverTrendCandleType);
		silverSubscription.Bind(_silverTrendHighest, _silverTrendLowest, ProcessSilverTrend).Start();

		var colorSubscription = SubscribeCandles(ColorJfatlCandleType);
		colorSubscription.Bind(ProcessColorJfatl).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, silverSubscription);
			DrawIndicator(area, _silverTrendHighest);
			DrawIndicator(area, _silverTrendLowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessSilverTrend(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_silverTrendHighest.IsFormed || !_silverTrendLowest.IsFormed)
		return;

		var range = highestValue - lowestValue;
		if (range <= 0m)
		{
			UpdateSilverTrendTrend(_silverTrendLastTrend, candle);
			return;
		}

		var riskModifier = 33m - SilverTrendRisk;
		if (riskModifier < 0m)
		riskModifier = 0m;
		if (riskModifier > 33m)
		riskModifier = 33m;

		var thresholdPercent = riskModifier / 100m;
		var lowerThreshold = lowestValue + range * thresholdPercent;
		var upperThreshold = highestValue - range * thresholdPercent;

		var trend = _silverTrendLastTrend;

		if (candle.ClosePrice < lowerThreshold)
		trend = -1;
		else if (candle.ClosePrice > upperThreshold)
		trend = 1;

		UpdateSilverTrendTrend(trend, candle);
	}

	private void UpdateSilverTrendTrend(int trend, ICandleMessage candle)
	{
		if (trend == 0)
		trend = _silverTrendLastTrend;

		_silverTrendLastTrend = trend;

		EnqueueAndProcess(_silverTrendQueue, trend, SilverTrendSignalBar, (previous, current) =>
		{
			_silverTrendSignal = DetermineDirection(previous, current);
			LogInfo($"SilverTrend signal updated. Prev={previous}, Current={current}, Direction={_silverTrendSignal}");
			EvaluateCombinedSignal(candle);
		});
	}

	private void ProcessColorJfatl(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetAppliedPrice(candle, JmaPriceType);
		var jmaValue = _jma.Process(price, candle.CloseTime, true).ToDecimal();

		if (!_jma.IsFormed)
		{
			_lastJmaValue = jmaValue;
			return;
		}

		if (JmaRoundDigits >= 0)
		jmaValue = Math.Round(jmaValue, JmaRoundDigits);

		var trend = _jmaLastTrend;

		if (_lastJmaValue is decimal previous)
		{
			var diff = jmaValue - previous;

			if (diff > 0m)
			trend = 1;
			else if (diff < 0m)
			trend = -1;
		}

		_lastJmaValue = jmaValue;
		_jmaLastTrend = trend;

		EnqueueAndProcess(_jmaQueue, trend, JmaSignalBar, (previousTrend, currentTrend) =>
		{
			_jmaSignal = DetermineDirection(previousTrend, currentTrend);
			LogInfo($"ColorJFatl signal updated. Prev={previousTrend}, Current={currentTrend}, Direction={_jmaSignal}");
			EvaluateCombinedSignal(candle);
		});
	}

	private static int DetermineDirection(int previousTrend, int currentTrend)
	{
		if (previousTrend <= 0 && currentTrend > 0)
		return 1;

		if (previousTrend >= 0 && currentTrend < 0)
		return -1;

		return currentTrend;
	}

	private void EnqueueAndProcess(Queue<int> queue, int trend, int signalBar, Action<int, int> onReady)
	{
		queue.Enqueue(trend);

		var required = signalBar + 2;

		while (queue.Count > required)
		queue.Dequeue();

		if (queue.Count < required)
		return;

		var values = queue.ToArray();
		var currentIndex = values.Length - 1 - signalBar;
		var previousIndex = currentIndex - 1;

		var previous = values[previousIndex];
		var current = values[currentIndex];

		onReady(previous, current);
	}

	private void EvaluateCombinedSignal(ICandleMessage? candle)
	{
		if (_silverTrendSignal is null || _jmaSignal is null)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var direction = 0;

		if (_silverTrendSignal > 0 && _jmaSignal > 0)
		direction = 1;
		else if (_silverTrendSignal < 0 && _jmaSignal < 0)
		direction = -1;

		if (direction == _currentDirection)
		return;

		CancelActiveOrders();

		if (direction == 1)
		{
			CloseShortPositions();
			OpenLong(candle);
		}
		else if (direction == -1)
		{
			CloseLongPositions();
			OpenShort(candle);
		}
		else
		{
			if (Position > 0)
			CloseLongPositions();
			else if (Position < 0)
			CloseShortPositions();

			_currentDirection = 0;
		}
	}

	private void CloseLongPositions()
	{
		var volume = Math.Abs(Position);
		if (volume > 0m)
		SellMarket(volume);
	}

	private void CloseShortPositions()
	{
		var volume = Math.Abs(Position);
		if (volume > 0m)
		BuyMarket(volume);
	}

	private void OpenLong(ICandleMessage? candle)
	{
		if (Volume <= 0m)
		return;

		if (Position < 0)
		CloseShortPositions();

		if (Position <= 0)
		{
			BuyMarket(Volume);
			_currentDirection = 1;

			if (candle != null)
			LogInfo($"Opening long position at {candle.ClosePrice}.");
		}
	}

	private void OpenShort(ICandleMessage? candle)
	{
		if (Volume <= 0m)
		return;

		if (Position > 0)
		CloseLongPositions();

		if (Position >= 0)
		{
			SellMarket(Volume);
			_currentDirection = -1;

			if (candle != null)
			LogInfo($"Opening short position at {candle.ClosePrice}.");
		}
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice priceType)
	{
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		return priceType switch
		{
			AppliedPrice.Close => close,
			AppliedPrice.Open => open,
			AppliedPrice.High => high,
			AppliedPrice.Low => low,
			AppliedPrice.Median => (high + low) / 2m,
			AppliedPrice.Typical => (close + high + low) / 3m,
			AppliedPrice.Weighted => (2m * close + high + low) / 4m,
			AppliedPrice.Simple => (open + close) / 2m,
			AppliedPrice.Quarter => (open + close + high + low) / 4m,
			AppliedPrice.TrendFollow0 => close > open ? high : close < open ? low : close,
			AppliedPrice.TrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
			AppliedPrice.Demark => CalculateDemarkPrice(open, high, low, close),
			_ => close,
		};
	}

	private static decimal CalculateDemarkPrice(decimal open, decimal high, decimal low, decimal close)
	{
		var result = high + low + close;

		if (close < open)
		result = (result + low) / 2m;
		else if (close > open)
		result = (result + high) / 2m;
		else
		result = (result + close) / 2m;

		return ((result - low) + (result - high)) / 2m;
	}
}
