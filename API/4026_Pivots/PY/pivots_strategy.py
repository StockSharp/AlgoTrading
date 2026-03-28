import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DateTime
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class pivots_strategy(Strategy):
    def __init__(self):
        super(pivots_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for signal generation", "General")

        self._pivot_level = 0.0
        self._r1 = 0.0
        self._r2 = 0.0
        self._s1 = 0.0
        self._s2 = 0.0
        self._previous_close = None
        self._entry_price = None
        self._pivot_ready = False
        self._current_day = None
        self._day_high = 0.0
        self._day_low = float('inf')
        self._day_close = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(pivots_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = 2

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        candle_day = candle.OpenTime.Date
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._current_day is None or candle_day != self._current_day:
            if self._current_day is not None and self._day_high > 0:
                h = self._day_high
                l = self._day_low
                c = self._day_close
                self._pivot_level = (h + l + c) / 3.0
                self._r1 = 2.0 * self._pivot_level - l
                self._s1 = 2.0 * self._pivot_level - h
                self._r2 = self._pivot_level + (h - l)
                self._s2 = self._pivot_level - (h - l)
                self._pivot_ready = True

            self._current_day = candle_day
            self._day_high = high
            self._day_low = low
            self._day_close = close
        else:
            if high > self._day_high:
                self._day_high = high
            if low < self._day_low:
                self._day_low = low
            self._day_close = close

        if not self._pivot_ready:
            self._previous_close = close
            return

        if self._previous_close is None:
            self._previous_close = close
            return

        if self.Position > 0:
            if high >= self._r2 or low <= self._s1:
                self.SellMarket(Math.Abs(self.Position))
                self._entry_price = None

        elif self.Position < 0:
            if low <= self._s2 or high >= self._r1:
                self.BuyMarket(Math.Abs(self.Position))
                self._entry_price = None

        if self.Position == 0:
            cross_above_pivot = self._previous_close <= self._pivot_level and close > self._pivot_level
            cross_below_pivot = self._previous_close >= self._pivot_level and close < self._pivot_level

            if cross_above_pivot:
                self.BuyMarket()
                self._entry_price = close
            elif cross_below_pivot:
                self.SellMarket()
                self._entry_price = close

        self._previous_close = close

    def OnReseted(self):
        super(pivots_strategy, self).OnReseted()
        self._pivot_level = 0.0
        self._r1 = 0.0
        self._r2 = 0.0
        self._s1 = 0.0
        self._s2 = 0.0
        self._previous_close = None
        self._entry_price = None
        self._pivot_ready = False
        self._current_day = None
        self._day_high = 0.0
        self._day_low = float('inf')
        self._day_close = 0.0

    def CreateClone(self):
        return pivots_strategy()
