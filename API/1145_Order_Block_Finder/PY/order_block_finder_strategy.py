import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class order_block_finder_strategy(Strategy):
    def __init__(self):
        super(order_block_finder_strategy, self).__init__()
        self._periods = self.Param("Periods", 5) \
            .SetGreaterThanZero()
        self._threshold = self.Param("Threshold", 0.5)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._buffer = []
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(order_block_finder_strategy, self).OnReseted()
        self._buffer = []
        self._last_signal_ticks = 0

    def OnStarted(self, time):
        super(order_block_finder_strategy, self).OnStarted(time)
        self._buffer = []
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
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        need = self._periods.Value + 1
        self._buffer.append((opn, close))
        while len(self._buffer) > need:
            self._buffer.pop(0)
        if len(self._buffer) < need:
            return
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks < cooldown_ticks:
            return
        ob_open, ob_close = self._buffer[0]
        last_open, last_close = self._buffer[-1]
        move = abs((last_close - ob_close) / ob_close) * 100.0 if ob_close != 0 else 0.0
        thresh = float(self._threshold.Value)
        if move < thresh:
            return
        up = 0
        down = 0
        periods = self._periods.Value
        for i in range(1, len(self._buffer)):
            o, c = self._buffer[i]
            if c > o:
                up += 1
            if c < o:
                down += 1
        if ob_close < ob_open and up == periods and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_ticks = current_ticks
        elif ob_close > ob_open and down == periods and self.Position >= 0:
            self.SellMarket()
            self._last_signal_ticks = current_ticks

    def CreateClone(self):
        return order_block_finder_strategy()
