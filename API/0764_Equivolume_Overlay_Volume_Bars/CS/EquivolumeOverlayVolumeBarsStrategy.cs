using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Visual strategy that draws equivolume style boxes over price candles.
/// Width of each box corresponds to the ratio of current volume to the sum of volumes over a lookback period.
/// </summary>
public class EquivolumeOverlayVolumeBarsStrategy : Strategy
{
    private readonly StrategyParam<int> _volumeLookback;
    private readonly StrategyParam<int> _fullWidth;
    private readonly StrategyParam<decimal> _scalingValue;
    private readonly StrategyParam<MaTypes> _maType;
    private readonly StrategyParam<int> _maLength;
    private readonly StrategyParam<DataType> _candleType;

    private decimal _previousSumVolume;

    /// <summary>
    /// Number of bars to sum for width calculation.
    /// </summary>
    public int VolumeLookback
    {
        get => _volumeLookback.Value;
        set => _volumeLookback.Value = value;
    }

    /// <summary>
    /// Width in bars when current volume equals sum of lookback volumes.
    /// </summary>
    public int FullWidth
    {
        get => _fullWidth.Value;
        set => _fullWidth.Value = value;
    }

    /// <summary>
    /// Scaling factor applied to the volume line.
    /// </summary>
    public decimal ScalingValue
    {
        get => _scalingValue.Value;
        set => _scalingValue.Value = value;
    }

    /// <summary>
    /// Type of volume moving average.
    /// </summary>
    public MaTypes MaType
    {
        get => _maType.Value;
        set => _maType.Value = value;
    }

    /// <summary>
    /// Length of volume moving average.
    /// </summary>
    public int MaLength
    {
        get => _maLength.Value;
        set => _maLength.Value = value;
    }

    /// <summary>
    /// Candle type for calculations.
    /// </summary>
    public DataType CandleType
    {
        get => _candleType.Value;
        set => _candleType.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the strategy.
    /// </summary>
    public EquivolumeOverlayVolumeBarsStrategy()
    {
        _volumeLookback = Param(nameof(VolumeLookback), 60)
            .SetGreaterThanZero()
            .SetDisplay("Volume Lookback", "Number of bars to sum", "General");

        _fullWidth = Param(nameof(FullWidth), 500)
            .SetGreaterThanZero()
            .SetDisplay("Full Width", "Width in bars when current volume equals the lookback sum", "General");

        _scalingValue = Param(nameof(ScalingValue), 10m)
            .SetGreaterThanZero()
            .SetDisplay("Scaling Value", "Scaling factor for volume line", "General");

        _maType = Param(nameof(MaType), MaTypes.SMA)
            .SetDisplay("MA Type", "Type of volume moving average", "Volume MA");

        _maLength = Param(nameof(MaLength), 21)
            .SetGreaterThanZero()
            .SetDisplay("MA Length", "Length of volume moving average", "Volume MA");

        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
            .SetDisplay("Candle Type", "Type of candles", "General");
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
        _previousSumVolume = 0m;
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
        base.OnStarted(time);

        var volumeSma = new SMA { Length = VolumeLookback };
        Indicator volumeMa = MaType switch
        {
            MaTypes.SMA => new SMA { Length = MaLength },
            MaTypes.EMA => new EMA { Length = MaLength },
            _ => new WMA { Length = MaLength },
        };

        var subscription = SubscribeCandles(CandleType);
        subscription
            .Bind(volumeSma, volumeMa, ProcessCandle)
            .Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, subscription);
            DrawIndicator(area, volumeMa);
        }
    }

    private void ProcessCandle(ICandleMessage candle, decimal sumAvg, decimal maValue)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var sumVolume = sumAvg * VolumeLookback;

        if (_previousSumVolume == 0m)
        {
            _previousSumVolume = sumVolume;
            return;
        }

        var ratio = candle.TotalVolume / _previousSumVolume;
        var width = Math.Max((int)Math.Round(ratio * FullWidth), 1);
        var scaledLine = sumVolume * ScalingValue;

        LogInfo($"Time: {candle.OpenTime}, Volume: {candle.TotalVolume}, Width: {width}, VolumeMA: {maValue}, Scaled: {scaledLine}");

        _previousSumVolume = sumVolume;
    }

    /// <summary>
    /// Volume moving average types.
    /// </summary>
    public enum MaTypes
    {
        /// <summary>SMA.</summary>
        SMA,
        /// <summary>EMA.</summary>
        EMA,
        /// <summary>WMA.</summary>
        WMA
    }
}

