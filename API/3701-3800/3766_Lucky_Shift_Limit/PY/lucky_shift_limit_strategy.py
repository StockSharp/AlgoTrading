import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lucky_shift_limit_strategy(Strategy):
    """Candle-based reversion strategy that reacts to sudden price jumps (high/low shifts)
    and enforces a configurable loss cap. Adapted from a Level1 quote-reversion approach
    to work with candle data for backtesting."""

    def __init__(self):
        super(lucky_shift_limit_strategy, self).__init__()

        self._shift_points = self.Param("ShiftPoints", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift points", "Minimum price delta between consecutive candles", "Trading")
        self._limit_points = self.Param("LimitPoints", 18) \
            .SetGreaterThanZero() \
            .SetDisplay("Limit points", "Maximum allowed drawdown in percentage", "Risk management")

        self._previous_high = None
        self._previous_low = None
        self._shift_threshold = 0.0
        self._limit_threshold = 0.0
        self._entry_price = 0.0
        self._thresholds_ready = False
        self._hold_bars = 0

    @property
    def ShiftPoints(self):
        return self._shift_points.Value

    @property
    def LimitPoints(self):
        return self._limit_points.Value

    def OnReseted(self):
        super(lucky_shift_limit_strategy, self).OnReseted()
        self._previous_high = None
        self._previous_low = None
        self._shift_threshold = 0.0
        self._limit_threshold = 0.0
        self._entry_price = 0.0
        self._thresholds_ready = False
        self._hold_bars = 0

    def OnStarted2(self, time):
        super(lucky_shift_limit_strategy, self).OnStarted2(time)

        tf = DataType.TimeFrame(TimeSpan.FromMinutes(5))

        subscription = self.SubscribeCandles(tf)
        subscription.Bind(self._process_candle).Start()

    def _ensure_thresholds(self, price):
        if self._thresholds_ready:
            return

        if price <= 0:
            return

        # ShiftPoints=3 -> 0.9% shift threshold, LimitPoints=18 -> 1.8% limit threshold
        self._shift_threshold = float(price) * self.ShiftPoints * 0.003
        self._limit_threshold = float(price) * self.LimitPoints * 0.01
        self._thresholds_ready = True

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._ensure_thresholds(close)

        if not self._thresholds_ready:
            return

        # Count hold bars for position management.
        if self.Position != 0:
            self._hold_bars += 1

        # Entry logic: detect sudden shifts in high/low between consecutive candles.
        # Only enter when flat.
        if self.Position == 0 and self._previous_high is not None and self._previous_low is not None:
            prev_high = self._previous_high
            prev_low = self._previous_low

            # High jumped up sharply -> sell on expected reversion
            if high - prev_high >= self._shift_threshold:
                self.SellMarket()
                self._entry_price = close
                self._hold_bars = 0
            # Low dropped sharply -> buy on expected rebound
            elif prev_low - low >= self._shift_threshold:
                self.BuyMarket()
                self._entry_price = close
                self._hold_bars = 0

        self._previous_high = high
        self._previous_low = low

        self._try_close_position(close)

    def OnOwnTradeReceived(self, trade):
        super(lucky_shift_limit_strategy, self).OnOwnTradeReceived(trade)

        if self.Position != 0 and self._entry_price == 0:
            self._entry_price = float(trade.Trade.Price)

        if self.Position == 0:
            self._entry_price = 0.0

    def _try_close_position(self, current_price):
        if self.Position == 0:
            return

        avg_price = self._entry_price
        if avg_price <= 0:
            return

        # Minimum hold of 5 bars before checking exit.
        if self._hold_bars < 5:
            return

        # Use half of shift threshold as profit target.
        profit_target = self._shift_threshold * 0.5

        if self.Position > 0:
            # Close long on profit or loss cap.
            if current_price - avg_price >= profit_target:
                self.SellMarket()
                self._hold_bars = 0
            elif self._limit_threshold > 0 and avg_price - current_price >= self._limit_threshold:
                self.SellMarket()
                self._hold_bars = 0
        elif self.Position < 0:
            # Close short on profit or loss cap.
            if avg_price - current_price >= profit_target:
                self.BuyMarket()
                self._hold_bars = 0
            elif self._limit_threshold > 0 and current_price - avg_price >= self._limit_threshold:
                self.BuyMarket()
                self._hold_bars = 0

    def CreateClone(self):
        return lucky_shift_limit_strategy()
