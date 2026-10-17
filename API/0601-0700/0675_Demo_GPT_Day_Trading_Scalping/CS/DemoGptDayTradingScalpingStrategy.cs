using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Two-candle scalp: long on two green candles with a gap up and a volume
/// spike; exit on two red candles with a gap down and a volume spike.
/// EMA20, session VWAP and prior-day Camarilla levels are reference-only.
/// </summary>
public class DemoGptDayTradingScalpingStrategy : Strategy
{
    private readonly StrategyParam<int> _emaLength;
    private readonly StrategyParam<int> _volumeLookback;
    private readonly StrategyParam<DataType> _candleType;

    private ExponentialMovingAverage _ema;
    private readonly List<decimal> _priorVolumes = [];
    private decimal? _previousClose;
    private bool _previousGreen;
    private bool _previousRed;
    private DateTime? _sessionDate;
    private decimal _sessionHigh;
    private decimal _sessionLow;
    private decimal _sessionClose;
    private decimal _sessionSumPriceVolume;
    private decimal _sessionSumVolume;

    public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
    public int VolumeLookback { get => _volumeLookback.Value; set => _volumeLookback.Value = value; }
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    /// <summary>Reference-only EMA of close prices.</summary>
    public decimal ReferenceEma { get; private set; }
    /// <summary>Current session VWAP, weighted by total candle volume.</summary>
    public decimal ReferenceVwap { get; private set; }
    /// <summary>Prior-day Camarilla H3 level.</summary>
    public decimal ReferenceCamarillaH3 { get; private set; }
    /// <summary>Prior-day Camarilla L3 level.</summary>
    public decimal ReferenceCamarillaL3 { get; private set; }

    public DemoGptDayTradingScalpingStrategy()
    {
        _emaLength = Param(nameof(EmaLength), 20)
            .SetGreaterThanZero()
            .SetDisplay("EMA Length", "Reference-only exponential moving average", "Indicators");
        _volumeLookback = Param(nameof(VolumeLookback), 20)
            .SetGreaterThanZero()
            .SetDisplay("Volume Lookback", "Preceding candles in the volume confirmation", "Signals");
        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
            .SetDisplay("Candle Type", "Scalping candle interval", "General");
    }

    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
    {
        return [(Security, CandleType)];
    }

    protected override void OnReseted()
    {
        base.OnReseted();
        _ema = null;
        _priorVolumes.Clear();
        _previousClose = null;
        _previousGreen = false;
        _previousRed = false;
        _sessionDate = null;
        _sessionHigh = 0m;
        _sessionLow = 0m;
        _sessionClose = 0m;
        _sessionSumPriceVolume = 0m;
        _sessionSumVolume = 0m;
        ReferenceEma = 0m;
        ReferenceVwap = 0m;
        ReferenceCamarillaH3 = 0m;
        ReferenceCamarillaL3 = 0m;
    }

    protected override void OnStarted2(DateTime time)
    {
        base.OnStarted2(time);
        _ema = new ExponentialMovingAverage { Length = EmaLength };
        _priorVolumes.Clear();
        _previousClose = null;
        _previousGreen = false;
        _previousRed = false;
        _sessionDate = null;
        _sessionSumPriceVolume = 0m;
        _sessionSumVolume = 0m;

        var subscription = SubscribeCandles(CandleType);
        subscription.Bind(_ema, ProcessCandle).Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, subscription);
            DrawIndicator(area, _ema);
            DrawOwnTrades(area);
        }
    }

    private void ProcessCandle(ICandleMessage candle, decimal emaValue)
    {
        if (candle.State != CandleStates.Finished)
            return;

        ReferenceEma = emaValue;
        var date = candle.OpenTime.Date;

        if (_sessionDate != date)
        {
            if (_sessionDate is not null)
            {
                var range = _sessionHigh - _sessionLow;
                ReferenceCamarillaH3 = _sessionClose + range * 1.1m / 4m;
                ReferenceCamarillaL3 = _sessionClose - range * 1.1m / 4m;
            }

            _sessionDate = date;
            _sessionHigh = candle.HighPrice;
            _sessionLow = candle.LowPrice;
            _sessionSumPriceVolume = 0m;
            _sessionSumVolume = 0m;
        }

        _sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
        _sessionLow = Math.Min(_sessionLow, candle.LowPrice);
        _sessionClose = candle.ClosePrice;

        var volume = Math.Max(0m, candle.TotalVolume);
        _sessionSumPriceVolume += ((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m) * volume;
        _sessionSumVolume += volume;
        ReferenceVwap = _sessionSumVolume > 0m
            ? _sessionSumPriceVolume / _sessionSumVolume
            : candle.ClosePrice;

        decimal volumeSum = 0m;
        foreach (var previousVolume in _priorVolumes)
            volumeSum += previousVolume;
        var volumeConfirmed = _priorVolumes.Count >= VolumeLookback &&
            candle.TotalVolume > volumeSum / VolumeLookback;

        var green = candle.ClosePrice > candle.OpenPrice;
        var red = candle.ClosePrice < candle.OpenPrice;
        var upGap = _previousClose is decimal lastCloseUp && candle.OpenPrice > lastCloseUp;
        var downGap = _previousClose is decimal lastCloseDown && candle.OpenPrice < lastCloseDown;

        // The previous-candle flags are updated only AFTER testing signals.
        var buy = _previousGreen && green && upGap && volumeConfirmed;
        var exit = _previousRed && red && downGap && volumeConfirmed;

        _previousClose = candle.ClosePrice;
        _previousGreen = green;
        _previousRed = red;

        _priorVolumes.Add(volume);
        if (_priorVolumes.Count > VolumeLookback)
            _priorVolumes.RemoveAt(0);

        if (!IsFormedAndOnlineAndAllowTrading())
            return;

        if (Position == 0m && buy)
            BuyMarket();
        else if (Position > 0m && exit)
            SellMarket(Position);
    }
}
