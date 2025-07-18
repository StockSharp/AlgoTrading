import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class volume_weighted_price_breakout_strategy(Strategy):
    """
    Volume Weighted Price Breakout Strategy
    Long entry: Price rises above the volume-weighted average price over N periods
    Short entry: Price falls below the volume-weighted average price over N periods
    Exit: Price crosses MA in the opposite direction
    
    """
    def __init__(self):
        super(volume_weighted_price_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._vwapPeriod = self.Param("VWAPPeriod", 20) \
            .SetDisplay("VWAP Period", "Period for Volume Weighted Average Price calculation", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def VWAPPeriod(self):
        return self._vwapPeriod.Value

    @VWAPPeriod.setter
    def VWAPPeriod(self, value):
        self._vwapPeriod.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(volume_weighted_price_breakout_strategy, self).OnStarted(time)

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self.VWAPPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, vwma, self.ProcessCandle).Start()

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
            self.DrawIndicator(area, vwma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, vwmaValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        :param vwmaValue: The VWMA value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Log current values
        self.LogInfo("Candle Close: {0}, MA: {1}, VWMA: {2}".format(
            candle.ClosePrice, maValue, vwmaValue))

        # Trading logic:
        # Long: Price above VWMA
        if candle.ClosePrice > vwmaValue and self.Position <= 0:
            self.LogInfo("Buy Signal: Price ({0}) > VWMA ({1})".format(candle.ClosePrice, vwmaValue))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: Price below VWMA
        elif candle.ClosePrice < vwmaValue and self.Position >= 0:
            self.LogInfo("Sell Signal: Price ({0}) < VWMA ({1})".format(candle.ClosePrice, vwmaValue))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Price crosses MA in the opposite direction
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
        return volume_weighted_price_breakout_strategy()