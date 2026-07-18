import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class pavan_cpr_strategy(Strategy):
    def __init__(self):
        super(pavan_cpr_strategy, self).__init__()
        self._take_profit_target = self.Param("TakeProfitTarget", 50.0) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._today_pivot = 0.0
        self._today_top = 0.0
        self._last_close = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_close = 0.0
        self._current_day = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(pavan_cpr_strategy, self).OnReseted()
        self._today_pivot = 0.0
        self._today_top = 0.0
        self._last_close = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_close = 0.0
        self._current_day = None

    def OnStarted2(self, time):
        super(pavan_cpr_strategy, self).OnStarted2(time)
        self._today_pivot = 0.0
        self._today_top = 0.0
        self._last_close = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_close = 0.0
        self._current_day = None
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        day = candle.OpenTime.Date
        if self._current_day is None or self._current_day != day:
            if self._session_high > 0:
                pivot = (self._session_high + self._session_low + self._session_close) / 3.0
                top = (self._session_high + self._session_low) / 2.0
                self._today_pivot = pivot
                self._today_top = top
            self._session_high = high
            self._session_low = low
            self._current_day = day
        else:
            self._session_high = max(self._session_high, high)
            self._session_low = min(self._session_low, low)
        self._session_close = close
        if self._today_top == 0.0:
            self._last_close = close
            return
        if self.Position == 0 and self._last_close > 0 and self._last_close < self._today_top and close > self._today_top:
            self.BuyMarket()
            tp = float(self._take_profit_target.Value)
            self._take_profit_price = close + tp
            self._stop_loss_price = self._today_pivot
        elif self.Position > 0 and self._stop_loss_price > 0:
            if low <= self._stop_loss_price or high >= self._take_profit_price:
                self.SellMarket()
                self._stop_loss_price = 0.0
                self._take_profit_price = 0.0
        elif self.Position < 0:
            self.BuyMarket()
        self._last_close = close

    def CreateClone(self):
        return pavan_cpr_strategy()
