import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class bollinger_band_width_strategy(Strategy):
    """
    Strategy that trades on Bollinger Bands Width expansion.
    It identifies periods of increasing volatility (widening Bollinger Bands)
    and trades in the direction of the trend as identified by price position relative to the middle band.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(bollinger_band_width_strategy, self).__init__()
        
        # Initialize internal state
        self._prevWidth = 0

        # Initialize strategy parameters
        self._bollingerPeriod = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Bollinger Parameters")

        self._bollingerDeviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Bollinger Parameters")

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")

    @property
    def BollingerPeriod(self):
        return self._bollingerPeriod.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollingerPeriod.Value = value

    @property
    def BollingerDeviation(self):
        return self._bollingerDeviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollingerDeviation.Value = value

    @property
    def AtrMultiplier(self):
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(bollinger_band_width_strategy, self).OnReseted()
        self._prevWidth = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(bollinger_band_width_strategy, self).OnStarted(time)

        # Reset state variables
        self._prevWidth = 0

        # Create indicators
        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        atr = AverageTrueRange()
        atr.Length = self.BollingerPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, atr, self.ProcessCandle).Start()

        # Configure chart if GUI is available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bollingerValue, atrValue):
        """
        Process candle and calculate Bollinger Band Width
        
        :param candle: The candle message.
        :param bollingerValue: The Bollinger Bands indicator value.
        :param atrValue: The ATR indicator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Bollinger Band values
        if bollingerValue.UpBand is None or bollingerValue.LowBand is None:
            return

        upperBand = bollingerValue.UpBand
        lowerBand = bollingerValue.LowBand

        # Calculate Bollinger Band Width
        bbWidth = upperBand - lowerBand
        
        # Initialize _prevWidth on first formed candle
        if self._prevWidth == 0:
            self._prevWidth = bbWidth
            return

        # Check if Bollinger Band Width is expanding (increasing)
        isBBWidthExpanding = bbWidth > self._prevWidth
        
        # Determine price position relative to middle band for trend direction
        isPriceAboveMiddleBand = candle.ClosePrice > bollingerValue.MovingAverage
        
        # Calculate stop-loss amount based on ATR
        stopLossAmount = to_float(atrValue) * self.AtrMultiplier

        if self.Position == 0:
            # No position - check for entry signals
            if isBBWidthExpanding:
                if isPriceAboveMiddleBand:
                    # BB Width expanding and price above middle band - buy (long)
                    self.BuyMarket(self.Volume)
                else:
                    # BB Width expanding and price below middle band - sell (short)
                    self.SellMarket(self.Volume)
        elif self.Position > 0:
            # Long position - check for exit signal
            if not isBBWidthExpanding:
                # BB Width contracting - exit long
                self.SellMarket(self.Position)
        elif self.Position < 0:
            # Short position - check for exit signal
            if not isBBWidthExpanding:
                # BB Width contracting - exit short
                self.BuyMarket(Math.Abs(self.Position))

        # Update previous BB Width
        self._prevWidth = bbWidth

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return bollinger_band_width_strategy()