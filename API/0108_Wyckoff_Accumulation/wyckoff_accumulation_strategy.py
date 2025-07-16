import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class WyckoffPhase:
    """Enumeration representing Wyckoff phases."""
    NonePhase = 0
    PhaseA = 1  # Selling climax, automatic rally, secondary test
    PhaseB = 2  # Accumulation, base building
    PhaseC = 3  # Spring, test of support
    PhaseD = 4  # Sign of strength, successful test
    PhaseE = 5  # Markup, price rise


class wyckoff_accumulation_strategy(Strategy):
    """Strategy based on Wyckoff Accumulation pattern, which identifies a period of institutional accumulation that leads to an upward price movement."""

    def __init__(self):
        super(wyckoff_accumulation_strategy, self).__init__()

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")

        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Trend") \
            .SetRange(10, 50)

        self._volumeAvgPeriod = self.Param("VolumeAvgPeriod", 20) \
            .SetDisplay("Volume Avg Period", "Period for volume average calculation", "Volume") \
            .SetRange(10, 50)

        self._highestPeriod = self.Param("HighestPeriod", 20) \
            .SetDisplay("High/Low Period", "Period for high/low calculation", "Range") \
            .SetRange(10, 50)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection") \
            .SetRange(1.0, 5.0)

        # Internal state variables
        self._ma = None
        self._volumeAvg = None
        self._highest = None
        self._lowest = None
        self._currentPhase = WyckoffPhase.NonePhase
        self._lastRangeHigh = 0
        self._lastRangeLow = 0
        self._sidewaysCount = 0
        self._springLow = 0
        self._positionOpened = False

    @property
    def CandleType(self):
        """Candle type and timeframe for the strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def MaPeriod(self):
        """Period for moving average calculation."""
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def VolumeAvgPeriod(self):
        """Period for volume average calculation."""
        return self._volumeAvgPeriod.Value

    @VolumeAvgPeriod.setter
    def VolumeAvgPeriod(self, value):
        self._volumeAvgPeriod.Value = value

    @property
    def HighestPeriod(self):
        """Period for highest/lowest calculation."""
        return self._highestPeriod.Value

    @HighestPeriod.setter
    def HighestPeriod(self, value):
        self._highestPeriod.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage from entry price."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(wyckoff_accumulation_strategy, self).OnReseted()
        self._currentPhase = WyckoffPhase.NonePhase
        self._lastRangeHigh = 0
        self._lastRangeLow = 0
        self._sidewaysCount = 0
        self._springLow = 0
        self._positionOpened = False

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up indicators, subscriptions, and charting."""
        super(wyckoff_accumulation_strategy, self).OnStarted(time)

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MaPeriod
        self._volumeAvg = SimpleMovingAverage()
        self._volumeAvg.Length = self.VolumeAvgPeriod
        self._highest = Highest()
        self._highest.Length = self.HighestPeriod
        self._lowest = Lowest()
        self._lowest.Length = self.HighestPeriod

        self._currentPhase = WyckoffPhase.NonePhase
        self._sidewaysCount = 0
        self._positionOpened = False
        self._lastRangeHigh = 0
        self._lastRangeLow = 0
        self._springLow = 0

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators and processor
        subscription.Bind(self._ma, self._volumeAvg, self._highest, self._lowest, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, volume_avg, highest, lowest):
        """Processes each finished candle and implements Wyckoff Accumulation logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update range values
        self._lastRangeHigh = highest
        self._lastRangeLow = lowest

        # Determine candle characteristics
        is_bullish = candle.ClosePrice > candle.OpenPrice
        high_volume = candle.TotalVolume > volume_avg * 1.5
        price_above_ma = candle.ClosePrice > ma_value
        price_below_ma = candle.ClosePrice < ma_value
        is_narrow_range = (candle.HighPrice - candle.LowPrice) < (highest - lowest) * 0.3

        # State machine for Wyckoff Accumulation phases
        if self._currentPhase == WyckoffPhase.NonePhase:
            # Look for Phase A: Selling climax (high volume, wide range down bar)
            if not is_bullish and high_volume and candle.ClosePrice < lowest:
                self._currentPhase = WyckoffPhase.PhaseA
                self.LogInfo("Wyckoff Phase A detected: Selling climax at {0}".format(candle.ClosePrice))
        elif self._currentPhase == WyckoffPhase.PhaseA:
            # Look for automatic rally (rebound from selling climax)
            if is_bullish and candle.ClosePrice > ma_value:
                self._currentPhase = WyckoffPhase.PhaseB
                self.LogInfo("Entering Wyckoff Phase B: Automatic rally at {0}".format(candle.ClosePrice))
                self._sidewaysCount = 0
        elif self._currentPhase == WyckoffPhase.PhaseB:
            # Phase B is characterized by sideways movement (accumulation)
            if is_narrow_range and candle.ClosePrice > self._lastRangeLow and candle.ClosePrice < self._lastRangeHigh:
                self._sidewaysCount += 1
                # After sufficient sideways movement, look for Phase C
                if self._sidewaysCount >= 5:
                    self._currentPhase = WyckoffPhase.PhaseC
                    self.LogInfo("Entering Wyckoff Phase C: Accumulation complete after {0} sideways candles".format(self._sidewaysCount))
            else:
                self._sidewaysCount = 0  # Reset if we don't see sideways movement
        elif self._currentPhase == WyckoffPhase.PhaseC:
            # Phase C includes a spring (price briefly goes below support)
            if candle.LowPrice < self._lastRangeLow and candle.ClosePrice > self._lastRangeLow:
                self._springLow = candle.LowPrice
                self._currentPhase = WyckoffPhase.PhaseD
                self.LogInfo("Entering Wyckoff Phase D: Spring detected at {0}".format(self._springLow))
        elif self._currentPhase == WyckoffPhase.PhaseD:
            # Phase D shows sign of strength (strong move up with volume)
            if is_bullish and high_volume and price_above_ma:
                self._currentPhase = WyckoffPhase.PhaseE
                self.LogInfo("Entering Wyckoff Phase E: Sign of strength detected at {0}".format(candle.ClosePrice))
        elif self._currentPhase == WyckoffPhase.PhaseE:
            # Phase E is the markup phase where we enter our position
            if is_bullish and price_above_ma and not self._positionOpened:
                # Enter long position
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self._positionOpened = True
                self.LogInfo("Wyckoff Accumulation complete. Long entry at {0}".format(candle.ClosePrice))

        # Exit conditions
        if self._positionOpened and self.Position > 0:
            # Exit when price exceeds previous high (target achieved)
            if candle.HighPrice > self._lastRangeHigh:
                self.SellMarket(Math.Abs(self.Position))
                self._positionOpened = False
                self._currentPhase = WyckoffPhase.NonePhase  # Reset the pattern detection
                self.LogInfo("Exit signal: Price broke above range high ({0}). Closed long position at {1}".format(self._lastRangeHigh, candle.ClosePrice))
            # Exit also if price falls back below MA (failed pattern)
            elif price_below_ma:
                self.SellMarket(Math.Abs(self.Position))
                self._positionOpened = False
                self._currentPhase = WyckoffPhase.NonePhase  # Reset the pattern detection
                self.LogInfo("Exit signal: Price fell below MA. Pattern may have failed. Closed long position at {0}".format(candle.ClosePrice))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return wyckoff_accumulation_strategy()

