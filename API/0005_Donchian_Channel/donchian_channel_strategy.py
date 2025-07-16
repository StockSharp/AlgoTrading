import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class donchian_channel_strategy(Strategy):
    """
    Strategy based on Donchian Channel.
    It enters long position when price breaks through the upper band 
    and short position when price breaks through the lower band.
    
    """
    
    def __init__(self):
        super(donchian_channel_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Period for Donchian Channel calculation", "Indicators")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current state
        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0

    @property
    def channel_period(self):
        """Period for Donchian Channel."""
        return self._channel_period.Value

    @channel_period.setter
    def channel_period(self, value):
        self._channel_period.Value = value

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
        super(donchian_channel_strategy, self).OnReseted()
        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(donchian_channel_strategy, self).OnStarted(time)

        # Initialize state
        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0

        # Create indicators
        donchian = DonchianChannels()
        donchian.Length = self.channel_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, donchian_value):
        """
        Processes each finished candle and executes Donchian Channel breakout logic.
        
        :param candle: The processed candle message.
        :param donchian_value: The current value of the Donchian Channel indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from Donchian Channel indicator
        try:
            if donchian_value.UpperBand is None:
                return
            upper_value = float(donchian_value.UpperBand)

            if donchian_value.LowerBand is None:
                return
            lower_value = float(donchian_value.LowerBand)

            if donchian_value.Middle is None:
                return
            mid_value = float(donchian_value.Middle)
        except:
            # If we can't extract values, skip this candle
            return

        # Skip the first received value for proper comparison
        if self._prev_upper_band == 0:
            self._prev_close_price = candle.ClosePrice
            self._prev_upper_band = upper_value
            self._prev_lower_band = lower_value
            return

        # Check for breakouts
        is_upper_breakout = (candle.ClosePrice > self._prev_upper_band and 
                           self._prev_close_price <= self._prev_upper_band)
        is_lower_breakout = (candle.ClosePrice < self._prev_lower_band and 
                           self._prev_close_price >= self._prev_lower_band)

        # Check for exit conditions
        should_exit_long = candle.ClosePrice < mid_value and self.Position > 0
        should_exit_short = candle.ClosePrice > mid_value and self.Position < 0

        # Entry logic
        if is_upper_breakout and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Price {0} broke above upper band {1}".format(
                candle.ClosePrice, self._prev_upper_band))
        elif is_lower_breakout and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Price {0} broke below lower band {1}".format(
                candle.ClosePrice, self._prev_lower_band))
        # Exit logic
        elif should_exit_long:
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Price {0} dropped below middle line {1}".format(
                candle.ClosePrice, mid_value))
        elif should_exit_short:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Price {0} rose above middle line {1}".format(
                candle.ClosePrice, mid_value))

        # Update previous values
        self._prev_close_price = candle.ClosePrice
        self._prev_upper_band = upper_value
        self._prev_lower_band = lower_value

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return donchian_channel_strategy()
