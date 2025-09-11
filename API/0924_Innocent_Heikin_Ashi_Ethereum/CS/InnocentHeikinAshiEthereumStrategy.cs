using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Goes long on Ethereum after bearish activity below EMA50 followed by a
/// bullish candle above EMA50.
/// </summary>
public class InnocentHeikinAshiEthereumStrategy : Strategy {
    private readonly StrategyParam<decimal> _riskReward;
    private readonly StrategyParam<int> _confirmationLevel;
    private readonly StrategyParam<bool> _enableMoonMode;
    private readonly StrategyParam<bool> _showSellSignals;
    private readonly StrategyParam<bool> _showBullTraps;
    private readonly StrategyParam<bool> _showBearTraps;
    private readonly StrategyParam<DataType> _candleType;

    private ExponentialMovingAverage _ema50;
    private ExponentialMovingAverage _ema200;
    private Lowest _lowest;

    private int? _lastRedVectorBelowEma50;
    private int? _lastBuySignalIndex;
    private int? _lastSellSignalIndex;
    private int _redCountUnderEma50;
    private int _greenCountAboveEma200;
    private int _barIndex;
    private decimal _prevHaOpen;
    private decimal _prevHaClose;
    private decimal _stopPrice;
    private decimal _takePrice;

    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="InnocentHeikinAshiEthereumStrategy"/> class.
    /// </summary>
    public InnocentHeikinAshiEthereumStrategy() {
	_riskReward =
	    Param(nameof(RiskReward), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Risk/Reward", "Take profit to stop ratio", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);

	_confirmationLevel =
	    Param(nameof(ConfirmationLevel), 1)
		.SetGreaterOrEqual(0)
		.SetDisplay(
		    "Confirmation Level",
		    "Number of red candles below EMA50 required before entry",
		    "General");

	_enableMoonMode =
	    Param(nameof(EnableMoonMode), true)
		.SetDisplay("Enable Moon Mode", "Allow entries above EMA200",
			    "General");

	_showSellSignals = Param(nameof(ShowSellSignals), true)
			       .SetDisplay("Show Sell Signals",
					   "Close on sell signals", "General");

	_showBullTraps =
	    Param(nameof(ShowBullTraps), true)
		.SetDisplay("Show Bull Traps",
			    "Close if next candle after buy is red", "General");

	_showBearTraps =
	    Param(nameof(ShowBearTraps), true)
		.SetDisplay("Show Bear Traps", "Close if sell signal fails",
			    "General");

	_candleType =
	    Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
    }

    /// <summary>
    /// Risk/reward multiplier.
    /// </summary>
    public decimal RiskReward {
	get => _riskReward.Value;
	set => _riskReward.Value = value;
    }

    /// <summary>
    /// Required number of red candles below EMA50.
    /// </summary>
    public int ConfirmationLevel {
	get => _confirmationLevel.Value;
	set => _confirmationLevel.Value = value;
    }

    /// <summary>
    /// Allow aggressive entries above EMA200.
    /// </summary>
    public bool EnableMoonMode {
	get => _enableMoonMode.Value;
	set => _enableMoonMode.Value = value;
    }

    /// <summary>
    /// Close position on sell signals.
    /// </summary>
    public bool ShowSellSignals {
	get => _showSellSignals.Value;
	set => _showSellSignals.Value = value;
    }

    /// <summary>
    /// Close if the candle after a buy is bearish.
    /// </summary>
    public bool ShowBullTraps {
	get => _showBullTraps.Value;
	set => _showBullTraps.Value = value;
    }

    /// <summary>
    /// Close if sell signal fails.
    /// </summary>
    public bool ShowBearTraps {
	get => _showBearTraps.Value;
	set => _showBearTraps.Value = value;
    }

    /// <summary>
    /// Candle type used for calculations.
    /// </summary>
    public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
    }

    /// <inheritdoc />
    public override IEnumerable<(Security sec, DataType dt)>
    GetWorkingSecurities() {
	return [(Security, CandleType)];
    }

