import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, AverageTrueRange, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class ichimoku_volatility_contraction_strategy(Strategy):
    """
    Ichimoku with Volatility Contraction strategy.
    Enters positions when Ichimoku signals a trend and volatility is contracting.
    """

    def __init__(self):
        super(ichimoku_volatility_contraction_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Period for Tenkan-sen (Conversion Line)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 11, 1)

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Period for Kijun-sen (Base Line)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B (Leading Span B)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 60, 4)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for Average True Range calculation", "Volatility Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._deviation_factor = self.Param("DeviationFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Factor", "Factor multiplied by standard deviation to detect volatility contraction", "Volatility Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._avg_atr = 0.0
        self._atr_std_dev = 0.0
        self._processed_candles = 0
        self._ichimoku = None
        self._atr = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(ichimoku_volatility_contraction_strategy, self).OnReseted()
        self._avg_atr = 0.0
        self._atr_std_dev = 0.0
        self._processed_candles = 0
        self._ichimoku = None
        self._atr = None

    def OnStarted2(self, time):
        super(ichimoku_volatility_contraction_strategy, self).OnStarted2(time)

        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = int(self._tenkan_period.Value)
        self._ichimoku.Kijun.Length = int(self._kijun_period.Value)
        self._ichimoku.SenkouB.Length = int(self._senkou_span_b_period.Value)

        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ich_iv = CandleIndicatorValue(self._ichimoku, candle)
        ich_iv.IsFinal = True
        ichimoku_value = self._ichimoku.Process(ich_iv)

        atr_iv = CandleIndicatorValue(self._atr, candle)
        atr_iv.IsFinal = True
        atr_value = self._atr.Process(atr_iv)

        if not ichimoku_value.IsFinal or not atr_value.IsFinal or not self._ichimoku.IsFormed or not self._atr.IsFormed:
            return

        current_atr = float(atr_value)
        self._processed_candles += 1
        atr_period = int(self._atr_period.Value)

        if self._processed_candles == 1:
            self._avg_atr = current_atr
            self._atr_std_dev = 0.0
        else:
            alpha = 2.0 / (atr_period + 1)
            old_avg = self._avg_atr
            self._avg_atr = alpha * current_atr + (1.0 - alpha) * self._avg_atr
            atr_dev = abs(current_atr - old_avg)
            self._atr_std_dev = alpha * atr_dev + (1.0 - alpha) * self._atr_std_dev

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if ichimoku_value.Tenkan is None or ichimoku_value.Kijun is None or \
           ichimoku_value.SenkouA is None or ichimoku_value.SenkouB is None:
            return

        tenkan = float(ichimoku_value.Tenkan)
        kijun = float(ichimoku_value.Kijun)
        senkou_a = float(ichimoku_value.SenkouA)
        senkou_b = float(ichimoku_value.SenkouB)

        upper_kumo = max(senkou_a, senkou_b)
        lower_kumo = min(senkou_a, senkou_b)
        is_volatility_contraction = current_atr <= self._avg_atr

        close_price = float(candle.ClosePrice)

        if is_volatility_contraction:
            if close_price > upper_kumo and tenkan > kijun and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif close_price < lower_kumo and tenkan < kijun and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        if self.Position > 0 and close_price < lower_kumo:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close_price > upper_kumo:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return ichimoku_volatility_contraction_strategy()
