using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Executes market orders when price touches predefined horizontal levels.
/// Converted from the MetaTrader expert advisor "MyLineOrder".
/// </summary>
public class LineOrderStrategy : Strategy
{
    private readonly StrategyParam<decimal> _buyPrice;
    private readonly StrategyParam<decimal> _sellPrice;
    private readonly StrategyParam<decimal> _stopLossPips;
    private readonly StrategyParam<decimal> _takeProfitPips;
    private readonly StrategyParam<decimal> _trailingStopPips;
    private readonly StrategyParam<decimal> _tradeVolume;
    private readonly StrategyParam<DataType> _candleType;

    private bool _buyExecuted;
    private bool _sellExecuted;
    private decimal _entryPrice;
    private decimal _highestPrice;
    private decimal _lowestPrice;

    public decimal BuyPrice { get => _buyPrice.Value; set => _buyPrice.Value = value; }
    public decimal SellPrice { get => _sellPrice.Value; set => _sellPrice.Value = value; }
    public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
    public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
    public decimal TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
    public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    public LineOrderStrategy()
    {
        _buyPrice = Param(nameof(BuyPrice), 0m)
            .SetDisplay("Buy Price", "Price level to open long", "Lines");

        _sellPrice = Param(nameof(SellPrice), 0m)
            .SetDisplay("Sell Price", "Price level to open short", "Lines");

        _stopLossPips = Param(nameof(StopLossPips), 20m)
            .SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

        _takeProfitPips = Param(nameof(TakeProfitPips), 30m)
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

        _trailingStopPips = Param(nameof(TrailingStopPips), 0m)
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

        _tradeVolume = Param(nameof(TradeVolume), 1m)
            .SetGreaterThanZero()
            .SetDisplay("Volume", "Order volume", "General");

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
        ResetState();
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
        base.OnStarted(time);

        Volume = TradeVolume;
        StartProtection();

        SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
    }

    private void ProcessCandle(ICandleMessage candle)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var step = (decimal)Security.PriceStep;

        // Entry logic
        if (!_buyExecuted && BuyPrice > 0 && candle.HighPrice >= BuyPrice && Position <= 0)
        {
            BuyMarket(TradeVolume);
            _buyExecuted = true;
            _entryPrice = candle.ClosePrice;
            _highestPrice = _entryPrice;
            _lowestPrice = _entryPrice;
        }
        else if (!_sellExecuted && SellPrice > 0 && candle.LowPrice <= SellPrice && Position >= 0)
        {
            SellMarket(TradeVolume);
            _sellExecuted = true;
            _entryPrice = candle.ClosePrice;
            _highestPrice = _entryPrice;
            _lowestPrice = _entryPrice;
        }

        // Exit logic for long position
        if (Position > 0)
        {
            _highestPrice = Math.Max(_highestPrice, candle.HighPrice);

            var stopPrice = _entryPrice - StopLossPips * step;
            var takePrice = _entryPrice + TakeProfitPips * step;
            var trailPrice = _highestPrice - TrailingStopPips * step;

            if (StopLossPips > 0 && candle.LowPrice <= stopPrice ||
                TakeProfitPips > 0 && candle.HighPrice >= takePrice ||
                TrailingStopPips > 0 && candle.LowPrice <= trailPrice)
            {
                SellMarket(Position);
                ResetState();
            }
        }
        // Exit logic for short position
        else if (Position < 0)
        {
            _lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);

            var stopPrice = _entryPrice + StopLossPips * step;
            var takePrice = _entryPrice - TakeProfitPips * step;
            var trailPrice = _lowestPrice + TrailingStopPips * step;

            if (StopLossPips > 0 && candle.HighPrice >= stopPrice ||
                TakeProfitPips > 0 && candle.LowPrice <= takePrice ||
                TrailingStopPips > 0 && candle.HighPrice >= trailPrice)
            {
                BuyMarket(-Position);
                ResetState();
            }
        }
    }

    private void ResetState()
    {
        _buyExecuted = false;
        _sellExecuted = false;
        _entryPrice = 0m;
        _highestPrice = 0m;
        _lowestPrice = 0m;
    }
}
