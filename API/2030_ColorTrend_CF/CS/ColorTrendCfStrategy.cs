using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossing of two exponential moving averages.
/// Shorter EMA represents rising pressure while longer EMA acts as trend
/// filter.
/// </summary>
public class ColorTrendCfStrategy : Strategy {
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLong;

	/// <summary>
	/// Base period for the fast EMA. Slow EMA uses double value.
	/// </summary>
	public int Period {
	get => _period.Value;
	set => _period.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss {
	get => _stopLoss.Value;
	set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit {
	get => _takeProfit.Value;
	set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Whether long entries are allowed.
	/// </summary>
	public bool AllowBuyOpen {
	get => _allowBuyOpen.Value;
	set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Whether short entries are allowed.
	/// </summary>
	public bool AllowSellOpen {
	get => _allowSellOpen.Value;
	set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Whether closing long positions on sell signals is allowed.
	/// </summary>
	public bool AllowBuyClose {
	get => _allowBuyClose.Value;
	set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Whether closing short positions on buy signals is allowed.
	/// </summary>
	public bool AllowSellClose {
	get => _allowSellClose.Value;
	set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculation.
	/// </summary>
	public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorTrendCfStrategy"/>.
	/// </summary>
	public ColorTrendCfStrategy() {
	_period = Param(nameof(Period), 30)
			  .SetGreaterThanZero()
			  .SetDisplay("CF Period", "Base period for fast EMA",
				  "Indicator")
			  .SetCanOptimize(true)
			  .SetOptimize(10, 60, 10);

	_stopLoss =
		Param(nameof(StopLoss), 1000m)
		.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 100m);

	_takeProfit =
		Param(nameof(TakeProfit), 2000m)
		.SetDisplay("Take Profit", "Take profit in price units", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 4000m, 100m);

	_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
				.SetDisplay("Allow Buy", "Permission to open long",
					"Permissions");

	_allowSellOpen =
		Param(nameof(AllowSellOpen), true)
		.SetDisplay("Allow Sell", "Permission to open short",
				"Permissions");

	_allowBuyClose =
		Param(nameof(AllowBuyClose), true)
		.SetDisplay("Close Long", "Allow closing long positions",
				"Permissions");

	_allowSellClose =
		Param(nameof(AllowSellClose), true)
		.SetDisplay("Close Short", "Allow closing short positions",
				"Permissions");

	_candleType =
		Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for indicator",
				"General");
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
	_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);

	var fastEma = new ExponentialMovingAverage { Length = Period };
	var slowEma = new ExponentialMovingAverage { Length = Period * 2 };

	var subscription = SubscribeCandles(CandleType);
	var prevFast = 0m;
	var prevSlow = 0m;
	var initialized = false;

	subscription
		.Bind(fastEma, slowEma,
		  (candle, fast, slow) => {
			  if (candle.State != CandleStates.Finished)
			  return;

			  if (!IsFormedAndOnlineAndAllowTrading())
			  return;

			  if (!initialized) {
			  prevFast = fast;
			  prevSlow = slow;
			  initialized = true;
			  return;
			  }

			  var crossUp = prevFast <= prevSlow && fast > slow;
			  var crossDown = prevFast >= prevSlow && fast < slow;

			  prevFast = fast;
			  prevSlow = slow;

			  if (crossUp) {
			  if (AllowSellClose && Position < 0)
				  BuyMarket(Math.Abs(Position));

			  if (AllowBuyOpen && Position <= 0) {
				  _entryPrice = candle.ClosePrice;
				  _isLong = true;
				  BuyMarket(Volume + Math.Abs(Position));
			  }
			  } else if (crossDown) {
			  if (AllowBuyClose && Position > 0)
				  SellMarket(Position);

			  if (AllowSellOpen && Position >= 0) {
				  _entryPrice = candle.ClosePrice;
				  _isLong = false;
				  SellMarket(Volume + Math.Abs(Position));
			  }
			  }

			  if (_entryPrice != 0m) {
			  if (_isLong && Position > 0) {
				  var stop = _entryPrice - StopLoss;
				  var take = _entryPrice + TakeProfit;
				  if (candle.LowPrice <= stop ||
				  candle.HighPrice >= take)
				  SellMarket(Position);
			  } else if (!_isLong && Position < 0) {
				  var stop = _entryPrice + StopLoss;
				  var take = _entryPrice - TakeProfit;
				  if (candle.HighPrice >= stop ||
				  candle.LowPrice <= take)
				  BuyMarket(Math.Abs(Position));
			  }
			  }
		  })
		.Start();

	StartProtection();
	}
}
