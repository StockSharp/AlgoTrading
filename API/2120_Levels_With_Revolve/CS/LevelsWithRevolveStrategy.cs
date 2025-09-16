using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens a position when price crosses a user defined level.
/// Optionally reverses the position when price crosses in the opposite direction.
/// Supports optional stop loss and take profit levels.
/// </summary>
public class LevelsWithRevolveStrategy : Strategy
{
    private readonly StrategyParam<decimal> _levelPrice;
    private readonly StrategyParam<decimal> _stopLoss;
    private readonly StrategyParam<decimal> _takeProfit;
    private readonly StrategyParam<bool> _enableReversal;
    private readonly StrategyParam<DataType> _candleType;

    private decimal? _entryPrice;
    private decimal? _lastPrice;

    /// <summary>
    /// Price level to watch for crossovers.
    /// </summary>
    public decimal LevelPrice
    {
        get => _levelPrice.Value;
        set => _levelPrice.Value = value;
    }

    /// <summary>
    /// Stop loss distance in price units. Zero disables stop loss.
    /// </summary>
    public decimal StopLoss
    {
        get => _stopLoss.Value;
        set => _stopLoss.Value = value;
    }

    /// <summary>
    /// Take profit distance in price units. Zero disables take profit.
    /// </summary>
    public decimal TakeProfit
    {
        get => _takeProfit.Value;
        set => _takeProfit.Value = value;
    }

    /// <summary>
    /// Enable reversing an existing position when opposite signal appears.
    /// </summary>
    public bool EnableReversal
    {
        get => _enableReversal.Value;
        set => _enableReversal.Value = value;
    }

    /// <summary>
    /// Type of candles used for price data.
    /// </summary>
    public DataType CandleType
    {
        get => _candleType.Value;
        set => _candleType.Value = value;
    }

    /// <summary>
    /// Initializes strategy parameters.
    /// </summary>
    public LevelsWithRevolveStrategy()
    {
        _levelPrice = Param(nameof(LevelPrice), 100m)
            .SetGreaterThanZero()
            .SetDisplay("Level Price", "Price level where trades are triggered", "General");

        _stopLoss = Param(nameof(StopLoss), 0m)
            .SetDisplay("Stop Loss", "Price distance for stop loss", "Risk")
            .SetCanOptimize(true)
            .SetOptimize(0m, 100m, 10m);

        _takeProfit = Param(nameof(TakeProfit), 0m)
            .SetDisplay("Take Profit", "Price distance for take profit", "Risk")
            .SetCanOptimize(true)
            .SetOptimize(0m, 200m, 10m);

        _enableReversal = Param(nameof(EnableReversal), false)
            .SetDisplay("Enable Reversal", "Reverse position on opposite signal", "General");

        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
            .SetDisplay("Candle Type", "Type of candles used for calculations", "General");
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
        _entryPrice = null;
        _lastPrice = null;
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
        base.OnStarted(time);

        var subscription = SubscribeCandles(CandleType);
        subscription
            .Bind(OnCandle)
            .Start();
    }

    private void OnCandle(ICandleMessage candle)
    {
        if (candle.State != CandleStates.Finished)
            return;

        if (!IsFormedAndOnlineAndAllowTrading())
            return;

        var price = candle.ClosePrice;

        if (_lastPrice != null)
        {
            ProcessSignal(price);
            CheckStops(price);
        }

        _lastPrice = price;
    }

    private void ProcessSignal(decimal price)
    {
        var lastPrice = _lastPrice!.Value;

        if (Position == 0)
        {
            if (lastPrice < LevelPrice && price >= LevelPrice)
            {
                _entryPrice = price;
                BuyMarket();
            }
            else if (lastPrice > LevelPrice && price <= LevelPrice)
            {
                _entryPrice = price;
                SellMarket();
            }
        }
        else if (EnableReversal)
        {
            if (Position > 0 && lastPrice > LevelPrice && price <= LevelPrice)
            {
                _entryPrice = price;
                SellMarket(Volume + Math.Abs(Position));
            }
            else if (Position < 0 && lastPrice < LevelPrice && price >= LevelPrice)
            {
                _entryPrice = price;
                BuyMarket(Volume + Math.Abs(Position));
            }
        }
    }

    private void CheckStops(decimal price)
    {
        if (_entryPrice == null)
            return;

        var entry = _entryPrice.Value;

        if (Position > 0)
        {
            if (StopLoss > 0 && price <= entry - StopLoss)
                SellMarket(Math.Abs(Position));
            else if (TakeProfit > 0 && price >= entry + TakeProfit)
                SellMarket(Math.Abs(Position));
        }
        else if (Position < 0)
        {
            if (StopLoss > 0 && price >= entry + StopLoss)
                BuyMarket(Math.Abs(Position));
            else if (TakeProfit > 0 && price <= entry - TakeProfit)
                BuyMarket(Math.Abs(Position));
        }
    }
}

