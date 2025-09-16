using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bull vs Medved strategy converted from MetaTrader 5.
/// Places limit orders during predefined intraday windows when bullish or bearish candle sequences appear.
/// </summary>
public class BullVsMedvedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _candleSizePips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _indentUpPips;
	private readonly StrategyParam<decimal> _indentDownPips;
	private readonly StrategyParam<int> _entryWindowMinutes;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _startTime0;
	private readonly StrategyParam<TimeSpan> _startTime1;
	private readonly StrategyParam<TimeSpan> _startTime2;
	private readonly StrategyParam<TimeSpan> _startTime3;
	private readonly StrategyParam<TimeSpan> _startTime4;
	private readonly StrategyParam<TimeSpan> _startTime5;

	private decimal _pointValue;
	private decimal _pipValue;
	private decimal _bodyMinSize;
	private decimal _pullbackSize;
	private decimal _candleSizeThreshold;
	private decimal _buyIndent;
	private decimal _sellIndent;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _orderPlacedInWindow;

	private ICandleMessage _previousCandle1;
	private ICandleMessage _previousCandle2;
	private TimeSpan[] _entryTimes = Array.Empty<TimeSpan>();

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public BullVsMedvedStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Order Volume", "Volume for pending orders", "Trading")
		.SetGreaterThanZero();

		_candleSizePips = Param(nameof(CandleSizePips), 75m)
		.SetDisplay("Candle Size (pips)", "Minimum body size for the latest candle", "Filters")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(25m, 150m, 25m);

		_stopLossPips = Param(nameof(StopLossPips), 60m)
		.SetDisplay("Stop Loss (pips)", "Distance from entry for protective stop", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(20m, 120m, 20m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 60m)
		.SetDisplay("Take Profit (pips)", "Distance from entry for profit target", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(20m, 120m, 20m);

		_indentUpPips = Param(nameof(IndentUpPips), 16m)
		.SetDisplay("Buy Limit Offset (pips)", "Indent below the ask for buy limit orders", "Execution")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(8m, 32m, 4m);

		_indentDownPips = Param(nameof(IndentDownPips), 20m)
		.SetDisplay("Sell Limit Offset (pips)", "Indent above the bid for sell limit orders", "Execution")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(8m, 32m, 4m);

		_entryWindowMinutes = Param(nameof(EntryWindowMinutes), 5)
		.SetDisplay("Entry Window (min)", "Duration of each trading window", "Timing")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for pattern detection", "Data");

		_startTime0 = Param(nameof(StartTime0), new TimeSpan(0, 5, 0))
		.SetDisplay("Start Time #1", "First trading window start", "Timing");

		_startTime1 = Param(nameof(StartTime1), new TimeSpan(4, 5, 0))
		.SetDisplay("Start Time #2", "Second trading window start", "Timing");

		_startTime2 = Param(nameof(StartTime2), new TimeSpan(8, 5, 0))
		.SetDisplay("Start Time #3", "Third trading window start", "Timing");

		_startTime3 = Param(nameof(StartTime3), new TimeSpan(12, 5, 0))
		.SetDisplay("Start Time #4", "Fourth trading window start", "Timing");

		_startTime4 = Param(nameof(StartTime4), new TimeSpan(16, 5, 0))
		.SetDisplay("Start Time #5", "Fifth trading window start", "Timing");

		_startTime5 = Param(nameof(StartTime5), new TimeSpan(20, 5, 0))
		.SetDisplay("Start Time #6", "Sixth trading window start", "Timing");
	}

	/// <summary>
	/// Order volume for pending orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Minimum body size (in pips) required for the most recent candle.
	/// </summary>
	public decimal CandleSizePips
	{
		get => _candleSizePips.Value;
		set => _candleSizePips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Offset below the ask for buy limit orders (pips).
	/// </summary>
	public decimal IndentUpPips
	{
		get => _indentUpPips.Value;
		set => _indentUpPips.Value = value;
	}

	/// <summary>
	/// Offset above the bid for sell limit orders (pips).
	/// </summary>
	public decimal IndentDownPips
	{
		get => _indentDownPips.Value;
		set => _indentDownPips.Value = value;
	}

	/// <summary>
	/// Duration of each entry window in minutes.
	/// </summary>
	public int EntryWindowMinutes
	{
		get => _entryWindowMinutes.Value;
		set => _entryWindowMinutes.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate price patterns.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// First trading window start time.
	/// </summary>
	public TimeSpan StartTime0
	{
		get => _startTime0.Value;
		set => _startTime0.Value = value;
	}

	/// <summary>
	/// Second trading window start time.
	/// </summary>
	public TimeSpan StartTime1
	{
		get => _startTime1.Value;
		set => _startTime1.Value = value;
	}

	/// <summary>
	/// Third trading window start time.
	/// </summary>
	public TimeSpan StartTime2
	{
		get => _startTime2.Value;
		set => _startTime2.Value = value;
	}

	/// <summary>
	/// Fourth trading window start time.
	/// </summary>
	public TimeSpan StartTime3
	{
		get => _startTime3.Value;
		set => _startTime3.Value = value;
	}

	/// <summary>
	/// Fifth trading window start time.
	/// </summary>
	public TimeSpan StartTime4
	{
		get => _startTime4.Value;
		set => _startTime4.Value = value;
	}

	/// <summary>
	/// Sixth trading window start time.
	/// </summary>
	public TimeSpan StartTime5
	{
		get => _startTime5.Value;
		set => _startTime5.Value = value;
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

		_bestBid = 0m;
		_bestAsk = 0m;
		_orderPlacedInWindow = false;
		_previousCandle1 = null;
		_previousCandle2 = null;
		_entryTimes = Array.Empty<TimeSpan>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var decimals = Security?.Decimals ?? 0;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;

		_pointValue = Security?.PriceStep ?? 1m;
		_pipValue = _pointValue * adjust;
		_bodyMinSize = 10m * _pointValue;
		_pullbackSize = 20m * _pointValue;
		_candleSizeThreshold = CandleSizePips * _pipValue;
		_buyIndent = IndentUpPips * _pipValue;
		_sellIndent = IndentDownPips * _pipValue;
		_stopLossOffset = StopLossPips * _pipValue;
		_takeProfitOffset = TakeProfitPips * _pipValue;

		_entryTimes = new[]
		{
			StartTime0,
			StartTime1,
			StartTime2,
			StartTime3,
			StartTime4,
			StartTime5
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(depth =>
		{
			// Track latest bid and ask to position limit orders around the spread.
			_bestBid = depth.GetBestBid()?.Price ?? _bestBid;
			_bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
		})
		.Start();

		StartProtection(
		stopLoss: _stopLossOffset > 0m ? new Unit(_stopLossOffset, UnitTypes.Absolute) : null,
		takeProfit: _takeProfitOffset > 0m ? new Unit(_takeProfitOffset, UnitTypes.Absolute) : null,
		useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ignore unfinished candles because their prices are not final.
		if (candle.State != CandleStates.Finished)
			return;

		// Skip processing if trading environment is not ready.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var inWindow = IsWithinEntryWindow(candle.CloseTime);
		if (!inWindow)
			{
			_orderPlacedInWindow = false;
			ShiftHistory(candle);
			return;
		}

		if (_orderPlacedInWindow || HasActivePendingOrder())
			{
			ShiftHistory(candle);
			return;
		}

		if (_previousCandle1 is null || _previousCandle2 is null)
			{
			ShiftHistory(candle);
			return;
		}

		var shift1 = candle;
		var shift2 = _previousCandle1;
		var shift3 = _previousCandle2;

		var placedOrder = false;

		var isBull = IsBull(shift3, shift2, shift1);
		var isBadBull = IsBadBull(shift3, shift2, shift1);
		var isCoolBull = IsCoolBull(shift2, shift1);
		var isBear = IsBear(shift1);

		if (isBull && !isBadBull)
			placedOrder = TryPlaceBuyLimit(shift1);
		else if (isCoolBull)
			placedOrder = TryPlaceBuyLimit(shift1);
		else if (isBear)
			placedOrder = TryPlaceSellLimit(shift1);

		if (placedOrder)
			_orderPlacedInWindow = true;

		ShiftHistory(candle);
	}

	private bool IsWithinEntryWindow(DateTimeOffset time)
	{
		var window = TimeSpan.FromMinutes(EntryWindowMinutes);
		var tod = time.TimeOfDay;

		for (var i = 0; i < _entryTimes.Length; i++)
			{
			var start = _entryTimes[i];
			var end = start + window;

			if (tod >= start && tod <= end)
				return true;
		}

		return false;
	}

	private bool HasActivePendingOrder()
	{
		foreach (var order in Orders)
			{
			if (order.Security != Security)
				continue;

			if (order.State == OrderStates.Active)
				return true;
		}

		return false;
	}

	private bool TryPlaceBuyLimit(ICandleMessage referenceCandle)
	{
		if (OrderVolume <= 0m)
			return false;

		var ask = _bestAsk > 0m ? _bestAsk : referenceCandle.ClosePrice;
		var price = ask - _buyIndent;

		if (price <= 0m)
			return false;

		BuyLimit(price, OrderVolume);
		LogInfo($"Placed buy limit at {price} with volume {OrderVolume}.");
		return true;
	}

	private bool TryPlaceSellLimit(ICandleMessage referenceCandle)
	{
		if (OrderVolume <= 0m)
			return false;

		var bid = _bestBid > 0m ? _bestBid : referenceCandle.ClosePrice;
		var price = bid + _sellIndent;

		if (price <= 0m)
			return false;

		SellLimit(price, OrderVolume);
		LogInfo($"Placed sell limit at {price} with volume {OrderVolume}.");
		return true;
	}

	private bool IsBull(ICandleMessage shift3, ICandleMessage shift2, ICandleMessage shift1)
	{
		return shift3.ClosePrice > shift2.OpenPrice &&
		(shift2.ClosePrice - shift2.OpenPrice) >= _bodyMinSize &&
		(shift1.ClosePrice - shift1.OpenPrice) >= _candleSizeThreshold;
	}

	private bool IsBadBull(ICandleMessage shift3, ICandleMessage shift2, ICandleMessage shift1)
	{
		return (shift3.ClosePrice - shift3.OpenPrice) >= _bodyMinSize &&
		(shift2.ClosePrice - shift2.OpenPrice) >= _bodyMinSize &&
		(shift1.ClosePrice - shift1.OpenPrice) >= _candleSizeThreshold;
	}

	private bool IsCoolBull(ICandleMessage shift2, ICandleMessage shift1)
	{
		return (shift2.OpenPrice - shift2.ClosePrice) >= _pullbackSize &&
		shift2.ClosePrice <= shift1.OpenPrice &&
		shift1.ClosePrice > shift2.OpenPrice &&
		(shift1.ClosePrice - shift1.OpenPrice) >= 0.4m * _candleSizeThreshold;
	}

	private bool IsBear(ICandleMessage shift1)
	{
		return (shift1.OpenPrice - shift1.ClosePrice) >= _candleSizeThreshold;
	}

	private void ShiftHistory(ICandleMessage candle)
	{
		_previousCandle2 = _previousCandle1;
		_previousCandle1 = candle;
	}
}
