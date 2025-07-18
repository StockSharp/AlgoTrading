import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class parabolic_sar_rsi_divergence_strategy(Strategy):
    """Strategy that trades based on Parabolic SAR signals when RSI shows divergence from price."""

    def __init__(self):
        super(parabolic_sar_rsi_divergence_strategy, self).__init__()

        # Strategy parameter: Parabolic SAR acceleration factor.
        self._sar_acceleration_factor = self.Param("SarAccelerationFactor", 0.02) \
            .SetRange(0.01, 0.25) \
            .SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "Indicator Settings")

        # Strategy parameter: Parabolic SAR maximum acceleration factor.
        self._sar_max_acceleration_factor = self.Param("SarMaxAccelerationFactor", 0.2) \
            .SetRange(0.1, 0.5) \
            .SetDisplay("SAR Max Acceleration Factor", "Maximum acceleration factor for Parabolic SAR", "Indicator Settings")

        # Strategy parameter: RSI period.
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicator Settings")

        # Strategy parameter: Candle type.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_rsi = 0
        self._prev_price = 0
        self._divergence_detected = False

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
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!!"""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(parabolic_sar_rsi_divergence_strategy, self).OnStarted(time)

        # Reset state variables
        self._prev_rsi = 0
        self._prev_price = 0
        self._divergence_detected = False

        # Create Parabolic SAR indicator
        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = self.SarAccelerationFactor
        parabolic_sar.AccelerationMax = self.SarMaxAccelerationFactor

        # Create RSI indicator
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription and start
        subscription.Bind(parabolic_sar, rsi, self.ProcessSignals).Start()

        # Add chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessSignals(self, candle, sar_value, rsi_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check for RSI divergence
        self.CheckRsiDivergence(candle.ClosePrice, rsi_value)

        # Trading logic based on Parabolic SAR and RSI divergence
        if self._divergence_detected:
            is_below_sar = candle.ClosePrice < sar_value

            # Bullish divergence (price falling but RSI rising) and price above SAR
            if not is_below_sar and self._prev_price > candle.ClosePrice and self._prev_rsi < rsi_value and self.Position <= 0:
                self.LogInfo("Buy signal: Bullish divergence with price ({0}) above SAR ({1})".format(candle.ClosePrice, sar_value))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self._divergence_detected = False
            # Bearish divergence (price rising but RSI falling) and price below SAR
            elif is_below_sar and self._prev_price < candle.ClosePrice and self._prev_rsi > rsi_value and self.Position >= 0:
                self.LogInfo("Sell signal: Bearish divergence with price ({0}) below SAR ({1})".format(candle.ClosePrice, sar_value))
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self._divergence_detected = False

        # Exit logic based on SAR flips
        if (self.Position > 0 and candle.ClosePrice < sar_value) or (self.Position < 0 and candle.ClosePrice > sar_value):
            self.LogInfo("Exit signal: Price crossed SAR in opposite direction. Price: {0}, SAR: {1}".format(candle.ClosePrice, sar_value))
            self.ClosePosition()

        # Store previous values for next comparison
        self._prev_rsi = rsi_value
        self._prev_price = float(candle.ClosePrice)

    def CheckRsiDivergence(self, current_price, current_rsi):
        # If we have previous values to compare
        if self._prev_price != 0 and self._prev_rsi != 0:
            # Bullish divergence: price making lower lows but RSI making higher lows
            bullish_divergence = current_price < self._prev_price and current_rsi > self._prev_rsi

            # Bearish divergence: price making higher highs but RSI making lower highs
            bearish_divergence = current_price > self._prev_price and current_rsi < self._prev_rsi

            if bullish_divergence or bearish_divergence:
                self._divergence_detected = True
                self.LogInfo("Divergence detected: {0}. Price: {1}->{2}, RSI: {3}->{4}".format(
                    "Bullish" if bullish_divergence else "Bearish",
                    self._prev_price, current_price, self._prev_rsi, current_rsi))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return parabolic_sar_rsi_divergence_strategy()
