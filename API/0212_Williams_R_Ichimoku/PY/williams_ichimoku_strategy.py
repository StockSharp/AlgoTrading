import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR, Ichimoku
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class williams_ichimoku_strategy(Strategy):
    """
    Strategy based on Williams %R and Ichimoku indicators.
    """

    def __init__(self):
        super(williams_ichimoku_strategy, self).__init__()

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicators")

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line (Ichimoku)", "Indicators")

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun-sen Period", "Period for Kijun-sen line (Ichimoku)", "Indicators")

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B line (Ichimoku)", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 60) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_williams_r = -50.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_ichimoku_strategy, self).OnReseted()
        self._prev_williams_r = -50.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(williams_ichimoku_strategy, self).OnStarted(time)
        self._prev_williams_r = -50.0
        self._cooldown = 0

        williams_r = WilliamsR()
        williams_r.Length = self._williams_r_period.Value

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_b_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(williams_r, ichimoku, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, williams_r)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, williams_r_value, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

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

        kumo_top = max(senkou_a, senkou_b)
        kumo_bottom = min(senkou_a, senkou_b)
        price = float(candle.ClosePrice)
        is_price_above_kumo = price > kumo_top
        is_price_below_kumo = price < kumo_bottom

        wr = float(williams_r_value)
        crossed_below_90 = self._prev_williams_r >= -90 and wr < -90
        crossed_above_10 = self._prev_williams_r <= -10 and wr > -10
        self._prev_williams_r = wr

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and crossed_below_90 and price > kumo_top * 1.002 and is_price_above_kumo and tenkan > kijun:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                self._cooldown = cooldown_val
        elif self._cooldown == 0 and crossed_above_10 and price < kumo_bottom * 0.998 and is_price_below_kumo and tenkan < kijun:
            if self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
                self._cooldown = cooldown_val
        elif (self.Position > 0 and price < kumo_bottom) or (self.Position < 0 and price > kumo_top):
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
                self._cooldown = cooldown_val
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
                self._cooldown = cooldown_val

    def CreateClone(self):
        return williams_ichimoku_strategy()
