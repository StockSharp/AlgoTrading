import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AroonOscillator
from StockSharp.Algo.Strategies import Strategy


class aroon_oscillator_sign_alert_strategy(Strategy):
    def __init__(self):
        super(aroon_oscillator_sign_alert_strategy, self).__init__()
        self._aroon_period = self.Param("AroonPeriod", 9) \
            .SetDisplay("Aroon Period", "Lookback for Aroon oscillator", "Indicator")
        self._up_level = self.Param("UpLevel", 50) \
            .SetDisplay("Up Level", "Upper threshold for sell signal", "Indicator")
        self._down_level = self.Param("DownLevel", -50) \
            .SetDisplay("Down Level", "Lower threshold for buy signal", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for processing", "General")
        self._previous_value = None

    @property
    def aroon_period(self):
        return self._aroon_period.Value

    @property
    def up_level(self):
        return self._up_level.Value

    @property
    def down_level(self):
        return self._down_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(aroon_oscillator_sign_alert_strategy, self).OnReseted()
        self._previous_value = None

    def OnStarted(self, time):
        super(aroon_oscillator_sign_alert_strategy, self).OnStarted(time)
        self._previous_value = None
        aroon = AroonOscillator()
        aroon.Length = self.aroon_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(aroon, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, aroon)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, aroon_value):
        if candle.State != CandleStates.Finished:
            return
        aroon_value = float(aroon_value)
        if self._previous_value is None:
            self._previous_value = aroon_value
            return
        down_level = float(self.down_level)
        up_level = float(self.up_level)
        if self._previous_value <= down_level and aroon_value > down_level and self.Position <= 0:
            self.BuyMarket()
        elif self._previous_value >= up_level and aroon_value < up_level and self.Position >= 0:
            self.SellMarket()
        self._previous_value = aroon_value

    def CreateClone(self):
        return aroon_oscillator_sign_alert_strategy()
