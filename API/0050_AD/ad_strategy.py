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
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Indicators import AccumulationDistributionLine
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ad_strategy(Strategy):
    """
    Accumulation/Distribution (A/D) Strategy
    Long entry: A/D rising and price above MA
    Short entry: A/D falling and price below MA
    Exit: A/D changes direction
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(ad_strategy, self).__init__()
        
        # Initialize internal state
        self._previousADValue = 0
        self._isFirstCandle = True

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

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
        super(ad_strategy, self).OnReseted()
        self._previousADValue = 0
        self._isFirstCandle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(ad_strategy, self).OnStarted(time)

        self._previousADValue = 0
        self._isFirstCandle = True

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        ad = AccumulationDistributionLine()

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        
        # We need to bind both indicators but handle with one callback
        subscription.Bind(ma, ad, self.ProcessCandle).Start()

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
            self.DrawIndicator(area, ad)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, adValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        :param adValue: The A/D Line value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        
        # Skip the first candle, just initialize values
        if self._isFirstCandle:
            self._previousADValue = adValue
            self._isFirstCandle = False
            return
        
        # Check for A/D direction
        adRising = adValue > self._previousADValue
        adFalling = adValue < self._previousADValue
        
        # Log current values
        self.LogInfo("Candle Close: {0}, MA: {1}, A/D: {2}".format(
            candle.ClosePrice, maValue, adValue))
        self.LogInfo("Previous A/D: {0}, A/D Rising: {1}, A/D Falling: {2}".format(
            self._previousADValue, adRising, adFalling))

        # Trading logic:
        # Long: A/D rising and price above MA
        if adRising and candle.ClosePrice > maValue and self.Position <= 0:
            self.LogInfo("Buy Signal: A/D rising and Price ({0}) > MA ({1})".format(
                candle.ClosePrice, maValue))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: A/D falling and price below MA
        elif adFalling and candle.ClosePrice < maValue and self.Position >= 0:
            self.LogInfo("Sell Signal: A/D falling and Price ({0}) < MA ({1})".format(
                candle.ClosePrice, maValue))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: A/D changes direction
        if self.Position > 0 and adFalling:
            self.LogInfo("Exit Long: A/D changing direction (falling)")
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and adRising:
            self.LogInfo("Exit Short: A/D changing direction (rising)")
            self.BuyMarket(Math.Abs(self.Position))

        # Store current A/D value for next comparison
        self._previousADValue = adValue

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return ad_strategy()