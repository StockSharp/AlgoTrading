namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Williams %R based multi-currency strategy with trailing stop logic.
/// </summary>
public class ForexFrausPortfolioStrategy : Strategy {
  private readonly StrategyParam<int> _wprPeriod;
  private readonly StrategyParam<decimal> _buyThreshold;
  private readonly StrategyParam<decimal> _sellThreshold;
  private readonly StrategyParam<int> _startHour;
  private readonly StrategyParam<int> _stopHour;
  private readonly StrategyParam<int> _slPoints;
  private readonly StrategyParam<int> _tpPoints;
  private readonly StrategyParam<bool> _useTrailing;
  private readonly StrategyParam<int> _trailingStop;
  private readonly StrategyParam<int> _trailingStep;
  private readonly StrategyParam<DataType> _candleType;

  private decimal _entryPrice;
  private decimal _takePrice;
  private decimal _stopPrice;
  private bool _okBuy;
  private bool _okSell;

  public int WprPeriod {
	get => _wprPeriod.Value;
	set => _wprPeriod.Value = value;
  }
  public decimal BuyThreshold {
	get => _buyThreshold.Value;
	set => _buyThreshold.Value = value;
  }
  public decimal SellThreshold {
	get => _sellThreshold.Value;
	set => _sellThreshold.Value = value;
  }
  public int StartHour {
	get => _startHour.Value;
	set => _startHour.Value = value;
  }
  public int StopHour {
	get => _stopHour.Value;
	set => _stopHour.Value = value;
  }
  public int SlPoints {
	get => _slPoints.Value;
	set => _slPoints.Value = value;
  }
  public int TpPoints {
	get => _tpPoints.Value;
	set => _tpPoints.Value = value;
  }
  public bool UseTrailing {
	get => _useTrailing.Value;
	set => _useTrailing.Value = value;
  }
  public int TrailingStop {
	get => _trailingStop.Value;
	set => _trailingStop.Value = value;
  }
  public int TrailingStep {
	get => _trailingStep.Value;
	set => _trailingStep.Value = value;
  }
  public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
  }

  public ForexFrausPortfolioStrategy() {
	_wprPeriod = Param(nameof(WprPeriod), 360)
					 .SetDisplay("WPR Period", "Williams %R calculation period",
								 "Parameters")
					 .SetCanOptimize(true)
					 .SetOptimize(50, 500, 50);

	_buyThreshold =
		Param(nameof(BuyThreshold), -99.9m)
			.SetDisplay("Buy Threshold", "Trigger level for long entry",
						"Parameters");

	_sellThreshold =
		Param(nameof(SellThreshold), -0.1m)
			.SetDisplay("Sell Threshold", "Trigger level for short entry",
						"Parameters");

	_startHour = Param(nameof(StartHour), 7)
					 .SetDisplay("Start Hour", "Trading start hour", "Time");

	_stopHour = Param(nameof(StopHour), 17)
					.SetDisplay("Stop Hour", "Trading stop hour", "Time");

	_slPoints = Param(nameof(SlPoints), 0)
					.SetDisplay("Stop Loss (points)",
								"Protective stop loss in points", "Protection");

	_tpPoints = Param(nameof(TpPoints), 0)
					.SetDisplay("Take Profit (points)", "Take profit in points",
								"Protection");

	_useTrailing =
		Param(nameof(UseTrailing), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Protection");

	_trailingStop =
		Param(nameof(TrailingStop), 30)
			.SetDisplay("Trailing Stop", "Trailing stop distance in points",
						"Protection");

	_trailingStep = Param(nameof(TrailingStep), 1)
						.SetDisplay("Trailing Step", "Trailing step in points",
									"Protection");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

	_entryPrice = default;
	_takePrice = default;
	_stopPrice = default;
	_okBuy = false;
	_okSell = false;
  }

  /// <inheritdoc />
  protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);

	StartProtection();

	var wpr = new WilliamsPercentRange { Length = WprPeriod };

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(wpr, ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null) {
	  DrawCandles(area, subscription);
	  DrawOwnTrades(area);
	}
  }

  private void ProcessCandle(ICandleMessage candle, decimal wprValue) {
	if (candle.State != CandleStates.Finished)
	  return;

	var hour = candle.OpenTime.Hour;
	var inTime = StartHour <= StopHour ? hour >= StartHour && hour < StopHour
									   : hour >= StartHour || hour < StopHour;

	if (!inTime) {
	  if (Position != 0)
		ClosePosition();
	  return;
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	  return;

	var close = candle.ClosePrice;

	if (wprValue < BuyThreshold)
	  _okBuy = true;

	if (wprValue > BuyThreshold && _okBuy) {
	  _okBuy = false;
	  OpenPosition(true, close);
	  return;
	}

	if (wprValue > SellThreshold)
	  _okSell = true;

	if (wprValue < SellThreshold && _okSell) {
	  _okSell = false;
	  OpenPosition(false, close);
	}

	TrailStop(close);
  }

  private void OpenPosition(bool isLong, decimal price) {
	if (isLong) {
	  BuyMarket();
	  _entryPrice = price;
	  if (SlPoints > 0)
		_stopPrice = price - SlPoints * Security.PriceStep;
	  if (TpPoints > 0)
		_takePrice = price + TpPoints * Security.PriceStep;
	  else
		_takePrice = 0m;
	} else {
	  SellMarket();
	  _entryPrice = price;
	  if (SlPoints > 0)
		_stopPrice = price + SlPoints * Security.PriceStep;
	  if (TpPoints > 0)
		_takePrice = price - TpPoints * Security.PriceStep;
	  else
		_takePrice = 0m;
	}
  }

  private void TrailStop(decimal price) {
	if (TpPoints > 0)
	  if (Position > 0 ? price >= _takePrice : price <= _takePrice)
		ClosePosition();

	if (!UseTrailing || Position == 0 || _entryPrice == 0)
	  return;

	var step = TrailingStep * Security.PriceStep;
	var dist = TrailingStop * Security.PriceStep;

	if (Position > 0) {
	  var profit = price - _entryPrice;
	  if (profit > dist && price - _stopPrice > step)
		_stopPrice = price - dist;
	  else if (price <= _stopPrice)
		ClosePosition();
	} else {
	  var profit = _entryPrice - price;
	  if (profit > dist && _stopPrice - price > step)
		_stopPrice = price + dist;
	  else if (price >= _stopPrice)
		ClosePosition();
	}
  }
}
