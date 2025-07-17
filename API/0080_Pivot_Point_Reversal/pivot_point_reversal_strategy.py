import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DateTimeOffset
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class pivot_point_reversal_strategy(Strategy):
    """
    Pivot Point Reversal strategy.
    The strategy calculates daily pivot points and their support/resistance levels,
    and enters positions when price bounces off these levels with confirmation.
    
    """
    def __init__(self):
        super(pivot_point_reversal_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossPercentParam = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        # Store pivot points
        self._pivot = 0.0
        self._r1 = 0.0  # Resistance level 1
        self._r2 = 0.0  # Resistance level 2
        self._s1 = 0.0  # Support level 1
        self._s2 = 0.0  # Support level 2
        
        # Previous day's OHLC
        self._prevHigh = 0.0
        self._prevLow = 0.0
        self._prevClose = 0.0
        self._currentDay = DateTimeOffset.MinValue
        self._newDayStarted = False

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
        super(pivot_point_reversal_strategy, self).OnStarted(time)

        # Initialize variables
        self._prevHigh = 0.0
        self._prevLow = 0.0
        self._prevClose = 0.0
        self._pivot = 0.0
        self._r1 = 0.0
        self._r2 = 0.0
        self._s1 = 0.0
        self._s2 = 0.0
        self._currentDay = DateTimeOffset.MinValue
        self._newDayStarted = True

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        
        # Subscribe to the previous day's candles to get OHLC data
        dailySubscription = self.SubscribeCandles(tf(1*1440))
        
        # Process daily candles to get previous day's data
        dailySubscription.Bind(self.ProcessDailyCandle).Start()
        
        # Process regular candles for trading signals
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessDailyCandle(self, candle):
        """
        Process daily candles to get previous day's OHLC data.
        """
        if candle.State != CandleStates.Finished:
            return
        
        # Store previous day's OHLC for pivot point calculation
        self._prevHigh = candle.HighPrice
        self._prevLow = candle.LowPrice
        self._prevClose = float(candle.ClosePrice)
        
        # Calculate pivot points for the new day
        self.CalculatePivotPoints()
        
        self.LogInfo("New daily candle: High={0}, Low={1}, Close={2}", 
                    self._prevHigh, self._prevLow, self._prevClose)
        self.LogInfo("Pivot Points: P={0}, R1={1}, R2={2}, S1={3}, S2={4}", 
                    self._pivot, self._r1, self._r2, self._s1, self._s2)

    def CalculatePivotPoints(self):
        """
        Calculate pivot points based on previous day's OHLC.
        """
        # Only calculate if we have valid data
        if self._prevHigh == 0 or self._prevLow == 0 or self._prevClose == 0:
            return
        
        # Calculate pivot point (standard formula)
        self._pivot = (self._prevHigh + self._prevLow + self._prevClose) / 3
        
        # Calculate resistance levels
        self._r1 = (2 * self._pivot) - self._prevLow
        self._r2 = self._pivot + (self._prevHigh - self._prevLow)
        
        # Calculate support levels
        self._s1 = (2 * self._pivot) - self._prevHigh
        self._s2 = self._pivot - (self._prevHigh - self._prevLow)

    def CheckForNewDay(self, time):
        """
        Check if a new trading day started.
        """
        if self._currentDay == DateTimeOffset.MinValue:
            self._currentDay = time.Date
            self._newDayStarted = True
            return
        
        if time.Date > self._currentDay:
            self._currentDay = time.Date
            self._newDayStarted = True
            
            # If new day started but we don't have daily candle data yet,
            # we can still use the previous day's high, low, and close
            if self._prevHigh > 0 and self._prevLow > 0 and self._prevClose > 0:
                self.CalculatePivotPoints()

    def ProcessCandle(self, candle):
        """
        Process each finished candle and execute trading logic.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        
        # Check if a new day started
        self.CheckForNewDay(candle.OpenTime)
        
        # Clear positions at the start of a new day
        if self._newDayStarted:
            if self.Position != 0:
                if self.Position > 0:
                    self.SellMarket(abs(self.Position))
                else:
                    self.BuyMarket(abs(self.Position))
                
                self.LogInfo("New day started, closing position at {0}", candle.ClosePrice)
            self._newDayStarted = False
        
        # Skip trading if pivot points are not calculated yet
        if self._pivot == 0:
            return
        
        # Determine if candle is bullish or bearish
        isBullish = candle.ClosePrice > candle.OpenPrice
        isBearish = candle.ClosePrice < candle.OpenPrice
        
        # Calculate proximity to pivot points
        priceThreshold = candle.ClosePrice * 0.001  # 0.1% threshold
        
        # Check if price is near S1 and bullish
        nearS1 = abs(candle.LowPrice - self._s1) <= priceThreshold
        if nearS1 and isBullish and self.Position <= 0:
            # Bullish candle bouncing off S1 - go long
            self.CancelActiveOrders()
            self.BuyMarket(self.Volume + abs(self.Position))
            self.LogInfo("Long entry at {0} on S1 bounce at {1}", candle.ClosePrice, self._s1)
        
        # Check if price is near R1 and bearish
        nearR1 = abs(candle.HighPrice - self._r1) <= priceThreshold
        if nearR1 and isBearish and self.Position >= 0:
            # Bearish candle bouncing off R1 - go short
            self.CancelActiveOrders()
            self.SellMarket(self.Volume + abs(self.Position))
            self.LogInfo("Short entry at {0} on R1 bounce at {1}", candle.ClosePrice, self._r1)
        
        # Exit logic - target pivot point
        if self.Position > 0 and candle.ClosePrice > self._pivot:
            self.SellMarket(abs(self.Position))
            self.LogInfo("Long exit at {0} (price above pivot {1})", candle.ClosePrice, self._pivot)

        elif self.Position < 0 and candle.ClosePrice < self._pivot:
            self.BuyMarket(abs(self.Position))
            self.LogInfo("Short exit at {0} (price below pivot {1})", candle.ClosePrice, self._pivot)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return pivot_point_reversal_strategy()