import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class mikuls_ichimoku_cloud_v2_strategy(Strategy):
    """
    Mikul's Ichimoku Cloud v2: Tenkan/Kijun cross with ATR trailing stop.
    """

    def __init__(self):
        super(mikuls_ichimoku_cloud_v2_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "ATR period", "Indicators")
        self._atr_mult = self.Param("AtrMultiplier", 1.5).SetDisplay("ATR Mult", "Trailing ATR mult", "Risk")
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetDisplay("Tenkan", "Ichimoku Tenkan", "Indicators")
        self._kijun_period = self.Param("KijunPeriod", 26).SetDisplay("Kijun", "Ichimoku Kijun", "Indicators")
        self._senkou_b_period = self.Param("SenkouBPeriod", 52).SetDisplay("Senkou B", "Ichimoku SenkouB", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 50).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._trail_price = None
        self._prev_tenkan = None
        self._prev_kijun = None
        self._bars_from_signal = 50
        self._bar_index = 0
        self._entry_bar = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mikuls_ichimoku_cloud_v2_strategy, self).OnReseted()
        self._trail_price = None
        self._prev_tenkan = None
        self._prev_kijun = None
        self._bars_from_signal = self._cooldown_bars.Value
        self._bar_index = 0
        self._entry_bar = -1

    def OnStarted(self, time):
        super(mikuls_ichimoku_cloud_v2_strategy, self).OnStarted(time)
        ich = Ichimoku()
        ich.Tenkan.Length = self._tenkan_period.Value
        ich.Kijun.Length = self._kijun_period.Value
        ich.SenkouB.Length = self._senkou_b_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ich, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ich)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ich_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._bar_index += 1
        self._bars_from_signal += 1
        close = float(candle.ClosePrice)
        atr = float(atr_value.ToDecimal())
        tenkan = ich_value.Tenkan
        kijun = ich_value.Kijun
        senkou_a = ich_value.SenkouA
        senkou_b = ich_value.SenkouB
        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return
        tenkan_f = float(tenkan)
        kijun_f = float(kijun)
        sa = float(senkou_a)
        sb = float(senkou_b)
        lower_cloud = min(sa, sb)
        cross_up = (self._prev_tenkan is not None and self._prev_kijun is not None and
                    self._prev_tenkan <= self._prev_kijun and tenkan_f > kijun_f)
        cross_down = (self._prev_tenkan is not None and self._prev_kijun is not None and
                      self._prev_tenkan >= self._prev_kijun and tenkan_f < kijun_f)
        mult = float(self._atr_mult.Value)
        if self._bars_from_signal >= self._cooldown_bars.Value and cross_up and self.Position <= 0:
            self.BuyMarket()
            self._trail_price = close - atr * mult
            self._entry_bar = self._bar_index
            self._bars_from_signal = 0
        if self.Position > 0 and self._bar_index > self._entry_bar:
            next_trail = close - atr * mult
            if self._trail_price is None or next_trail > self._trail_price:
                self._trail_price = next_trail
            if close <= self._trail_price:
                self.SellMarket()
                self._trail_price = None
            elif cross_down or close < lower_cloud:
                self.SellMarket()
                self._trail_price = None
        self._prev_tenkan = tenkan_f
        self._prev_kijun = kijun_f

    def CreateClone(self):
        return mikuls_ichimoku_cloud_v2_strategy()
