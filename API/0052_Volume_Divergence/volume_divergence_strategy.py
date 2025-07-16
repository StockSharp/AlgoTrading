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
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class volume_divergence_strategy(Strategy):
    """
    Volume Divergence strategy
    Long entry: Price falls but volume increases (possible accumulation)
    Short entry: Price rises but volume increases (possible distribution)
    Exit: Price crosses MA
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(volume_divergence_strategy, self).__init__()
        
        # Initialize internal state
        self._previousClose = 0
        self._previousVolume = 0
        self._isFirstCandle = True

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._atrPeriod = self.Param("ATRPeriod", 14) \
            .SetDisplay("ATR Period", "Period for Average True Range calculation", "Strategy Parameters")

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
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(volume_divergence_strategy, self).OnReseted()
        self._previousClose = 0
        self._previousVolume = 0
        self._isFirstCandle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(volume_divergence_strategy, self).OnStarted(time)

        self._previousClose = 0
        self._previousVolume = 0
        self._isFirstCandle = True

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
        
        # Skip the first candle, just initialize values
        if self._isFirstCandle:
            self._previousClose = candle.ClosePrice
            self._previousVolume = candle.TotalVolume
            self._isFirstCandle = False
            return
        
        # Calculate price change and volume change
        priceDown = candle.ClosePrice < self._previousClose
        priceUp = candle.ClosePrice > self._previousClose
        volumeUp = candle.TotalVolume > self._previousVolume
        
        # Identify divergences
        bullishDivergence = priceDown and volumeUp  # Price down but volume up (accumulation)
        bearishDivergence = priceUp and volumeUp   # Price up but volume up too much (distribution)
        
        # Log current values
        self.LogInfo("Candle Close: {0}, Previous Close: {1}, MA: {2}".format(
            candle.ClosePrice, self._previousClose, maValue))
        self.LogInfo("Volume: {0}, Previous Volume: {1}".format(
            candle.TotalVolume, self._previousVolume))
        self.LogInfo("Bullish Divergence: {0}, Bearish Divergence: {1}".format(
            bullishDivergence, bearishDivergence))

        # Trading logic:
        # Long: Price down but volume up (accumulation)
        if bullishDivergence and self.Position <= 0:
            self.LogInfo("Buy Signal: Price down but volume up (possible accumulation)")
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: Price up but volume up too much (distribution)
        elif bearishDivergence and self.Position >= 0:
            self.LogInfo("Sell Signal: Price up but volume up too much (possible distribution)")
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Price crosses MA
        if self.Position > 0 and candle.ClosePrice < maValue:
            self.LogInfo("Exit Long: Price ({0}) < MA ({1})".format(candle.ClosePrice, maValue))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > maValue:
            self.LogInfo("Exit Short: Price ({0}) > MA ({1})".format(candle.ClosePrice, maValue))
            self.BuyMarket(Math.Abs(self.Position))

        # Store current values for next comparison
        self._previousClose = candle.ClosePrice
        self._previousVolume = candle.TotalVolume

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return volume_divergence_strategy()