import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class breadandbutter2_strategy(Strategy):
    def __init__(self):
        super(breadandbutter2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_wma5 = 0.0; self._prev_wma10 = 0.0; self._has_prev = False
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(breadandbutter2_strategy, self).OnReseted()
        self._prev_wma5 = 0.0; self._prev_wma10 = 0.0; self._has_prev = False
    def OnStarted(self, time):
        super(breadandbutter2_strategy, self).OnStarted(time)
        self._has_prev = False
        wma5 = WeightedMovingAverage()
        wma5.Length = 5
        wma10 = WeightedMovingAverage()
        wma10.Length = 10
        wma15 = WeightedMovingAverage()
        wma15.Length = 15
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wma5, wma10, wma15, self.process_candle).Start()
    def process_candle(self, candle, wma5, wma10, wma15):
        if candle.State != CandleStates.Finished: return
        w5 = float(wma5); w10 = float(wma10); w15 = float(wma15)
        if not self._has_prev:
            self._prev_wma5 = w5; self._prev_wma10 = w10; self._has_prev = True; return
        if self._prev_wma5 <= self._prev_wma10 and w5 > w10 and w10 > w15 and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif self._prev_wma5 >= self._prev_wma10 and w5 < w10 and w10 < w15 and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_wma5 = w5; self._prev_wma10 = w10
    def CreateClone(self): return breadandbutter2_strategy()
