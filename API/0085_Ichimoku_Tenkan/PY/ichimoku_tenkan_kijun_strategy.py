import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy

class ichimoku_tenkan_kijun_strategy(Strategy):
    """
    Ichimoku Tenkan/Kijun Cross strategy.
    Enters long when Tenkan crosses above Kijun and price is above Kumo.
    Enters short when Tenkan crosses below Kijun and price is below Kumo.
    Exits on opposite cross or Kumo breach.
    """

    def __init__(self):
        super(ichimoku_tenkan_kijun_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetDisplay("Tenkan Period", "Period for Tenkan-sen", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetDisplay("Kijun Period", "Period for Kijun-sen", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52).SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Ichimoku")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_tenkan = 0.0
        self._prev_kijun = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_tenkan_kijun_strategy, self).OnReseted()
        self._prev_tenkan = 0.0
        self._prev_kijun = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ichimoku_tenkan_kijun_strategy, self).OnStarted2(time)

        self._prev_tenkan = 0.0
        self._prev_kijun = 0.0
        self._cooldown = 0

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_b_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ichimoku_iv):
        if candle.State != CandleStates.Finished:
            return

        if not ichimoku_iv.IsFormed:
            return

        tenkan_val = ichimoku_iv.Tenkan
        kijun_val = ichimoku_iv.Kijun
        senkou_a_val = ichimoku_iv.SenkouA
        senkou_b_val = ichimoku_iv.SenkouB

        if tenkan_val is None or kijun_val is None or senkou_a_val is None or senkou_b_val is None:
            return

        tenkan = float(tenkan_val)
        kijun = float(kijun_val)
        senkou_a = float(senkou_a_val)
        senkou_b = float(senkou_b_val)

        if self._prev_tenkan == 0 or self._prev_kijun == 0:
            self._prev_tenkan = tenkan
            self._prev_kijun = kijun
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_tenkan = tenkan
            self._prev_kijun = kijun
            return

        bullish_cross = self._prev_tenkan <= self._prev_kijun and tenkan > kijun
        bearish_cross = self._prev_tenkan >= self._prev_kijun and tenkan < kijun

        upper_kumo = max(senkou_a, senkou_b)
        lower_kumo = min(senkou_a, senkou_b)

        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and bullish_cross and close > upper_kumo:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bearish_cross and close < lower_kumo:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and (bearish_cross or close < lower_kumo):
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and (bullish_cross or close > upper_kumo):
            self.BuyMarket()
            self._cooldown = cd

        self._prev_tenkan = tenkan
        self._prev_kijun = kijun

    def CreateClone(self):
        return ichimoku_tenkan_kijun_strategy()
