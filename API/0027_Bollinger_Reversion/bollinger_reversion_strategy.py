import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bollinger_reversion_strategy(Strategy):
    """
    Strategy based on Bollinger Bands mean reversion.
    It buys when price touches the lower band and sells when price touches the upper band,
    expecting prices to revert to the middle band (mean).
    
    """
    
    def __init__(self):
        super(bollinger_reversion_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR stop loss", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

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
    def atr_multiplier(self):
        """ATR multiplier for stop-loss."""
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
        super(bollinger_reversion_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(bollinger_reversion_strategy, self).OnStarted(time)

        # Create indicators
        bollinger_bands = BollingerBands()
        bollinger_bands.Length = self.bollinger_period
        bollinger_bands.Width = self.bollinger_deviation
        
        atr = AverageTrueRange()
        atr.Length = 14

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger_bands, atr, self.ProcessCandle).Start()

        # Enable position protection with ATR-based stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.atr_multiplier, UnitTypes.Absolute)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger_bands)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bollinger_value, atr_value):
        """
        Processes each finished candle and executes Bollinger Bands mean reversion logic.
        
        :param candle: The processed candle message.
        :param bollinger_value: The current value of the Bollinger Bands indicator.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Bollinger Bands values
        try:
            if bollinger_value.UpBand is None:
                return
            upper = float(bollinger_value.UpBand)

            if bollinger_value.LowBand is None:
                return
            lower = float(bollinger_value.LowBand)

            if bollinger_value.MovingAverage is None:
                return
            middle = float(bollinger_value.MovingAverage)
        except:
            # If we can't extract values, skip this candle
            return

        # Get current price
        close_price = candle.ClosePrice

        # Entry logic
        if close_price < lower and self.Position <= 0:
            # Buy when price falls below lower band
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Price {0} below lower band {1:F2}".format(
                close_price, lower))
        elif close_price > upper and self.Position >= 0:
            # Sell when price rises above upper band
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Price {0} above upper band {1:F2}".format(
                close_price, upper))

        # Exit logic
        if self.Position > 0 and close_price > middle:
            # Exit long position when price returns to middle band
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: Price {0} returned to middle band {1:F2}".format(
                close_price, middle))
        elif self.Position < 0 and close_price < middle:
            # Exit short position when price returns to middle band
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: Price {0} returned to middle band {1:F2}".format(
                close_price, middle))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return bollinger_reversion_strategy()
