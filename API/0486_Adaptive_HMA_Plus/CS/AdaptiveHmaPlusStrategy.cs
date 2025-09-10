using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on adaptive Hull Moving Average.
/// </summary>
public class AdaptiveHmaPlusStrategy : Strategy
{
    private readonly StrategyParam<int> _minPeriod;
    private readonly StrategyParam<int> _maxPeriod;
    private readonly StrategyParam<decimal> _adaptPercent;
    private readonly StrategyParam<decimal> _flatThreshold;
    private readonly StrategyParam<bool> _useVolume;
    private readonly StrategyParam<DataType> _candleType;

    private HullMovingAverage _hma;
    private AverageTrueRange _atrShort;
    private AverageTrueRange _atrLong;
    private SimpleMovingAverage _volumeSma;

    private decimal _dynamicLength;
    private decimal _prevHma;

    /// <summary>
    /// Minimum period for adaptive HMA.
    /// </summary>
    public int MinPeriod
    {
        get => _minPeriod.Value;
        set => _minPeriod.Value = value;
    }

    /// <summary>
    /// Maximum period for adaptive HMA.
    /// </summary>
    public int MaxPeriod
    {
        get => _maxPeriod.Value;
        set => _maxPeriod.Value = value;
    }

    /// <summary>
    /// Adaptation percentage per bar.
    /// </summary>
    public decimal AdaptPercent
    {
        get => _adaptPercent.Value;
        set => _adaptPercent.Value = value;
    }

    /// <summary>
    /// Threshold to treat slope as flat.
    /// </summary>
    public decimal FlatThreshold
    {
        get => _flatThreshold.Value;
        set => _flatThreshold.Value = value;
    }

    /// <summary>
    /// Use volume instead of volatility for adaptation.
    /// </summary>
    public bool UseVolume
    {
        get => _useVolume.Value;
        set => _useVolume.Value = value;
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
    /// Initialize a new instance of <see cref="AdaptiveHmaPlusStrategy"/>.
    /// </summary>
    public AdaptiveHmaPlusStrategy()
    {
        _minPeriod = Param(nameof(MinPeriod), 172)
            .SetDisplay("Min Period", "Minimum period for HMA", "General")
            .SetCanOptimize(true)
            .SetOptimize(100, 200, 10);

        _maxPeriod = Param(nameof(MaxPeriod), 233)
            .SetDisplay("Max Period", "Maximum period for HMA", "General")
            .SetCanOptimize(true)
            .SetOptimize(200, 260, 10);

        _adaptPercent = Param(nameof(AdaptPercent), 0.031m)
            .SetDisplay("Adapt Percent", "Percentage for length adaptation", "General")
            .SetCanOptimize(true)
            .SetOptimize(0.01m, 0.05m, 0.01m);

        _flatThreshold = Param(nameof(FlatThreshold), 0m)
            .SetDisplay("Flat Threshold", "Slope difference treated as flat", "General")
            .SetCanOptimize(true)
            .SetOptimize(0m, 1m, 0.1m);

        _useVolume = Param(nameof(UseVolume), false)
            .SetDisplay("Use Volume", "Adapt based on volume instead of volatility", "General");

        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
            .SetDisplay("Candle Type", "Type of candles to use", "General");
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
        _dynamicLength = 0;
        _prevHma = 0;
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
        base.OnStarted(time);

        _hma = new HullMovingAverage { Length = MinPeriod };
        _atrShort = new AverageTrueRange { Length = 14 };
        _atrLong = new AverageTrueRange { Length = 46 };
        _volumeSma = new SimpleMovingAverage { Length = 20 };

        var subscription = SubscribeCandles(CandleType);
        subscription
            .Bind(_hma, _atrShort, _atrLong, ProcessCandle)
            .Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, subscription);
            DrawIndicator(area, _hma);
            DrawOwnTrades(area);
        }
    }

    private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal atrShortValue, decimal atrLongValue)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var volumeSmaValue = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

        var plugged = UseVolume
            ? candle.TotalVolume > volumeSmaValue
            : atrShortValue > atrLongValue;

        if (_dynamicLength == 0)
            _dynamicLength = (MinPeriod + MaxPeriod) / 2m;

        _dynamicLength = plugged
            ? Math.Max(MinPeriod, _dynamicLength * (1 - AdaptPercent))
            : Math.Min(MaxPeriod, _dynamicLength * (1 + AdaptPercent));

        _hma.Length = (int)_dynamicLength;

        if (!IsFormedAndOnlineAndAllowTrading())
        {
            _prevHma = hmaValue;
            return;
        }

        var slope = hmaValue - _prevHma;

        var longSignal = slope >= FlatThreshold && plugged && Position <= 0;
        var shortSignal = slope <= -FlatThreshold && plugged && Position >= 0;
        var exitLong = Position > 0 && slope <= 0;
        var exitShort = Position < 0 && slope >= 0;

        if (longSignal)
        {
            var volume = Volume + Math.Abs(Position);
            BuyMarket(volume);
        }
        else if (shortSignal)
        {
            var volume = Volume + Math.Abs(Position);
            SellMarket(volume);
        }
        else if (exitLong)
        {
            SellMarket(Position);
        }
        else if (exitShort)
        {
            BuyMarket(Math.Abs(Position));
        }

        _prevHma = hmaValue;
    }
}

