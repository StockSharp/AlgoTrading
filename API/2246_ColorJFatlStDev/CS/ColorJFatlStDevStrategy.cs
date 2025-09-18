using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Jurik moving average and standard deviation bands.
/// Opens and closes positions according to signal modes similar to the original MQL version.
/// </summary>
public class ColorJFatlStDevStrategy : Strategy
{
    private readonly StrategyParam<TimeSpan> _candleTimeFrame;
    private readonly StrategyParam<int> _jmaLength;
    private readonly StrategyParam<int> _jmaPhase;
    private readonly StrategyParam<int> _stdPeriod;
    private readonly StrategyParam<decimal> _k1;
    private readonly StrategyParam<decimal> _k2;
    private readonly StrategyParam<SignalMode> _buyOpenMode;
    private readonly StrategyParam<SignalMode> _sellOpenMode;
    private readonly StrategyParam<SignalMode> _buyCloseMode;
    private readonly StrategyParam<SignalMode> _sellCloseMode;

    private decimal? _prevJma;
    private decimal? _prevPrevJma;

    /// <summary>
    /// Time frame for candle subscription.
    /// </summary>
    public TimeSpan CandleTimeFrame
    {
        get => _candleTimeFrame.Value;
        set => _candleTimeFrame.Value = value;
    }

    /// <summary>
    /// Length of Jurik moving average.
    /// </summary>
    public int JmaLength
    {
        get => _jmaLength.Value;
        set => _jmaLength.Value = value;
    }

    /// <summary>
    /// Phase parameter for Jurik moving average.
    /// </summary>
    public int JmaPhase
    {
        get => _jmaPhase.Value;
        set => _jmaPhase.Value = value;
    }

    /// <summary>
    /// Period for standard deviation calculation.
    /// </summary>
    public int StdPeriod
    {
        get => _stdPeriod.Value;
        set => _stdPeriod.Value = value;
    }

    /// <summary>
    /// First deviation multiplier.
    /// </summary>
    public decimal K1
    {
        get => _k1.Value;
        set => _k1.Value = value;
    }

    /// <summary>
    /// Second deviation multiplier.
    /// </summary>
    public decimal K2
    {
        get => _k2.Value;
        set => _k2.Value = value;
    }

    /// <summary>
    /// Buy open signal mode.
    /// </summary>
    public SignalMode BuyOpenMode
    {
        get => _buyOpenMode.Value;
        set => _buyOpenMode.Value = value;
    }

    /// <summary>
    /// Sell open signal mode.
    /// </summary>
    public SignalMode SellOpenMode
    {
        get => _sellOpenMode.Value;
        set => _sellOpenMode.Value = value;
    }

    /// <summary>
    /// Buy close signal mode.
    /// </summary>
    public SignalMode BuyCloseMode
    {
        get => _buyCloseMode.Value;
        set => _buyCloseMode.Value = value;
    }

