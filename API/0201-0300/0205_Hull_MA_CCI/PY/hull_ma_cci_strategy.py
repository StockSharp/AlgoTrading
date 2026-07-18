import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class hull_ma_cci_strategy(Strategy):
    """Strategy based on Hull Moving Average and CCI indicators."""

    def __init__(self):
        super(hull_ma_cci_strategy, self).__init__()

        self._hull_period = self.Param("HullPeriod", 9) \
            .SetRange(5, 20) \
            .SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicators")

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ATR Period", "ATR period for stop-loss calculation", "Risk Management")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._previous_hull_value = 0.0
        self._previous_cci_value = 0.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_cci_strategy, self).OnReseted()
        self._previous_hull_value = 0.0
        self._previous_cci_value = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(hull_ma_cci_strategy, self).OnStarted2(time)
        self._previous_hull_value = 0.0
        self._previous_cci_value = 0.0
        self._cooldown = 0

        hull_ma = ExponentialMovingAverage()
        hull_ma.Length = self._hull_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(hull_ma, cci, atr, self.ProcessIndicators).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull_ma)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, hull_value, cci_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        previous_hull_value = self._previous_hull_value
        self._previous_hull_value = float(hull_value)
        previous_cci_value = self._previous_cci_value
        self._previous_cci_value = float(cci_value)

        if previous_hull_value == 0:
            return

        hull_slope = float(hull_value) > previous_hull_value
        crossed_up = previous_cci_value <= 100 and float(cci_value) > 100
        crossed_down = previous_cci_value >= -100 and float(cci_value) < -100

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and hull_slope and crossed_up and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and not hull_slope and crossed_down and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = cooldown_val
        elif self.Position > 0 and not hull_slope and float(cci_value) < 0:
            self.SellMarket(self.Position)
            self._cooldown = cooldown_val
        elif self.Position < 0 and hull_slope and float(cci_value) > 0:
            self.BuyMarket(abs(self.Position))
            self._cooldown = cooldown_val

    def CreateClone(self):
        return hull_ma_cci_strategy()
