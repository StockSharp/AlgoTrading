using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SilverTrend CrazyChart strategy ported from MQL implementation.
/// The strategy detects channel inversions calculated by the SilverTrend indicator
/// and opens or closes positions when the delayed band crosses the current band.
/// </summary>
public class SilverTrendCrazyChartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _kMin;
	private readonly StrategyParam<decimal> _kMax;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private SilverTrendCrazyChartIndicator _indicator = null!;
	private readonly List<(decimal current, decimal delayed)> _history = new();
	private decimal _entryPrice;

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Swing period for the indicator (SSP in the original script).
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Lower coefficient controlling the channel offset.
	/// </summary>
	public decimal KMin
	{
		get => _kMin.Value;
		set => _kMin.Value = value;
	}

	/// <summary>
	/// Upper coefficient controlling the channel offset.
	/// </summary>
	public decimal KMax
	{
		get => _kMax.Value;
		set => _kMax.Value = value;
	}

	/// <summary>
	/// Number of bars back used for signal evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable opening of long positions.
	/// </summary>
	public bool AllowBuyEntry
	{
		get => _allowBuyEntry.Value;
		set => _allowBuyEntry.Value = value;
	}

	/// <summary>
	/// Enable opening of short positions.
	/// </summary>
	public bool AllowSellEntry
	{
		get => _allowSellEntry.Value;
		set => _allowSellEntry.Value = value;
	}

	/// <summary>
	/// Enable closing of existing long positions.
	/// </summary>
	public bool AllowBuyExit
	{
		get => _allowBuyExit.Value;
		set => _allowBuyExit.Value = value;
	}

	/// <summary>
	/// Enable closing of existing short positions.
	/// </summary>
	public bool AllowSellExit
	{
		get => _allowSellExit.Value;
		set => _allowSellExit.Value = value;
	}

	/// <summary>
	/// Absolute price distance for stop-loss management.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Absolute price distance for take-profit management.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initialize SilverTrend CrazyChart strategy parameters.
	/// </summary>
	public SilverTrendCrazyChartStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for indicator calculations", "General");

		_length = Param(nameof(Length), 7)
		.SetGreaterThanZero()
		.SetDisplay("SSP", "Swing period length", "Indicator");

		_kMin = Param(nameof(KMin), 1.6m)
		.SetDisplay("K Min", "Lower band multiplier", "Indicator");

		_kMax = Param(nameof(KMax), 50.6m)
		.SetDisplay("K Max", "Upper band multiplier", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Bar", "Bars back used for signals", "Indicator");

		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
		.SetDisplay("Allow Buy Entry", "Enable long entries", "Trading");

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
		.SetDisplay("Allow Sell Entry", "Enable short entries", "Trading");

		_allowBuyExit = Param(nameof(AllowBuyExit), true)
		.SetDisplay("Allow Buy Exit", "Enable long exits", "Trading");

		_allowSellExit = Param(nameof(AllowSellExit), true)
		.SetDisplay("Allow Sell Exit", "Enable short exits", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetDisplay("Stop Loss", "Stop distance in price units", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetDisplay("Take Profit", "Take profit distance in price units", "Risk");
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

		_history.Clear();
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new SilverTrendCrazyChartIndicator
		{
			Length = Length,
			KMin = KMin,
			KMax = KMax,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_indicator, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageRisk(candle);

		var values = (SilverTrendCrazyChartValue)indicatorValue;

		if (!values.IsFormed)
			return;

		_history.Add((values.CurrentBand, values.DelayedBand));

		var required = SignalBar + 2;
		if (_history.Count > required)
			_history.RemoveRange(0, _history.Count - required);

		var currentIndex = _history.Count - 1 - SignalBar;
		if (currentIndex <= 0)
			return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var (currentUp, currentDown) = _history[currentIndex];
		var (previousUp, previousDown) = _history[previousIndex];

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shouldCloseShort = AllowSellExit && previousUp > previousDown && Position < 0;
		var shouldCloseLong = AllowBuyExit && previousUp < previousDown && Position > 0;

		if (shouldCloseLong)
			CloseLong();

		if (shouldCloseShort)
			CloseShort();

		var shouldOpenLong = AllowBuyEntry && previousUp > previousDown && currentUp <= currentDown && Position <= 0;
		var shouldOpenShort = AllowSellEntry && previousUp < previousDown && currentUp >= currentDown && Position >= 0;

		if (shouldOpenLong)
			EnterLong(candle.ClosePrice);
		else if (shouldOpenShort)
			EnterShort(candle.ClosePrice);
	}

	private void EnterLong(decimal price)
	{
		var volume = Volume + Math.Max(0m, -Position);
		if (volume <= 0)
			return;

		BuyMarket(volume);
		_entryPrice = price;
	}

	private void EnterShort(decimal price)
	{
		var volume = Volume + Math.Max(0m, Position);
		if (volume <= 0)
			return;

		SellMarket(volume);
		_entryPrice = price;
	}

	private void CloseLong()
	{
		if (Position <= 0)
			return;

		SellMarket(Position);
		_entryPrice = 0m;
	}

	private void CloseShort()
	{
		if (Position >= 0)
			return;

		BuyMarket(Math.Abs(Position));
		_entryPrice = 0m;
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (_entryPrice == 0m)
			return;

		if (Position > 0)
		{
			if (StopLossPoints > 0m && candle.ClosePrice <= _entryPrice - StopLossPoints)
			{
				CloseLong();
				return;
			}

			if (TakeProfitPoints > 0m && candle.ClosePrice >= _entryPrice + TakeProfitPoints)
			{
				CloseLong();
			}
		}
		else if (Position < 0)
		{
			if (StopLossPoints > 0m && candle.ClosePrice >= _entryPrice + StopLossPoints)
			{
				CloseShort();
				return;
			}

			if (TakeProfitPoints > 0m && candle.ClosePrice <= _entryPrice - TakeProfitPoints)
			{
				CloseShort();
			}
		}
		else
		{
			_entryPrice = 0m;
		}
	}
}

/// <summary>
/// Indicator replicating the SilverTrend CrazyChart buffers.
/// </summary>
public class SilverTrendCrazyChartIndicator : BaseIndicator<decimal>
{
	private readonly Highest _highest = new();
	private readonly Lowest _lowest = new();
	private readonly Queue<decimal> _delayQueue = new();

	/// <summary>
	/// Swing lookback length.
	/// </summary>
	public int Length { get; set; } = 7;

	/// <summary>
	/// Lower offset multiplier.
	/// </summary>
	public decimal KMin { get; set; } = 1.6m;

	/// <summary>
	/// Upper offset multiplier.
	/// </summary>
	public decimal KMax { get; set; } = 50.6m;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_highest.Length = Length;
		_lowest.Length = Length;
		_highest.Reset();
		_lowest.Reset();
		_delayQueue.Clear();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new SilverTrendCrazyChartValue(this, input, 0m, 0m, false);

		if (_highest.Length != Length)
			_highest.Length = Length;

		if (_lowest.Length != Length)
			_lowest.Length = Length;

		var highestValue = _highest.Process(candle);
		var lowestValue = _lowest.Process(candle);

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return new SilverTrendCrazyChartValue(this, input, 0m, 0m, false);

		var high = highestValue.GetValue<decimal>();
		var low = lowestValue.GetValue<decimal>();
		var range = high - low;
		var upper = high - range * (KMax / 100m);

		_delayQueue.Enqueue(upper);
		var delay = Length + 1;

		if (_delayQueue.Count <= delay)
			return new SilverTrendCrazyChartValue(this, input, upper, 0m, false);

		var delayed = _delayQueue.Dequeue();
		return new SilverTrendCrazyChartValue(this, input, upper, delayed, true);
	}
}

/// <summary>
/// Indicator value for SilverTrend CrazyChart indicator.
/// </summary>
public class SilverTrendCrazyChartValue : ComplexIndicatorValue
{
	/// <summary>
	/// Initializes value container.
	/// </summary>
	public SilverTrendCrazyChartValue(IIndicator indicator, IIndicatorValue input, decimal current, decimal delayed, bool isFormed)
	: base(indicator, input, (nameof(CurrentBand), current), (nameof(DelayedBand), delayed))
	{
		CurrentBand = current;
		DelayedBand = delayed;
		IsFormed = isFormed;
	}

	/// <summary>
	/// Current band value (buffer 0 in original indicator).
	/// </summary>
	public decimal CurrentBand { get; }

	/// <summary>
	/// Delayed band value (buffer 1 in original indicator).
	/// </summary>
	public decimal DelayedBand { get; }

	/// <summary>
	/// True when both buffers are available.
	/// </summary>
	public bool IsFormed { get; }
}
