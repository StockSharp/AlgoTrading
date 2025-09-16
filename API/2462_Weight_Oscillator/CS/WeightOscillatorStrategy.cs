using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on a weighted oscillator composed of RSI, MFI,
/// Williams %R and DeMarker values smoothed by SMA.
/// </summary>
public class WeightOscillatorStrategy : Strategy
{
    private readonly StrategyParam<int> _rsiPeriod;
    private readonly StrategyParam<decimal> _rsiWeight;
    private readonly StrategyParam<int> _mfiPeriod;
    private readonly StrategyParam<decimal> _mfiWeight;
    private readonly StrategyParam<int> _wprPeriod;
    private readonly StrategyParam<decimal> _wprWeight;
    private readonly StrategyParam<int> _deMarkerPeriod;
    private readonly StrategyParam<decimal> _deMarkerWeight;
    private readonly StrategyParam<int> _smoothingPeriod;
    private readonly StrategyParam<decimal> _highLevel;
    private readonly StrategyParam<decimal> _lowLevel;
    private readonly StrategyParam<TrendMode> _trend;
    private readonly StrategyParam<DataType> _candleType;

    private readonly WeightOscillator _oscillator = new();
    private decimal _prevValue;
    private bool _hasPrev;

    /// <summary>
    /// RSI calculation period.
    /// </summary>
    public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

    /// <summary>
    /// Weight applied to RSI component.
    /// </summary>
    public decimal RsiWeight { get => _rsiWeight.Value; set => _rsiWeight.Value = value; }

    /// <summary>
    /// MFI calculation period.
    /// </summary>
    public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }

    /// <summary>
    /// Weight applied to MFI component.
    /// </summary>
    public decimal MfiWeight { get => _mfiWeight.Value; set => _mfiWeight.Value = value; }

    /// <summary>
    /// Williams %R calculation period.
    /// </summary>
    public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }

    /// <summary>
    /// Weight applied to Williams %R component.
    /// </summary>
    public decimal WprWeight { get => _wprWeight.Value; set => _wprWeight.Value = value; }

    /// <summary>
    /// DeMarker calculation period.
    /// </summary>
    public int DeMarkerPeriod { get => _deMarkerPeriod.Value; set => _deMarkerPeriod.Value = value; }

    /// <summary>
    /// Weight applied to DeMarker component.
    /// </summary>
    public decimal DeMarkerWeight { get => _deMarkerWeight.Value; set => _deMarkerWeight.Value = value; }

    /// <summary>
    /// Smoothing SMA period.
    /// </summary>
    public int SmoothingPeriod { get => _smoothingPeriod.Value; set => _smoothingPeriod.Value = value; }

    /// <summary>
    /// Upper level for oscillator.
    /// </summary>
    public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }

    /// <summary>
    /// Lower level for oscillator.
    /// </summary>
    public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }

    /// <summary>
    /// Trading direction relative to oscillator signals.
    /// </summary>
    public TrendMode Trend { get => _trend.Value; set => _trend.Value = value; }

    /// <summary>
    /// Candle type for calculations.
    /// </summary>
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    /// <summary>
    /// Initializes a new instance of <see cref="WeightOscillatorStrategy"/>.
    /// </summary>
    public WeightOscillatorStrategy()
    {
        _rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI length", "Indicators");
        _rsiWeight = Param(nameof(RsiWeight), 1m).SetDisplay("RSI Weight", "Weight for RSI", "Indicators");
        _mfiPeriod = Param(nameof(MfiPeriod), 14).SetGreaterThanZero().SetDisplay("MFI Period", "MFI length", "Indicators");
        _mfiWeight = Param(nameof(MfiWeight), 1m).SetDisplay("MFI Weight", "Weight for MFI", "Indicators");
        _wprPeriod = Param(nameof(WprPeriod), 14).SetGreaterThanZero().SetDisplay("WPR Period", "Williams %R length", "Indicators");
        _wprWeight = Param(nameof(WprWeight), 1m).SetDisplay("WPR Weight", "Weight for Williams %R", "Indicators");
        _deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14).SetGreaterThanZero().SetDisplay("DeMarker Period", "DeMarker length", "Indicators");
        _deMarkerWeight = Param(nameof(DeMarkerWeight), 1m).SetDisplay("DeMarker Weight", "Weight for DeMarker", "Indicators");
        _smoothingPeriod = Param(nameof(SmoothingPeriod), 5).SetGreaterThanZero().SetDisplay("SMA Period", "Smoothing period", "Indicators");
        _highLevel = Param(nameof(HighLevel), 70m).SetRange(0m, 100m).SetDisplay("High Level", "Overbought level", "Signals");
        _lowLevel = Param(nameof(LowLevel), 30m).SetRange(0m, 100m).SetDisplay("Low Level", "Oversold level", "Signals");
        _trend = Param(nameof(Trend), TrendMode.Direct).SetDisplay("Trend Mode", "Trade direction", "General");
        _candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame()).SetDisplay("Candle Type", "Working candles", "General");
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
        StartProtection();

        _oscillator.RsiPeriod = RsiPeriod;
        _oscillator.RsiWeight = RsiWeight;
        _oscillator.MfiPeriod = MfiPeriod;
        _oscillator.MfiWeight = MfiWeight;
        _oscillator.WprPeriod = WprPeriod;
        _oscillator.WprWeight = WprWeight;
        _oscillator.DeMarkerPeriod = DeMarkerPeriod;
        _oscillator.DeMarkerWeight = DeMarkerWeight;
        _oscillator.SmoothingPeriod = SmoothingPeriod;

        var subscription = SubscribeCandles(CandleType);
        subscription.Bind(_oscillator, ProcessCandle).Start();
    }

    private void ProcessCandle(ICandleMessage candle, decimal osc)
    {
        if (candle.State != CandleStates.Finished)
            return;

        if (!_oscillator.IsFormed)
            return;

        if (!_hasPrev)
        {
            _prevValue = osc;
            _hasPrev = true;
            return;
        }

        var crossLow = _prevValue > LowLevel && osc <= LowLevel;
        var crossHigh = _prevValue < HighLevel && osc >= HighLevel;
        _prevValue = osc;

        if (Trend == TrendMode.Direct)
        {
            if (crossLow && Position <= 0)
            {
                var volume = Volume + (Position < 0 ? -Position : 0m);
                BuyMarket(volume);
            }
            else if (crossHigh && Position >= 0)
            {
                var volume = Volume + (Position > 0 ? Position : 0m);
                SellMarket(volume);
            }
        }
        else
        {
            if (crossLow && Position >= 0)
            {
                var volume = Volume + (Position > 0 ? Position : 0m);
                SellMarket(volume);
            }
            else if (crossHigh && Position <= 0)
            {
                var volume = Volume + (Position < 0 ? -Position : 0m);
                BuyMarket(volume);
            }
        }
    }
}

