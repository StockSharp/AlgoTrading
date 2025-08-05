import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class keltner_width_mean_reversion_strategy(Strategy):
    """
    Keltner Width Mean Reversion Strategy.
    Strategy trades based on mean reversion of Keltner Channel width.

    """

    def __init__(self):
        super(keltner_width_mean_reversion_strategy, self).__init__()

        # Parameters
        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._keltnerMultiplier = self.Param("KeltnerMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Keltner Multiplier", "Multiplier for Keltner Channel bands", "Indicators") \
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

        self._atrStopMultiplier = self.Param("AtrStopMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Stop Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use in the strategy", "General")

        # Internal state
        self._ema = None
        self._atr = None
        self._lastEma = 0.0
        self._lastAtr = 0.0
        self._lastChannelWidth = 0.0
        self._widthAvg = None
        self._widthStdDev = None
        self._lastWidthAvg = 0.0
        self._lastWidthStdDev = 0.0

    @property
    def EmaPeriod(self):
        """Period for EMA calculation."""
        return self._emaPeriod.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._emaPeriod.Value = value

    @property
    def AtrPeriod(self):
        """Period for ATR calculation."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def KeltnerMultiplier(self):
        """Multiplier for Keltner Channel bands."""
        return self._keltnerMultiplier.Value

    @KeltnerMultiplier.setter
    def KeltnerMultiplier(self, value):
        self._keltnerMultiplier.Value = value

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
    def AtrStopMultiplier(self):
        """Multiplier for ATR to determine stop-loss distance."""
        return self._atrStopMultiplier.Value

    @AtrStopMultiplier.setter
    def AtrStopMultiplier(self, value):
        self._atrStopMultiplier.Value = value

    @property
    def CandleType(self):
        """Type of candles to use in the strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """Return the security and candle type this strategy works with."""
        return [(self.Security, self.CandleType)]


    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(keltner_width_mean_reversion_strategy, self).OnReseted()
        self._lastEma = 0.0
        self._lastAtr = 0.0
        self._lastChannelWidth = 0.0
        self._lastWidthAvg = 0.0
        self._lastWidthStdDev = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(keltner_width_mean_reversion_strategy, self).OnStarted(time)


        # Initialize indicators
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self._widthAvg = SimpleMovingAverage()
        self._widthAvg.Length = self.WidthLookbackPeriod
        self._widthStdDev = StandardDeviation()
        self._widthStdDev.Length = self.WidthLookbackPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Setup candle processing
        subscription.Bind(self.ProcessCandle).Start()

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        """Process candle and update indicators."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Process EMA
        emaValue = process_candle(self._ema, candle)
        if emaValue.IsFinal:
            self._lastEma = float(emaValue)

        # Process ATR
        atrValue = process_candle(self._atr, candle)
        if atrValue.IsFinal:
            self._lastAtr = float(atrValue)

        # Calculate Keltner Channel
        if self._lastEma > 0 and self._lastAtr > 0:
            # Calculate upper and lower bands
            upperBand = self._lastEma + self.KeltnerMultiplier * self._lastAtr
            lowerBand = self._lastEma - self.KeltnerMultiplier * self._lastAtr

            # Calculate channel width
            channelWidth = upperBand - lowerBand
            self._lastChannelWidth = channelWidth

            # Process width's average and standard deviation
            widthAvgValue = process_float(self._widthAvg, channelWidth, candle.ServerTime, candle.State == CandleStates.Finished)
            widthStdDevValue = process_float(self._widthStdDev, channelWidth, candle.ServerTime, candle.State == CandleStates.Finished)

            if widthAvgValue.IsFinal and widthStdDevValue.IsFinal:
                self._lastWidthAvg = float(widthAvgValue)
                self._lastWidthStdDev = float(widthStdDevValue)

                # Check if strategy is ready to trade
                if not self.IsFormedAndOnlineAndAllowTrading():
                    return

                # Calculate thresholds
                lowerThreshold = self._lastWidthAvg - self.WidthDeviationMultiplier * self._lastWidthStdDev
                upperThreshold = self._lastWidthAvg + self.WidthDeviationMultiplier * self._lastWidthStdDev

                # Trading logic
                if self._lastChannelWidth < lowerThreshold and self.Position <= 0:
                    # Channel width is compressed - Long signal (expecting expansion)
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                elif self._lastChannelWidth > upperThreshold and self.Position >= 0:
                    # Channel width is expanded - Short signal (expecting contraction)
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                # Exit logic
                elif self._lastChannelWidth > self._lastWidthAvg and self.Position > 0:
                    # Width returned to average - Exit long position
                    self.SellMarket(self.Position)
                elif self._lastChannelWidth < self._lastWidthAvg and self.Position < 0:
                    # Width returned to average - Exit short position
                    self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return keltner_width_mean_reversion_strategy()