    /// <summary>
    /// Sell close signal mode.
    /// </summary>
    public SignalMode SellCloseMode
    {
        get => _sellCloseMode.Value;
        set => _sellCloseMode.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the strategy.
    /// </summary>
    public ColorJFatlStDevStrategy()
    {
        _candleTimeFrame = Param(nameof(CandleTimeFrame), TimeSpan.FromHours(4))
            .SetDisplay("Time Frame", "Candles time frame", "General");

        _jmaLength = Param(nameof(JmaLength), 5)
            .SetGreaterThanZero()
            .SetDisplay("JMA Length", "JMA period", "General");

        _jmaPhase = Param(nameof(JmaPhase), -100)
            .SetDisplay("JMA Phase", "JMA phase", "General");

        _stdPeriod = Param(nameof(StdPeriod), 9)
            .SetGreaterThanZero()
            .SetDisplay("Std Period", "Standard deviation period", "General");

        _k1 = Param(nameof(K1), 1.5m)
            .SetDisplay("K1", "First deviation multiplier", "General");

        _k2 = Param(nameof(K2), 2.5m)
            .SetDisplay("K2", "Second deviation multiplier", "General");

        _buyOpenMode = Param(nameof(BuyOpenMode), SignalMode.Point)
            .SetDisplay("Buy Open", "Mode for opening long", "Signals");

        _sellOpenMode = Param(nameof(SellOpenMode), SignalMode.Point)
            .SetDisplay("Sell Open", "Mode for opening short", "Signals");

        _buyCloseMode = Param(nameof(BuyCloseMode), SignalMode.Direct)
            .SetDisplay("Buy Close", "Mode for closing long", "Signals");

        _sellCloseMode = Param(nameof(SellCloseMode), SignalMode.Direct)
            .SetDisplay("Sell Close", "Mode for closing short", "Signals");
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
        base.OnStarted(time);

        var jma = new JurikMovingAverage { Length = JmaLength, Phase = JmaPhase };
        var std = new StandardDeviation { Length = StdPeriod };

        var candleSeries = SubscribeCandles(CandleTimeFrame.TimeFrame());
        candleSeries
            .Bind(jma, std, ProcessCandle)
            .Start();

        StartProtection();
    }

    private void ProcessCandle(ICandleMessage candle, decimal jmaValue, decimal stdValue)
    {
        if (candle.State != CandleStates.Finished)
            return;

        if (_prevJma is null || _prevPrevJma is null)
        {
            _prevPrevJma = _prevJma;
            _prevJma = jmaValue;
            return;
        }

        var upper1 = jmaValue + K1 * stdValue;
        var upper2 = jmaValue + K2 * stdValue;
        var lower1 = jmaValue - K1 * stdValue;
        var lower2 = jmaValue - K2 * stdValue;

        var buyOpen = false;
        var sellOpen = false;
        var buyClose = false;
        var sellClose = false;

        // Evaluate buy open signal.
        switch (BuyOpenMode)
        {
            case SignalMode.Point:
                buyOpen = candle.ClosePrice > upper1 || candle.ClosePrice > upper2;
                break;
            case SignalMode.Direct:
                buyOpen = jmaValue > _prevJma && _prevJma < _prevPrevJma;
                break;
        }

        // Evaluate sell open signal.
        switch (SellOpenMode)
        {
            case SignalMode.Point:
                sellOpen = candle.ClosePrice < lower1 || candle.ClosePrice < lower2;
                break;
            case SignalMode.Direct:
                sellOpen = jmaValue < _prevJma && _prevJma > _prevPrevJma;
                break;
        }

        // Evaluate buy close signal.
        switch (BuyCloseMode)
        {
            case SignalMode.Point:
                buyClose = candle.ClosePrice < lower1 || candle.ClosePrice < lower2;
                break;
            case SignalMode.Direct:
                buyClose = jmaValue > _prevJma;
                break;
        }

        // Evaluate sell close signal.
        switch (SellCloseMode)
        {
            case SignalMode.Point:
                sellClose = candle.ClosePrice > upper1 || candle.ClosePrice > upper2;
                break;
            case SignalMode.Direct:
                sellClose = jmaValue < _prevJma;
                break;
        }

        if (buyClose && Position > 0)
            SellMarket();
        else if (sellClose && Position < 0)
            BuyMarket();
        else if (buyOpen && Position <= 0)
            BuyMarket();
        else if (sellOpen && Position >= 0)
            SellMarket();

        _prevPrevJma = _prevJma;
        _prevJma = jmaValue;
    }
}

/// <summary>
/// Modes for signal evaluation.
/// </summary>
public enum SignalMode
{
    /// <summary>
    /// Use indicator buffer signals.
    /// </summary>
    Point,

    /// <summary>
    /// Use direct calculation on indicator line.
    /// </summary>
    Direct,

    /// <summary>
    /// Do not generate signals.
    /// </summary>
    Without
}

