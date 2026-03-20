import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class tcp_pivot_stop_strategy(Strategy):
    """Pivot-based breakout with support/resistance exits."""
    def __init__(self):
        super(tcp_pivot_stop_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Time frame", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tcp_pivot_stop_strategy, self).OnReseted()
        self._pivot = 0
        self._res1 = 0
        self._sup1 = 0
        self._prev_close = 0
        self._prev_high = 0
        self._prev_low = 0
        self._bar_count = 0

    def OnStarted(self, time):
        super(tcp_pivot_stop_strategy, self).OnStarted(time)
        self._pivot = 0
        self._res1 = 0
        self._sup1 = 0
        self._prev_close = 0
        self._prev_high = 0
        self._prev_low = 0
        self._bar_count = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bar_count += 1
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        # Recalculate pivot every 12 bars
        if self._bar_count % 12 == 0 and self._prev_high > 0:
            self._pivot = (self._prev_high + self._prev_low + self._prev_close) / 3.0
            self._res1 = 2.0 * self._pivot - self._prev_low
            self._sup1 = 2.0 * self._pivot - self._prev_high

        self._prev_high = high
        self._prev_low = low

        if self._pivot > 0 and self._prev_close > 0:
            # Cross above pivot => long
            if self._prev_close <= self._pivot and close > self._pivot and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            # Cross below pivot => short
            elif self._prev_close >= self._pivot and close < self._pivot and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
            # Exit long at resistance or support
            elif self.Position > 0 and (close >= self._res1 or close <= self._sup1):
                self.SellMarket()
            # Exit short at support or resistance
            elif self.Position < 0 and (close <= self._sup1 or close >= self._res1):
                self.BuyMarket()

        self._prev_close = close

    def CreateClone(self):
        return tcp_pivot_stop_strategy()
