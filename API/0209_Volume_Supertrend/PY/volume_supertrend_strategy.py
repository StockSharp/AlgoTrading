import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SuperTrend
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class volume_supertrend_strategy(Strategy):
    """Strategy based on Volume and Supertrend indicators"""

    def __init__(self):
        super(volume_supertrend_strategy, self).__init__()

        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Volume Avg Period", "Period for volume average calculation", "Volume")

        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetRange(5, 30) \
            .SetDisplay("Supertrend Period", "ATR period for Supertrend", "Supertrend")

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend calculation", "Supertrend")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_supertrend_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volume_supertrend_strategy, self).OnStarted2(time)
        self._cooldown = 0

        volume_ma = ExponentialMovingAverage()
        volume_ma.Length = self._volume_avg_period.Value
        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = self._supertrend_multiplier.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(supertrend, volume_ma, self.ProcessSignals).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def ProcessSignals(self, candle, supertrend_value, volume_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1

        if self._cooldown == 0 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 500

    def CreateClone(self):
        return volume_supertrend_strategy()
