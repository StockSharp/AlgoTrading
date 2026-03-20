import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class outside_bar_strategy(Strategy):
    def __init__(self):
        super(outside_bar_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(outside_bar_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
        self._last_signal_ticks = 0

    def OnStarted(self, time):
        super(outside_bar_strategy, self).OnStarted(time)
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
        self._last_signal_ticks = 0
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
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        if not self._has_prev:
            self._prev_high = high
            self._prev_low = low
            self._has_prev = True
            return
        is_outside_bar = high > self._prev_high and low < self._prev_low
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if is_outside_bar and current_ticks - self._last_signal_ticks >= cooldown_ticks:
            is_bullish = close > opn
            if is_bullish and close > sv and self.Position <= 0:
                self.BuyMarket()
                self._last_signal_ticks = current_ticks
            elif not is_bullish and close < sv and self.Position >= 0:
                self.SellMarket()
                self._last_signal_ticks = current_ticks
        self._prev_high = high
        self._prev_low = low

    def CreateClone(self):
        return outside_bar_strategy()
