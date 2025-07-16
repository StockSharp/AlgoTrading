import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KeltnerChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class keltner_channel_breakout_strategy(Strategy):
    """
    Strategy based on Keltner Channel breakout.
    It enters long position when price breaks through the upper band 
    and short position when price breaks through the lower band.
    
    """
    
    def __init__(self):
        super(keltner_channel_breakout_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for Exponential Moving Average", "Indicators")
        
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for Average True Range", "Indicators")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to determine channel width", "Indicators")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current state
        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._prev_ema = 0.0

    @property
    def ema_period(self):
        """Period for EMA calculation."""
        return self._ema_period.Value

    @ema_period.setter
    def ema_period(self, value):
        self._ema_period.Value = value

    @property
    def atr_period(self):
        """Period for ATR calculation."""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def atr_multiplier(self):
        """Multiplier for ATR to determine channel width."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

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
        super(keltner_channel_breakout_strategy, self).OnReseted()
        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._prev_ema = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(keltner_channel_breakout_strategy, self).OnStarted(time)

        # Initialize state
        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._prev_ema = 0.0

        # Create indicators
        keltner_channel = KeltnerChannels()
        keltner_channel.Length = self.ema_period
        keltner_channel.Multiplier = self.atr_multiplier

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(keltner_channel, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner_channel)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, keltner_value):
        """
        Processes each finished candle and executes Keltner Channel breakout logic.
        
        :param candle: The processed candle message.
        :param keltner_value: The current value of the Keltner Channel indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from Keltner Channel indicator
        try:
            if hasattr(keltner_value, 'Upper') and keltner_value.Upper is not None:
                upper_value = float(keltner_value.Upper)
            else:
                return
                
            if hasattr(keltner_value, 'Lower') and keltner_value.Lower is not None:
                lower_value = float(keltner_value.Lower)
            else:
                return
                
            if hasattr(keltner_value, 'Middle') and keltner_value.Middle is not None:
                middle_value = float(keltner_value.Middle)
            else:
                return
        except:
            # If we can't extract values, skip this candle
            return

        # Skip the first received value for proper comparison
        if self._prev_upper_band == 0:
            self._prev_close_price = candle.ClosePrice
            self._prev_upper_band = upper_value
            self._prev_lower_band = lower_value
            self._prev_ema = middle_value
            return

        # Check for breakouts
        is_upper_breakout = (candle.ClosePrice > self._prev_upper_band and 
                           self._prev_close_price <= self._prev_upper_band)
        is_lower_breakout = (candle.ClosePrice < self._prev_lower_band and 
                           self._prev_close_price >= self._prev_lower_band)

        # Check for exit conditions
        should_exit_long = candle.ClosePrice < self._prev_ema and self.Position > 0
        should_exit_short = candle.ClosePrice > self._prev_ema and self.Position < 0

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
            self.LogInfo("Exit long: Price {0} dropped below EMA {1}".format(
                candle.ClosePrice, self._prev_ema))
        elif should_exit_short:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Price {0} rose above EMA {1}".format(
                candle.ClosePrice, self._prev_ema))

        # Update previous values
        self._prev_close_price = candle.ClosePrice
        self._prev_upper_band = upper_value
        self._prev_lower_band = lower_value
        self._prev_ema = middle_value

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return keltner_channel_breakout_strategy()
