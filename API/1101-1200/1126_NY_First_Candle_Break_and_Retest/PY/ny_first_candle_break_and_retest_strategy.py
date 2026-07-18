import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ny_first_candle_break_and_retest_strategy(Strategy):
    def __init__(self):
        super(ny_first_candle_break_and_retest_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._ema_length = self.Param("EmaLength", 20) \
            .SetGreaterThanZero()
        self._current_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_range_set = False
        self._traded_today = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ny_first_candle_break_and_retest_strategy, self).OnReseted()
        self._current_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_range_set = False
        self._traded_today = False

    def OnStarted2(self, time):
        super(ny_first_candle_break_and_retest_strategy, self).OnStarted2(time)
        self._current_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_range_set = False
        self._traded_today = False
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self.OnProcess).Start()

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        day = candle.OpenTime.Date
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        ev = float(ema_val)
        if self._current_day is None or day != self._current_day:
            self._current_day = day
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._day_high = high
            self._day_low = low
            self._day_range_set = True
            self._traded_today = False
            return
        if not self._day_range_set or self._traded_today:
            return
        if self.Position <= 0 and close > self._day_high and close > ev:
            self.BuyMarket()
            self._traded_today = True
        elif self.Position >= 0 and close < self._day_low and close < ev:
            self.SellMarket()
            self._traded_today = True

    def CreateClone(self):
        return ny_first_candle_break_and_retest_strategy()