    /// <inheritdoc />
    protected override void OnReseted() {
	base.OnReseted();
	_lastRedVectorBelowEma50 = null;
	_lastBuySignalIndex = null;
	_lastSellSignalIndex = null;
	_redCountUnderEma50 = 0;
	_greenCountAboveEma200 = 0;
	_barIndex = 0;
	_prevHaOpen = 0m;
	_prevHaClose = 0m;
	_stopPrice = 0m;
	_takePrice = 0m;
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);
	StartProtection();

	_ema50 = new ExponentialMovingAverage { Length = 50 };
	_ema200 = new ExponentialMovingAverage { Length = 200 };
	_lowest = new Lowest { Length = 28 };

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(_ema50, _ema200, _lowest, ProcessCandle).Start();
    }

    private void ProcessCandle(ICandleMessage candle, decimal ema50,
			       decimal ema200, decimal lowest) {
	if (candle.State != CandleStates.Finished)
	    return;

	decimal haOpen;
	decimal haClose;

	if (_barIndex == 0) {
	    haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
	    haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice +
		       candle.ClosePrice) /
		      4;
	} else {
	    haOpen = (_prevHaOpen + _prevHaClose) / 2;
	    haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice +
		       candle.ClosePrice) /
		      4;
	}

	var isGreen = haClose > haOpen;
	var isRed = !isGreen;

	if (isRed && candle.ClosePrice < ema50)
	    _redCountUnderEma50++;

	if (isRed && candle.OpenPrice < ema50 && candle.ClosePrice < ema50)
	    _lastRedVectorBelowEma50 = _barIndex;

	if (isGreen && candle.OpenPrice > ema200 && candle.OpenPrice > ema50)
	    _lastSellSignalIndex = _barIndex;

	if (isGreen && candle.ClosePrice > ema200)
	    _greenCountAboveEma200++;

	if (_lastRedVectorBelowEma50.HasValue && isGreen) {
	    _stopPrice = lowest;
	    _takePrice = candle.ClosePrice +
			 (candle.ClosePrice - _stopPrice) * RiskReward;
	}

	var canBuy =
	    _lastRedVectorBelowEma50.HasValue && isGreen &&
	    candle.OpenPrice > ema50 &&
	    (_lastBuySignalIndex == null || _barIndex > _lastBuySignalIndex);

	if (canBuy) {
	    if (candle.ClosePrice < ema200 &&
		_redCountUnderEma50 >= ConfirmationLevel) {
		BuyMarket();
		_lastBuySignalIndex = _barIndex;
		_lastRedVectorBelowEma50 = null;
		_redCountUnderEma50 = 0;
	    } else if (EnableMoonMode && candle.ClosePrice > ema200 &&
		       _redCountUnderEma50 >= ConfirmationLevel) {
		BuyMarket();
		_lastBuySignalIndex = _barIndex;
		_lastRedVectorBelowEma50 = null;
		_redCountUnderEma50 = 0;
	    }
	}

	if (Position > 0) {
	    if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
		SellMarket();
	}

	if (ShowSellSignals && _lastSellSignalIndex.HasValue && isRed &&
	    candle.OpenPrice > ema200 && candle.ClosePrice > ema200 &&
	    _barIndex == _lastSellSignalIndex + 1) {
	    if (_greenCountAboveEma200 >= ConfirmationLevel) {
		SellMarket();
		_lastSellSignalIndex = null;
		_greenCountAboveEma200 = 0;
	    }
	}

	if (ShowBullTraps && _lastBuySignalIndex.HasValue &&
	    _barIndex == _lastBuySignalIndex + 1 && isRed)
	    SellMarket();

	if (ShowBearTraps && _lastSellSignalIndex.HasValue &&
	    _barIndex == _lastSellSignalIndex + 1 && isGreen) {
	    SellMarket();
	    _lastSellSignalIndex = null;
	    _greenCountAboveEma200 = 0;
	}

	_prevHaOpen = haOpen;
	_prevHaClose = haClose;
	_barIndex++;
    }
}
