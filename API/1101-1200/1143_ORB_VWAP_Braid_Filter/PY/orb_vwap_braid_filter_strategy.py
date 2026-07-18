import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class orb_vwap_braid_filter_strategy(Strategy):
    def __init__(self):
        super(orb_vwap_braid_filter_strategy, self).__init__()
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
        super(orb_vwap_braid_filter_strategy, self).OnReseted()
        self._or_high = 0.0
        self._or_low = 0.0
        self._trade_taken_today = False
        self._was_in_or = False
        self._current_day = None
        self._or_established = False

    def OnStarted2(self, time):
        super(orb_vwap_braid_filter_strategy, self).OnStarted2(time)
        self._or_high = 0.0
        self._or_low = 0.0
        self._trade_taken_today = False
        self._was_in_or = False
        self._current_day = None
        self._or_established = False
        self._ema1 = ExponentialMovingAverage()
        self._ema1.Length = 3
        self._ema2 = ExponentialMovingAverage()
        self._ema2.Length = 7
        self._ema3 = ExponentialMovingAverage()
        self._ema3.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema1, self._ema2, self._ema3, self.OnProcess).Start()

    def OnProcess(self, candle, e1, e2, e3):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema1.IsFormed or not self._ema2.IsFormed or not self._ema3.IsFormed:
            return
        e1v = float(e1)
        e2v = float(e2)
        e3v = float(e3)
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
        in_or = hour < 1
        if in_or:
            self._or_high = max(self._or_high, high) if self._or_high > 0 else high
            self._or_low = min(self._or_low, low) if self._or_low > 0 else low
        if self._was_in_or and not in_or and self._or_high > 0 and self._or_low > 0 and self._or_high - self._or_low > 0:
            self._or_established = True
        bull_braid = e1v > e2v and e2v > e3v
        bear_braid = e1v < e2v and e2v < e3v
        if not self._trade_taken_today and self._or_established and not in_or:
            if close > self._or_high and bull_braid and self.Position <= 0:
                self.BuyMarket()
                self._trade_taken_today = True
            elif close < self._or_low and bear_braid and self.Position >= 0:
                self.SellMarket()
                self._trade_taken_today = True
        self._was_in_or = in_or

    def CreateClone(self):
        return orb_vwap_braid_filter_strategy()
