using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based hybrid scalping strategy with adjustable sensitivity.
/// Combines RSI, EMA trend filters and optional volume filter.
/// </summary>
public class HybridScalpingBotStrategy : Strategy
{
	private readonly StrategyParam<int> _dailyTradeLimit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useQuickExit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<string> _signalSensitivity;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<bool> _useVolumeFilter;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private int _tradesToday;
	private DateTime _lastDate;
	private bool _isLong;

	/// <summary>
	/// Maximum trades per day.
	/// </summary>
	public int DailyTradeLimit { get => _dailyTradeLimit.Value; set => _dailyTradeLimit.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Exit early on RSI or EMA reversal.
	/// </summary>
	public bool UseQuickExit { get => _useQuickExit.Value; set => _useQuickExit.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent { get => _trailingStopPercent.Value; set => _trailingStopPercent.Value = value; }

	/// <summary>
	/// Signal sensitivity level.
	/// </summary>
	public string SignalSensitivity { get => _signalSensitivity.Value; set => _signalSensitivity.Value = value; }

	/// <summary>
	/// Use EMA trend filter.
	/// </summary>
	public bool UseTrendFilter { get => _useTrendFilter.Value; set => _useTrendFilter.Value = value; }

	/// <summary>
	/// Require high volume.
	/// </summary>
	public bool UseVolumeFilter { get => _useVolumeFilter.Value; set => _useVolumeFilter.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="HybridScalpingBotStrategy"/> class.
	/// </summary>
	public HybridScalpingBotStrategy()
	{
	    _dailyTradeLimit = Param(nameof(DailyTradeLimit), 15)
	        .SetGreaterThanZero()
	        .SetDisplay("Daily Trades", "Maximum trades per day", "General");

	    _takeProfitPercent = Param(nameof(TakeProfitPercent), 0.8m)
	        .SetGreaterThanZero()
	        .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
	        .SetCanOptimize(true)
	        .SetOptimize(0.5m, 3m, 0.5m);

	    _stopLossPercent = Param(nameof(StopLossPercent), 0.6m)
	        .SetGreaterThanZero()
	        .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
	        .SetCanOptimize(true)
	        .SetOptimize(0.5m, 2m, 0.5m);

	    _useQuickExit = Param(nameof(UseQuickExit), true)
	        .SetDisplay("Use Quick Exit", "Exit on RSI or EMA pullback", "General");

	    _useTrailingStop = Param(nameof(UseTrailingStop), true)
	        .SetDisplay("Use Trailing Stop", "Trail profit after entry", "General");

	    _trailingStopPercent = Param(nameof(TrailingStopPercent), 0.4m)
	        .SetGreaterThanZero()
	        .SetDisplay("Trailing Stop %", "Trailing stop percent", "Risk")
	        .SetCanOptimize(true)
	        .SetOptimize(0.2m, 1m, 0.2m);

	    _signalSensitivity = Param(nameof(SignalSensitivity), "Easy")
	        .SetDisplay("Signal Level", "VeryEasy / Easy / Medium / Strong", "General");

	    _useTrendFilter = Param(nameof(UseTrendFilter), true)
	        .SetDisplay("Use Trend Filter", "Trade only with trend", "General");

	    _useVolumeFilter = Param(nameof(UseVolumeFilter), false)
	        .SetDisplay("Use Volume Filter", "Require high volume", "General");

	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	    return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    var rsi = new RSI { Length = 14 };
	    var ema9 = new EMA { Length = 9 };
	    var ema21 = new EMA { Length = 21 };
	    var ema50 = new EMA { Length = 50 };
	    var volumeSma = new SMA { Length = 10 };

	    var subscription = SubscribeCandles(CandleType);
	    subscription
	        .Bind(rsi, ema9, ema21, ema50, volumeSma, ProcessCandle)
	        .Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal ema9, decimal ema21, decimal ema50, decimal avgVolume)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    if (candle.OpenTime.Date != _lastDate)
	    {
	        _tradesToday = 0;
	        _lastDate = candle.OpenTime.Date;
	    }

	    var bullish = candle.ClosePrice > candle.OpenPrice;
	    var bearish = candle.ClosePrice < candle.OpenPrice;
	    var bodyRatio = candle.HighPrice == candle.LowPrice ? 0m : (candle.ClosePrice - candle.OpenPrice) / (candle.HighPrice - candle.LowPrice);
	    var strongBullish = bullish && bodyRatio > 0.6m;
	    var strongBearish = bearish && bodyRatio > 0.6m;

	    var uptrend = ema21 > ema50;
	    var downtrend = ema21 < ema50;
	    var strongUptrend = ema9 > ema21 && ema21 > ema50;
	    var strongDowntrend = ema9 < ema21 && ema21 < ema50;

	    var volumeOk = !UseVolumeFilter || candle.TotalVolume > avgVolume * 1.2m;

	    bool buySignal;
	    bool sellSignal;

	    switch (SignalSensitivity)
	    {
	        case "VeryEasy":
	            buySignal = rsi < 60 && bullish;
	            sellSignal = rsi > 40 && bearish;
	            break;
	        case "Medium":
	            buySignal = rsi < 30 && bullish && (!UseTrendFilter || uptrend);
	            sellSignal = rsi > 70 && bearish && (!UseTrendFilter || downtrend);
	            break;
	        case "Strong":
	            buySignal = rsi < 30 && strongBullish && (!UseTrendFilter || strongUptrend) && volumeOk && candle.ClosePrice > ema21;
	            sellSignal = rsi > 70 && strongBearish && (!UseTrendFilter || strongDowntrend) && volumeOk && candle.ClosePrice < ema21;
	            break;
	        default:
	            buySignal = rsi < 30 && bullish;
	            sellSignal = rsi > 70 && bearish;
	            break;
	    }

	    var canTrade = _tradesToday < DailyTradeLimit && Position == 0;

	    if (buySignal && canTrade)
	    {
	        BuyMarket();
	        _tradesToday++;
	        _entryPrice = candle.ClosePrice;
	        _highestPrice = candle.ClosePrice;
	        _isLong = true;
	    }
	    else if (sellSignal && canTrade)
	    {
	        SellMarket();
	        _tradesToday++;
	        _entryPrice = candle.ClosePrice;
	        _lowestPrice = candle.ClosePrice;
	        _isLong = false;
	    }

	    if (Position > 0)
	    {
	        _highestPrice = Math.Max(_highestPrice, candle.HighPrice);

	        if (UseTrailingStop)
	        {
	            var trail = _highestPrice * (1 - TrailingStopPercent / 100m);
	            if (candle.ClosePrice <= trail)
	                SellMarket();
	        }

	        if (candle.ClosePrice <= _entryPrice * (1 - StopLossPercent / 100m))
	            SellMarket();
	        else if (candle.ClosePrice >= _entryPrice * (1 + TakeProfitPercent / 100m))
	            SellMarket();
	        else if (UseQuickExit && (rsi > 70 || rsi < 25 || candle.ClosePrice < ema21))
	            SellMarket();
	    }
	    else if (Position < 0)
	    {
	        _lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);

	        if (UseTrailingStop)
	        {
	            var trail = _lowestPrice * (1 + TrailingStopPercent / 100m);
	            if (candle.ClosePrice >= trail)
	                BuyMarket();
	        }

	        if (candle.ClosePrice >= _entryPrice * (1 + StopLossPercent / 100m))
	            BuyMarket();
	        else if (candle.ClosePrice <= _entryPrice * (1 - TakeProfitPercent / 100m))
	            BuyMarket();
	        else if (UseQuickExit && (rsi < 30 || rsi > 75 || candle.ClosePrice > ema21))
	            BuyMarket();
	    }
	}
}
