import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import StandardDeviation, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class hv_breakout_strategy(Strategy):
    """
    Strategy that trades breakouts based on historical volatility.
    It calculates price levels for breakouts using the historical volatility of the instrument
    and enters positions when price breaks above or below those levels.
    
    """
    def __init__(self):
        super(hv_breakout_strategy, self).__init__()
        
        # Initialize internal state
        self._referencePrice = 0
        self._historicalVolatility = 0
        self._isReferenceSet = False

        # Initialize strategy parameters
        self._hvPeriod = self.Param("HvPeriod", 20) \
            .SetDisplay("HV Period", "Period for Historical Volatility calculation", "Volatility Parameters")

        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation for exit", "Exit Parameters")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")

    @property
    def HvPeriod(self):
        return self._hvPeriod.Value

    @HvPeriod.setter
    def HvPeriod(self, value):
        self._hvPeriod.Value = value

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

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
        super(hv_breakout_strategy, self).OnReseted()
        self._referencePrice = 0
        self._historicalVolatility = 0
        self._isReferenceSet = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(hv_breakout_strategy, self).OnStarted(time)

        # Create indicators
        standardDeviation = StandardDeviation()
        standardDeviation.Length = self.HvPeriod
        
        sma = SimpleMovingAverage()
        sma.Length = self.MAPeriod
        
        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(standardDeviation, sma, self.ProcessCandle).Start()

        # Configure chart if GUI is available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, standardDeviation)
            self.DrawOwnTrades(area)

        # Setup protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, stdDevValue, smaValue):
        """
        Process candle and check for HV breakout signals
        
        :param candle: The candle message.
        :param stdDevValue: The standard deviation value.
        :param smaValue: The SMA value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate historical volatility based on standard deviation
        # HV is annualized by multiplying by sqrt(252) for daily data
        # Note: We're using a simplified approach for demonstration
        self._historicalVolatility = float(stdDevValue / candle.ClosePrice)

        # On first formed candle, set reference price
        if not self._isReferenceSet:
            self._referencePrice = float(candle.ClosePrice)
            self._isReferenceSet = True
            return

        # Calculate breakout levels
        upperBreakoutLevel = self._referencePrice * (1 + self._historicalVolatility)
        lowerBreakoutLevel = self._referencePrice * (1 - self._historicalVolatility)

        if self.Position == 0:
            # No position - check for entry signals
            if candle.ClosePrice > upperBreakoutLevel:
                # Price broke above upper level - buy (long)
                self.BuyMarket(self.Volume)
                
                # Update reference price after breakout
                self._referencePrice = float(candle.ClosePrice)
            elif candle.ClosePrice < lowerBreakoutLevel:
                # Price broke below lower level - sell (short)
                self.SellMarket(self.Volume)
                
                # Update reference price after breakout
                self._referencePrice = float(candle.ClosePrice)
        elif self.Position > 0:
            # Long position - check for exit signal
            if candle.ClosePrice < smaValue:
                # Price below MA - exit long
                self.SellMarket(self.Position)
        elif self.Position < 0:
            # Short position - check for exit signal
            if candle.ClosePrice > smaValue:
                # Price above MA - exit short
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return hv_breakout_strategy()