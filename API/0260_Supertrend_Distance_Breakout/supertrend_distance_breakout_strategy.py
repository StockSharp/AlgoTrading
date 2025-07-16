import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class supertrend_distance_breakout_strategy(Strategy):
    """
    Strategy that enters positions when the distance between price and Supertrend
    exceeds the average distance plus a multiple of standard deviation
    """

    def __init__(self):
        """Constructor"""
        super(supertrend_distance_breakout_strategy, self).__init__()

        self._supertrendPeriod = self.Param("SupertrendPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "Period for Supertrend indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._supertrendMultiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for statistical calculations", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._supertrend = None
        self._atr = None
        self._avgDistanceLong = 0
        self._stdDevDistanceLong = 0
        self._avgDistanceShort = 0
        self._stdDevDistanceShort = 0
        self._lastLongDistance = 0
        self._lastShortDistance = 0
        self._samplesCount = 0

    @property
    def SupertrendPeriod(self):
        """Supertrend period"""
        return self._supertrendPeriod.Value

    @SupertrendPeriod.setter
    def SupertrendPeriod(self, value):
        self._supertrendPeriod.Value = value

    @property
    def SupertrendMultiplier(self):
        """Supertrend multiplier"""
        return self._supertrendMultiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrendMultiplier.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for distance statistics calculation"""
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def DeviationMultiplier(self):
        """Standard deviation multiplier for breakout detection"""
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type"""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(supertrend_distance_breakout_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.SupertrendPeriod
        self._supertrend = SuperTrend()
        self._supertrend.Length = self.SupertrendPeriod
        self._supertrend.Multiplier = self.SupertrendMultiplier

        self._avgDistanceLong = 0
        self._stdDevDistanceLong = 0
        self._avgDistanceShort = 0
        self._stdDevDistanceShort = 0
        self._lastLongDistance = 0
        self._lastShortDistance = 0
        self._samplesCount = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._supertrend, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawOwnTrades(area)

        # Set up position protection with dynamic stop-loss
        self.StartProtection(
            takeProfit=None,  # We'll handle exits via our strategy logic
            stopLoss=Unit(2, UnitTypes.Percent)  # 2% stop-loss
        )

    def ProcessCandle(self, candle, supertrendPrice):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate distances
        longDistance = 0
        shortDistance = 0

        # If price is above Supertrend, calculate distance for long case
        if candle.ClosePrice > supertrendPrice:
            longDistance = candle.ClosePrice - supertrendPrice
        # If price is below Supertrend, calculate distance for short case
        elif candle.ClosePrice < supertrendPrice:
            shortDistance = supertrendPrice - candle.ClosePrice

        # Update statistics
        self.UpdateDistanceStatistics(longDistance, shortDistance)

        # Trading logic
        if self._samplesCount >= self.LookbackPeriod:
            # Long signal: distance exceeds average + k*stddev and we don't have a long position
            if longDistance > 0 and \
               longDistance > self._avgDistanceLong + self.DeviationMultiplier * self._stdDevDistanceLong and \
               self.Position <= 0:
                # Cancel existing orders
                self.CancelActiveOrders()
                # Enter long position
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
                self.LogInfo(f"Long signal: Distance {longDistance} > Avg {self._avgDistanceLong} + {self.DeviationMultiplier}*StdDev {self._stdDevDistanceLong}")
            # Short signal: distance exceeds average + k*stddev and we don't have a short position
            elif shortDistance > 0 and \
                 shortDistance > self._avgDistanceShort + self.DeviationMultiplier * self._stdDevDistanceShort and \
                 self.Position >= 0:
                # Cancel existing orders
                self.CancelActiveOrders()
                # Enter short position
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)
                self.LogInfo(f"Short signal: Distance {shortDistance} > Avg {self._avgDistanceShort} + {self.DeviationMultiplier}*StdDev {self._stdDevDistanceShort}")

            # Exit conditions - when distance returns to average
            if self.Position > 0 and longDistance < self._avgDistanceLong:
                # Exit long position
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo(f"Exit long: Distance {longDistance} < Avg {self._avgDistanceLong}")
            elif self.Position < 0 and shortDistance < self._avgDistanceShort:
                # Exit short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo(f"Exit short: Distance {shortDistance} < Avg {self._avgDistanceShort}")

        # Store current distances for next update
        self._lastLongDistance = longDistance
        self._lastShortDistance = shortDistance

    def UpdateDistanceStatistics(self, longDistance, shortDistance):
        # Simple calculation of running average and standard deviation
        self._samplesCount += 1

        if self._samplesCount == 1:
            # Initialize with first values
            self._avgDistanceLong = longDistance
            self._avgDistanceShort = shortDistance
            self._stdDevDistanceLong = 0
            self._stdDevDistanceShort = 0
        else:
            # Update running average
            oldAvgLong = self._avgDistanceLong
            oldAvgShort = self._avgDistanceShort

            self._avgDistanceLong = oldAvgLong + (longDistance - oldAvgLong) / self._samplesCount
            self._avgDistanceShort = oldAvgShort + (shortDistance - oldAvgShort) / self._samplesCount

            # Update running standard deviation using Welford's algorithm
            if self._samplesCount > 1:
                self._stdDevDistanceLong = (1 - 1.0 / (self._samplesCount - 1)) * self._stdDevDistanceLong + \
                    self._samplesCount * ((self._avgDistanceLong - oldAvgLong) * (self._avgDistanceLong - oldAvgLong))

                self._stdDevDistanceShort = (1 - 1.0 / (self._samplesCount - 1)) * self._stdDevDistanceShort + \
                    self._samplesCount * ((self._avgDistanceShort - oldAvgShort) * (self._avgDistanceShort - oldAvgShort))

            # We only need last LookbackPeriod samples
            if self._samplesCount > self.LookbackPeriod:
                self._samplesCount = self.LookbackPeriod

        # Calculate square root for final standard deviation
        self._stdDevDistanceLong = Math.Sqrt(float(self._stdDevDistanceLong) / self._samplesCount)
        self._stdDevDistanceShort = Math.Sqrt(float(self._stdDevDistanceShort) / self._samplesCount)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_distance_breakout_strategy()
