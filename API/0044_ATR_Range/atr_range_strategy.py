import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_range_strategy(Strategy):
    """
    ATR Range strategy
    Enters long when price moves up by at least ATR over N candles
    Enters short when price moves down by at least ATR over N candles
    
    """
    def __init__(self):
        super(atr_range_strategy, self).__init__()
        
        # Initialize internal state
        self._nBarsAgoPrice = 0
        self._barCounter = 0

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._atrPeriod = self.Param("ATRPeriod", 14) \
            .SetDisplay("ATR Period", "Period for Average True Range calculation", "Strategy Parameters")

        self._lookbackPeriod = self.Param("LookbackPeriod", 5) \
            .SetDisplay("Lookback Period", "Number of candles to measure price movement", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def ATRPeriod(self):
        return self._atrPeriod.Value

    @ATRPeriod.setter
    def ATRPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

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
        super(atr_range_strategy, self).OnReseted()
        self._nBarsAgoPrice = 0
        self._barCounter = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(atr_range_strategy, self).OnStarted(time)

        self._nBarsAgoPrice = 0
        self._barCounter = 0

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        atr = AverageTrueRange()
        atr.Length = self.ATRPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, atr, self.ProcessCandle).Start()

        # Configure protection
        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, atrValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        :param atrValue: The ATR value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Increment bar counter
        self._barCounter += 1

        # Store price for first bar of lookback period
        if self._barCounter == 1 or self._barCounter % self.LookbackPeriod == 1:
            self._nBarsAgoPrice = candle.ClosePrice
            self.LogInfo("Storing reference price: {0} at bar {1}".format(self._nBarsAgoPrice, self._barCounter))
            return

        # Only check for signals at the end of each lookback period
        if self._barCounter % self.LookbackPeriod != 0:
            return

        # Calculate price movement over the lookback period
        priceMovement = candle.ClosePrice - self._nBarsAgoPrice
        absMovement = Math.Abs(priceMovement)
        
        # Log current values
        self.LogInfo("Candle Close: {0}, Reference Price: {1}, Movement: {2}".format(
            candle.ClosePrice, self._nBarsAgoPrice, priceMovement))
        self.LogInfo("ATR: {0}, MA: {1}, Absolute Movement: {2}".format(atrValue, maValue, absMovement))

        # Check if price movement exceeds ATR
        if absMovement >= atrValue:
            # Long signal: Price moved up by at least ATR
            if priceMovement > 0 and self.Position <= 0:
                self.LogInfo("Buy Signal: Price movement ({0}) > ATR ({1})".format(priceMovement, atrValue))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            # Short signal: Price moved down by at least ATR
            elif priceMovement < 0 and self.Position >= 0:
                self.LogInfo("Sell Signal: Price movement ({0}) < -ATR ({1})".format(priceMovement, -atrValue))
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Price crosses MA
        if self.Position > 0 and candle.ClosePrice < maValue:
            self.LogInfo("Exit Long: Price ({0}) < MA ({1})".format(candle.ClosePrice, maValue))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > maValue:
            self.LogInfo("Exit Short: Price ({0}) > MA ({1})".format(candle.ClosePrice, maValue))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return atr_range_strategy()