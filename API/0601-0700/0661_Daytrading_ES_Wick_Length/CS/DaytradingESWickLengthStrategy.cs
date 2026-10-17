using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only wick-length strategy. Compares total candle-shadow length with
/// the preceding bars' volume-weighted (or simple) shadow average plus a
/// configurable offset in the traded security's price ticks.
/// </summary>
public class DaytradingESWickLengthStrategy : Strategy
{
    private readonly StrategyParam<int> _maLength;
    private readonly StrategyParam<string> _maType;
    private readonly StrategyParam<decimal> _maOffset;
    private readonly StrategyParam<int> _holdPeriods;
    private readonly StrategyParam<DataType> _candleType;
    private readonly List<(decimal wick, decimal volume)> _samples = [];

    private int _finishedBars;
    private int _entryBar;

    public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
    public string MaType { get => _maType.Value; set => _maType.Value = value; }
    public decimal MaOffset { get => _maOffset.Value; set => _maOffset.Value = value; }
    public int HoldPeriods { get => _holdPeriods.Value; set => _holdPeriods.Value = value; }
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    public DaytradingESWickLengthStrategy()
    {
        _maLength = Param(nameof(MaLength), 20)
            .SetGreaterThanZero()
            .SetDisplay("MA Length", "Number of preceding completed candles used for the wick average", "Indicators");

        _maType = Param(nameof(MaType), "VolumeWeighted")
            .SetDisplay("MA Type", "VolumeWeighted or Simple average of preceding candle wicks", "Indicators");

        _maOffset = Param(nameof(MaOffset), 10m)
            .SetRange(0m, 100000m)
            .SetDisplay("MA Offset", "Wick-length threshold offset, measured in security price ticks", "Signals");

        _holdPeriods = Param(nameof(HoldPeriods), 18)
            .SetGreaterThanZero()
            .SetDisplay("Hold Periods", "Completed candles to hold a filled long position", "Trading");

        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
            .SetDisplay("Candle Type", "Signal candle interval", "General");
    }

    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
    {
        return [(Security, CandleType)];
    }

    protected override void OnReseted()
    {
        base.OnReseted();
        _samples.Clear();
        _finishedBars = 0;
        _entryBar = 0;
    }

    protected override void OnStarted2(DateTime time)
    {
        base.OnStarted2(time);

        if (MaType != "VolumeWeighted" && MaType != "Simple")
            throw new ArgumentException("MaType must be VolumeWeighted or Simple.", nameof(MaType));

        _samples.Clear();
        _finishedBars = 0;
        _entryBar = 0;

        var subscription = SubscribeCandles(CandleType);
        subscription.Bind(ProcessCandle).Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, subscription);
            DrawOwnTrades(area);
        }
    }

    /// <summary>Combined upper and lower candle-shadow length.</summary>
    public static decimal CalculateWickLength(decimal open, decimal high, decimal low, decimal close)
        => Math.Max(0m, high - Math.Max(open, close)) +
           Math.Max(0m, Math.Min(open, close) - low);

    private void ProcessCandle(ICandleMessage candle)
    {
        if (candle.State != CandleStates.Finished)
            return;

        _finishedBars++;
        var wick = CalculateWickLength(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);

        // Compute the threshold from completed PRIOR candles, without the
        // current signal candle leaking into its own volume-weighted average.
        var formed = _samples.Count >= MaLength;
        decimal average = 0m;
        if (formed)
        {
            decimal sumWick = 0m;
            decimal sumVolume = 0m;
            decimal weightedWick = 0m;

            foreach (var sample in _samples)
            {
                sumWick += sample.wick;
                sumVolume += sample.volume;
                weightedWick += sample.wick * sample.volume;
            }

            average = MaType == "VolumeWeighted" && sumVolume > 0m
                ? weightedWick / sumVolume
                : sumWick / _samples.Count;
        }

        _samples.Add((wick, Math.Max(0m, candle.TotalVolume)));
        if (_samples.Count > MaLength)
            _samples.RemoveAt(0);

        if (!formed || !IsFormedAndOnlineAndAllowTrading())
            return;

        // Exits are independent of new wick signals; close the ACTUAL residual
        // size, not the nominal default order size.
        if (Position > 0m)
        {
            if (_entryBar > 0 && _finishedBars - _entryBar >= HoldPeriods)
            {
                SellMarket(Position);
                _entryBar = 0;
            }
        }
        else if (Position == 0m && _entryBar == 0)
        {
            var tick = Security?.PriceStep ?? 0m;
            if (tick <= 0m)
                return;

            if (wick > average + MaOffset * tick)
            {
                BuyMarket();
                _entryBar = _finishedBars;
            }
        }
    }
}
