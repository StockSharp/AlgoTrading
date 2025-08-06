import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("Ecng.Common")

from System import TimeSpan, Math
from Ecng.Common import RandomGen
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class supertrend_put_call_ratio_strategy(Strategy):
    """
    Supertrend with Put/Call Ratio strategy.
    Entry condition:
    Long: Price > Supertrend && PCR < Avg(PCR, N) - k*StdDev(PCR, N)
    Short: Price < Supertrend && PCR > Avg(PCR, N) + k*StdDev(PCR, N)
    Exit condition:
    Long: Price < Supertrend
    Short: Price > Supertrend
    """

    def __init__(self):
        super(supertrend_put_call_ratio_strategy, self).__init__()

        self._period = self.Param("Period", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "Supertrend ATR period", "Supertrend Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 3)

        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(2.0, 4.0, 0.5)

        self._pcrPeriod = self.Param("PCRPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("PCR Period", "Put/Call Ratio averaging period", "PCR Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._pcrMultiplier = self.Param("PCRMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("PCR Std Dev Multiplier", "Multiplier for PCR standard deviation", "PCR Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._pcrHistory = []
        self._pcrAverage = 0.0
        self._pcrStdDev = 0.0
        self._isLong = False
        self._isShort = False
        # Simulated PCR value (in real implementation this would come from market data)
        self._currentPcr = 0.0

        self._supertrend = None

    @property
    def Period(self):
        """Supertrend period."""
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def Multiplier(self):
        """Supertrend multiplier."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def PCRPeriod(self):
        """PCR averaging period."""
        return self._pcrPeriod.Value

    @PCRPeriod.setter
    def PCRPeriod(self, value):
        self._pcrPeriod.Value = value

    @property
    def PCRMultiplier(self):
        """PCR standard deviation multiplier for thresholds."""
        return self._pcrMultiplier.Value

    @PCRMultiplier.setter
    def PCRMultiplier(self, value):
        self._pcrMultiplier.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Returns securities for strategy."""
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(supertrend_put_call_ratio_strategy, self).OnReseted()
        self._pcrHistory.clear()
        self._isLong = False
        self._isShort = False
        self._currentPcr = 0.0
        self._pcrAverage = 0.0
        self._pcrStdDev = 0.0

    def OnStarted(self, time):
        super(supertrend_put_call_ratio_strategy, self).OnStarted(time)

        # Create Supertrend indicator
        self._supertrend = SuperTrend()
        self._supertrend.Length = self.Period
        self._supertrend.Multiplier = self.Multiplier

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._supertrend, self.ProcessCandle).Start()

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, supertrend_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update PCR value (in a real system, this would come from market data)
        self.UpdatePCR(candle)

        # Calculate PCR thresholds based on historical data
        bullishPcrThreshold = self._pcrAverage - self.PCRMultiplier * self._pcrStdDev
        bearishPcrThreshold = self._pcrAverage + self.PCRMultiplier * self._pcrStdDev

        price = float(candle.ClosePrice)
        priceAboveSupertrend = price > supertrend_value
        priceBelowSupertrend = price < supertrend_value

        # Trading logic

        # Entry conditions

        # Long entry: Price > Supertrend && PCR < bullish threshold (bullish PCR)
        if (priceAboveSupertrend and self._currentPcr < bullishPcrThreshold
                and not self._isLong and self.Position <= 0):
            self.LogInfo(f"Long signal: Price {price} > Supertrend {supertrend_value}, PCR {self._currentPcr} < Threshold {bullishPcrThreshold}")
            self.BuyMarket(self.Volume)
            self._isLong = True
            self._isShort = False
        # Short entry: Price < Supertrend && PCR > bearish threshold (bearish PCR)
        elif (priceBelowSupertrend and self._currentPcr > bearishPcrThreshold
                and not self._isShort and self.Position >= 0):
            self.LogInfo(f"Short signal: Price {price} < Supertrend {supertrend_value}, PCR {self._currentPcr} > Threshold {bearishPcrThreshold}")
            self.SellMarket(self.Volume)
            self._isShort = True
            self._isLong = False

        # Exit conditions (based only on Supertrend, not PCR)

        # Exit long: Price < Supertrend
        if self._isLong and priceBelowSupertrend and self.Position > 0:
            self.LogInfo(f"Exit long: Price {price} < Supertrend {supertrend_value}")
            self.SellMarket(Math.Abs(self.Position))
            self._isLong = False
        # Exit short: Price > Supertrend
        elif self._isShort and priceAboveSupertrend and self.Position < 0:
            self.LogInfo(f"Exit short: Price {price} > Supertrend {supertrend_value}")
            self.BuyMarket(Math.Abs(self.Position))
            self._isShort = False

    def UpdatePCR(self, candle):
        """Update Put/Call Ratio value. In a real implementation, this would fetch data from market."""
        # Base PCR on candle pattern with some randomness
        if candle.ClosePrice > candle.OpenPrice:
            # Bullish candle tends to have lower PCR
            pcr = 0.7 + RandomGen.GetDouble() * 0.3
        else:
            # Bearish candle tends to have higher PCR
            pcr = 1.0 + RandomGen.GetDouble() * 0.5

        self._currentPcr = pcr

        # Add to history
        self._pcrHistory.append(self._currentPcr)
        if len(self._pcrHistory) > self.PCRPeriod:
            self._pcrHistory.pop(0)

        # Calculate average
        total = 0.0
        for value in self._pcrHistory:
            total += value

        self._pcrAverage = total / len(self._pcrHistory) if len(self._pcrHistory) > 0 else 1.0  # Default to neutral (1.0)

        # Calculate standard deviation
        if len(self._pcrHistory) > 1:
            sum_squared_diffs = 0.0
            for value in self._pcrHistory:
                diff = value - self._pcrAverage
                sum_squared_diffs += diff * diff
            self._pcrStdDev = Math.Sqrt(sum_squared_diffs / (len(self._pcrHistory) - 1))
        else:
            self._pcrStdDev = 0.1  # Default value until we have enough data

        self.LogInfo(f"PCR: {self._currentPcr}, Avg: {self._pcrAverage}, StdDev: {self._pcrStdDev}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_put_call_ratio_strategy()
