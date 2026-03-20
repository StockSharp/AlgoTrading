import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ichimoku_cloud_buy_sell_strategy(Strategy):
    def __init__(self):
        super(ichimoku_cloud_buy_sell_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Tenkan-sen periods", "Ichimoku Settings")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Kijun-sen periods", "Ichimoku Settings")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Senkou Span B periods", "Ichimoku Settings")
        self._ema_period = self.Param("EmaPeriod", 44) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "EMA length for exit", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ichimoku_cloud_buy_sell_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(ichimoku_cloud_buy_sell_strategy, self).OnStarted(time)
        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self._tenkan_period.Value
        self._ichimoku.Kijun.Length = self._kijun_period.Value
        self._ichimoku.SenkouB.Length = self._senkou_span_b_period.Value
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._ichimoku, self._ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ichimoku_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        if ema_val.IsEmpty or ichimoku_val.IsEmpty:
            return
        ema_v = float(ema_val.ToDecimal())
        senkou_a = ichimoku_val.SenkouA
        senkou_b = ichimoku_val.SenkouB
        if senkou_a is None or senkou_b is None:
            return
        senkou_a_v = float(senkou_a)
        senkou_b_v = float(senkou_b)
        upper_kumo = max(senkou_a_v, senkou_b_v)
        lower_kumo = min(senkou_a_v, senkou_b_v)
        close = float(candle.ClosePrice)
        buy_cond = close > upper_kumo and close > ema_v
        sell_cond = close < lower_kumo and close < ema_v
        if buy_cond and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 5
        elif sell_cond and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 5
        elif self.Position > 0 and close < ema_v:
            self.SellMarket()
            self._cooldown = 5
        elif self.Position < 0 and close > ema_v:
            self.BuyMarket()
            self._cooldown = 5

    def CreateClone(self):
        return ichimoku_cloud_buy_sell_strategy()
