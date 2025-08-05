import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class parabolic_sar_trend_strategy(Strategy):
    """
    Strategy based on Parabolic SAR indicator.
    It enters long position when price is above SAR and short position when price is below SAR.
    
    """
    
    def __init__(self):
        super(parabolic_sar_trend_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._acceleration_factor = self.Param("AccelerationFactor", 0.02) \
            .SetDisplay("Acceleration Factor", "Initial acceleration factor for SAR calculation", "Indicators")
        
        self._max_acceleration_factor = self.Param("MaxAccelerationFactor", 0.2) \
            .SetDisplay("Max Acceleration Factor", "Maximum acceleration factor for SAR calculation", "Indicators")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current state
        self._prev_sar_value = 0.0
        self._prev_is_price_above_sar = False

    @property
    def acceleration_factor(self):
        """Initial acceleration factor for SAR."""
        return self._acceleration_factor.Value

    @acceleration_factor.setter
    def acceleration_factor(self, value):
        self._acceleration_factor.Value = value

    @property
    def max_acceleration_factor(self):
        """Maximum acceleration factor for SAR."""
        return self._max_acceleration_factor.Value

    @max_acceleration_factor.setter
    def max_acceleration_factor(self, value):
        self._max_acceleration_factor.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(parabolic_sar_trend_strategy, self).OnReseted()
        self._prev_sar_value = 0.0
        self._prev_is_price_above_sar = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(parabolic_sar_trend_strategy, self).OnStarted(time)

        # Create Parabolic SAR indicator
        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = self.acceleration_factor
        parabolic_sar.AccelerationMax = self.max_acceleration_factor

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(parabolic_sar, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sar_value):
        """
        Processes each finished candle and executes Parabolic SAR-based trading logic.
        
        :param candle: The processed candle message.
        :param sar_value: The current value of the Parabolic SAR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert SAR value to decimal
        sar_decimal = float(sar_value)

        # Check the price position relative to SAR
        is_price_above_sar = candle.ClosePrice > sar_decimal

        # Detect signal - crossing of price and SAR
        is_entry_signal = self._prev_sar_value > 0 and is_price_above_sar != self._prev_is_price_above_sar
        
        if is_entry_signal:
            volume = self.Volume + Math.Abs(self.Position)

            # Long entry - price crosses above SAR
            if is_price_above_sar and self.Position <= 0:
                self.BuyMarket(volume)
                self.LogInfo("Buy signal: Price {0} crossed above SAR {1}".format(
                    candle.ClosePrice, sar_decimal))
            # Short entry - price crosses below SAR
            elif not is_price_above_sar and self.Position >= 0:
                self.SellMarket(volume)
                self.LogInfo("Sell signal: Price {0} crossed below SAR {1}".format(
                    candle.ClosePrice, sar_decimal))
        
        # Exit logic - when SAR catches up with price
        elif ((self.Position > 0 and not is_price_above_sar) or 
              (self.Position < 0 and is_price_above_sar)):
            self.ClosePosition()
            self.LogInfo("Exit signal: SAR {0} catching up with price {1}".format(
                sar_decimal, candle.ClosePrice))

        # Update previous values
        self._prev_sar_value = sar_decimal
        self._prev_is_price_above_sar = is_price_above_sar

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return parabolic_sar_trend_strategy()
