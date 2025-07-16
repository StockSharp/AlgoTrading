import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class tradingview_supertrend_flip_strategy(Strategy):
    """
    Strategy based on Supertrend indicator flips with volume confirmation.
    It detects when Supertrend flips from above price to below (bullish) or 
    from below price to above (bearish) and confirms the signal with above-average volume.
    
    """
    
    def __init__(self):
        super(tradingview_supertrend_flip_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetDisplay("Supertrend Period", "Period for Supertrend calculation", "Indicators")
        
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend calculation", "Indicators")
        
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetDisplay("Volume Avg Period", "Period for volume average calculation", "Indicators")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # State tracking
        self._prev_supertrend_value = 0.0
        self._prev_is_price_above_supertrend = False
        self._avg_volume = 0.0
        self._supertrend_value = 0.0
        self._volume_queue = []

    @property
    def supertrend_period(self):
        """Period for Supertrend calculation."""
        return self._supertrend_period.Value

    @supertrend_period.setter
    def supertrend_period(self, value):
        self._supertrend_period.Value = value

    @property
    def supertrend_multiplier(self):
        """Multiplier for Supertrend calculation."""
        return self._supertrend_multiplier.Value

    @supertrend_multiplier.setter
    def supertrend_multiplier(self, value):
        self._supertrend_multiplier.Value = value

    @property
    def volume_avg_period(self):
        """Period for volume average calculation."""
        return self._volume_avg_period.Value

    @volume_avg_period.setter
    def volume_avg_period(self, value):
        self._volume_avg_period.Value = value

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
        super(tradingview_supertrend_flip_strategy, self).OnReseted()
        self._prev_supertrend_value = 0.0
        self._prev_is_price_above_supertrend = False
        self._avg_volume = 0.0
        self._supertrend_value = 0.0
        self._volume_queue = []

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(tradingview_supertrend_flip_strategy, self).OnStarted(time)

        # Initialize state
        self._prev_supertrend_value = 0.0
        self._prev_is_price_above_supertrend = False
        self._avg_volume = 0.0
        self._supertrend_value = 0.0
        self._volume_queue = []

        # Create custom indicators
        # Since StockSharp doesn't have built-in Supertrend, we use ATR and customize calculation
        atr = AverageTrueRange()
        atr.Length = self.supertrend_period
        
        sma = SimpleMovingAverage()  # For volume average
        sma.Length = self.volume_avg_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, sma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atr_value, volume_avg_value):
        """
        Processes each finished candle and executes Supertrend flip logic with volume confirmation.
        
        :param candle: The processed candle message.
        :param atr_value: The current value of the ATR indicator.
        :param volume_avg_value: The current value of the volume average (not used directly).
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert ATR value to float
        atr_decimal = float(atr_value)

        # Calculate Supertrend components
        median_price = (candle.HighPrice + candle.LowPrice) / 2
        basic_upper_band = median_price + self.supertrend_multiplier * atr_decimal
        basic_lower_band = median_price - self.supertrend_multiplier * atr_decimal

        # If this is the first processed candle, initialize values
        if self._prev_supertrend_value == 0:
            if candle.ClosePrice > median_price:
                self._supertrend_value = basic_lower_band
            else:
                self._supertrend_value = basic_upper_band
            self._prev_supertrend_value = self._supertrend_value
            self._prev_is_price_above_supertrend = candle.ClosePrice > self._supertrend_value
            
            # Initialize volume tracking
            self._volume_queue.append(candle.TotalVolume)
            self._avg_volume = candle.TotalVolume
            return

        # Determine current Supertrend value based on previous value and current price
        if self._prev_supertrend_value <= candle.HighPrice:
            # Previous Supertrend was resistance
            self._supertrend_value = max(basic_lower_band, self._prev_supertrend_value)
        elif self._prev_supertrend_value >= candle.LowPrice:
            # Previous Supertrend was support
            self._supertrend_value = min(basic_upper_band, self._prev_supertrend_value)
        else:
            # Price crossed the Supertrend
            if candle.ClosePrice > self._prev_supertrend_value:
                self._supertrend_value = basic_lower_band
            else:
                self._supertrend_value = basic_upper_band

        # Update volume tracking
        self._volume_queue.append(candle.TotalVolume)
        if len(self._volume_queue) > self.volume_avg_period:
            self._volume_queue.pop(0)
            
        # Calculate average volume
        total_volume = sum(self._volume_queue)
        self._avg_volume = total_volume / len(self._volume_queue)

        # Check if price is above or below Supertrend
        is_price_above_supertrend = candle.ClosePrice > self._supertrend_value
        
        # Check for Supertrend flip
        is_flipped_bullish = (not self._prev_is_price_above_supertrend and 
                            is_price_above_supertrend)
        is_flipped_bearish = (self._prev_is_price_above_supertrend and 
                            not is_price_above_supertrend)
        
        # Check volume confirmation
        is_high_volume = candle.TotalVolume > self._avg_volume

        # Trading logic with volume confirmation
        if is_flipped_bullish and is_high_volume and self.Position <= 0:
            # Supertrend flipped bullish with high volume - Buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Supertrend flipped bullish with volume {0} (avg: {1})".format(
                candle.TotalVolume, self._avg_volume))
        elif is_flipped_bearish and is_high_volume and self.Position >= 0:
            # Supertrend flipped bearish with high volume - Sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Supertrend flipped bearish with volume {0} (avg: {1})".format(
                candle.TotalVolume, self._avg_volume))
        # Exit logic
        elif is_flipped_bearish and self.Position > 0:
            # Supertrend flipped bearish - Exit long position
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Supertrend flipped bearish")
        elif is_flipped_bullish and self.Position < 0:
            # Supertrend flipped bullish - Exit short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Supertrend flipped bullish")

        # Update previous values for next candle
        self._prev_supertrend_value = self._supertrend_value
        self._prev_is_price_above_supertrend = is_price_above_supertrend

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return tradingview_supertrend_flip_strategy()
