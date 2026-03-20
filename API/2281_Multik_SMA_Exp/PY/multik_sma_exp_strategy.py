import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class multik_sma_exp_strategy(Strategy):
    def __init__(self):
        super(multik_sma_exp_strategy, self).__init__()
        self._period = self.Param("Period", 50) \
            .SetDisplay("MA Period", "Length of the moving average", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma0 = None
        self._ma1 = None
        self._ma2 = None

    @property
    def period(self):
        return self._period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multik_sma_exp_strategy, self).OnReseted()
        self._ma0 = None
        self._ma1 = None
        self._ma2 = None

    def OnStarted(self, time):
        super(multik_sma_exp_strategy, self).OnStarted(time)
        self._ma0 = None
        self._ma1 = None
        self._ma2 = None
        sma = ExponentialMovingAverage()
        sma.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        sma_value = float(sma_value)
        self._ma2 = self._ma1
        self._ma1 = self._ma0
        self._ma0 = sma_value
        if self._ma2 is None or self._ma1 is None:
            return
        dsma1 = self._ma0 - self._ma1
        dsma2 = self._ma1 - self._ma2
        if dsma2 < 0 and dsma1 < 0 and self.Position <= 0:
            self.BuyMarket()
        elif dsma2 > 0 and dsma1 > 0 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return multik_sma_exp_strategy()
