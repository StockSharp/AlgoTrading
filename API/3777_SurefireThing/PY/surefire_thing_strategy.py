import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class surefire_thing_strategy(Strategy):
    """Daily range breakout strategy. Calculates buy/sell levels from previous day's range.
    Buys when price drops below the lower level, sells when above the upper level.
    Closes position at end of day."""

    def __init__(self):
        super(surefire_thing_strategy, self).__init__()

        self._range_multiplier = self.Param("RangeMultiplier", 0.5) \
            .SetDisplay("Range Mult", "Multiplier for range-based levels", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle series", "General")

        self._current_day = None
        self._buy_level = 0.0
        self._sell_level = 0.0
        self._levels_ready = False
        self._prev_day_close = 0.0
        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_close = 0.0
        self._has_prev_day = False
        self._traded_today = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RangeMultiplier(self):
        return self._range_multiplier.Value

    def OnReseted(self):
        super(surefire_thing_strategy, self).OnReseted()
        self._current_day = None
        self._buy_level = 0.0
        self._sell_level = 0.0
        self._levels_ready = False
        self._prev_day_close = 0.0
        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_close = 0.0
        self._has_prev_day = False
        self._traded_today = False

    def OnStarted(self, time):
        super(surefire_thing_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        # New day detected
        if self._current_day is None or day > self._current_day:
            # Close position at end of previous day
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

            # Save previous day stats
            if self._current_day is not None:
                self._prev_day_close = self._day_close
                self._prev_day_high = self._day_high
                self._prev_day_low = self._day_low
                self._has_prev_day = True

            # Calculate new levels
            if self._has_prev_day:
                day_range = self._prev_day_high - self._prev_day_low
                if day_range > 0:
                    half_range = day_range * float(self.RangeMultiplier)
                    self._buy_level = self._prev_day_close - half_range
                    self._sell_level = self._prev_day_close + half_range
                    self._levels_ready = True

            self._current_day = day
            self._day_high = high
            self._day_low = low
            self._day_close = close
            self._traded_today = False
        else:
            if high > self._day_high:
                self._day_high = high
            if low < self._day_low:
                self._day_low = low
            self._day_close = close

        if not self._levels_ready:
            return

        # Only one trade per day
        if not self._traded_today and self.Position == 0:
            if close <= self._buy_level:
                self.BuyMarket()
                self._traded_today = True
            elif close >= self._sell_level:
                self.SellMarket()
                self._traded_today = True

    def CreateClone(self):
        return surefire_thing_strategy()
