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
from StockSharp.Algo.Indicators import KeltnerChannels
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class keltner_channel_reversal_strategy(Strategy):
    """
    Keltner Channel Reversal strategy.
    The strategy enters long when price is below lower Keltner Channel and a bullish candle appears,
    enters short when price is above upper Keltner Channel and a bearish candle appears.
    
    """
    def __init__(self):
        super(keltner_channel_reversal_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._emaPeriodParam = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for the EMA in Keltner Channel", "Indicators")
        
        self._atrMultiplierParam = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for the ATR in Keltner Channel", "Indicators")
        
        self._atrPeriodParam = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossAtrMultiplierParam = self.Param("StopLossAtrMultiplier", 2.0) \
            .SetDisplay("Stop Loss ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")
        
        # Indicators
        self._keltnerChannel = None
        self._atr = None

    @property
    def EmaPeriod(self):
        return self._emaPeriodParam.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._emaPeriodParam.Value = value

    @property
    def AtrMultiplier(self):
        return self._atrMultiplierParam.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplierParam.Value = value

    @property
    def AtrPeriod(self):
        return self._atrPeriodParam.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriodParam.Value = value

    @property
    def CandleType(self):
        return self._candleTypeParam.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleTypeParam.Value = value

    @property
    def StopLossAtrMultiplier(self):
        return self._stopLossAtrMultiplierParam.Value

    @StopLossAtrMultiplier.setter
    def StopLossAtrMultiplier(self, value):
        self._stopLossAtrMultiplierParam.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(keltner_channel_reversal_strategy, self).OnStarted(time)

        # Create indicators
        self._keltnerChannel = KeltnerChannels()
        self._keltnerChannel.Length = self.EmaPeriod
        self._keltnerChannel.Multiplier = self.AtrMultiplier

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._keltnerChannel, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._keltnerChannel)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossAtrMultiplier, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, keltnerValue, atrValue):
        """
        Process each finished candle and execute trading logic.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get Keltner Channel values
        upper = keltnerValue.Upper
        lower = keltnerValue.Lower
        middle = keltnerValue.Middle

        # Determine if the candle is bullish or bearish
        isBullish = candle.ClosePrice > candle.OpenPrice
        isBearish = candle.ClosePrice < candle.OpenPrice

        # Long entry: Price below lower band and bullish candle
        if candle.ClosePrice < lower and isBullish and self.Position <= 0:
            # Cancel active orders first
            self.CancelActiveOrders()
            
            # Enter long position
            self.BuyMarket(self.Volume + abs(self.Position))
            
            self.LogInfo("Long entry: Price {0} below lower band {1} with bullish candle", 
                        candle.ClosePrice, lower)

        # Short entry: Price above upper band and bearish candle
        elif candle.ClosePrice > upper and isBearish and self.Position >= 0:
            # Cancel active orders first
            self.CancelActiveOrders()
            
            # Enter short position
            self.SellMarket(self.Volume + abs(self.Position))
            
            self.LogInfo("Short entry: Price {0} above upper band {1} with bearish candle", 
                        candle.ClosePrice, upper)

        # Long exit: Price returns to middle band
        elif candle.ClosePrice > middle and self.Position > 0:
            self.SellMarket(abs(self.Position))
            self.LogInfo("Long exit: Price {0} above middle band {1}", candle.ClosePrice, middle)

        # Short exit: Price returns to middle band
        elif candle.ClosePrice < middle and self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self.LogInfo("Short exit: Price {0} below middle band {1}", candle.ClosePrice, middle)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return keltner_channel_reversal_strategy()