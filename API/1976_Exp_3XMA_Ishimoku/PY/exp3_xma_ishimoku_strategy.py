import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy


class exp3_xma_ishimoku_strategy(Strategy):

    def __init__(self):
        super(exp3_xma_ishimoku_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 3) \
            .SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 6) \
            .SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")
        self._senkou_span_period = self.Param("SenkouSpanPeriod", 9) \
            .SetDisplay("Senkou B Period", "Senkou Span B period", "Ichimoku")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._prev_kijun = None
        self._prev_upper = None
        self._prev_lower = None

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanPeriod(self):
        return self._senkou_span_period.Value

    @SenkouSpanPeriod.setter
    def SenkouSpanPeriod(self, value):
        self._senkou_span_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(exp3_xma_ishimoku_strategy, self).OnStarted(time)

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouSpanPeriod

        self.SubscribeCandles(self.CandleType) \
            .BindEx(ichimoku, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        kijun_raw = ichimoku_value.Kijun
        senkou_a_raw = ichimoku_value.SenkouA
        senkou_b_raw = ichimoku_value.SenkouB

        if kijun_raw is None or senkou_a_raw is None or senkou_b_raw is None:
            return

        kijun = float(kijun_raw)
        senkou_a = float(senkou_a_raw)
        senkou_b = float(senkou_b_raw)

        upper = max(senkou_a, senkou_b)
        lower = min(senkou_a, senkou_b)

        if self._prev_kijun is None:
            self._prev_kijun = kijun
            self._prev_upper = upper
            self._prev_lower = lower
            return

        cross_down = self._prev_kijun > self._prev_upper and kijun <= upper
        cross_up = self._prev_kijun < self._prev_lower and kijun >= lower

        if cross_down and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_up and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_kijun = kijun
        self._prev_upper = upper
        self._prev_lower = lower

    def OnReseted(self):
        super(exp3_xma_ishimoku_strategy, self).OnReseted()
        self._prev_kijun = None
        self._prev_upper = None
        self._prev_lower = None

    def CreateClone(self):
        return exp3_xma_ishimoku_strategy()
