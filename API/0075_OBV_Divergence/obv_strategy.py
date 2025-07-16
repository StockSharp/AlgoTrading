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
from StockSharp.Algo.Indicators import OnBalanceVolume
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class obv_divergence_strategy(Strategy):
    """
    OBV (On-Balance Volume) Divergence strategy.
    The strategy uses divergence between price and OBV indicator to identify potential reversal points.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(obv_divergence_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._divergencePeriodParam = self.Param("DivergencePeriod", 5) \
            .SetDisplay("Divergence Period", "Number of periods to look back for divergence", "Indicators")
        
        self._maPeriodParam = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation (used for exit signal)", "Indicators")
        
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossPercentParam = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        # Indicators
        self._obv = None
        self._ma = None
        
        # Store historical values for divergence detection
        self._previousPrice = 0.0
        self._previousObv = 0.0
        self._currentPrice = 0.0
        self._currentObv = 0.0

    @property
    def DivergencePeriod(self):
        return self._divergencePeriodParam.Value

    @DivergencePeriod.setter
    def DivergencePeriod(self, value):
        self._divergencePeriodParam.Value = value

    @property
    def MAPeriod(self):
        return self._maPeriodParam.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriodParam.Value = value

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
        super(obv_divergence_strategy, self).OnStarted(time)

        # Initialize values
        self._previousPrice = 0.0
        self._previousObv = 0.0
        self._currentPrice = 0.0
        self._currentObv = 0.0

        # Create indicators
        self._obv = OnBalanceVolume()
        
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MAPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._obv, self._ma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, self._obv)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(self.StopLossPercent, UnitTypes.Percent)  # Stop loss
        )

    def ProcessCandle(self, candle, obvValue, maValue):
        """
        Process each finished candle and execute trading logic.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store price and OBV values
        if self.DivergencePeriod <= 0:
            return

        self._previousPrice = self._currentPrice
        self._previousObv = self._currentObv
        
        self._currentPrice = candle.ClosePrice
        self._currentObv = obvValue
        
        maPrice = maValue

        # We need at least two points to detect divergence
        if self._previousPrice == 0:
            return

        # Check for bullish divergence
        # Price makes lower low but OBV makes higher low
        bullishDivergence = (self._currentPrice < self._previousPrice and 
                           self._currentObv > self._previousObv)

        # Check for bearish divergence
        # Price makes higher high but OBV makes lower high
        bearishDivergence = (self._currentPrice > self._previousPrice and 
                           self._currentObv < self._previousObv)

        # Log divergence information
        self.LogInfo("Price: {0} -> {1}, OBV: {2} -> {3}", 
                    self._previousPrice, self._currentPrice, 
                    self._previousObv, self._currentObv)
        self.LogInfo("Bullish divergence: {0}, Bearish divergence: {1}", 
                    bullishDivergence, bearishDivergence)

        # Trading decisions based on divergence
        if bullishDivergence and self.Position <= 0:
            # Bullish divergence - go long
            self.CancelActiveOrders()
            self.BuyMarket(self.Volume + abs(self.Position))
            self.LogInfo("Long entry: Bullish divergence detected at price {0}", self._currentPrice)

        elif bearishDivergence and self.Position >= 0:
            # Bearish divergence - go short
            self.CancelActiveOrders()
            self.SellMarket(self.Volume + abs(self.Position))
            self.LogInfo("Short entry: Bearish divergence detected at price {0}", self._currentPrice)

        # Exit logic based on moving average
        if self.Position > 0 and self._currentPrice > maPrice:
            # Exit long position when price is above MA
            self.SellMarket(abs(self.Position))
            self.LogInfo("Long exit: Price {0} above MA {1}", self._currentPrice, maPrice)

        elif self.Position < 0 and self._currentPrice < maPrice:
            # Exit short position when price is below MA
            self.BuyMarket(abs(self.Position))
            self.LogInfo("Short exit: Price {0} below MA {1}", self._currentPrice, maPrice)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return obv_divergence_strategy()