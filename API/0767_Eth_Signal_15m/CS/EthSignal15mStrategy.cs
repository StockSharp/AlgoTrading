using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ETH Signal strategy for 15-minute timeframe.
/// Generates entries on Supertrend direction changes filtered by RSI levels.
/// Uses ATR-based stop loss and take profit.
/// </summary>
public class EthSignal15mStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;

	private decimal _supertrendValue;
	private bool _isLongTrend;
	private int _previousDirection;
	private decimal _lastClose;
	private decimal _entryPrice;

	/// <summary>
	/// ATR period for Supertrend and exit calculations.
	/// </summary>
	public int AtrPeriod
	{
	    get => _atrPeriod.Value;
	    set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Supertrend.
	/// </summary>
	public decimal Factor
	{
	    get => _factor.Value;
	    set => _factor.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
	    get => _rsiLength.Value;
	    set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
	    get => _rsiOverbought.Value;
	    set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
	    get => _rsiOversold.Value;
	    set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Backtest start time.
	/// </summary>
	public DateTimeOffset StartTime
	{
	    get => _startTime.Value;
	    set => _startTime.Value = value;
	}

	/// <summary>
	/// Backtest end time.
	/// </summary>
	public DateTimeOffset EndTime
	{
	    get => _endTime.Value;
	    set => _endTime.Value = value;
	}

	/// <summary>
	/// Initializes the strategy.
	/// </summary>
	public EthSignal15mStrategy()
	{
	    _atrPeriod = Param(nameof(AtrPeriod), 12)
	        .SetGreaterThanZero()
	        .SetDisplay("ATR Length", "ATR period for Supertrend and exits", "Indicators")
	        .SetCanOptimize(true)
	        .SetOptimize(7, 21, 1);

	    _factor = Param(nameof(Factor), 2.76m)
	        .SetGreaterThanZero()
	        .SetDisplay("Factor", "ATR multiplier for Supertrend", "Indicators")
	        .SetCanOptimize(true)
	        .SetOptimize(1m, 5m, 0.1m);

	    _rsiLength = Param(nameof(RsiLength), 12)
	        .SetGreaterThanZero()
	        .SetDisplay("RSI Length", "RSI calculation length", "Indicators")
	        .SetCanOptimize(true)
	        .SetOptimize(7, 21, 1);

	    _rsiOverbought = Param(nameof(RsiOverbought), 70m)
	        .SetRange(0m, 100m)
	        .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
	        .SetCanOptimize(true)
	        .SetOptimize(60m, 80m, 5m);

	    _rsiOversold = Param(nameof(RsiOversold), 30m)
	        .SetRange(0m, 100m)
	        .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
	        .SetCanOptimize(true)
	        .SetOptimize(20m, 40m, 5m);

	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
	        .SetDisplay("Candle Type", "Timeframe for candles", "General");

	    _startTime = Param(nameof(StartTime), new DateTimeOffset(new DateTime(2024, 8, 1)))
	        .SetDisplay("Start Time", "Backtest start time", "Time Settings");

	    _endTime = Param(nameof(EndTime), new DateTimeOffset(new DateTime(2054, 1, 1)))
	        .SetDisplay("End Time", "Backtest end time", "Time Settings");
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
	    _atr = null;
	    _rsi = null;
	    _supertrendValue = default;
	    _isLongTrend = default;
	    _previousDirection = default;
	    _lastClose = default;
	    _entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    _atr = new AverageTrueRange { Length = AtrPeriod };
	    _rsi = new RelativeStrengthIndex { Length = RsiLength };

	    var subscription = SubscribeCandles(CandleType);
	    subscription
	        .Bind(_atr, _rsi, ProcessCandle)
	        .Start();

	    var area = CreateChartArea();
	    if (area != null)
	    {
	        DrawCandles(area, subscription);
	        DrawIndicator(area, _rsi);
	        DrawOwnTrades(area);
	    }
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal rsiValue)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    var time = candle.OpenTime;
	    if (time < StartTime || time > EndTime)
	        return;

	    var medianPrice = (candle.HighPrice + candle.LowPrice) / 2;
	    var upperBand = medianPrice + atrValue * Factor;
	    var lowerBand = medianPrice - atrValue * Factor;

	    if (_lastClose == 0)
	    {
	        _supertrendValue = candle.ClosePrice > medianPrice ? lowerBand : upperBand;
	        _isLongTrend = candle.ClosePrice > _supertrendValue;
	        _previousDirection = _isLongTrend ? 1 : -1;
	        _lastClose = candle.ClosePrice;
	        return;
	    }

	    if (_isLongTrend)
	    {
	        if (candle.ClosePrice < _supertrendValue)
	        {
	            _isLongTrend = false;
	            _supertrendValue = upperBand;
	        }
	        else
	        {
	            _supertrendValue = Math.Max(lowerBand, _supertrendValue);
	        }
	    }
	    else
	    {
	        if (candle.ClosePrice > _supertrendValue)
	        {
	            _isLongTrend = true;
	            _supertrendValue = lowerBand;
	        }
	        else
	        {
	            _supertrendValue = Math.Min(upperBand, _supertrendValue);
	        }
	    }

	    var direction = _isLongTrend ? 1 : -1;
	    var change = direction - _previousDirection;

	    if (change < 0 && rsiValue < RsiOverbought && Position <= 0)
	    {
	        BuyMarket();
	        _entryPrice = candle.ClosePrice;
	    }
	    else if (change > 0 && rsiValue > RsiOversold && Position >= 0)
	    {
	        SellMarket();
	        _entryPrice = candle.ClosePrice;
	    }
	    else if (Position > 0)
	    {
	        var stop = _entryPrice - atrValue * 4m;
	        var target = _entryPrice + atrValue * 2m;
	        if (candle.LowPrice <= stop || candle.HighPrice >= target)
	        {
	            SellMarket(Position);
	            _entryPrice = default;
	        }
	    }
	    else if (Position < 0)
	    {
	        var stop = _entryPrice + atrValue * 4m;
	        var target = _entryPrice - atrValue * 2.237m;
	        if (candle.HighPrice >= stop || candle.LowPrice <= target)
	        {
	            BuyMarket(Math.Abs(Position));
	            _entryPrice = default;
	        }
	    }

	    _previousDirection = direction;
	    _lastClose = candle.ClosePrice;
	}
}
