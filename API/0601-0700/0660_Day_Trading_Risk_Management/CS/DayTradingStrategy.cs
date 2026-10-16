using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intraday EMA cross with an RSI confirmation and native percent take-profit
/// and stop-loss protection on actual executions.
/// </summary>
public class DayTradingStrategy : Strategy
{
    private readonly StrategyParam<int> _fastEmaPeriod;
    private readonly StrategyParam<int> _slowEmaPeriod;
    private readonly StrategyParam<int> _rsiLength;
    private readonly StrategyParam<decimal> _rsiOverbought;
    private readonly StrategyParam<decimal> _rsiOversold;
    private readonly StrategyParam<decimal> _takeProfitPercent;
    private readonly StrategyParam<decimal> _stopLossPercent;
    private readonly StrategyParam<DataType> _candleType;

    private ExponentialMovingAverage _fast;
    private ExponentialMovingAverage _slow;
    private RelativeStrengthIndex _rsi;
    private decimal? _previousFast;
    private decimal? _previousSlow;

    public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }
    public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }
    public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
    public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
    public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
    public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
    public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    public DayTradingStrategy()
    {
        _fastEmaPeriod = Param(nameof(FastEmaPeriod), 20)
            .SetGreaterThanZero()
            .SetDisplay("Fast EMA Length", "Fast EMA signal length", "Indicators");
        _slowEmaPeriod = Param(nameof(SlowEmaPeriod), 50)
            .SetGreaterThanZero()
            .SetDisplay("Slow EMA Length", "Slow EMA signal length", "Indicators");
        _rsiLength = Param(nameof(RsiLength), 14)
            .SetGreaterThanZero()
            .SetDisplay("RSI Length", "RSI confirmation length", "Indicators");
        _rsiOverbought = Param(nameof(RsiOverbought), 70m)
            .SetRange(0m, 100m)
            .SetDisplay("RSI Overbought", "Strict upper RSI bound for long entries", "Signals");
        _rsiOversold = Param(nameof(RsiOversold), 30m)
            .SetRange(0m, 100m)
            .SetDisplay("RSI Oversold", "Strict lower RSI bound for short entries", "Signals");
        _takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
            .SetGreaterThanZero()
            .SetDisplay("Take Profit %", "Native take-profit percent on filled positions", "Risk");
        _stopLossPercent = Param(nameof(StopLossPercent), 1m)
            .SetGreaterThanZero()
            .SetDisplay("Stop Loss %", "Native stop-loss percent on filled positions", "Risk");
        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
            .SetDisplay("Candle Type", "Finished candles for EMA and RSI", "General");
    }

    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
    {
        return [(Security, CandleType)];
    }

    protected override void OnReseted()
    {
        base.OnReseted();
        _fast = null;
        _slow = null;
        _rsi = null;
        _previousFast = null;
        _previousSlow = null;
    }

    protected override void OnStarted2(DateTime time)
    {
        base.OnStarted2(time);

        if (FastEmaPeriod >= SlowEmaPeriod)
            throw new ArgumentException("FastEmaPeriod must be less than SlowEmaPeriod.");
        if (RsiOversold >= RsiOverbought)
            throw new ArgumentException("RsiOversold must be less than RsiOverbought.");

        _fast = new ExponentialMovingAverage { Length = FastEmaPeriod };
        _slow = new ExponentialMovingAverage { Length = SlowEmaPeriod };
        _rsi = new RelativeStrengthIndex { Length = RsiLength };
        _previousFast = null;
        _previousSlow = null;

        var subscription = SubscribeCandles(CandleType);
        subscription.Bind(_fast, _slow, _rsi, ProcessCandle).Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, subscription);
            DrawIndicator(area, _fast);
            DrawIndicator(area, _slow);
            DrawIndicator(area, _rsi);
            DrawOwnTrades(area);
        }

        StartProtection(
            new Unit(TakeProfitPercent, UnitTypes.Percent),
            new Unit(StopLossPercent, UnitTypes.Percent),
            false);
    }

    private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var crossUp = _previousFast is decimal prevFast &&
            _previousSlow is decimal prevSlow &&
            prevFast <= prevSlow && fast > slow;
        var crossDown = _previousFast is decimal prevFastDown &&
            _previousSlow is decimal prevSlowDown &&
            prevFastDown >= prevSlowDown && fast < slow;

        // Always record finished-bar averages, including warmup and periods
        // when signals are not permitted, so the next cross is genuine.
        _previousFast = fast;
        _previousSlow = slow;

        if (!_fast.IsFormed || !_slow.IsFormed || !_rsi.IsFormed ||
            !IsFormedAndOnlineAndAllowTrading())
            return;

        if (crossUp && rsi < RsiOverbought && Position <= 0m)
            BuyMarket(Volume + Math.Abs(Position));
        else if (crossDown && rsi > RsiOversold && Position >= 0m)
            SellMarket(Volume + Math.Abs(Position));
    }
}
