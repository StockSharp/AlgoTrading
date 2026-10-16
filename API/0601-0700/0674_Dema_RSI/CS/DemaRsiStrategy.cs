using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade strict crossings of RSI computed from a double exponential moving
/// average (DEMA) and its own SMA, with optional session and point protections.
/// </summary>
public class DemaRsiStrategy : Strategy
{
    private readonly StrategyParam<int> _maLength;
    private readonly StrategyParam<int> _rsiLength;
    private readonly StrategyParam<int> _rsiSmoothingLength;
    private readonly StrategyParam<decimal> _takeProfitPoints;
    private readonly StrategyParam<decimal> _trailStopPoints;
    private readonly StrategyParam<bool> _useSession;
    private readonly StrategyParam<TimeSpan> _sessionStart;
    private readonly StrategyParam<TimeSpan> _sessionEnd;
    private readonly StrategyParam<DataType> _candleType;

    private ExponentialMovingAverage _ema1;
    private ExponentialMovingAverage _ema2;
    private RelativeStrengthIndex _rsi;
    private SimpleMovingAverage _smoothRsi;
    private decimal? _previousRsi;
    private decimal? _previousSmooth;

    public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
    public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
    public int RsiSmoothingLength { get => _rsiSmoothingLength.Value; set => _rsiSmoothingLength.Value = value; }
    public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
    public decimal TrailStopPoints { get => _trailStopPoints.Value; set => _trailStopPoints.Value = value; }
    public bool UseSession { get => _useSession.Value; set => _useSession.Value = value; }
    public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }
    public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    public DemaRsiStrategy()
    {
        _maLength = Param(nameof(MaLength), 20)
            .SetGreaterThanZero().SetDisplay("MA Length", "DEMA first/second EMA length", "Indicators");
        _rsiLength = Param(nameof(RsiLength), 14)
            .SetGreaterThanZero().SetDisplay("RSI Length", "RSI of DEMA", "Indicators");
        _rsiSmoothingLength = Param(nameof(RsiSmoothingLength), 5)
            .SetGreaterThanZero().SetDisplay("RSI Smoothing Length", "SMA of RSI(DEMA)", "Indicators");
        _takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
            .SetRange(0m, 100000m).SetDisplay("Take Profit Points", "Take profit in security price ticks; zero disables", "Risk");
        _trailStopPoints = Param(nameof(TrailStopPoints), 0m)
            .SetRange(0m, 100000m).SetDisplay("Trail Stop Points", "Trailing stop in security price ticks; zero disables", "Risk");
        _useSession = Param(nameof(UseSession), false)
            .SetDisplay("Use Session", "Restrict signal entries to a time window", "Calendar");
        _sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(9))
            .SetDisplay("Session Start", "Inclusive start of entry session", "Calendar");
        _sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(17))
            .SetDisplay("Session End", "Exclusive end of entry session", "Calendar");
        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
            .SetDisplay("Candle Type", "Completed candle interval", "General");
    }

    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
    {
        return [(Security, CandleType)];
    }

    protected override void OnReseted()
    {
        base.OnReseted();
        _ema1 = null;
        _ema2 = null;
        _rsi = null;
        _smoothRsi = null;
        _previousRsi = null;
        _previousSmooth = null;
    }

    protected override void OnStarted2(DateTime time)
    {
        base.OnStarted2(time);
        if (SessionStart < TimeSpan.Zero || SessionStart >= TimeSpan.FromDays(1) ||
            SessionEnd < TimeSpan.Zero || SessionEnd >= TimeSpan.FromDays(1))
            throw new ArgumentException("Session times must be within one day.");

        _ema1 = new ExponentialMovingAverage { Length = MaLength };
        _ema2 = new ExponentialMovingAverage { Length = MaLength };
        _rsi = new RelativeStrengthIndex { Length = RsiLength };
        _smoothRsi = new SimpleMovingAverage { Length = RsiSmoothingLength };
        _previousRsi = null;
        _previousSmooth = null;

        var subscription = SubscribeCandles(CandleType);
        subscription.Bind(_ema1, ProcessCandle).Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, subscription);
            DrawIndicator(area, _ema1);
            DrawOwnTrades(area);
        }

        // Protection uses ABSOLUTE price distance in minimum price ticks, not percent.
        var tick = Security?.PriceStep ?? 0m;
        if (tick > 0m && (TakeProfitPoints > 0m || TrailStopPoints > 0m))
        {
            StartProtection(
                new Unit(TakeProfitPoints * tick, UnitTypes.Absolute),
                new Unit(TrailStopPoints * tick, UnitTypes.Absolute),
                TrailStopPoints > 0m);
        }
    }

    private bool IsInSession(TimeSpan time)
    {
        if (!UseSession || SessionStart == SessionEnd)
            return true;

        return SessionStart < SessionEnd
            ? time >= SessionStart && time < SessionEnd
            : time >= SessionStart || time < SessionEnd;
    }

    private void ProcessCandle(ICandleMessage candle, decimal firstEma)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var secondEma = _ema2.Process(firstEma, candle.OpenTime, true).ToDecimal();
        var dema = 2m * firstEma - secondEma;
        var rsi = _rsi.Process(dema, candle.OpenTime, true).ToDecimal();
        var smoothed = _smoothRsi.Process(rsi, candle.OpenTime, true).ToDecimal();

        var crossUp = _previousRsi is decimal pr && _previousSmooth is decimal ps &&
            pr <= ps && rsi > smoothed;
        var crossDown = _previousRsi is decimal qr && _previousSmooth is decimal qs &&
            qr >= qs && rsi < smoothed;

        _previousRsi = rsi;
        _previousSmooth = smoothed;

        if (!_ema1.IsFormed || !_ema2.IsFormed || !_rsi.IsFormed || !_smoothRsi.IsFormed ||
            !IsFormedAndOnlineAndAllowTrading() || !IsInSession(candle.OpenTime.TimeOfDay))
            return;

        if (crossUp && Position <= 0m)
            BuyMarket(Volume + Math.Abs(Position));
        else if (crossDown && Position >= 0m)
            SellMarket(Volume + Math.Abs(Position));
    }
}
