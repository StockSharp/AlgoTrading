import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class liquidex_v1_strategy(Strategy):
    def __init__(self):
        super(liquidex_v1_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 10) \
            .SetDisplay("MA Period", "WMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_close = 0.0
        self._prev_wma = 0.0
        self._has_prev = False
    @property
    def ma_period(self):
        return self._ma_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    def OnReseted(self):
        super(liquidex_v1_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_wma = 0.0
        self._has_prev = False
    def OnStarted2(self, time):
        super(liquidex_v1_strategy, self).OnStarted2(time)
        wma = WeightedMovingAverage()
        wma.Length = self.ma_period
        self.SubscribeCandles(self.candle_type).Bind(wma, self.process_candle).Start()
    def process_candle(self, candle, wma_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        wv = float(wma_val)
        if not self._has_prev:
            self._prev_close = close
            self._prev_wma = wv
            self._has_prev = True
            return
        cross_up = self._prev_close <= self._prev_wma and close > wv
        cross_down = self._prev_close >= self._prev_wma and close < wv
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_close = close
        self._prev_wma = wv
    def CreateClone(self):
        return liquidex_v1_strategy()
