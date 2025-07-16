import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class outside_bar_reversal_strategy(Strategy):
    """
    Outside Bar Reversal strategy.
    The strategy looks for outside bar patterns (a bar with higher high and lower low than the previous bar)
    and takes positions based on the direction (bullish or bearish) of the outside bar.
    
    """
    def __init__(self):
        super(outside_bar_reversal_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossPercentParam = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        # Variables to track outside bar pattern
        self._previousCandle = None

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
        super(outside_bar_reversal_strategy, self).OnStarted(time)

        # Reset variables
        self._previousCandle = None

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle):
        """
        Process each finished candle and execute trading logic.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # First candle - just store it
        if self._previousCandle is None:
            self._previousCandle = candle
            return

        # Check if current candle is an outside bar compared to previous candle
        isOutsideBar = self.IsOutsideBar(self._previousCandle, candle)
        
        if isOutsideBar:
            self.LogInfo("Outside bar detected: High {0} > Previous High {1}, Low {2} < Previous Low {3}", 
                       candle.HighPrice, self._previousCandle.HighPrice, 
                       candle.LowPrice, self._previousCandle.LowPrice)

            # Determine if the outside bar is bullish or bearish
            isBullish = candle.ClosePrice > candle.OpenPrice
            isBearish = candle.ClosePrice < candle.OpenPrice

            # Trading logic based on outside bar direction
            if isBullish and self.Position <= 0:
                # Bullish outside bar - go long
                self.CancelActiveOrders()
                self.BuyMarket(self.Volume + abs(self.Position))
                self.LogInfo("Long entry at {0} on bullish outside bar", candle.ClosePrice)

            elif isBearish and self.Position >= 0:
                # Bearish outside bar - go short
                self.CancelActiveOrders()
                self.SellMarket(self.Volume + abs(self.Position))
                self.LogInfo("Short entry at {0} on bearish outside bar", candle.ClosePrice)

        # Exit logic
        if self.Position > 0:
            # Exit long position if price breaks above the outside bar's high
            if candle.HighPrice > self._previousCandle.HighPrice:
                self.SellMarket(abs(self.Position))
                self.LogInfo("Long exit at {0} (price above outside bar high {1})", 
                           candle.ClosePrice, self._previousCandle.HighPrice)

        elif self.Position < 0:
            # Exit short position if price breaks below the outside bar's low
            if candle.LowPrice < self._previousCandle.LowPrice:
                self.BuyMarket(abs(self.Position))
                self.LogInfo("Short exit at {0} (price below outside bar low {1})", 
                           candle.ClosePrice, self._previousCandle.LowPrice)

        # Update previous candle for next iteration
        self._previousCandle = candle

    def IsOutsideBar(self, previous, current):
        """
        Check if current candle is an outside bar compared to previous candle.
        An outside bar has its high higher than the previous candle's high
        and its low lower than the previous candle's low.
        """
        return (current.HighPrice > previous.HighPrice and 
                current.LowPrice < previous.LowPrice)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return outside_bar_reversal_strategy()