import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage, StandardDeviation, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bollinger_width_mean_reversion_strategy(Strategy):
    """
    Bollinger Width Mean Reversion Strategy.
    Strategy trades based on mean reversion of Bollinger Bands width.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(bollinger_width_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._bollingerLength = self.Param("BollingerLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Length", "Period for Bollinger Bands calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._bollingerDeviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._widthLookbackPeriod = self.Param("WidthLookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Width Lookback", "Lookback period for width's mean and standard deviation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._widthDeviationMultiplier = self.Param("WidthDeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Width Deviation Multiplier", "Multiplier for width's standard deviation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use in the strategy", "General")

        # Internal variables
        self._bollinger = None
        self._widthAvg = None
        self._widthStdDev = None
        self._atr = None
        self._lastWidthAvg = 0.0
        self._lastWidthStdDev = 0.0

    @property
    def BollingerLength(self):
        """Length of Bollinger Bands period."""
        return self._bollingerLength.Value

    @BollingerLength.setter
    def BollingerLength(self, value):
        self._bollingerLength.Value = value

    @property
    def BollingerDeviation(self):
        """Deviation multiplier for Bollinger Bands."""
        return self._bollingerDeviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollingerDeviation.Value = value

    @property
    def WidthLookbackPeriod(self):
        """Lookback period for width's mean and standard deviation."""
        return self._widthLookbackPeriod.Value

    @WidthLookbackPeriod.setter
    def WidthLookbackPeriod(self, value):
        self._widthLookbackPeriod.Value = value

    @property
    def WidthDeviationMultiplier(self):
        """Multiplier for width's standard deviation to determine entry threshold."""
        return self._widthDeviationMultiplier.Value

    @WidthDeviationMultiplier.setter
    def WidthDeviationMultiplier(self, value):
        self._widthDeviationMultiplier.Value = value

    @property
    def AtrPeriod(self):
        """Period for ATR calculation."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def AtrMultiplier(self):
        """Multiplier for ATR to determine stop-loss distance."""
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def CandleType(self):
        """Type of candles to use in the strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Returns securities and data types the strategy works with."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(bollinger_width_mean_reversion_strategy, self).OnStarted(time)

        self._lastWidthAvg = 0.0
        self._lastWidthStdDev = 0.0

        # Initialize indicators
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.BollingerLength
        self._bollinger.Width = self.BollingerDeviation

        self._widthAvg = SimpleMovingAverage()
        self._widthAvg.Length = self.WidthLookbackPeriod
        self._widthStdDev = StandardDeviation()
        self._widthStdDev.Length = self.WidthLookbackPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._bollinger, self._atr, self.ProcessBollinger).Start()

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def ProcessBollinger(self, candle, bollinger_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Process ATR
        lastAtr = float(atr_value)

        bollingerTyped = bollinger_value

        # Calculate Bollinger width
        lastWidth = float(bollingerTyped.UpBand) - float(bollingerTyped.LowBand)

        # Calculate width's average and standard deviation
        widthAvg = process_float(self._widthAvg, lastWidth, candle.ServerTime, candle.State == CandleStates.Finished)
        widthStdDev = process_float(self._widthStdDev, lastWidth, candle.ServerTime, candle.State == CandleStates.Finished)

        if widthAvg.IsFinal and widthStdDev.IsFinal:
            self._lastWidthAvg = float(widthAvg)
            self._lastWidthStdDev = float(widthStdDev)

            # Check if strategy is ready to trade
            if not self.IsFormedAndOnlineAndAllowTrading():
                return

            # Calculate thresholds
            lowerThreshold = self._lastWidthAvg - self.WidthDeviationMultiplier * self._lastWidthStdDev
            upperThreshold = self._lastWidthAvg + self.WidthDeviationMultiplier * self._lastWidthStdDev

            # Trading logic
            if lastWidth < lowerThreshold and self.Position <= 0:
                # Width is compressed - Long signal (expecting expansion)
                self.BuyMarket(self.Volume + Math.Abs(self.Position))

                # Set ATR-based stop loss
                if lastAtr > 0:
                    stopPrice = candle.ClosePrice - self.AtrMultiplier * lastAtr
                    self.PlaceStopLoss(stopPrice)
            elif lastWidth > upperThreshold and self.Position >= 0:
                # Width is expanded - Short signal (expecting contraction)
                self.SellMarket(self.Volume + Math.Abs(self.Position))

                # Set ATR-based stop loss
                if lastAtr > 0:
                    stopPrice = candle.ClosePrice + self.AtrMultiplier * lastAtr
                    self.PlaceStopLoss(stopPrice)
            # Exit logic
            elif lastWidth > self._lastWidthAvg and self.Position > 0:
                # Width returned to average - Exit long position
                self.SellMarket(self.Position)
            elif lastWidth < self._lastWidthAvg and self.Position < 0:
                # Width returned to average - Exit short position
                self.BuyMarket(Math.Abs(self.Position))

    def PlaceStopLoss(self, price):
        # Place a stop order as stop loss
        stopOrder = self.CreateOrder(
            self.Position > 0 and Sides.Sell or Sides.Buy,
            price,
            Math.Abs(self.Position)
        )
        self.RegisterOrder(stopOrder)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_width_mean_reversion_strategy()

