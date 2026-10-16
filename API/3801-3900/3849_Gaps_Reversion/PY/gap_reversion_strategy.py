import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy


class gap_reversion_strategy(Strategy):
    """Trades against opening gaps beyond the previous candle's high/low and spread."""

    def __init__(self):
        super(gap_reversion_strategy, self).__init__()
        self._min_gap_size = self.Param("MinGapSize", 1.0).SetGreaterThanZero().SetDisplay(
            "Minimum Gap", "Minimum open gap in price steps", "Signals")
        self._gap_volume = self.Param("GapVolume", 0.1).SetGreaterThanZero().SetDisplay(
            "Gap Volume", "Volume of each market gap entry", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay(
            "Candle Type", "Candle timeframe", "General")
        self._clear_state()

    @property
    def min_gap_size(self):
        return self._min_gap_size.Value

    @property
    def gap_volume(self):
        return self._gap_volume.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _clear_state(self):
        self._previous_high = None
        self._previous_low = None
        self._observed_bar = None
        self._best_bid = None
        self._best_ask = None

    def OnReseted(self):
        super(gap_reversion_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(gap_reversion_strategy, self).OnStarted2(time)
        self._clear_state()
        self.Volume = Decimal(self.gap_volume)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._process_quote).Start()
        self.SubscribeCandles(self.candle_type).Bind(self._process_candle).Start()

    def _process_quote(self, quote):
        bid = quote.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = quote.TryGetDecimal(Level1Fields.BestAskPrice)
        if bid is not None:
            self._best_bid = float(bid)
        if ask is not None:
            self._best_ask = float(ask)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Active and candle.State != CandleStates.Finished:
            return

        # Active updates allow execution at the open. Finished-only feeds necessarily
        # discover the opening gap at close; this is not equivalent to open execution.
        if self._observed_bar != candle.OpenTime:
            self._observed_bar = candle.OpenTime
            if self._previous_high is not None and self._previous_low is not None:
                step = float(self.Security.PriceStep)
                if step > 0.0:
                    spread = (self._best_ask - self._best_bid
                              if self._best_ask is not None and self._best_bid is not None
                              and self._best_ask > self._best_bid else step)
                    buffer = float(self.min_gap_size) * step + spread
                    op = float(candle.OpenPrice)
                    if op > self._previous_high + buffer:
                        self.SellMarket(Decimal(self.gap_volume))
                    elif op < self._previous_low - buffer:
                        self.BuyMarket(Decimal(self.gap_volume))

        if candle.State == CandleStates.Finished:
            self._previous_high = float(candle.HighPrice)
            self._previous_low = float(candle.LowPrice)

    def CreateClone(self):
        return gap_reversion_strategy()
