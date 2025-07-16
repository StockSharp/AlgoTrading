import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bollinger_squeeze_strategy(Strategy):
    """
    Strategy based on Bollinger Bands squeeze.
    It detects when Bollinger Bands narrow (squeeze) and then trades the breakout 
    when the bands start expanding again.
    
    """
    
    def __init__(self):
        super(bollinger_squeeze_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
        
        self._squeeze_threshold = self.Param("SqueezeThreshold", 0.1) \
            .SetDisplay("Squeeze Threshold", "Threshold for Bollinger Bands width to identify squeeze", "Strategy")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # State tracking
        self._previous_band_width = 0.0
        self._is_first_value = True
        self._is_in_squeeze = False

    @property
    def bollinger_period(self):
        """Bollinger Bands period."""
        return self._bollinger_period.Value

    @bollinger_period.setter
    def bollinger_period(self, value):
        self._bollinger_period.Value = value

    @property
    def bollinger_deviation(self):
        """Bollinger Bands deviation multiplier."""
        return self._bollinger_deviation.Value

    @bollinger_deviation.setter
    def bollinger_deviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def squeeze_threshold(self):
        """Squeeze threshold."""
        return self._squeeze_threshold.Value

    @squeeze_threshold.setter
    def squeeze_threshold(self, value):
        self._squeeze_threshold.Value = value

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
        super(bollinger_squeeze_strategy, self).OnReseted()
        self._previous_band_width = 0.0
        self._is_first_value = True
        self._is_in_squeeze = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(bollinger_squeeze_strategy, self).OnStarted(time)

        # Reset state variables
        self._previous_band_width = 0.0
        self._is_first_value = True
        self._is_in_squeeze = False

        # Create Bollinger Bands indicator
        bollinger_bands = BollingerBands()
        bollinger_bands.Length = self.bollinger_period
        bollinger_bands.Width = self.bollinger_deviation

        # Subscribe to candles and bind the indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger_bands, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger_bands)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bollinger_value):
        """
        Processes each finished candle and executes Bollinger squeeze logic.
        
        :param candle: The processed candle message.
        :param bollinger_value: The current value of the Bollinger Bands indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from Bollinger Bands indicator
        try:
            if hasattr(bollinger_value, 'UpBand') and bollinger_value.UpBand is not None:
                upper_band = float(bollinger_value.UpBand)
            else:
                return
                
            if hasattr(bollinger_value, 'LowBand') and bollinger_value.LowBand is not None:
                lower_band = float(bollinger_value.LowBand)
            else:
                return
                
            if hasattr(bollinger_value, 'MovingAverage') and bollinger_value.MovingAverage is not None:
                middle_band = float(bollinger_value.MovingAverage)
            else:
                return
        except:
            # If we can't extract values, skip this candle
            return

        # Calculate Bollinger Bands width relative to the middle band
        band_width = (upper_band - lower_band) / middle_band

        if self._is_first_value:
            self._previous_band_width = band_width
            self._is_first_value = False
            return

        # Detect squeeze (narrow Bollinger Bands)
        is_squeeze = band_width < self.squeeze_threshold

        # Check for breakout from squeeze
        if (self._is_in_squeeze and not is_squeeze and 
            band_width > self._previous_band_width):
            # Squeeze is ending with expanding bands - potential breakout
            
            # Determine breakout direction by price relative to bands
            if candle.ClosePrice > upper_band and self.Position <= 0:
                # Bullish breakout (price breaks above upper band)
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
                self.LogInfo("Buy signal: Bollinger squeeze breakout upward. Width: {0:F4}, Price: {1}, Upper Band: {2}".format(
                    band_width, candle.ClosePrice, upper_band))
            elif candle.ClosePrice < lower_band and self.Position >= 0:
                # Bearish breakout (price breaks below lower band)
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)
                self.LogInfo("Sell signal: Bollinger squeeze breakout downward. Width: {0:F4}, Price: {1}, Lower Band: {2}".format(
                    band_width, candle.ClosePrice, lower_band))

        # Update squeeze state
        self._is_in_squeeze = is_squeeze
        
        # Exit logic
        if self.Position > 0 and candle.ClosePrice < middle_band:
            # Exit long position when price falls below middle band
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: Price below middle band. Price: {0}, Middle Band: {1}".format(
                candle.ClosePrice, middle_band))
        elif self.Position < 0 and candle.ClosePrice > middle_band:
            # Exit short position when price rises above middle band
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: Price above middle band. Price: {0}, Middle Band: {1}".format(
                candle.ClosePrice, middle_band))

        # Store current band width for next comparison
        self._previous_band_width = band_width

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return bollinger_squeeze_strategy()
