import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class btc_difficulty_adjustments_strategy(Strategy):
    def __init__(self):
        super(btc_difficulty_adjustments_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._roc_period = self.Param("RocPeriod", 50) \
            .SetDisplay("ROC Period", "Rate of change period", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 100) \
            .SetDisplay("SMA Period", "Trend filter SMA period", "Indicators")
        self._prev_roc = 0.0
        self._prev_sma = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def roc_period(self):
        return self._roc_period.Value
    @property
    def sma_period(self):
        return self._sma_period.Value

    def OnReseted(self):
        super(btc_difficulty_adjustments_strategy, self).OnReseted()
        self._prev_roc = 0.0
        self._prev_sma = 0.0

    def OnStarted(self, time):
        super(btc_difficulty_adjustments_strategy, self).OnStarted(time)
        roc = RateOfChange()
        roc.Length = self.roc_period
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(roc, sma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            roc_area = self.CreateChartArea()
            if roc_area is not None:
                self.DrawIndicator(roc_area, roc)

    def OnProcess(self, candle, roc_value, sma_value):
        if candle.State != CandleStates.Finished:
            return
        roc_val = float(roc_value)
        sma_val = float(sma_value)
        if self._prev_roc == 0 or self._prev_sma == 0:
            self._prev_roc = roc_val
            self._prev_sma = sma_val
            return

        if self._prev_roc <= 0 and roc_val > 0 and float(candle.ClosePrice) > sma_val and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_roc >= 0 and roc_val < 0 and float(candle.ClosePrice) < sma_val and self.Position >= 0:
            self.SellMarket()

        self._prev_roc = roc_val
        self._prev_sma = sma_val

    def CreateClone(self):
        return btc_difficulty_adjustments_strategy()
