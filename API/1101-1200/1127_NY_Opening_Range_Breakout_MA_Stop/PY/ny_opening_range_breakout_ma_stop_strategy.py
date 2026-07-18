import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ny_opening_range_breakout_ma_stop_strategy(Strategy):
    def __init__(self):
        super(ny_opening_range_breakout_ma_stop_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 50) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._day_high = 0.0
        self._day_low = 0.0
        self._current_day = None
        self._range_set = False
        self._traded_today = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ny_opening_range_breakout_ma_stop_strategy, self).OnReseted()
        self._day_high = 0.0
        self._day_low = 0.0
        self._current_day = None
        self._range_set = False
        self._traded_today = False

    def OnStarted2(self, time):
        super(ny_opening_range_breakout_ma_stop_strategy, self).OnStarted2(time)
        self._day_high = 0.0
        self._day_low = 0.0
        self._current_day = None
        self._range_set = False
        self._traded_today = False
        self._ma = SimpleMovingAverage()
        self._ma.Length = self._ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ma, self.OnProcess).Start()

    def OnProcess(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        day = candle.OpenTime.Date
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        mv = float(ma_val)
        if self._current_day is None or day != self._current_day:
            self._current_day = day
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._day_high = high
            self._day_low = low
            self._range_set = True
            self._traded_today = False
            return
        if not self._range_set or self._traded_today:
            return
        if self.Position > 0 and close < mv:
            self.SellMarket()
            self._traded_today = True
            return
        if self.Position < 0 and close > mv:
            self.BuyMarket()
            self._traded_today = True
            return
        if self.Position <= 0 and close > self._day_high and close > mv:
            self.BuyMarket()
            self._traded_today = True
        elif self.Position >= 0 and close < self._day_low and close < mv:
            self.SellMarket()
            self._traded_today = True

    def CreateClone(self):
        return ny_opening_range_breakout_ma_stop_strategy()
