import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class lsma_fast_simple_alternative_calculation_strategy(Strategy):
    def __init__(self):
        super(lsma_fast_simple_alternative_calculation_strategy, self).__init__()
        self._length = self.Param("Length", 50) \
            .SetDisplay("LSMA Length", "Length for LSMA calculation", "LSMA")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_diff = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(lsma_fast_simple_alternative_calculation_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(lsma_fast_simple_alternative_calculation_strategy, self).OnStarted2(time)
        self._prev_diff = 0.0
        self._cooldown = 0
        self._wma = WeightedMovingAverage()
        self._wma.Length = self._length.Value
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._wma, self._sma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, wma_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._wma.IsFormed or not self._sma.IsFormed:
            return
        wv = float(wma_val)
        sv = float(sma_val)
        close = float(candle.ClosePrice)
        lsma = 3.0 * wv - 2.0 * sv
        diff = close - lsma
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_diff = diff
            return
        if self._prev_diff <= 0.0 and diff > 0.0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown = 10
        elif self._prev_diff >= 0.0 and diff < 0.0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown = 10
        self._prev_diff = diff

    def CreateClone(self):
        return lsma_fast_simple_alternative_calculation_strategy()
