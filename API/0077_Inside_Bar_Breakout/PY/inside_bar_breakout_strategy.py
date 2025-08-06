import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class inside_bar_breakout_strategy(Strategy):
    """
    Inside Bar Breakout strategy.
    The strategy looks for inside bar patterns (a bar with high lower than the previous bar's high and low higher than the previous bar's low)
    and enters positions on breakouts of the inside bar's high or low.
    
    """
    def __init__(self):
        super(inside_bar_breakout_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossPercentParam = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        # Variables to track inside bar pattern
        self._previousCandle = None
        self._insideCandle = None
        self._waitingForBreakout = False

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

    def OnReseted(self):
        """Resets internal state when the strategy is reset."""
        super(inside_bar_breakout_strategy, self).OnReseted()
        self._previousCandle = None
        self._insideCandle = None
        self._waitingForBreakout = False

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(inside_bar_breakout_strategy, self).OnStarted(time)

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

        # Check if we're waiting for a breakout of an inside bar
        if self._waitingForBreakout:
            # Check for breakout of inside bar's high or low
            if candle.HighPrice > self._insideCandle.HighPrice:
                # Breakout above inside bar's high - bullish signal
                if self.Position <= 0:
                    self.CancelActiveOrders()
                    self.BuyMarket(self.Volume + abs(self.Position))
                    self.LogInfo("Long entry at {0} on breakout above inside bar high {1}", 
                               candle.ClosePrice, self._insideCandle.HighPrice)

                self._waitingForBreakout = False

            elif candle.LowPrice < self._insideCandle.LowPrice:
                # Breakout below inside bar's low - bearish signal
                if self.Position >= 0:
                    self.CancelActiveOrders()
                    self.SellMarket(self.Volume + abs(self.Position))
                    self.LogInfo("Short entry at {0} on breakout below inside bar low {1}", 
                               candle.ClosePrice, self._insideCandle.LowPrice)
                
                self._waitingForBreakout = False

        # Check if current candle is an inside bar compared to previous candle
        isInsideBar = self.IsInsideBar(self._previousCandle, candle)
        
        if isInsideBar:
            self._insideCandle = candle
            self._waitingForBreakout = True
            self.LogInfo("Inside bar detected: High {0} < Previous High {1}, Low {2} > Previous Low {3}", 
                       candle.HighPrice, self._previousCandle.HighPrice, 
                       candle.LowPrice, self._previousCandle.LowPrice)

        # Update previous candle for next iteration
        self._previousCandle = candle

        # Exit logic if we have an open position but not waiting for a breakout
        if not self._waitingForBreakout:
            # For long positions, exit if the price drops below the previous candle's low
            if self.Position > 0 and candle.LowPrice < self._previousCandle.LowPrice:
                self.SellMarket(abs(self.Position))
                self.LogInfo("Long exit at {0} (price below previous candle low {1})", 
                           candle.ClosePrice, self._previousCandle.LowPrice)

            # For short positions, exit if the price rises above the previous candle's high
            elif self.Position < 0 and candle.HighPrice > self._previousCandle.HighPrice:
                self.BuyMarket(abs(self.Position))
                self.LogInfo("Short exit at {0} (price above previous candle high {1})", 
                           candle.ClosePrice, self._previousCandle.HighPrice)

    def IsInsideBar(self, previous, current):
        """
        Check if current candle is an inside bar compared to previous candle.
        An inside bar has its high lower than the previous candle's high
        and its low higher than the previous candle's low.
        """
        return (current.HighPrice < previous.HighPrice and 
                current.LowPrice > previous.LowPrice)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return inside_bar_breakout_strategy()