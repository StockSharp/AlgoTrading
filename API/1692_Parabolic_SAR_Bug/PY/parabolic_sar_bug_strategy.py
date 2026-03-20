import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_bug_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_bug_strategy, self).__init__()
        self._step = self.Param("Step", 0.02) \
            .SetDisplay("Step", "Acceleration factor", "Indicator")
        self._max_step = self.Param("MaxStep", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Max Step", "Maximum acceleration", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._initialized = False

    @property
    def step(self):
        return self._step.Value

    @property
    def max_step(self):
        return self._max_step.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_bug_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(parabolic_sar_bug_strategy, self).OnStarted(time)
        sar = ParabolicSar()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._initialized:
            self._prev_sar = sar_value
            self._prev_close = close
            self._initialized = True
            return
        cross_up = close > sar_value and self._prev_close <= self._prev_sar
        cross_down = close < sar_value and self._prev_close >= self._prev_sar
        if cross_up and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        self._prev_sar = sar_value
        self._prev_close = close

    def CreateClone(self):
        return parabolic_sar_bug_strategy()
