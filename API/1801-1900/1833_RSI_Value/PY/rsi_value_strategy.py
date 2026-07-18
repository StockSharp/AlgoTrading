import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_value_strategy(Strategy):
    def __init__(self):
        super(rsi_value_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_level = self.Param("RsiLevel", 50.0) \
            .SetDisplay("RSI Level", "RSI crossing level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_level(self):
        return self._rsi_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_value_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(rsi_value_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        self.SubscribeCandles(self.candle_type) \
            .Bind(rsi, self.process_candle) \
            .Start()

    def process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        rsi_val = float(rsi_val)
        if not self._has_prev:
            self._prev_rsi = rsi_val
            self._has_prev = True
            return
        level = float(self.rsi_level)
        cross_up = self._prev_rsi <= level and rsi_val > level
        cross_down = self._prev_rsi >= level and rsi_val < level
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return rsi_value_strategy()
