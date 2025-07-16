import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class supertrend_strategy(Strategy):
    """
    Strategy based on Supertrend indicator.
    It enters long position when price is above Supertrend line 
    and short position when price is below Supertrend line.
    
    """
    
    def __init__(self):
        super(supertrend_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._period = self.Param("Period", 10) \
            .SetDisplay("Period", "Period for Supertrend calculation", "Indicators")
        
        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Indicators")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current state tracking
        self._prev_is_price_above_supertrend = False
        self._prev_supertrend_value = 0.0

    @property
    def period(self):
        """Period for Supertrend calculation."""
        return self._period.Value

    @period.setter
    def period(self, value):
        self._period.Value = value

    @property
    def multiplier(self):
        """Multiplier for Supertrend calculation."""
        return self._multiplier.Value

    @multiplier.setter
    def multiplier(self, value):
        self._multiplier.Value = value

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
        super(supertrend_strategy, self).OnReseted()
        self._prev_is_price_above_supertrend = False
        self._prev_supertrend_value = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(supertrend_strategy, self).OnStarted(time)

        # Initialize state
        self._prev_is_price_above_supertrend = False
        self._prev_supertrend_value = 0.0

        # Create ATR indicator for Supertrend calculation
        # Since StockSharp doesn't have built-in Supertrend, we calculate it manually
        atr = AverageTrueRange()
        atr.Length = self.period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)
        
        # Process candles manually and calculate Supertrend in the handler
        subscription.Bind(atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atr_value):
        """
        Processes each finished candle and executes Supertrend-based trading logic.
        
        :param candle: The processed candle message.
        :param atr_value: The current value of the ATR indicator.
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
        basic_upper_band = median_price + self.multiplier * atr_decimal
        basic_lower_band = median_price - self.multiplier * atr_decimal

        # If this is the first processed candle, initialize values
        if self._prev_supertrend_value == 0:
            if candle.ClosePrice > median_price:
                supertrend_value = basic_lower_band
            else:
                supertrend_value = basic_upper_band
            self._prev_supertrend_value = supertrend_value
            self._prev_is_price_above_supertrend = candle.ClosePrice > supertrend_value
            return

        # Determine current Supertrend value based on previous value and current price
        if self._prev_supertrend_value <= candle.HighPrice:
            # Previous Supertrend was resistance
            supertrend_value = max(basic_lower_band, self._prev_supertrend_value)
        elif self._prev_supertrend_value >= candle.LowPrice:
            # Previous Supertrend was support
            supertrend_value = min(basic_upper_band, self._prev_supertrend_value)
        else:
            # Price crossed the Supertrend
            if candle.ClosePrice > self._prev_supertrend_value:
                supertrend_value = basic_lower_band
            else:
                supertrend_value = basic_upper_band

        # Check if price is above or below Supertrend
        is_price_above_supertrend = candle.ClosePrice > supertrend_value
        
        # Detect crossovers
        is_crossed_above = is_price_above_supertrend and not self._prev_is_price_above_supertrend
        is_crossed_below = not is_price_above_supertrend and self._prev_is_price_above_supertrend

        # Trading logic
        if is_crossed_above and self.Position <= 0:
            # Price crossed above Supertrend - Buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Price ({0}) crossed above Supertrend ({1})".format(
                candle.ClosePrice, supertrend_value))
        elif is_crossed_below and self.Position >= 0:
            # Price crossed below Supertrend - Sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Price ({0}) crossed below Supertrend ({1})".format(
                candle.ClosePrice, supertrend_value))
        # Exit logic for existing positions
        elif is_crossed_below and self.Position > 0:
            # Exit long position
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Price ({0}) crossed below Supertrend ({1})".format(
                candle.ClosePrice, supertrend_value))
        elif is_crossed_above and self.Position < 0:
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Price ({0}) crossed above Supertrend ({1})".format(
                candle.ClosePrice, supertrend_value))

        # Update state for the next candle
        self._prev_supertrend_value = supertrend_value
        self._prev_is_price_above_supertrend = is_price_above_supertrend

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return supertrend_strategy()
