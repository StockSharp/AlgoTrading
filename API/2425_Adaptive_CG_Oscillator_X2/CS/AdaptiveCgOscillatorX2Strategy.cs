namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Adaptive CG Oscillator crossovers on two timeframes.
/// Higher timeframe defines the trend; lower timeframe generates entries and exits.
/// </summary>
public class AdaptiveCgOscillatorX2Strategy : Strategy
{
    private readonly StrategyParam<decimal> _trendAlpha;
    private readonly StrategyParam<decimal> _signalAlpha;
    private readonly StrategyParam<DataType> _trendCandleType;
    private readonly StrategyParam<DataType> _signalCandleType;
    private readonly StrategyParam<bool> _buyPosOpen;
    private readonly StrategyParam<bool> _sellPosOpen;
    private readonly StrategyParam<bool> _buyPosCloseTrend;
    private readonly StrategyParam<bool> _sellPosCloseTrend;
    private readonly StrategyParam<bool> _buyPosCloseSignal;
    private readonly StrategyParam<bool> _sellPosCloseSignal;

    private AdaptiveCgOscillator _trendOsc;
    private AdaptiveCgOscillator _signalOsc;

    private int _trend;

    private decimal _prevMain;
    private decimal _prevSignal;
    private bool _hasPrevSignal;

    /// <summary>
    /// Alpha parameter for trend oscillator.
    /// </summary>
    public decimal TrendAlpha
    {
        get => _trendAlpha.Value;
        set => _trendAlpha.Value = value;
    }

    /// <summary>
    /// Alpha parameter for signal oscillator.
    /// </summary>
    public decimal SignalAlpha
    {
        get => _signalAlpha.Value;
        set => _signalAlpha.Value = value;
    }

    /// <summary>
    /// Higher timeframe used for trend detection.
    /// </summary>
    public DataType TrendCandleType
    {
        get => _trendCandleType.Value;
        set => _trendCandleType.Value = value;
    }

    /// <summary>
    /// Lower timeframe used for signal generation.
    /// </summary>
    public DataType SignalCandleType
    {
        get => _signalCandleType.Value;
        set => _signalCandleType.Value = value;
    }

    /// <summary>
    /// Enable long entries.
    /// </summary>
    public bool BuyPosOpen
    {
        get => _buyPosOpen.Value;
        set => _buyPosOpen.Value = value;
    }

    /// <summary>
    /// Enable short entries.
    /// </summary>
    public bool SellPosOpen
    {
        get => _sellPosOpen.Value;
        set => _sellPosOpen.Value = value;
    }

    /// <summary>
    /// Close long position on opposite trend.
    /// </summary>
    public bool BuyPosClose
    {
        get => _buyPosCloseTrend.Value;
        set => _buyPosCloseTrend.Value = value;
    }

    /// <summary>
    /// Close short position on opposite trend.
    /// </summary>
    public bool SellPosClose
    {
        get => _sellPosCloseTrend.Value;
        set => _sellPosCloseTrend.Value = value;
    }

    /// <summary>
    /// Close long position on indicator signal.
    /// </summary>
    public bool BuyPosCloseSignal
    {
        get => _buyPosCloseSignal.Value;
        set => _buyPosCloseSignal.Value = value;
    }

    /// <summary>
    /// Close short position on indicator signal.
    /// </summary>
    public bool SellPosCloseSignal
    {
        get => _sellPosCloseSignal.Value;
        set => _sellPosCloseSignal.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AdaptiveCgOscillatorX2Strategy"/>.
    /// </summary>
    public AdaptiveCgOscillatorX2Strategy()
    {
        _trendAlpha = Param(nameof(TrendAlpha), 0.07m)
            .SetDisplay("Trend Alpha", "Smoothing for trend oscillator", "Parameters");

        _signalAlpha = Param(nameof(SignalAlpha), 0.07m)
            .SetDisplay("Signal Alpha", "Smoothing for signal oscillator", "Parameters");

        _trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(6).TimeFrame())
            .SetDisplay("Trend Candle Type", "Higher timeframe", "General");

        _signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(30).TimeFrame())
            .SetDisplay("Signal Candle Type", "Lower timeframe", "General");

        _buyPosOpen = Param(nameof(BuyPosOpen), true)
            .SetDisplay("Buy Entry", "Enable long entries", "Trading");

        _sellPosOpen = Param(nameof(SellPosOpen), true)
            .SetDisplay("Sell Entry", "Enable short entries", "Trading");

        _buyPosCloseTrend = Param(nameof(BuyPosClose), true)
            .SetDisplay("Close Buy on Trend", "Close longs on opposite trend", "Trading");

        _sellPosCloseTrend = Param(nameof(SellPosClose), true)
            .SetDisplay("Close Sell on Trend", "Close shorts on opposite trend", "Trading");

        _buyPosCloseSignal = Param(nameof(BuyPosCloseSignal), false)
            .SetDisplay("Close Buy on Signal", "Close longs on indicator cross", "Trading");

