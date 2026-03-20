import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lucky_code_strategy(Strategy):
    """Momentum strategy converted from Level1 bid/ask jumps to candle-based.
    Detects large consecutive close-to-close moves and enters on reversal expectation.
    Exits quickly on any profit or caps the loss at a configurable distance."""

    def __init__(self):
        super(lucky_code_strategy, self).__init__()

        self._shift_points = self.Param("ShiftPoints", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift points", "Minimum close-to-close jump required to trigger entries", "Trading")
        self._limit_points = self.Param("LimitPoints", 18) \
            .SetGreaterThanZero() \
            .SetDisplay("Limit points", "Maximum number of points allowed against the position", "Risk management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Timeframe of candles used for price tracking", "General")

        self._previous_close = None
        self._entry_price = 0.0
        self._shift_threshold = 0.0
        self._limit_threshold = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ShiftPoints(self):
        return self._shift_points.Value

    @property
    def LimitPoints(self):
        return self._limit_points.Value

    def OnReseted(self):
        super(lucky_code_strategy, self).OnReseted()
        self._previous_close = None
        self._entry_price = 0.0
        self._shift_threshold = 0.0
        self._limit_threshold = 0.0

    def OnStarted(self, time):
        super(lucky_code_strategy, self).OnStarted(time)

        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            step = 1.0
        step = float(step)

        self._shift_threshold = self.ShiftPoints * step
        self._limit_threshold = self.LimitPoints * step

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Try to close existing position first
        self._try_close_position(candle)

        if self._previous_close is not None and self.Position == 0:
            prev = self._previous_close

            # Ask jumped up (close rose sharply) -> sell on reversal expectation
            if self._shift_threshold > 0 and close - prev >= self._shift_threshold:
                self.SellMarket()
                self._entry_price = close

            # Bid dropped (close fell sharply) -> buy on reversal expectation
            elif self._shift_threshold > 0 and prev - close >= self._shift_threshold:
                self.BuyMarket()
                self._entry_price = close

        self._previous_close = close

    def _try_close_position(self, candle):
        if self.Position == 0:
            return

        avg_price = self._entry_price
        if avg_price <= 0:
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            # Close long on any profit
            if close > avg_price:
                self.SellMarket()
                self._entry_price = 0.0
            # Close long on drawdown limit
            elif self._limit_threshold > 0 and avg_price - close >= self._limit_threshold:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            # Close short on any profit
            if close < avg_price:
                self.BuyMarket()
                self._entry_price = 0.0
            # Close short on drawdown limit
            elif self._limit_threshold > 0 and close - avg_price >= self._limit_threshold:
                self.BuyMarket()
                self._entry_price = 0.0

    def CreateClone(self):
        return lucky_code_strategy()
