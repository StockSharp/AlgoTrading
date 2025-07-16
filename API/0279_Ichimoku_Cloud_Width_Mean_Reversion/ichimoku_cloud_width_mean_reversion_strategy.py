import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, SimpleMovingAverage, StandardDeviation, IchimokuValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class ichimoku_cloud_width_mean_reversion_strategy(Strategy):
    """
    Ichimoku Cloud Width Mean Reversion Strategy.
    This strategy trades based on the mean reversion of the Ichimoku Cloud width.
    """

    def __init__(self):
        super(ichimoku_cloud_width_mean_reversion_strategy, self).__init__()

        # Constructor.
        self._tenkanPeriod = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan Period", "Tenkan-sen (Conversion Line) period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._kijunPeriod = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun Period", "Kijun-sen (Base Line) period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 40, 2)

        self._senkouSpanBPeriod = self.Param("SenkouSpanBPeriod", 52) \
            .SetDisplay("Senkou Span B Period", "Senkou Span B (Leading Span B) period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 80, 4)

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Lookback period for calculating the average and standard deviation of cloud width", "Mean Reversion") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Mean Reversion") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._ichimoku = None
        self._cloudWidthAverage = None
        self._cloudWidthStdDev = None

        self._currentCloudWidth = 0.0
        self._prevCloudWidth = 0.0
        self._prevCloudWidthAverage = 0.0
        self._prevCloudWidthStdDev = 0.0

    @property
    def TenkanPeriod(self):
        """Tenkan-sen (Conversion Line) period."""
        return self._tenkanPeriod.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkanPeriod.Value = value

    @property
    def KijunPeriod(self):
        """Kijun-sen (Base Line) period."""
        return self._kijunPeriod.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijunPeriod.Value = value

    @property
    def SenkouSpanBPeriod(self):
        """Senkou Span B (Leading Span B) period."""
        return self._senkouSpanBPeriod.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkouSpanBPeriod.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for calculating the average and standard deviation of cloud width."""
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def DeviationMultiplier(self):
        """Deviation multiplier for mean reversion detection."""
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Returns securities for strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(ichimoku_cloud_width_mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.TenkanPeriod
        self._ichimoku.Kijun.Length = self.KijunPeriod
        self._ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        self._cloudWidthAverage = SimpleMovingAverage()
        self._cloudWidthAverage.Length = self.LookbackPeriod
        self._cloudWidthStdDev = StandardDeviation()
        self._cloudWidthStdDev.Length = self.LookbackPeriod

        # Reset stored values
        self._currentCloudWidth = 0
        self._prevCloudWidth = 0
        self._prevCloudWidthAverage = 0
        self._prevCloudWidthStdDev = 0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._ichimoku, self.ProcessIchimoku).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

        # Start position protection using Kijun-sen
        self.StartProtection(
            takeProfit=None,
            stopLoss=None
        )
    def ProcessIchimoku(self, candle, ichimokuValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from the Ichimoku indicator
        ichimokuTyped = ichimokuValue

        tenkan = ichimokuTyped.Tenkan if isinstance(ichimokuTyped.Tenkan, float) or isinstance(ichimokuTyped.Tenkan, int) else None
        if tenkan is None:
            return
        kijun = ichimokuTyped.Kijun if isinstance(ichimokuTyped.Kijun, float) or isinstance(ichimokuTyped.Kijun, int) else None
        if kijun is None:
            return
        senkouA = ichimokuTyped.SenkouA if isinstance(ichimokuTyped.SenkouA, float) or isinstance(ichimokuTyped.SenkouA, int) else None
        if senkouA is None:
            return
        senkouB = ichimokuTyped.SenkouB if isinstance(ichimokuTyped.SenkouB, float) or isinstance(ichimokuTyped.SenkouB, int) else None
        if senkouB is None:
            return

        # Calculate cloud width (absolute difference between Senkou Span A and B)
        self._currentCloudWidth = Math.Abs(senkouA - senkouB)

        # Calculate average and standard deviation of cloud width
        cloudWidthAverage = to_float(process_float(self._cloudWidthAverage, self._currentCloudWidth, candle.ServerTime, candle.State == CandleStates.Finished))
        cloudWidthStdDev = to_float(process_float(self._cloudWidthStdDev, self._currentCloudWidth, candle.ServerTime, candle.State == CandleStates.Finished))

        # Skip the first value
        if self._prevCloudWidth == 0:
            self._prevCloudWidth = self._currentCloudWidth
            self._prevCloudWidthAverage = cloudWidthAverage
            self._prevCloudWidthStdDev = cloudWidthStdDev
            return

        # Calculate thresholds
        narrowThreshold = self._prevCloudWidthAverage - self._prevCloudWidthStdDev * self.DeviationMultiplier
        wideThreshold = self._prevCloudWidthAverage + self._prevCloudWidthStdDev * self.DeviationMultiplier

        # Trading logic:
        # When cloud is narrowing (compression)
        if (self._currentCloudWidth < narrowThreshold and self._prevCloudWidth >= narrowThreshold and self.Position == 0):
            # Determine direction based on price position relative to cloud
            if candle.ClosePrice > max(senkouA, senkouB):
                self.BuyMarket(self.Volume)
                self.LogInfo(f"Ichimoku cloud compression (bullish): {self._currentCloudWidth} < {narrowThreshold}. Buying at {candle.ClosePrice}")
            elif candle.ClosePrice < min(senkouA, senkouB):
                self.SellMarket(self.Volume)
                self.LogInfo(f"Ichimoku cloud compression (bearish): {self._currentCloudWidth} < {narrowThreshold}. Selling at {candle.ClosePrice}")
        # When cloud is widening (expansion)
        elif (self._currentCloudWidth > wideThreshold and self._prevCloudWidth <= wideThreshold and self.Position == 0):
            # Determine direction based on price position relative to cloud
            if candle.ClosePrice < min(senkouA, senkouB):
                self.SellMarket(self.Volume)
                self.LogInfo(f"Ichimoku cloud expansion (bearish): {self._currentCloudWidth} > {wideThreshold}. Selling at {candle.ClosePrice}")
            elif candle.ClosePrice > max(senkouA, senkouB):
                self.BuyMarket(self.Volume)
                self.LogInfo(f"Ichimoku cloud expansion (bullish): {self._currentCloudWidth} > {wideThreshold}. Buying at {candle.ClosePrice}")
        # Exit positions when width returns to average
        elif (self._currentCloudWidth >= 0.9 * self._prevCloudWidthAverage and
              self._currentCloudWidth <= 1.1 * self._prevCloudWidthAverage and
              (self._prevCloudWidth < 0.9 * self._prevCloudWidthAverage or self._prevCloudWidth > 1.1 * self._prevCloudWidthAverage) and
              self.Position != 0):
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
                self.LogInfo(f"Ichimoku cloud width returned to average: {self._currentCloudWidth} ≈ {self._prevCloudWidthAverage}. Closing long position at {candle.ClosePrice}")
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
                self.LogInfo(f"Ichimoku cloud width returned to average: {self._currentCloudWidth} ≈ {self._prevCloudWidthAverage}. Closing short position at {candle.ClosePrice}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_cloud_width_mean_reversion_strategy()
