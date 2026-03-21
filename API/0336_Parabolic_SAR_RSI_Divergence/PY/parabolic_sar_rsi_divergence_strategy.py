import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class parabolic_sar_rsi_divergence_strategy(Strategy):
    """
    Strategy that trades Parabolic SAR trend direction with RSI divergence-style reversals.
    """

    def __init__(self):
        super(parabolic_sar_rsi_divergence_strategy, self).__init__()

        self._sar_acceleration_factor = self.Param("SarAccelerationFactor", 0.02) \
            .SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "Indicator Settings")

        self._sar_max_acceleration_factor = self.Param("SarMaxAccelerationFactor", 0.2) \
            .SetDisplay("SAR Max Acceleration Factor", "Maximum acceleration factor for Parabolic SAR", "Indicator Settings")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicator Settings")

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level for bullish reversal detection", "Indicator Settings")

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level for bearish reversal detection", "Indicator Settings")

        self._cooldown_bars = self.Param("CooldownBars", 24) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_rsi = 0.0
        self._prev_price = 0.0
        self._has_prev_values = False
        self._cooldown_remaining = 0

    @property
    def SarAccelerationFactor(self):
        return self._sar_acceleration_factor.Value

    @SarAccelerationFactor.setter
    def SarAccelerationFactor(self, value):
        self._sar_acceleration_factor.Value = value

    @property
    def SarMaxAccelerationFactor(self):
        return self._sar_max_acceleration_factor.Value

    @SarMaxAccelerationFactor.setter
    def SarMaxAccelerationFactor(self, value):
        self._sar_max_acceleration_factor.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @RsiOversold.setter
    def RsiOversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @RsiOverbought.setter
    def RsiOverbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(parabolic_sar_rsi_divergence_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_price = 0.0
        self._has_prev_values = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(parabolic_sar_rsi_divergence_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        close_price = float(candle.ClosePrice)

        if not self._has_prev_values:
            self._prev_price = close_price
            self._prev_rsi = rsi_val
            self._has_prev_values = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        bullish_divergence = close_price < self._prev_price and rsi_val > self._prev_rsi
        bearish_divergence = close_price > self._prev_price and rsi_val < self._prev_rsi
        bullish_reversal = self._prev_rsi < self.RsiOversold and rsi_val >= self.RsiOversold
        bearish_reversal = self._prev_rsi > self.RsiOverbought and rsi_val <= self.RsiOverbought
        can_trade = self._cooldown_remaining == 0

        if can_trade and (bullish_divergence or bullish_reversal) and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = self.CooldownBars
        elif can_trade and (bearish_divergence or bearish_reversal) and self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + abs(self.Position)
            self.SellMarket(vol)
            self._cooldown_remaining = self.CooldownBars

        self._prev_price = close_price
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return parabolic_sar_rsi_divergence_strategy()
