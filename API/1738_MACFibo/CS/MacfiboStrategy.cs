using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACFibo strategy based on EMA/SMA cross with Fibonacci targets.
/// </summary>
public class MacfiboStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _midLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _minTakeProfit;
	private readonly StrategyParam<decimal> _maxStopLoss;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _fridayTrade;
	private readonly StrategyParam<bool> _mondayTrade;
	private readonly StrategyParam<bool> _closeAtFastMid;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _fastMa;
	private SMA _midMa;
	private SMA _slowMa;

	private decimal _entryPrice;
	private decimal _targetPrice;
	private decimal _stopPrice;
	private bool _isLong;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevFastMid;
	private decimal _prevMid;
	private bool _isInitialized;

	private decimal _lowestLow;
	private decimal _highestHigh;
	private bool _trackingDown;
	private bool _trackingUp;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength {
	get => _fastLength.Value;
	set => _fastLength.Value = value;
	}

	/// <summary>
	/// Mid SMA length.
	/// </summary>
	public int MidLength {
	get => _midLength.Value;
	set => _midLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength {
	get => _slowLength.Value;
	set => _slowLength.Value = value;
	}

	/// <summary>
	/// Minimum take profit in price units.
	/// </summary>
	public decimal MinTakeProfit {
	get => _minTakeProfit.Value;
	set => _minTakeProfit.Value = value;
	}

	/// <summary>
	/// Maximum stop loss in price units.
	/// </summary>
	public decimal MaxStopLoss {
	get => _maxStopLoss.Value;
	set => _maxStopLoss.Value = value;
	}

	/// <summary>
	/// Trading start hour (0-23).
	/// </summary>
	public int StartHour {
	get => _startHour.Value;
	set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading end hour (0-23).
	/// </summary>
	public int EndHour {
	get => _endHour.Value;
	set => _endHour.Value = value;
	}

	/// <summary>
	/// Allow trading on Fridays.
	/// </summary>
	public bool FridayTrade {
	get => _fridayTrade.Value;
	set => _fridayTrade.Value = value;
	}

	/// <summary>
	/// Allow trading on Mondays.
	/// </summary>
	public bool MondayTrade {
	get => _mondayTrade.Value;
	set => _mondayTrade.Value = value;
	}

	/// <summary>
	/// Close losing positions when fast EMA crosses mid SMA.
	/// </summary>
	public bool CloseAtFastMid {
	get => _closeAtFastMid.Value;
	set => _closeAtFastMid.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MacfiboStrategy() {
	_fastLength = Param(nameof(FastLength), 5)
			  .SetGreaterThanZero()
			  .SetDisplay("Fast EMA", "Length of the fast EMA",
					  "Moving Averages")
			  .SetCanOptimize(true)
			  .SetOptimize(5, 20, 1);

	_midLength = Param(nameof(MidLength), 8)
			 .SetGreaterThanZero()
			 .SetDisplay("Mid SMA", "Length of the mid SMA",
					 "Moving Averages");

	_slowLength = Param(nameof(SlowLength), 20)
			  .SetGreaterThanZero()
			  .SetDisplay("Slow SMA", "Length of the slow SMA",
					  "Moving Averages");

	_minTakeProfit =
		Param(nameof(MinTakeProfit), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Min TP", "Minimum take profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 5m);

	_maxStopLoss = Param(nameof(MaxStopLoss), 50m)
			   .SetGreaterThanZero()
			   .SetDisplay("Max SL", "Maximum stop loss", "Risk")
			   .SetCanOptimize(true)
			   .SetOptimize(20m, 100m, 10m);

	_startHour =
		Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Trading start hour", "Time");

	_endHour = Param(nameof(EndHour), 16)
			   .SetDisplay("End Hour", "Trading end hour", "Time");

	_fridayTrade =
		Param(nameof(FridayTrade), true)
		.SetDisplay("Trade Friday", "Allow trading on Fridays", "Time");

	_mondayTrade =
		Param(nameof(MondayTrade), true)
		.SetDisplay("Trade Monday", "Allow trading on Mondays", "Time");

	_closeAtFastMid =
		Param(nameof(CloseAtFastMid), true)
		.SetDisplay("Close at Fast/Mid",
				"Close losing positions on fast-mid cross", "Risk");

	_candleType =
		Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
	_entryPrice = 0m;
	_targetPrice = 0m;
	_stopPrice = 0m;
	_isLong = false;
	_isInitialized = false;
	_lowestLow = 0m;
	_highestHigh = 0m;
	_trackingDown = false;
	_trackingUp = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);

		_fastMa = new EMA { Length = FastLength };
	_midMa = new SMA { Length = MidLength };
	_slowMa = new SMA { Length = SlowLength };

	var subscription = SubscribeCandles(CandleType);

	subscription.Bind(_fastMa, _midMa, _slowMa, ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null) {
		DrawCandles(area, subscription);
		DrawIndicator(area, _fastMa);
		DrawIndicator(area, _midMa);
		DrawIndicator(area, _slowMa);
		DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue,
				   decimal midValue, decimal slowValue) {
	if (candle.State != CandleStates.Finished)
		return;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	var hour = candle.OpenTime.Hour;
	var day = candle.OpenTime.DayOfWeek;

	if (hour < StartHour || hour >= EndHour)
		return;

	if (day == DayOfWeek.Friday && !FridayTrade)
		return;

	if (day == DayOfWeek.Monday && !MondayTrade)
		return;

	if (!_isInitialized && _fastMa.IsFormed && _slowMa.IsFormed &&
		_midMa.IsFormed) {
		_prevFast = fastValue;
		_prevSlow = slowValue;
		_prevFastMid = fastValue;
		_prevMid = midValue;
		_isInitialized = true;
		_lowestLow = candle.LowPrice;
		_highestHigh = candle.HighPrice;
		return;
	}

	if (!_isInitialized)
		return;

	if (fastValue < slowValue) {
		if (!_trackingDown) {
		_trackingDown = true;
		_lowestLow = candle.LowPrice;
		} else
		_lowestLow = Math.Min(_lowestLow, candle.LowPrice);

		_trackingUp = false;
	} else if (fastValue > slowValue) {
		if (!_trackingUp) {
		_trackingUp = true;
		_highestHigh = candle.HighPrice;
		} else
		_highestHigh = Math.Max(_highestHigh, candle.HighPrice);

		_trackingDown = false;
	}

	var crossUp = _prevFast < _prevSlow && fastValue > slowValue;
	var crossDown = _prevFast > _prevSlow && fastValue < slowValue;

	if (Position == 0) {
		if (crossUp)
		OpenLong(candle);
		else if (crossDown)
		OpenShort(candle);
	} else {
		CheckTargets(candle, fastValue, midValue);
	}

	_prevFast = fastValue;
	_prevSlow = slowValue;
	_prevFastMid = fastValue;
	_prevMid = midValue;
	}

	private void OpenLong(ICandleMessage candle) {
	var diff = candle.ClosePrice - _lowestLow;
	var p1618 = _lowestLow + diff * 1.618m;
	var p0382 = _lowestLow + diff * 0.382m;
	_targetPrice = Math.Max(candle.ClosePrice + MinTakeProfit, p1618);
	_stopPrice = Math.Max(candle.ClosePrice - MaxStopLoss, p0382);

	BuyMarket(Volume);
	_entryPrice = candle.ClosePrice;
	_isLong = true;
	}

	private void OpenShort(ICandleMessage candle) {
	var diff = _highestHigh - candle.ClosePrice;
	var p1618 = _highestHigh - diff * 1.618m;
	var p0382 = _highestHigh - diff * 0.382m;
	_targetPrice = Math.Min(candle.ClosePrice - MinTakeProfit, p1618);
	_stopPrice = Math.Min(candle.ClosePrice + MaxStopLoss, p0382);

	SellMarket(Volume);
	_entryPrice = candle.ClosePrice;
	_isLong = false;
	}

	private void CheckTargets(ICandleMessage candle, decimal fastValue,
				  decimal midValue) {
	if (_isLong && Position > 0) {
		if (candle.LowPrice <= _stopPrice)
		SellMarket(Position);
		else if (candle.HighPrice >= _targetPrice)
		SellMarket(Position);
	} else if (!_isLong && Position < 0) {
		if (candle.HighPrice >= _stopPrice)
		BuyMarket(-Position);
		else if (candle.LowPrice <= _targetPrice)
		BuyMarket(-Position);
	}

	if (CloseAtFastMid && Position != 0) {
		var crossMidDown = _prevFastMid > _prevMid && fastValue < midValue;
		var crossMidUp = _prevFastMid < _prevMid && fastValue > midValue;

		if (_isLong && crossMidDown && candle.ClosePrice < _entryPrice)
		SellMarket(Position);
		else if (!_isLong && crossMidUp && candle.ClosePrice > _entryPrice)
		BuyMarket(-Position);
	}
	}
}
