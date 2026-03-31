import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class ichimoku_width_breakout_strategy(Strategy):
    """
    Strategy that trades on Ichimoku Cloud width breakouts.
    When Ichimoku Cloud width increases significantly above its average,
    it enters position in the direction determined by price location relative to the cloud.
    """

    def __init__(self):
        super(ichimoku_width_breakout_strategy, self).__init__()

        self._tenkanPeriod = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Period for Tenkan-sen line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._kijunPeriod = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Period for Kijun-sen line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 40, 2)

        self._senkouSpanBPeriod = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 80, 4)

        self._avgPeriod = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for cloud width average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._multiplier = self.Param("Multiplier", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLoss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._ichimoku = None
        self._widthAverage = None

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
    def AvgPeriod(self):
        return self._avgPeriod.Value

    @AvgPeriod.setter
    def AvgPeriod(self, value):
        self._avgPeriod.Value = value

    @property
    def Multiplier(self):
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLoss(self):
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(ichimoku_width_breakout_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(ichimoku_width_breakout_strategy, self).OnStarted2(time)

        # Create indicators
        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.TenkanPeriod
        self._ichimoku.Kijun.Length = self.KijunPeriod
        self._ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        self._widthAverage = SimpleMovingAverage()
        self._widthAverage.Length = self.AvgPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind Ichimoku
        subscription.BindEx(self._ichimoku, self.ProcessIchimoku).Start()

        # Enable stop loss protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent)
        )

        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

    def ProcessIchimoku(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not ichimoku_value.IsFinal:
            return

        # Get Ichimoku values
        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB

        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return

        tenkan = float(tenkan)
        kijun = float(kijun)
        senkou_a = float(senkou_a)
        senkou_b = float(senkou_b)

        # Calculate Cloud width
        width = abs(senkou_a - senkou_b)

        # Process width through average
        avg_result = process_float(self._widthAverage, width, candle.ServerTime, True)
        avg_width = float(avg_result)

        # Skip if indicators are not formed yet
        if not self._ichimoku.IsFormed or not self._widthAverage.IsFormed:
            return

        # Cloud width breakout detection
        if width > avg_width * self.Multiplier and self.Position == 0:
            # Determine trade direction based on price relative to cloud
            upper_cloud = max(senkou_a, senkou_b)
            lower_cloud = min(senkou_a, senkou_b)

            close_price = float(candle.ClosePrice)
            bullish = close_price > upper_cloud
            bearish = close_price < lower_cloud

            if bullish:
                self.BuyMarket()
            elif bearish:
                self.SellMarket()

    def CreateClone(self):
        return ichimoku_width_breakout_strategy()
