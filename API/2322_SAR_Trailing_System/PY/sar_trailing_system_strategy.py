import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class sar_trailing_system_strategy(Strategy):
    def __init__(self):
        super(sar_trailing_system_strategy, self).__init__()
        self._acceleration_step = self.Param("AccelerationStep", 0.02) \
            .SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Indicators")
        self._acceleration_max = self.Param("AccelerationMax", 0.2) \
            .SetDisplay("SAR Max", "Parabolic SAR maximum acceleration", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def acceleration_step(self):
        return self._acceleration_step.Value

    @property
    def acceleration_max(self):
        return self._acceleration_max.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(sar_trailing_system_strategy, self).OnStarted(time)
        sar = ParabolicSar()
        sar.Acceleration = self.acceleration_step
        sar.AccelerationStep = self.acceleration_step
        sar.AccelerationMax = self.acceleration_max
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close_price = float(candle.ClosePrice)
        sar_value = float(sar_value)
        if close_price > sar_value and self.Position <= 0:
            self.BuyMarket()
        elif close_price < sar_value and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return sar_trailing_system_strategy()
