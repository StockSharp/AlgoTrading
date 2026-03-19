import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class good_gbbi_strategy(Strategy):
    """
    Good Gbbi strategy.
    Opens positions based on historical open price differences
    using circular buffer comparison. Manages SL/TP and time-based exit.
    """

    def __init__(self):
        super(good_gbbi_strategy, self).__init__()
        self._take_profit_long = self.Param("TakeProfitLong", 39) \
            .SetDisplay("Take Profit Long", "Profit target for longs in points", "Risk Management")
        self._stop_loss_long = self.Param("StopLossLong", 147) \
            .SetDisplay("Stop Loss Long", "Stop loss for longs in points", "Risk Management")
        self._take_profit_short = self.Param("TakeProfitShort", 15) \
            .SetDisplay("Take Profit Short", "Profit target for shorts in points", "Risk Management")
        self._stop_loss_short = self.Param("StopLossShort", 6000) \
            .SetDisplay("Stop Loss Short", "Stop loss for shorts in points", "Risk Management")
        self._t1 = self.Param("T1", 6) \
            .SetDisplay("T1", "First open price offset", "Logic")
        self._t2 = self.Param("T2", 2) \
            .SetDisplay("T2", "Second open price offset", "Logic")
        self._delta_long = self.Param("DeltaLong", 6) \
            .SetDisplay("Delta Long", "Open difference for long entries in points", "Logic")
        self._delta_short = self.Param("DeltaShort", 21) \
            .SetDisplay("Delta Short", "Open difference for short entries in points", "Logic")
        self._max_open_time = self.Param("MaxOpenTime", 504) \
            .SetDisplay("Max Open Time", "Maximum holding time in hours (0 = unlimited)", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._open_prices = [0.0] * 7
        self._candles_count = 0
        self._entry_price = 0.0
        self._entry_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(good_gbbi_strategy, self).OnReseted()
        self._open_prices = [0.0] * 7
        self._candles_count = 0
        self._entry_price = 0.0
        self._entry_time = None

    def OnStarted(self, time):
        super(good_gbbi_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        buf_len = len(self._open_prices)
        self._open_prices[self._candles_count % buf_len] = float(candle.OpenPrice)
        self._candles_count += 1

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        close = float(candle.ClosePrice)

        if self.Position > 0:
            tp = self._entry_price + self._take_profit_long.Value * step
            sl = self._entry_price - self._stop_loss_long.Value * step
            expired = False
            if self._max_open_time.Value > 0 and self._entry_time is not None:
                elapsed = (candle.OpenTime - self._entry_time).TotalHours
                expired = elapsed >= self._max_open_time.Value
            if close >= tp or close <= sl or expired:
                self.SellMarket()
            return
        elif self.Position < 0:
            tp = self._entry_price - self._take_profit_short.Value * step
            sl = self._entry_price + self._stop_loss_short.Value * step
            expired = False
            if self._max_open_time.Value > 0 and self._entry_time is not None:
                elapsed = (candle.OpenTime - self._entry_time).TotalHours
                expired = elapsed >= self._max_open_time.Value
            if close <= tp or close >= sl or expired:
                self.BuyMarket()
            return

        t1 = self._t1.Value
        t2 = self._t2.Value
        if self._candles_count <= max(t1, t2):
            return

        idx_t1 = (self._candles_count - 1 - t1 + buf_len) % buf_len
        idx_t2 = (self._candles_count - 1 - t2 + buf_len) % buf_len
        open_t1 = self._open_prices[idx_t1]
        open_t2 = self._open_prices[idx_t2]

        if open_t1 - open_t2 > self._delta_short.Value * step and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._entry_time = candle.OpenTime
        elif open_t2 - open_t1 > self._delta_long.Value * step and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._entry_time = candle.OpenTime

    def CreateClone(self):
        return good_gbbi_strategy()
