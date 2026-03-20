import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class breakout04_strategy(Strategy):
    def __init__(self):
        super(breakout04_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 30) \
            .SetDisplay("Lookback", "Channel lookback period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def lookback(self):
        return self._lookback.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(breakout04_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(breakout04_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.lookback
        lowest = Lowest()
        lowest.Length = self.lookback
        self.SubscribeCandles(self.candle_type).Bind(highest, lowest, self.process_candle).Start()

    def process_candle(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return

        if not self._has_prev:
            self._prev_high = float(highest)
            self._prev_low = float(lowest)
            self._has_prev = True
            return

        close = float(candle.ClosePrice)

        if close > self._prev_high and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close < self._prev_low and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_high = float(highest)
        self._prev_low = float(lowest)

    def CreateClone(self):
        return breakout04_strategy()
