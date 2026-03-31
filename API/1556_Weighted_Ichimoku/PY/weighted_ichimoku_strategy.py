import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy


class weighted_ichimoku_strategy(Strategy):
    def __init__(self):
        super(weighted_ichimoku_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan Period", "Tenkan length", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun Period", "Kijun length", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetDisplay("Senkou B Period", "Span B length", "Ichimoku")
        self._buy_threshold = self.Param("BuyThreshold", 70.0) \
            .SetDisplay("Buy Threshold", "Score to enter long", "General")
        self._sell_threshold = self.Param("SellThreshold", -70.0) \
            .SetDisplay("Sell Threshold", "Score to exit/short", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def tenkan_period(self):
        return self._tenkan_period.Value

    @property
    def kijun_period(self):
        return self._kijun_period.Value

    @property
    def senkou_span_b_period(self):
        return self._senkou_span_b_period.Value

    @property
    def buy_threshold(self):
        return self._buy_threshold.Value

    @property
    def sell_threshold(self):
        return self._sell_threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(weighted_ichimoku_strategy, self).OnStarted2(time)
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.tenkan_period
        ichimoku.Kijun.Length = self.kijun_period
        ichimoku.SenkouB.Length = self.senkou_span_b_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def on_process(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        t = value.Tenkan
        k = value.Kijun
        sa = value.SenkouA
        sb = value.SenkouB
        if t is None or k is None or sa is None or sb is None:
            return
        tenkan = float(t)
        kijun = float(k)
        senkou_a = float(sa)
        senkou_b = float(sb)
        if tenkan == 0 or kijun == 0 or senkou_a == 0 or senkou_b == 0:
            return
        cloud_top = max(senkou_a, senkou_b)
        cloud_bottom = min(senkou_a, senkou_b)
        close = float(candle.ClosePrice)
        score = 0.0
        if tenkan > kijun:
            score += 25.0
        elif tenkan < kijun:
            score -= 25.0
        if close > cloud_top:
            score += 30.0
        elif close < cloud_bottom:
            score -= 30.0
        if close > kijun:
            score += 15.0
        elif close < kijun:
            score -= 15.0
        if score >= self.buy_threshold and self.Position <= 0:
            self.BuyMarket()
        elif score <= self.sell_threshold and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return weighted_ichimoku_strategy()
