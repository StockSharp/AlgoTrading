import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class waindrops_makit0_strategy(Strategy):
    def __init__(self):
        super(waindrops_makit0_strategy, self).__init__()
        self._period_minutes = self.Param("PeriodMinutes", 120) \
            .SetDisplay("Period", "Full period in candles", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._left_vwap = None
        self._right_vwap = None
        self._counter = 0
        self._left_value = 0.0
        self._right_value = 0.0

    @property
    def period_minutes(self):
        return self._period_minutes.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(waindrops_makit0_strategy, self).OnReseted()
        self._left_vwap = None
        self._right_vwap = None
        self._counter = 0
        self._left_value = 0.0
        self._right_value = 0.0

    def OnStarted(self, time):
        super(waindrops_makit0_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        half = self.period_minutes / 2
        if self._counter < half:
            res = self._left_vwap.Process(candle)
            if res.IsEmpty:
                return
            self._left_value = res.ToDecimal()
        else:
            res = self._right_vwap.Process(candle)
            if res.IsEmpty:
                return
            self._right_value = res.ToDecimal()
        self._counter += 1
        if self._counter == half:
            self._right_vwap.Reset()
        elif self._counter >= self.period_minutes:
            self._counter = 0
            self._left_vwap.Reset()
            self._right_vwap.Reset()
            if self._right_value > self._left_value and self.Position <= 0:
                self.BuyMarket()
            elif self._right_value < self._left_value and self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return waindrops_makit0_strategy()
