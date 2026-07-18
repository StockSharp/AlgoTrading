import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sma_multi_hedge2_strategy(Strategy):
    def __init__(self):
        super(sma_multi_hedge2_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "Parameters")
        self._prev_ema1 = 0.0
        self._prev_ema2 = 0.0
        self._count = 0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sma_multi_hedge2_strategy, self).OnReseted()
        self._prev_ema1 = 0.0
        self._prev_ema2 = 0.0
        self._count = 0

    def OnStarted2(self, time):
        super(sma_multi_hedge2_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        self.SubscribeCandles(self.candle_type) \
            .Bind(ema, self.process_candle) \
            .Start()

    def process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        ema_val = float(ema_val)
        self._count += 1
        if self._count < 3:
            self._prev_ema2 = self._prev_ema1
            self._prev_ema1 = ema_val
            return
        trend = 0
        if self._prev_ema2 < self._prev_ema1 and self._prev_ema1 < ema_val:
            trend = 1
        elif self._prev_ema2 > self._prev_ema1 and self._prev_ema1 > ema_val:
            trend = -1
        self._prev_ema2 = self._prev_ema1
        self._prev_ema1 = ema_val
        if trend == 1 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif trend == -1 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return sma_multi_hedge2_strategy()
