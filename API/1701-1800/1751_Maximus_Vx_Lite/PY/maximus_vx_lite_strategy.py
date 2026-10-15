import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class maximus_vx_lite_strategy(Strategy):
    def __init__(self):
        super(maximus_vx_lite_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 20) \
            .SetDisplay("Lookback", "Highest/Lowest lookback period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
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
        super(maximus_vx_lite_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(maximus_vx_lite_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.lookback
        lowest = Lowest()
        lowest.Length = self.lookback
        self.SubscribeCandles(self.candle_type).Bind(highest, lowest, self.process_candle).Start()

    def process_candle(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return

        hv = float(highest)
        lv = float(lowest)

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
        return maximus_vx_lite_strategy()
