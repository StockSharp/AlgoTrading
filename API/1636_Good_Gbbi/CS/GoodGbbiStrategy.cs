using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Good Gbbi strategy.
/// Opens a position at specified hour based on historical open price
/// differences.
/// </summary>
public class GoodGbbiStrategy : Strategy {
	private readonly StrategyParam<int> _takeProfitLong;
	private readonly StrategyParam<int> _stopLossLong;
	private readonly StrategyParam<int> _takeProfitShort;
	private readonly StrategyParam<int> _stopLossShort;
	private readonly StrategyParam<int> _tradeTime;
	private readonly StrategyParam<int> _t1;
	private readonly StrategyParam<int> _t2;
	private readonly StrategyParam<int> _deltaLong;
	private readonly StrategyParam<int> _deltaShort;
	private readonly StrategyParam<int> _maxOpenTime;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _openPrices = new decimal[7];
	private int _candlesCount;
	private bool _canTrade = true;
	private DateTimeOffset _entryTime;
	private decimal _entryPrice;

	/// <summary>
	/// Take profit in points for long positions.
	/// </summary>
	public int TakeProfitLong {
	get => _takeProfitLong.Value;
	set => _takeProfitLong.Value = value;
	}

	/// <summary>
	/// Stop loss in points for long positions.
	/// </summary>
	public int StopLossLong {
	get => _stopLossLong.Value;
	set => _stopLossLong.Value = value;
	}

	/// <summary>
	/// Take profit in points for short positions.
	/// </summary>
	public int TakeProfitShort {
	get => _takeProfitShort.Value;
	set => _takeProfitShort.Value = value;
	}

	/// <summary>
	/// Stop loss in points for short positions.
	/// </summary>
	public int StopLossShort {
	get => _stopLossShort.Value;
	set => _stopLossShort.Value = value;
	}

	/// <summary>
	/// Hour of day to evaluate entries.
	/// </summary>
	public int TradeTime {
	get => _tradeTime.Value;
	set => _tradeTime.Value = value;
	}

	/// <summary>
	/// Bar offset for first open price.
	/// </summary>
	public int T1 {
	get => _t1.Value;
	set => _t1.Value = value;
	}

	/// <summary>
	/// Bar offset for second open price.
	/// </summary>
	public int T2 {
	get => _t2.Value;
	set => _t2.Value = value;
	}

	/// <summary>
	/// Required open difference for long entries in points.
	/// </summary>
	public int DeltaLong {
	get => _deltaLong.Value;
	set => _deltaLong.Value = value;
	}

	/// <summary>
	/// Required open difference for short entries in points.
	/// </summary>
	public int DeltaShort {
	get => _deltaShort.Value;
	set => _deltaShort.Value = value;
	}

	/// <summary>
	/// Maximum position lifetime in hours.
	/// </summary>
	public int MaxOpenTime {
	get => _maxOpenTime.Value;
	set => _maxOpenTime.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GoodGbbiStrategy"/>.
	/// </summary>
	public GoodGbbiStrategy() {
	_takeProfitLong =
		Param(nameof(TakeProfitLong), 39)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Long",
				"Profit target for long positions in points",
				"Risk Management");

	_stopLossLong =
		Param(nameof(StopLossLong), 147)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Long",
				"Stop loss for long positions in points",
				"Risk Management");

	_takeProfitShort =
		Param(nameof(TakeProfitShort), 15)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Short",
				"Profit target for short positions in points",
				"Risk Management");

	_stopLossShort =
		Param(nameof(StopLossShort), 6000)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Short",
				"Stop loss for short positions in points",
				"Risk Management");

	_tradeTime =
		Param(nameof(TradeTime), 18)
		.SetDisplay("Trade Time", "Hour of day to enter the market",
				"General");

	_t1 = Param(nameof(T1), 6)
		  .SetGreaterThanZero()
		  .SetDisplay("T1", "First open price offset", "Logic");

	_t2 = Param(nameof(T2), 2)
		  .SetGreaterThanZero()
		  .SetDisplay("T2", "Second open price offset", "Logic");

	_deltaLong =
		Param(nameof(DeltaLong), 6)
		.SetGreaterThanZero()
		.SetDisplay("Delta Long",
				"Open difference for long entries in points",
				"Logic");

	_deltaShort =
		Param(nameof(DeltaShort), 21)
		.SetGreaterThanZero()
		.SetDisplay("Delta Short",
				"Open difference for short entries in points",
				"Logic");

	_maxOpenTime =
		Param(nameof(MaxOpenTime), 504)
		.SetDisplay("Max Open Time",
				"Maximum holding time in hours (0 - unlimited)",
				"Risk Management");

	_candleType =
		Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
	base.OnReseted();
	_candlesCount = 0;
	_canTrade = true;
	_entryPrice = 0m;
	_entryTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();

	base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle) {
	if (candle.State != CandleStates.Finished)
		return;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	// store open price in circular buffer
	_openPrices[_candlesCount % _openPrices.Length] = candle.OpenPrice;
	_candlesCount++;

	var step = Security.PriceStep ?? 1m;

	// manage existing position and protection
	if (Position > 0) {
		var tp = _entryPrice + TakeProfitLong * step;
		var sl = _entryPrice - StopLossLong * step;
		var expired =
		MaxOpenTime > 0 &&
		(candle.OpenTime - _entryTime).TotalHours >= MaxOpenTime;
		if (candle.ClosePrice >= tp || candle.ClosePrice <= sl || expired)
		SellMarket(Position);
		return;
	} else if (Position < 0) {
		var tp = _entryPrice - TakeProfitShort * step;
		var sl = _entryPrice + StopLossShort * step;
		var expired =
		MaxOpenTime > 0 &&
		(candle.OpenTime - _entryTime).TotalHours >= MaxOpenTime;
		if (candle.ClosePrice <= tp || candle.ClosePrice >= sl || expired)
		BuyMarket(-Position);
		return;
	}

	// reset trade flag after configured hour
	if (candle.OpenTime.Hour > TradeTime)
		_canTrade = true;

	// ensure enough history is collected
	if (_candlesCount <= Math.Max(T1, T2))
		return;

	if (!_canTrade || candle.OpenTime.Hour != TradeTime)
		return;

	var openT1 = _openPrices[(_candlesCount - 1 - T1 + _openPrices.Length) %
				 _openPrices.Length];
	var openT2 = _openPrices[(_candlesCount - 1 - T2 + _openPrices.Length) %
				 _openPrices.Length];

	if (openT1 - openT2 > DeltaShort * step) {
		SellMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_entryTime = candle.OpenTime;
		_canTrade = false;
	} else if (openT2 - openT1 > DeltaLong * step) {
		BuyMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_entryTime = candle.OpenTime;
		_canTrade = false;
	}
	}
}