        _sellPosCloseSignal = Param(nameof(SellPosCloseSignal), false)
            .SetDisplay("Close Sell on Signal", "Close shorts on indicator cross", "Trading");
    }

    /// <inheritdoc />
    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
    {
        return [(Security, TrendCandleType), (Security, SignalCandleType)];
    }

    /// <inheritdoc />
    protected override void OnReseted()
    {
        base.OnReseted();
        _trendOsc?.Reset();
        _signalOsc?.Reset();
        _prevMain = _prevSignal = 0m;
        _hasPrevSignal = false;
        _trend = 0;
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
        base.OnStarted(time);

        _trendOsc = new AdaptiveCgOscillator { Alpha = TrendAlpha };
        _signalOsc = new AdaptiveCgOscillator { Alpha = SignalAlpha };

        var trendSub = SubscribeCandles(TrendCandleType);
        trendSub
            .BindEx(_trendOsc, ProcessTrend)
            .Start();

        var signalSub = SubscribeCandles(SignalCandleType);
        signalSub
            .BindEx(_signalOsc, ProcessSignal)
            .Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, trendSub);
            DrawIndicator(area, _trendOsc);
            DrawIndicator(area, _signalOsc);
            DrawOwnTrades(area);
        }
    }

    private void ProcessTrend(ICandleMessage candle, IIndicatorValue oscValue)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var val = (AdaptiveCgOscillatorValue)oscValue;
        if (!val.IsFormed)
            return;

        _trend = val.Main > val.Signal ? 1 : val.Main < val.Signal ? -1 : 0;
    }

    private void ProcessSignal(ICandleMessage candle, IIndicatorValue oscValue)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var val = (AdaptiveCgOscillatorValue)oscValue;
        if (!val.IsFormed)
            return;

        var main = val.Main;
        var signal = val.Signal;

        if (_hasPrevSignal)
        {
            if (BuyPosCloseSignal && _prevMain < _prevSignal && Position > 0)
                SellMarket();

            if (SellPosCloseSignal && _prevMain > _prevSignal && Position < 0)
                BuyMarket();

            if (_trend > 0)
            {
                if (BuyPosOpen && main <= signal && _prevMain > _prevSignal && Position <= 0)
                    BuyMarket();
                if (SellPosClose && _prevMain > _prevSignal && Position < 0)
                    BuyMarket();
            }
            else if (_trend < 0)
            {
                if (SellPosOpen && main >= signal && _prevMain < _prevSignal && Position >= 0)
                    SellMarket();
                if (BuyPosClose && _prevMain < _prevSignal && Position > 0)
                    SellMarket();
            }
        }

        _prevMain = main;
        _prevSignal = signal;
        _hasPrevSignal = true;
    }

    private class AdaptiveCgOscillator : Indicator
    {
        public decimal Alpha { get; set; } = 0.07m;

        private readonly CyclePeriod _cycle = new();
        private readonly List<decimal> _prices = new();
        private decimal _prevCg;

        protected override IIndicatorValue OnProcess(IIndicatorValue input)
        {
            _cycle.Alpha = Alpha;
            var price = input.GetValue<decimal>();
            var period = _cycle.Process(new DecimalIndicatorValue(_cycle, price, input.Time)).GetValue<decimal>();
            var intPeriod = Math.Max(1, (int)Math.Floor((double)period / 2.0));

            _prices.Insert(0, price);
            if (_prices.Count > intPeriod)
                _prices.RemoveAt(_prices.Count - 1);

            if (_prices.Count < intPeriod)
            {
                IsFormed = false;
                return new AdaptiveCgOscillatorValue(this, 0m, 0m, input.Time);
            }

            decimal num = 0m;
            decimal denom = 0m;

            for (var i = 0; i < _prices.Count; i++)
            {
                var p = _prices[i];
                num += (1 + i) * p;
                denom += p;
            }

            var cg = denom != 0 ? -num / denom + (intPeriod + 1m) / 2m : 0m;

            var value = new AdaptiveCgOscillatorValue(this, cg, _prevCg, input.Time);
            _prevCg = cg;
            IsFormed = true;
            return value;
        }

        public override void Reset()
        {
            base.Reset();
            _cycle.Reset();
            _prices.Clear();
            _prevCg = 0m;
        }
    }

    private class CyclePeriod : Indicator<decimal>
    {
        public decimal Alpha { get; set; } = 0.07m;
        private decimal _value = 10m;

        protected override IIndicatorValue OnProcess(IIndicatorValue input)
        {
            var price = input.GetValue<decimal>();
            _value = _value + Alpha * (price - _value);
            return new DecimalIndicatorValue(this, _value, input.Time);
        }

        public override void Reset()
        {
            base.Reset();
            _value = 10m;
        }
    }

    private class AdaptiveCgOscillatorValue : ComplexIndicatorValue
    {
        public AdaptiveCgOscillatorValue(IIndicator indicator, decimal main, decimal signal, DateTimeOffset time)
            : base(indicator, time)
        {
            Main = main;
            Signal = signal;
        }

        public decimal Main { get; }
        public decimal Signal { get; }
        public bool IsFormed => Indicator.IsFormed;
    }
}
