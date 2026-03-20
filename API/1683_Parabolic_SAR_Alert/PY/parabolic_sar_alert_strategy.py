import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_alert_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_alert_strategy, self).__init__()
        self._initial_acceleration = self.Param("InitialAcceleration", 0.02) \
            .SetDisplay("Initial Acceleration", "Initial acceleration factor for Parabolic SAR", "SAR Settings")
        self._max_acceleration = self.Param("MaxAcceleration", 0.2) \
            .SetDisplay("Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "SAR Settings")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._initialized = False

    @property
    def initial_acceleration(self):
        return self._initial_acceleration.Value

    @property
    def max_acceleration(self):
        return self._max_acceleration.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_alert_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(parabolic_sar_alert_strategy, self).OnStarted(time)
        parabolic_sar = ParabolicSar()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(parabolic_sar, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._initialized:
            self._prev_sar = sar_value
            self._prev_close = candle.ClosePrice
            self._initialized = True
            return
        cross_up = self._prev_sar > self._prev_close and sar_value < candle.ClosePrice
        cross_down = self._prev_sar < self._prev_close and sar_value > candle.ClosePrice
        if cross_up and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        self._prev_sar = sar_value
        self._prev_close = candle.ClosePrice

    def CreateClone(self):
        return parabolic_sar_alert_strategy()
