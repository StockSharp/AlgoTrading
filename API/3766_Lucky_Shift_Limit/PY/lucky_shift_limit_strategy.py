import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lucky_shift_limit_strategy(Strategy):
    """Quote-reversion strategy converted from Level1 bid/ask jumps to candle-based.
    Detects sudden close-to-close movements with pip multiplier logic and
    enforces a MetaTrader-style loss cap."""

    def __init__(self):
        super(lucky_shift_limit_strategy, self).__init__()

        self._shift_points = self.Param("ShiftPoints", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift points", "Minimum pip delta between consecutive closes", "Trading")
        self._limit_points = self.Param("LimitPoints", 18) \
            .SetGreaterThanZero() \
            .SetDisplay("Limit points", "Maximum allowed drawdown in pips", "Risk management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Timeframe of candles used for price tracking", "General")

        self._previous_close = None
        self._entry_price = 0.0
        self._shift_offset = 0.0
        self._limit_offset = 0.0

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
        super(lucky_shift_limit_strategy, self).OnReseted()
        self._previous_close = None
        self._entry_price = 0.0
        self._shift_offset = 0.0
        self._limit_offset = 0.0

    def _get_pip_multiplier(self, step):
        """Reproduces the MQL4 pip multiplier for 3/5-digit brokers."""
        digits = 0
        temp = step
        while temp > 0 and temp < 1.0 and digits < 10:
            temp *= 10.0
            digits += 1
        if digits == 3 or digits == 5:
            return 10.0
        return 1.0

    def _calculate_price_offset(self, points):
        if points <= 0:
            return 0.0
        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            return 0.0
        step = float(step)
        return points * step * self._get_pip_multiplier(step)

    def OnStarted2(self, time):
        super(lucky_shift_limit_strategy, self).OnStarted2(time)

        self._shift_offset = self._calculate_price_offset(self.ShiftPoints)
        self._limit_offset = self._calculate_price_offset(self.LimitPoints)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Try to close existing position first
        self._try_close_position(close)

        if self._previous_close is not None and self.Position == 0:
            prev = self._previous_close

            # Close jumped up sharply -> sell on reversal
            if self._shift_offset > 0 and close - prev >= self._shift_offset:
                self.SellMarket()
                self._entry_price = close

            # Close dropped sharply -> buy on reversal
            elif self._shift_offset > 0 and prev - close >= self._shift_offset:
                self.BuyMarket()
                self._entry_price = close

        self._previous_close = close

    def _try_close_position(self, close):
        if self.Position == 0:
            return

        avg_price = self._entry_price
        if avg_price <= 0:
            return

        if self.Position > 0:
            # Close long on any profit
            if close > avg_price:
                self.SellMarket()
                self._entry_price = 0.0
                return
            # Close long on loss cap
            if self._limit_offset > 0 and avg_price - close >= self._limit_offset:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            # Close short on any profit
            if close < avg_price:
                self.BuyMarket()
                self._entry_price = 0.0
                return
            # Close short on loss cap
            if self._limit_offset > 0 and close - avg_price >= self._limit_offset:
                self.BuyMarket()
                self._entry_price = 0.0

    def CreateClone(self):
        return lucky_shift_limit_strategy()
