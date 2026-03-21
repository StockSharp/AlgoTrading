import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class ichimoku_cloud_width_mean_reversion_strategy(Strategy):
    """
    Ichimoku cloud width mean reversion strategy.
    Trades contractions and expansions of the Ichimoku cloud width around its recent average.
    """

    def __init__(self):
        super(ichimoku_cloud_width_mean_reversion_strategy, self).__init__()

        self._tenkanPeriod = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._kijunPeriod = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 40, 2)

        self._senkouSpanBPeriod = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 80, 4)

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Lookback period for cloud width statistics", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviationMultiplier = self.Param("DeviationMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldownBars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._ichimoku = None
        self._cloudWidthHistory = []
        self._currentIndex = 0
        self._filledCount = 0
        self._cooldown = 0

    @property
    def TenkanPeriod(self):
        return self._tenkanPeriod.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkanPeriod.Value = value

    @property
    def KijunPeriod(self):
        return self._kijunPeriod.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijunPeriod.Value = value

    @property
    def SenkouSpanBPeriod(self):
        return self._senkouSpanBPeriod.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkouSpanBPeriod.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def DeviationMultiplier(self):
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CooldownBars(self):
        return self._cooldownBars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldownBars.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(ichimoku_cloud_width_mean_reversion_strategy, self).OnReseted()
        self._ichimoku = None
        self._currentIndex = 0
        self._filledCount = 0
        self._cooldown = 0
        self._cloudWidthHistory = [0.0] * self.LookbackPeriod

    def OnStarted(self, time):
        super(ichimoku_cloud_width_mean_reversion_strategy, self).OnStarted(time)

        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.TenkanPeriod
        self._ichimoku.Kijun.Length = self.KijunPeriod
        self._ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        self._cloudWidthHistory = [0.0] * self.LookbackPeriod
        self._currentIndex = 0
        self._filledCount = 0
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._ichimoku, self.ProcessIchimoku).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessIchimoku(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ichimoku.IsFormed:
            return

        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        if senkou_a is None or senkou_b is None:
            return

        senkou_a = float(senkou_a)
        senkou_b = float(senkou_b)
        cloud_width = abs(senkou_a - senkou_b)

        # Store in circular buffer
        self._cloudWidthHistory[self._currentIndex] = cloud_width
        self._currentIndex = (self._currentIndex + 1) % self.LookbackPeriod

        if self._filledCount < self.LookbackPeriod:
            self._filledCount += 1

        if self._filledCount < self.LookbackPeriod:
            return

        # Calculate average
        avg_width = sum(self._cloudWidthHistory) / self.LookbackPeriod

        # Calculate standard deviation
        sum_sq = 0.0
        for w in self._cloudWidthHistory:
            diff = w - avg_width
            sum_sq += diff * diff
        std_width = Math.Sqrt(sum_sq / self.LookbackPeriod)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        narrow_threshold = avg_width - std_width * self.DeviationMultiplier
        wide_threshold = avg_width + std_width * self.DeviationMultiplier
        upper_cloud = max(senkou_a, senkou_b)
        lower_cloud = min(senkou_a, senkou_b)
        close = float(candle.ClosePrice)
        price_above_cloud = close > upper_cloud
        price_below_cloud = close < lower_cloud

        if self.Position == 0:
            if cloud_width < narrow_threshold:
                if price_above_cloud:
                    self.BuyMarket()
                    self._cooldown = self.CooldownBars
                elif price_below_cloud:
                    self.SellMarket()
                    self._cooldown = self.CooldownBars
            elif cloud_width > wide_threshold:
                if price_below_cloud:
                    self.SellMarket()
                    self._cooldown = self.CooldownBars
                elif price_above_cloud:
                    self.BuyMarket()
                    self._cooldown = self.CooldownBars
        elif self.Position > 0 and cloud_width >= avg_width:
            self.SellMarket(abs(self.Position))
            self._cooldown = self.CooldownBars
        elif self.Position < 0 and cloud_width <= avg_width:
            self.BuyMarket(abs(self.Position))
            self._cooldown = self.CooldownBars

    def CreateClone(self):
        return ichimoku_cloud_width_mean_reversion_strategy()
