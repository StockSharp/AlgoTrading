namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Enters against gaps between a new candle's open and the previous completed range.
/// No EMA condition or automatic protection is part of the original gap rules.
/// </summary>
public class GapReversionStrategy : Strategy
{
    private readonly StrategyParam<decimal> _minGapSize;
    private readonly StrategyParam<decimal> _gapVolume;
    private readonly StrategyParam<DataType> _candleType;

    private decimal? _previousHigh;
    private decimal? _previousLow;
    private DateTimeOffset? _observedBar;
    private decimal? _bid;
    private decimal? _ask;

    public decimal MinGapSize { get => _minGapSize.Value; set => _minGapSize.Value = value; }
    public decimal GapVolume { get => _gapVolume.Value; set => _gapVolume.Value = value; }
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    public GapReversionStrategy()
    {
        _minGapSize = Param(nameof(MinGapSize), 1m)
            .SetGreaterThanZero()
            .SetDisplay("Minimum Gap", "Minimum open gap in price steps", "Signals");
        _gapVolume = Param(nameof(GapVolume), 0.1m)
            .SetGreaterThanZero()
            .SetDisplay("Gap Volume", "Volume of each market gap entry", "Trading");
        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
            .SetDisplay("Candle Type", "Candle timeframe", "General");
    }

    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        => [(Security, CandleType)];

    protected override void OnReseted()
    {
        base.OnReseted();
        ClearState();
    }

    private void ClearState()
    {
        _previousHigh = null;
        _previousLow = null;
        _observedBar = null;
        _bid = null;
        _ask = null;
    }

    protected override void OnStarted2(DateTime time)
    {
        base.OnStarted2(time);
        ClearState();
        Volume = GapVolume;

        // Quote subscriptions are optional: one price step is used as spread
        // if the replay or provider does not expose both best prices.
        foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
        {
            var quotes = new Subscription(DataType.Level1, Security);
            quotes.MarketData.BuildField = field;
            SubscribeLevel1(quotes).Bind(ProcessQuote).Start();
        }

        var candles = SubscribeCandles(CandleType);
        candles.Bind(ProcessCandle).Start();
    }

    private void ProcessQuote(Level1ChangeMessage quote)
    {
        if (quote.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
            _bid = bid;
        if (quote.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
            _ask = ask;
    }

    private void ProcessCandle(ICandleMessage candle)
    {
        if (candle.State is not (CandleStates.Active or CandleStates.Finished))
            return;

        // Process a bar just once, at its first update (Active if available).
        // Finished-only feeds can only evaluate the opening gap retrospectively.
        if (_observedBar != candle.OpenTime)
        {
            _observedBar = candle.OpenTime;
            if (_previousHigh is decimal high && _previousLow is decimal low)
            {
                var step = Security?.PriceStep ?? 0m;
                if (step > 0m)
                {
                    var spread = _bid is decimal bid && _ask is decimal ask && ask > bid
                        ? ask - bid
                        : step;
                    var buffer = MinGapSize * step + spread;
                    if (candle.OpenPrice > high + buffer)
                        SellMarket(GapVolume);
                    else if (candle.OpenPrice < low - buffer)
                        BuyMarket(GapVolume);
                }
            }
        }

        if (candle.State == CandleStates.Finished)
        {
            _previousHigh = candle.HighPrice;
            _previousLow = candle.LowPrice;
        }
    }
}
