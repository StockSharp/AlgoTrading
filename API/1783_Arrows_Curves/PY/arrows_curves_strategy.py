import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class arrows_curves_strategy(Strategy):
    def __init__(self):
        super(arrows_curves_strategy, self).__init__()
        self._period = self.Param("Period", 20)             .SetDisplay("Period", "Channel lookback period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))             .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def period(self):
        return self._period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(arrows_curves_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(arrows_curves_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.period
        lowest = Lowest()
        lowest.Length = self.period
        self.SubscribeCandles(self.candle_type).Bind(highest, lowest, self.process_candle).Start()

    def process_candle(self, candle, high, low):
        if candle.State != CandleStates.Finished:
            return

        hv = float(high)
        lv = float(low)

        if not self._has_prev:
            self._prev_high = hv
            self._prev_low = lv
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

        self._prev_high = hv
        self._prev_low = lv

    def CreateClone(self):
        return arrows_curves_strategy()