/// <summary>
/// Trade direction modes.
/// </summary>
public enum TrendMode
{
    /// <summary>
    /// Trade in the direction of oscillator signals.
    /// </summary>
    Direct,

    /// <summary>
    /// Trade against oscillator signals.
    /// </summary>
    Against
}

/// <summary>
/// Custom indicator combining RSI, MFI, Williams %R and DeMarker into
/// a single weighted oscillator smoothed by SMA.
/// </summary>
public class WeightOscillator : Indicator<decimal>
{
    public int RsiPeriod { get; set; } = 14;
    public decimal RsiWeight { get; set; } = 1m;
    public int MfiPeriod { get; set; } = 14;
    public decimal MfiWeight { get; set; } = 1m;
    public int WprPeriod { get; set; } = 14;
    public decimal WprWeight { get; set; } = 1m;
    public int DeMarkerPeriod { get; set; } = 14;
    public decimal DeMarkerWeight { get; set; } = 1m;
    public int SmoothingPeriod { get; set; } = 5;

    private readonly RSI _rsi = new();
    private readonly MFI _mfi = new();
    private readonly WilliamsR _wpr = new();
    private readonly DeMarker _deMarker = new();
    private readonly SMA _sma = new();

    protected override IIndicatorValue OnProcess(IIndicatorValue input)
    {
        _rsi.Length = RsiPeriod;
        _mfi.Length = MfiPeriod;
        _wpr.Length = WprPeriod;
        _deMarker.Length = DeMarkerPeriod;
        _sma.Length = SmoothingPeriod;

        var rsiVal = _rsi.Process(input);
        var mfiVal = _mfi.Process(input);
        var wprVal = _wpr.Process(input);
        var deVal = _deMarker.Process(input);

        if (!_rsi.IsFormed || !_mfi.IsFormed || !_wpr.IsFormed || !_deMarker.IsFormed)
        {
            IsFormed = false;
            return new DecimalIndicatorValue(this, default, input.Time);
        }

        var sumWeight = RsiWeight + MfiWeight + WprWeight + DeMarkerWeight;
        var raw = (RsiWeight * rsiVal.GetValue<decimal>()
        + MfiWeight * mfiVal.GetValue<decimal>()
        + WprWeight * (wprVal.GetValue<decimal>() + 100m)
        + DeMarkerWeight * (deVal.GetValue<decimal>() * 100m)) / sumWeight;

        var smooth = _sma.Process(new DecimalIndicatorValue(this, raw, input.Time));
        IsFormed = _sma.IsFormed;
        return new DecimalIndicatorValue(this, smooth.GetValue<decimal>(), input.Time);
    }

    public override void Reset()
    {
        base.Reset();
        _rsi.Reset();
        _mfi.Reset();
        _wpr.Reset();
        _deMarker.Reset();
        _sma.Reset();
    }
}
