import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class rsi_hull_ma_strategy(Strategy):
    """
    Strategy based on RSI and Hull Moving Average indicators
    """

    def __init__(self):
        super(rsi_hull_ma_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")

        self._hull_period = self.Param("HullPeriod", 9) \
            .SetRange(5, 20) \
            .SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 30) \
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
        self._previous_rsi_value = 50.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_hull_ma_strategy, self).OnReseted()
        self._previous_hull_value = 0.0
        self._previous_rsi_value = 50.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(rsi_hull_ma_strategy, self).OnStarted(time)
        self._previous_hull_value = 0.0
        self._previous_rsi_value = 50.0
        self._cooldown = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        hull_ma = ExponentialMovingAverage()
        hull_ma.Length = self._hull_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, hull_ma, atr, self.ProcessIndicators).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, hull_ma)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, rsi_value, hull_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        previous_hull = self._previous_hull_value
        self._previous_hull_value = float(hull_value)
        previous_rsi = self._previous_rsi_value
        self._previous_rsi_value = float(rsi_value)

        if previous_hull == 0:
            return

        hull_slope = float(hull_value) > previous_hull
        crossed_below = previous_rsi >= 45 and float(rsi_value) < 45
        crossed_above = previous_rsi <= 55 and float(rsi_value) > 55

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and crossed_below and hull_slope and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and crossed_above and not hull_slope and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = cooldown_val
        elif self.Position > 0 and (float(rsi_value) > 52 or not hull_slope):
            self.SellMarket(self.Position)
            self._cooldown = cooldown_val
        elif self.Position < 0 and (float(rsi_value) < 48 or hull_slope):
            self.BuyMarket(abs(self.Position))
            self._cooldown = cooldown_val

    def CreateClone(self):
        return rsi_hull_ma_strategy()
