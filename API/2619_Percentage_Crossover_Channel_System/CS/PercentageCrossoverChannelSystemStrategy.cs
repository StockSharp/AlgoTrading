using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Percentage Crossover Channel breakout system translated from MQL.
/// </summary>
public class PercentageCrossoverChannelSystemStrategy : Strategy
{
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<int> _colorHistory = new();
	private readonly List<decimal> _upperHistory = new();
	private readonly List<decimal> _lowerHistory = new();

	private decimal _previousMiddle;
	private bool _hasMiddle;
	private decimal? _entryPrice;

	public decimal Percent
	{
		get => _percent.Value;
		set => _percent.Value = value;
	}

	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public bool BuyPositionsOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	public bool SellPositionsOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	public bool BuyPositionsClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	public bool SellPositionsClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PercentageCrossoverChannelSystemStrategy()
	{
		_percent = Param(nameof(Percent), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Channel Percent", "Percentage width of the channel", "Indicator");

		_shift = Param(nameof(Shift), 1)
			.SetGreaterThanZero()
			.SetDisplay("Shift", "Number of bars used for crossover comparison", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Bars back to evaluate indicator colors", "Trading Rules");

		_buyOpen = Param(nameof(BuyPositionsOpen), true)
			.SetDisplay("Enable Long Entries", "Allow long position openings", "Trading Rules");

		_sellOpen = Param(nameof(SellPositionsOpen), true)
			.SetDisplay("Enable Short Entries", "Allow short position openings", "Trading Rules");

		_buyClose = Param(nameof(BuyPositionsClose), true)
			.SetDisplay("Allow Long Exits", "Permit closing long trades on bearish signals", "Trading Rules");

		_sellClose = Param(nameof(SellPositionsClose), true)
			.SetDisplay("Allow Short Exits", "Permit closing short trades on bullish signals", "Trading Rules");

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetDisplay("Stop Loss (steps)", "Protective stop loss distance in price steps", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetDisplay("Take Profit (steps)", "Target profit distance in price steps", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for analysis", "General");
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

		_colorHistory.Clear();
		_upperHistory.Clear();
		_lowerHistory.Clear();
		_hasMiddle = false;
		_previousMiddle = 0m;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ignore interim updates; we only react on closed candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Evaluate protective orders before generating new signals.
		var stopTriggered = HandleRisk(candle);

		// Mirror the MQL signal logic using cached indicator colors.
		if (IsFormedAndOnlineAndAllowTrading() && _colorHistory.Count > SignalBar)
		{
			// Equivalent to CopyBuffer(..., SignalBar, 2, ...) from the EA.
			var recentIndex = _colorHistory.Count - SignalBar;
			var olderIndex = recentIndex - 1;

			if (olderIndex >= 0)
			{
				var recentColor = _colorHistory[recentIndex];
				var olderColor = _colorHistory[olderIndex];

				var shouldCloseShort = SellPositionsClose && olderColor > 2;
				var shouldCloseLong = BuyPositionsClose && olderColor < 2;
				var shouldOpenBuy = BuyPositionsOpen && olderColor > 2 && recentColor < 3;
				var shouldOpenSell = SellPositionsOpen && olderColor < 2 && recentColor > 1;

				// Close existing positions according to the original toggles.
				if (shouldCloseLong && Position > 0)
				{
					SellMarket();
					_entryPrice = null;
				}

				if (shouldCloseShort && Position < 0)
				{
					BuyMarket();
					_entryPrice = null;
				}

				// Enter only when we are flat to match the EA behaviour.
				if (!stopTriggered && Position == 0)
				{
					if (shouldOpenBuy)
					{
						BuyMarket();
						_entryPrice = candle.ClosePrice;
					}
					else if (shouldOpenSell)
					{
						SellMarket();
						_entryPrice = candle.ClosePrice;
					}
				}
			}
		}

		// Update indicator state after trading decisions are made.
		var color = CalculateColor(candle);

		_colorHistory.Add(color);
		TrimHistory();
	}

	private bool HandleRisk(ICandleMessage candle)
	{
		// Exit early if there is no stored entry price.
		if (_entryPrice is null)
			return false;

		// Price step is required to translate MQL points into absolute prices.
		if (Security?.PriceStep is not decimal step || step <= 0)
			return false;

		var triggered = false;

		if (Position > 0)
		{
			// Long position risk checks.
			if (StopLoss > 0)
			{
				var stopLevel = _entryPrice.Value - StopLoss * step;
				if (candle.LowPrice <= stopLevel)
				{
					SellMarket();
					_entryPrice = null;
					triggered = true;
				}
			}

			if (!triggered && TakeProfit > 0)
			{
				var takeLevel = _entryPrice.Value + TakeProfit * step;
				if (candle.HighPrice >= takeLevel)
				{
					SellMarket();
					_entryPrice = null;
					triggered = true;
				}
			}
		}
		else if (Position < 0)
		{
			// Short position risk checks.
			if (StopLoss > 0)
			{
				var stopLevel = _entryPrice.Value + StopLoss * step;
				if (candle.HighPrice >= stopLevel)
				{
					BuyMarket();
					_entryPrice = null;
					triggered = true;
				}
			}

			if (!triggered && TakeProfit > 0)
			{
				var takeLevel = _entryPrice.Value - TakeProfit * step;
				if (candle.LowPrice <= takeLevel)
				{
					BuyMarket();
					_entryPrice = null;
					triggered = true;
				}
			}
		}

		// Reset cached entry price once we are flat.
		if (Position == 0)
			_entryPrice = null;

		return triggered;
	}

	private int CalculateColor(ICandleMessage candle)
	{
		// Recreate the Percentage Crossover Channel midline and colour logic.
		var percentFactor = Percent / 100m;
		var plusVar = 1m + percentFactor;
		var minusVar = 1m - percentFactor;
		var close = candle.ClosePrice;

		// Initialise the midline on the very first candle.
		if (!_hasMiddle)
		{
			_previousMiddle = close;
			_hasMiddle = true;
		}

		var middle = _previousMiddle;
		var lowerCandidate = close * minusVar;
		var upperCandidate = close * plusVar;

		// Adjust the midline exactly as in the original indicator.
		if (lowerCandidate > _previousMiddle)
		{
			middle = lowerCandidate;
		}
		else if (upperCandidate < _previousMiddle)
		{
			middle = upperCandidate;
		}

		var upper = middle + middle * percentFactor;
		var lower = middle - middle * percentFactor;

		_previousMiddle = middle;

		var color = 2;

		// Determine candle colour relative to past channel values.
		if (_upperHistory.Count >= Shift)
		{
			var referenceIndex = _upperHistory.Count - Shift;
			var referenceUpper = _upperHistory[referenceIndex];
			var referenceLower = _lowerHistory[referenceIndex];

			if (close > referenceUpper)
			{
				color = candle.OpenPrice <= close ? 4 : 3;
			}
			else if (close < referenceLower)
			{
				color = candle.OpenPrice > close ? 0 : 1;
			}
		}

		// Persist channel history for future signal checks.
		_upperHistory.Add(upper);
		_lowerHistory.Add(lower);

		return color;
	}

	private void TrimHistory()
	{
		// Keep only as much history as needed for Shift and SignalBar lookbacks.
		var maxCapacity = Math.Max(Shift + SignalBar + 5, 16);
		if (_colorHistory.Count <= maxCapacity)
			return;

		var removeCount = _colorHistory.Count - maxCapacity;
		_colorHistory.RemoveRange(0, removeCount);
		_upperHistory.RemoveRange(0, removeCount);
		_lowerHistory.RemoveRange(0, removeCount);
	}
}
