import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class parabolic_sar_cci_strategy(Strategy):
    """Strategy based on Parabolic SAR and CCI indicators"""

    def __init__(self):
        super(parabolic_sar_cci_strategy, self).__init__()

        self._sar_acceleration_factor = self.Param("SarAccelerationFactor", 0.02) \
            .SetRange(0.01, 0.05) \
            .SetDisplay("SAR AF", "Parabolic SAR acceleration factor", "Indicators")

        self._sar_max_acceleration_factor = self.Param("SarMaxAccelerationFactor", 0.2) \
            .SetRange(0.1, 0.5) \
            .SetDisplay("SAR Max AF", "Parabolic SAR maximum acceleration factor", "Indicators")

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown = 0
        self._prev_cci = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_cci_strategy, self).OnReseted()
        self._cooldown = 0
        self._prev_cci = 0.0

    def OnStarted2(self, time):
        super(parabolic_sar_cci_strategy, self).OnStarted2(time)
        self._cooldown = 0
        self._prev_cci = 0.0

        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = self._sar_acceleration_factor.Value
        parabolic_sar.AccelerationMax = self._sar_max_acceleration_factor.Value

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(parabolic_sar, cci, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sar_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price = float(candle.ClosePrice)
        sar = float(sar_value)
        cci = float(cci_value)

        crossed_up = self._prev_cci <= 100 and cci > 100
        crossed_down = self._prev_cci >= -100 and cci < -100
        self._prev_cci = cci

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and price > sar and crossed_up and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and price < sar and crossed_down and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = cooldown_val
        elif self.Position > 0 and price < sar and cci < 0:
            self.SellMarket(self.Position)
            self._cooldown = cooldown_val
        elif self.Position < 0 and price > sar and cci > 0:
            self.BuyMarket(abs(self.Position))
            self._cooldown = cooldown_val

    def CreateClone(self):
        return parabolic_sar_cci_strategy()
