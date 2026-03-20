import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class opening_range_breakout2_strategy(Strategy):
    def __init__(self):
        super(opening_range_breakout2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._or_high = 0.0
        self._or_low = 0.0
        self._trade_taken_today = False
        self._was_in_or = False
        self._current_day = None
        self._or_established = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(opening_range_breakout2_strategy, self).OnReseted()
        self._or_high = 0.0
        self._or_low = 0.0
        self._trade_taken_today = False
        self._was_in_or = False
        self._current_day = None
        self._or_established = False

    def OnStarted(self, time):
        super(opening_range_breakout2_strategy, self).OnStarted(time)
        self._or_high = 0.0
        self._or_low = 0.0
        self._trade_taken_today = False
        self._was_in_or = False
        self._current_day = None
        self._or_established = False
        self._sma = SimpleMovingAverage()
        self._sma.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.OnProcess).Start()

    def OnProcess(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._sma.IsFormed:
            return
        sv = float(sma_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        day = candle.OpenTime.Date
        if self._current_day is None or self._current_day != day:
            self._current_day = day
            self._or_high = 0.0
            self._or_low = 0.0
            self._trade_taken_today = False
            self._or_established = False
        hour = candle.OpenTime.TimeOfDay.TotalHours
        in_or = hour >= 0 and hour < 1
        if in_or:
            self._or_high = max(self._or_high, high) if self._or_high > 0 else high
            self._or_low = min(self._or_low, low) if self._or_low > 0 else low
        if self._was_in_or and not in_or and self._or_high > 0 and self._or_low > 0:
            rng = self._or_high - self._or_low
            if rng > 0:
                self._or_established = True
        if not self._trade_taken_today and self._or_established and not in_or:
            if close > self._or_high and close > sv and self.Position <= 0:
                self.BuyMarket()
                self._trade_taken_today = True
            elif close < self._or_low and close < sv and self.Position >= 0:
                self.SellMarket()
                self._trade_taken_today = True
        self._was_in_or = in_or

    def CreateClone(self):
        return opening_range_breakout2_strategy()
