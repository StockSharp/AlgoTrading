import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class close_cross_ma_strategy(Strategy):
    def __init__(self):
        super(close_cross_ma_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 50) \
            .SetDisplay("MA Period", "EMA period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_diff = 0.0
        self._has_prev = False
    @property
    def ma_period(self):
        return self._ma_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    def OnReseted(self):
        super(close_cross_ma_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._has_prev = False
    def OnStarted(self, time):
        super(close_cross_ma_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ma_period
        self.SubscribeCandles(self.candle_type).Bind(ema, self.process_candle).Start()
    def process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        diff = float(candle.ClosePrice) - float(ema_val)
        if not self._has_prev:
            self._prev_diff = diff
            self._has_prev = True
            return
        if self._prev_diff <= 0 and diff > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_diff >= 0 and diff < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_diff = diff
    def CreateClone(self):
        return close_cross_ma_strategy()
