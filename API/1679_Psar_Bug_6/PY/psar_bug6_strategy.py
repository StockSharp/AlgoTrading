import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class psar_bug6_strategy(Strategy):
    def __init__(self):
        super(psar_bug6_strategy, self).__init__()
        self._sar_step = self.Param("SarStep", 0.02) \
            .SetDisplay("SAR Step", "Acceleration factor step", "Indicator")
        self._sar_max = self.Param("SarMax", 0.2) \
            .SetDisplay("SAR Max", "Maximum acceleration factor", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._initialized = False

    @property
    def sar_step(self):
        return self._sar_step.Value

    @property
    def sar_max(self):
        return self._sar_max.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(psar_bug6_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(psar_bug6_strategy, self).OnStarted2(time)
        psar = ParabolicSar()
        psar.AccelerationStep = self.sar_step
        psar.AccelerationMax = self.sar_max
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(psar, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sar):
        if candle.State != CandleStates.Finished:
            return
        if not self._initialized:
            self._prev_sar = sar
            self._prev_close = candle.ClosePrice
            self._initialized = True
            return
        close = candle.ClosePrice
        cross_up = close > sar and self._prev_close <= self._prev_sar
        cross_down = close < sar and self._prev_close >= self._prev_sar
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_sar = sar
        self._prev_close = close

    def CreateClone(self):
        return psar_bug6_strategy()
