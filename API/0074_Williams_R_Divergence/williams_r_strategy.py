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
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class williams_percent_r_divergence_strategy(Strategy):
    """
    Williams %R Divergence strategy.
    The strategy looks for divergences between price and Williams %R indicator to identify potential reversal points.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(williams_percent_r_divergence_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._williamsRPeriodParam = self.Param("WilliamsRPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicators")
        
        self._divergencePeriodParam = self.Param("DivergencePeriod", 5) \
            .SetDisplay("Divergence Period", "Number of periods to look back for divergence", "Indicators")
        
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossPercentParam = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        # Indicators and variables
        self._williamsR = None
        
        # Store historical values to detect divergence
        self._previousPrice = 0.0
        self._previousWilliamsR = 0.0
        self._currentPrice = 0.0
        self._currentWilliamsR = 0.0

    @property
    def WilliamsRPeriod(self):
        return self._williamsRPeriodParam.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williamsRPeriodParam.Value = value

    @property
    def DivergencePeriod(self):
        return self._divergencePeriodParam.Value

    @DivergencePeriod.setter
    def DivergencePeriod(self, value):
        self._divergencePeriodParam.Value = value

    @property
    def CandleType(self):
        return self._candleTypeParam.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleTypeParam.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercentParam.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercentParam.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(williams_percent_r_divergence_strategy, self).OnStarted(time)

        # Initialize values
        self._previousPrice = 0.0
        self._previousWilliamsR = 0.0
        self._currentPrice = 0.0
        self._currentWilliamsR = 0.0

        # Create Williams %R indicator
        self._williamsR = WilliamsR()
        self._williamsR.Length = self.WilliamsRPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._williamsR, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._williamsR)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, williamsRValue):
        """
        Process each finished candle and execute trading logic.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store price and Williams %R values
        if self.DivergencePeriod <= 0:
            return

        self._previousPrice = self._currentPrice
        self._previousWilliamsR = self._currentWilliamsR
        
        self._currentPrice = candle.ClosePrice
        self._currentWilliamsR = williamsRValue

        # We need at least two points to detect divergence
        if self._previousPrice == 0:
            return

        # Check for bullish divergence
        # Price makes lower low but Williams %R makes higher low
        bullishDivergence = (self._currentPrice < self._previousPrice and 
                           self._currentWilliamsR > self._previousWilliamsR)

        # Check for bearish divergence
        # Price makes higher high but Williams %R makes lower high
        bearishDivergence = (self._currentPrice > self._previousPrice and 
                           self._currentWilliamsR < self._previousWilliamsR)

        # Log divergence information
        self.LogInfo("Price: {0} -> {1}, Williams %R: {2} -> {3}", 
                    self._previousPrice, self._currentPrice, 
                    self._previousWilliamsR, self._currentWilliamsR)
        self.LogInfo("Bullish divergence: {0}, Bearish divergence: {1}", 
                    bullishDivergence, bearishDivergence)

        # Trading decisions based on divergence and current Williams %R levels
        if bullishDivergence and self._currentWilliamsR < -80 and self.Position <= 0:
            # Bullish divergence with oversold condition - go long
            self.CancelActiveOrders()
            self.BuyMarket(self.Volume + abs(self.Position))
            self.LogInfo("Long entry: Bullish divergence detected with Williams %R oversold ({0})", 
                        self._currentWilliamsR)

        elif bearishDivergence and self._currentWilliamsR > -20 and self.Position >= 0:
            # Bearish divergence with overbought condition - go short
            self.CancelActiveOrders()
            self.SellMarket(self.Volume + abs(self.Position))
            self.LogInfo("Short entry: Bearish divergence detected with Williams %R overbought ({0})", 
                        self._currentWilliamsR)

        # Exit logic based on Williams %R levels
        if self.Position > 0 and self._currentWilliamsR > -20:
            # Exit long position when Williams %R reaches overbought level
            self.SellMarket(abs(self.Position))
            self.LogInfo("Long exit: Williams %R reached overbought level ({0})", 
                        self._currentWilliamsR)

        elif self.Position < 0 and self._currentWilliamsR < -80:
            # Exit short position when Williams %R reaches oversold level
            self.BuyMarket(abs(self.Position))
            self.LogInfo("Short exit: Williams %R reached oversold level ({0})", 
                        self._currentWilliamsR)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return williams_percent_r_divergence_strategy()