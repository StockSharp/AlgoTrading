import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class parabolic_sar_rsi_strategy(Strategy):
    """
    Strategy combining Parabolic SAR for trend direction
    and RSI for entry confirmation.
    """

    def __init__(self):
        super(parabolic_sar_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(7, 21) \
            .SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 130) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @rsi_oversold.setter
    def rsi_oversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @rsi_overbought.setter
    def rsi_overbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnStarted(self, time):
        super(parabolic_sar_rsi_strategy, self).OnStarted(time)

        self._cooldown = 0

        parabolic_sar = ParabolicSar()
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(parabolic_sar, rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawOwnTrades(area)

            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    def ProcessCandle(self, candle, sar_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        sv = float(sar_value)

        if sv == 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Long: price above SAR + RSI not overbought
        if close > sv and rsi_value < self.rsi_overbought and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        # Short: price below SAR + RSI not oversold
        elif close < sv and rsi_value > self.rsi_oversold and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        # Exit long: SAR flips above price
        if self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        # Exit short: SAR flips below price
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def OnReseted(self):
        super(parabolic_sar_rsi_strategy, self).OnReseted()
        self._cooldown = 0

    def CreateClone(self):
        return parabolic_sar_rsi_strategy()
