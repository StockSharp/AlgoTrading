import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, StochasticOscillator, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class kumo_trade_ichimoku_strategy(Strategy):
    def __init__(self):
        super(kumo_trade_ichimoku_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan-sen Period", "Period for Tenkan line", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun-sen Period", "Period for Kijun line", "Ichimoku")
        self._senkou_period = self.Param("SenkouPeriod", 52) \
            .SetDisplay("Senkou Span B Period", "Period for Senkou B", "Ichimoku")
        self._stoch_k = self.Param("StochK", 70) \
            .SetDisplay("Stochastic K", "Period for K line", "Stochastic")
        self._stoch_d = self.Param("StochD", 15) \
            .SetDisplay("Stochastic D", "Smoothing for D line", "Stochastic")
        self._atr_period = self.Param("AtrPeriod", 5) \
            .SetDisplay("ATR Period", "Period for ATR stop", "Risk")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 12000) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_stoch_d = 0.0
        self._prev_high = 0.0
        self._prev_kijun = 0.0
        self._trail_stop_long = None
        self._trail_stop_short = None
        self._highest_close = 0.0
        self._lowest_low = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(kumo_trade_ichimoku_strategy, self).OnReseted()
        self._prev_stoch_d = 0.0
        self._prev_high = 0.0
        self._prev_kijun = 0.0
        self._trail_stop_long = None
        self._trail_stop_short = None
        self._highest_close = 0.0
        self._lowest_low = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(kumo_trade_ichimoku_strategy, self).OnStarted(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_period.Value
        stoch = StochasticOscillator()
        stoch.K.Length = self._stoch_k.Value
        stoch.D.Length = self._stoch_d.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, stoch, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ichimoku_val, stoch_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        tenkan = ichimoku_val.Tenkan
        kijun = ichimoku_val.Kijun
        senkou_a = ichimoku_val.SenkouA
        senkou_b = ichimoku_val.SenkouB
        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return
        tenkan = float(tenkan)
        kijun = float(kijun)
        senkou_a = float(senkou_a)
        senkou_b = float(senkou_b)
        stoch_d_val = stoch_val.D
        if stoch_d_val is None:
            return
        stoch_d = float(stoch_d_val)
        atr_v = float(atr_val.ToDecimal())
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_p = float(candle.OpenPrice)
        upper_cloud = max(senkou_a, senkou_b)
        lower_cloud = min(senkou_a, senkou_b)
        no_kumo = high < (lower_cloud - atr_v / 2.0) or low > (upper_cloud + atr_v)
        kumo_red = senkou_b > senkou_a
        long_cond = (self.Position <= 0 and low > kijun and kijun > tenkan
                     and close < senkou_a and close > open_p and no_kumo and stoch_d < 29.0)
        crossed_above_kijun = high > kijun and self._prev_high <= self._prev_kijun
        short_cond = (self.Position >= 0 and close < lower_cloud and crossed_above_kijun
                      and stoch_d >= 90.0 and self._prev_stoch_d > stoch_d and kumo_red)
        if self._bars_since_signal < self._cooldown_bars.Value:
            self._prev_stoch_d = stoch_d
            self._prev_high = high
            self._prev_kijun = kijun
            return
        if self._entries_executed < self._max_entries.Value and long_cond:
            self.BuyMarket()
            self._trail_stop_long = None
            self._highest_close = close
            self._entries_executed += 1
            self._bars_since_signal = 0
        elif self._entries_executed < self._max_entries.Value and short_cond:
            self.SellMarket()
            self._trail_stop_short = None
            self._lowest_low = low
            self._entries_executed += 1
            self._bars_since_signal = 0
        if self.Position > 0:
            if close > self._highest_close:
                self._highest_close = close
            temp = self._highest_close - atr_v * 3.0
            if self._trail_stop_long is None or temp > self._trail_stop_long:
                self._trail_stop_long = temp
            if self._trail_stop_long is not None and close < self._trail_stop_long:
                self.SellMarket()
                self._trail_stop_long = None
                self._highest_close = 0.0
                self._bars_since_signal = 0
        elif self.Position < 0:
            if low < self._lowest_low or self._lowest_low == 0.0:
                self._lowest_low = low
            temp = self._lowest_low + atr_v * 3.0
            if self._trail_stop_short is None or temp < self._trail_stop_short:
                self._trail_stop_short = temp
            if self._trail_stop_short is not None and close > self._trail_stop_short:
                self.BuyMarket()
                self._trail_stop_short = None
                self._lowest_low = 0.0
                self._bars_since_signal = 0
        self._prev_stoch_d = stoch_d
        self._prev_high = high
        self._prev_kijun = kijun

    def CreateClone(self):
        return kumo_trade_ichimoku_strategy()
