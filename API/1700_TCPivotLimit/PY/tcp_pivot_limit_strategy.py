import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class tcp_pivot_limit_strategy(Strategy):
    def __init__(self):
        super(tcp_pivot_limit_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._current_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_close = 0.0
        self._pivot = 0.0
        self._r1 = 0.0
        self._s1 = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tcp_pivot_limit_strategy, self).OnReseted()
        self._current_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_close = 0.0
        self._pivot = 0.0
        self._r1 = 0.0
        self._s1 = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(tcp_pivot_limit_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        day = candle.OpenTime.Date
        if self._current_day != day:
            if self._current_day is not None:
                self._pivot = (self._day_high + self._day_low + self._day_close) / 3.0
                self._r1 = 2.0 * self._pivot - self._day_low
                self._s1 = 2.0 * self._pivot - self._day_high
            self._current_day = day
            self._day_high = candle.HighPrice
            self._day_low = candle.LowPrice
            self._day_close = candle.ClosePrice
            return
        self._day_high = max(float(self._day_high), float(candle.HighPrice))
        self._day_low = min(float(self._day_low), float(candle.LowPrice))
        self._day_close = candle.ClosePrice
        if self._pivot == 0:
            return
        close = candle.ClosePrice
        if self.Position == 0:
            # Buy at support
            if close <= self._s1:
                self.BuyMarket()
                self._entry_price = close
            # Sell at resistance
            elif close >= self._r1:
                self.SellMarket()
                self._entry_price = close
        elif self.Position > 0:
            # Exit long at resistance or stop at entry - (r1 - s1)
            if close >= self._r1 or close <= self._entry_price - (self._r1 - self._s1):
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            # Exit short at support or stop
            if close <= self._s1 or close >= self._entry_price + (self._r1 - self._s1):
                self.BuyMarket()
                self._entry_price = 0

    def CreateClone(self):
        return tcp_pivot_limit_strategy()
