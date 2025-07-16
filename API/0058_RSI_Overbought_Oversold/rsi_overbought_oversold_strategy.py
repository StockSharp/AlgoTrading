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
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class rsi_overbought_oversold_strategy(Strategy):
    """
    RSI Overbought/Oversold strategy that buys when RSI is oversold and sells when RSI is overbought.
    
    """
    def __init__(self):
        super(rsi_overbought_oversold_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsiPeriod = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Number of bars used in RSI calculation", "Indicator Parameters")

        self._overboughtLevel = self.Param("OverboughtLevel", 70) \
            .SetDisplay("Overbought Level", "RSI level considered overbought", "Signal Parameters")

        self._oversoldLevel = self.Param("OversoldLevel", 30) \
            .SetDisplay("Oversold Level", "RSI level considered oversold", "Signal Parameters")

        self._neutralLevel = self.Param("NeutralLevel", 50) \
            .SetDisplay("Neutral Level", "RSI level for exiting positions", "Signal Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Percentage-based stop loss from entry", "Risk Management")

    @property
    def RsiPeriod(self):
        return self._rsiPeriod.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsiPeriod.Value = value

    @property
    def OverboughtLevel(self):
        return self._overboughtLevel.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overboughtLevel.Value = value

    @property
    def OversoldLevel(self):
        return self._oversoldLevel.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversoldLevel.Value = value

    @property
    def NeutralLevel(self):
        return self._neutralLevel.Value

    @NeutralLevel.setter
    def NeutralLevel(self, value):
        self._neutralLevel.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(rsi_overbought_oversold_strategy, self).OnStarted(time)

        # Create RSI indicator
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind RSI indicator to candles
        subscription.Bind(rsi, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=False
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsiValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param rsiValue: The RSI value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return
        
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        
        self.LogInfo("RSI value: {0}, Position: {1}".format(rsiValue, self.Position))
        
        # Trading logic
        if rsiValue <= self.OversoldLevel and self.Position <= 0:
            # RSI indicates oversold condition - Buy signal
            if self.Position < 0:
                # Close any existing short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Closed short position at RSI {0}".format(rsiValue))

            # Open new long position
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy signal: RSI {0} is below oversold level {1}".format(
                rsiValue, self.OversoldLevel))
                
        elif rsiValue >= self.OverboughtLevel and self.Position >= 0:
            # RSI indicates overbought condition - Sell signal
            if self.Position > 0:
                # Close any existing long position
                self.SellMarket(self.Position)
                self.LogInfo("Closed long position at RSI {0}".format(rsiValue))

            # Open new short position
            self.SellMarket(self.Volume)
            self.LogInfo("Sell signal: RSI {0} is above overbought level {1}".format(
                rsiValue, self.OverboughtLevel))
                
        elif self.Position > 0 and rsiValue >= self.NeutralLevel:
            # Exit long position when RSI returns to neutral
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: RSI {0} returned to neutral level {1}".format(
                rsiValue, self.NeutralLevel))
        elif self.Position < 0 and rsiValue <= self.NeutralLevel:
            # Exit short position when RSI returns to neutral
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: RSI {0} returned to neutral level {1}".format(
                rsiValue, self.NeutralLevel))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return rsi_overbought_oversold_strategy()