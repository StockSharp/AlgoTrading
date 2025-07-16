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
from datatype_extensions import *

class bollinger_band_reversal_strategy(Strategy):
    """
    Bollinger Band Reversal strategy.
    The strategy enters long position when price is below the lower Bollinger Band and the candle is bullish,
    enters short position when price is above the upper Bollinger Band and the candle is bearish.
    
    """
    def __init__(self):
        super(bollinger_band_reversal_strategy, self).__init__()
        
        # Initialize internal state
        self._bollingerBands = None
        self._atr = None

        # Initialize strategy parameters
        self._bollingerPeriod = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Number of periods for Bollinger Bands", "Indicators")

        self._bollingerDeviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Number of standard deviations for Bollinger Bands", "Indicators")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to set stop-loss", "Risk Management")

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
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def AtrMultiplier(self):
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(bollinger_band_reversal_strategy, self).OnStarted(time)

        # Create indicators
        self._bollingerBands = BollingerBands()
        self._bollingerBands.Length = self.BollingerPeriod
        self._bollingerBands.Width = self.BollingerDeviation

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._bollingerBands, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollingerBands)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(10, UnitTypes.Percent),
            stopLoss=Unit(self.AtrMultiplier, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, bollingerValue, atrValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param bollingerValue: The Bollinger Bands value.
        :param atrValue: The ATR value.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get current Bollinger Bands values
        bollingerTyped = bollingerValue
        upperBand = bollingerTyped.UpBand
        lowerBand = bollingerTyped.LowBand
        middleBand = bollingerTyped.MovingAverage

        # Determine if the candle is bullish or bearish
        isBullish = candle.ClosePrice > candle.OpenPrice
        isBearish = candle.ClosePrice < candle.OpenPrice

        # Long entry: Price below lower band and bullish candle
        if candle.ClosePrice < lowerBand and isBullish and self.Position <= 0:
            # Cancel active orders first
            self.CancelActiveOrders()
            
            # Enter long position
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            
            self.LogInfo("Long entry: Price {0} below lower band {1} with bullish candle".format(
                candle.ClosePrice, lowerBand))
        # Short entry: Price above upper band and bearish candle
        elif candle.ClosePrice > upperBand and isBearish and self.Position >= 0:
            # Cancel active orders first
            self.CancelActiveOrders()
            
            # Enter short position
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            
            self.LogInfo("Short entry: Price {0} above upper band {1} with bearish candle".format(
                candle.ClosePrice, upperBand))
        # Long exit: Price above middle band
        elif candle.ClosePrice > middleBand and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Long exit: Price {0} above middle band {1}".format(candle.ClosePrice, middleBand))
        # Short exit: Price below middle band
        elif candle.ClosePrice < middleBand and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Short exit: Price {0} below middle band {1}".format(candle.ClosePrice, middleBand))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return bollinger_band_reversal_strategy()