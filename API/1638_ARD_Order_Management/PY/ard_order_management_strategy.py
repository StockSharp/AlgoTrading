import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy


class ard_order_management_strategy(Strategy):
    def __init__(self):
        super(ard_order_management_strategy, self).__init__()
        self._de_marker_period = self.Param("DeMarkerPeriod", 14) \
            .SetDisplay("DeMarker Period", "DeMarker indicator period", "Parameters")
        self._threshold = self.Param("Threshold", 0.5) \
            .SetDisplay("Threshold", "DeMarker crossing level", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._previous_value = 0.0
        self._has_prev = False

    @property
    def de_marker_period(self):
        return self._de_marker_period.Value

    @property
    def threshold(self):
        return self._threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ard_order_management_strategy, self).OnReseted()
        self._previous_value = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ard_order_management_strategy, self).OnStarted(time)
        de_marker = DeMarker()
        de_marker.Length = self.de_marker_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(de_marker, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, de_marker)
            self.DrawOwnTrades(area)

    def on_process(self, candle, de_marker_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._previous_value = de_marker_value
            self._has_prev = True
            return
        buy_signal = self._previous_value > self.threshold and de_marker_value < self.threshold
        sell_signal = self._previous_value < self.threshold and de_marker_value > self.threshold
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        self._previous_value = de_marker_value

    def CreateClone(self):
        return ard_order_management_strategy()
