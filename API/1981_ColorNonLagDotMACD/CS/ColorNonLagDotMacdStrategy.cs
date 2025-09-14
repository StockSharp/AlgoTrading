using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD signals with multiple detection modes.
/// Inspired by the original "Exp_ColorNonLagDotMACD" MQL expert.
/// </summary>
public class ColorNonLagDotMacdStrategy : Strategy
{
	/// <summary>
	/// Algorithm mode for generating signals.
	/// </summary>
	public enum AlgorithmMode
	{
	    /// <summary>
	    /// Trade on zero line breakout of MACD histogram.
	    /// </summary>
	    Breakdown,

	    /// <summary>
	    /// Trade on MACD direction change.
	    /// </summary>
	    MacdTwist,

	    /// <summary>
	    /// Trade on signal line direction change.
	    /// </summary>
	    SignalTwist,

	    /// <summary>
	    /// Trade on MACD crossing the signal line.
	    /// </summary>
	    MacdDisposition
	}

	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<AlgorithmMode> _mode;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacd;
	private decimal _prevMacd2;
	private decimal _prevSignal;
	private decimal _prevSignal2;

	/// <summary>
	/// Fast EMA period of MACD.
	/// </summary>
	public int FastLength
	{
	    get => _fastLength.Value;
	    set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period of MACD.
	/// </summary>
	public int SlowLength
	{
	    get => _slowLength.Value;
	    set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line period.
	/// </summary>
	public int SignalLength
	{
	    get => _signalLength.Value;
	    set => _signalLength.Value = value;
	}

	/// <summary>
	/// Mode that defines how signals are detected.
	/// </summary>
	public AlgorithmMode Mode
	{
	    get => _mode.Value;
	    set => _mode.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
	    get => _buyOpen.Value;
	    set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
	    get => _sellOpen.Value;
	    set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose
	{
	    get => _buyClose.Value;
	    set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose
	{
	    get => _sellClose.Value;
	    set => _sellClose.Value = value;
	}

	/// <summary>
	/// Take profit in percent from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
	    get => _takeProfit.Value;
	    set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in percent from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
	    get => _stopLoss.Value;
	    set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy with default parameters.
	/// </summary>
	public ColorNonLagDotMacdStrategy()
	{
	    _fastLength = Param(nameof(FastLength), 12)
	        .SetGreaterThanZero()
	        .SetDisplay("Fast Length", "Fast EMA period", "MACD Settings")
	        .SetCanOptimize(true)
	        .SetOptimize(6, 18, 2);

	    _slowLength = Param(nameof(SlowLength), 26)
	        .SetGreaterThanZero()
	        .SetDisplay("Slow Length", "Slow EMA period", "MACD Settings")
	        .SetCanOptimize(true)
	        .SetOptimize(20, 40, 2);

	    _signalLength = Param(nameof(SignalLength), 9)
	        .SetGreaterThanZero()
	        .SetDisplay("Signal Length", "Signal line period", "MACD Settings")
	        .SetCanOptimize(true)
	        .SetOptimize(5, 15, 2);

	    _mode = Param(nameof(Mode), AlgorithmMode.MacdDisposition)
	        .SetDisplay("Mode", "Signal detection mode", "General");

	    _buyOpen = Param(nameof(BuyPosOpen), true)
	        .SetDisplay("Allow Long", "Permission to open long positions", "Permissions");

	    _sellOpen = Param(nameof(SellPosOpen), true)
	        .SetDisplay("Allow Short", "Permission to open short positions", "Permissions");

	    _buyClose = Param(nameof(BuyPosClose), true)
	        .SetDisplay("Close Long", "Permission to close long positions", "Permissions");

	    _sellClose = Param(nameof(SellPosClose), true)
	        .SetDisplay("Close Short", "Permission to close short positions", "Permissions");

	    _takeProfit = Param(nameof(TakeProfitPercent), 4m)
	        .SetNotNegative()
	        .SetDisplay("Take Profit %", "Take profit in percent", "Risk Management");

	    _stopLoss = Param(nameof(StopLossPercent), 2m)
	        .SetNotNegative()
	        .SetDisplay("Stop Loss %", "Stop loss in percent", "Risk Management");

	    _candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles to use", "Data");
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
	    _prevMacd = _prevMacd2 = 0m;
	    _prevSignal = _prevSignal2 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    var macd = new MovingAverageConvergenceDivergenceSignal
	    {
	        Macd =
	        {
	            ShortMa = { Length = FastLength },
	            LongMa = { Length = SlowLength },
	        },
	        SignalMa = { Length = SignalLength }
	    };

	    var subscription = SubscribeCandles(CandleType);
	    subscription
	        .BindEx(macd, ProcessCandle)
	        .Start();

	    var area = CreateChartArea();
	    if (area != null)
	    {
	        DrawCandles(area, subscription);
	        DrawIndicator(area, macd);
	        DrawOwnTrades(area);
	    }

	    StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    if (!IsFormedAndOnlineAndAllowTrading())
	        return;

	    var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	    var macd = (decimal)macdTyped.Macd;
	    var signal = (decimal)macdTyped.Signal;

	    bool buySignal = false;
	    bool sellSignal = false;

	    switch (Mode)
	    {
	        case AlgorithmMode.Breakdown:
	            buySignal = _prevMacd <= 0 && macd > 0;
	            sellSignal = _prevMacd >= 0 && macd < 0;
	            break;
	        case AlgorithmMode.MacdTwist:
	            buySignal = _prevMacd < _prevMacd2 && macd > _prevMacd;
	            sellSignal = _prevMacd > _prevMacd2 && macd < _prevMacd;
	            break;
	        case AlgorithmMode.SignalTwist:
	            buySignal = _prevSignal < _prevSignal2 && signal > _prevSignal;
	            sellSignal = _prevSignal > _prevSignal2 && signal < _prevSignal;
	            break;
	        case AlgorithmMode.MacdDisposition:
	            buySignal = _prevMacd <= _prevSignal && macd > signal;
	            sellSignal = _prevMacd >= _prevSignal && macd < signal;
	            break;
	    }

	    if (buySignal)
	    {
	        if (BuyPosClose && Position < 0)
	            BuyMarket(Math.Abs(Position));

	        if (BuyPosOpen && Position <= 0)
	            BuyMarket(Volume);
	    }

	    if (sellSignal)
	    {
	        if (SellPosClose && Position > 0)
	            SellMarket(Position);

	        if (SellPosOpen && Position >= 0)
	            SellMarket(Volume + Math.Max(0m, Position));
	    }

	    _prevMacd2 = _prevMacd;
	    _prevMacd = macd;
	    _prevSignal2 = _prevSignal;
	    _prevSignal = signal;
	}
}

