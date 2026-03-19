import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class session_breakout_strategy(Strategy):
    def __init__(self):
        super(session_breakout_strategy, self).__init__()
        self._range_start = self.Param("RangeStartHour", 0).SetDisplay("Range Start", "Hour to start tracking range", "Sessions")
        self._range_end = self.Param("RangeEndHour", 8).SetDisplay("Range End", "Hour to stop tracking range", "Sessions")
        self._trade_end = self.Param("TradeEndHour", 20).SetDisplay("Trade End", "Hour to stop trading", "Sessions")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(session_breakout_strategy, self).OnReseted()
        self._current_date = None
        self._range_high = None
        self._range_low = None
        self._range_complete = False
        self._traded_today = False

    def OnStarted(self, time):
        super(session_breakout_strategy, self).OnStarted(time)
        self._current_date = None
        self._range_high = None
        self._range_low = None
        self._range_complete = False
        self._traded_today = False

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        candle_date = candle.OpenTime.Date
        hour = candle.OpenTime.Hour

        if self._current_date is None or candle_date != self._current_date:
            self._current_date = candle_date
            self._range_high = None
            self._range_low = None
            self._range_complete = False
            self._traded_today = False

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if hour >= self._range_start.Value and hour < self._range_end.Value:
            if self._range_high is None or high > self._range_high:
                self._range_high = high
            if self._range_low is None or low < self._range_low:
                self._range_low = low
            return

        if not self._range_complete and hour >= self._range_end.Value and self._range_high is not None and self._range_low is not None:
            self._range_complete = True

        if not self._range_complete or self._range_high is None or self._range_low is None:
            return

        if hour >= self._range_end.Value and hour < self._trade_end.Value:
            if self.Position <= 0 and close > self._range_high and not self._traded_today:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._traded_today = True
            elif self.Position >= 0 and close < self._range_low and not self._traded_today:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._traded_today = True

        if hour >= self._trade_end.Value and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()

    def CreateClone(self):
        return session_breakout_strategy()
