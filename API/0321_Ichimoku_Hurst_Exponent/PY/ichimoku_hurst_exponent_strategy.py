import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy


class ichimoku_hurst_exponent_strategy(Strategy):
    """
    Strategy based on Ichimoku Kinko Hyo indicator with Hurst exponent filter.
    """

    def __init__(self):
        super(ichimoku_hurst_exponent_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan Period", "Tenkan-sen (conversion line) period", "Ichimoku")

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun Period", "Kijun-sen (base line) period", "Ichimoku")

        self._senkou_spanb_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetDisplay("Senkou Span B Period", "Senkou Span B (leading span B) period", "Ichimoku")

        self._hurst_period = self.Param("HurstPeriod", 100) \
            .SetDisplay("Hurst Period", "Hurst exponent calculation period", "Hurst Exponent")

        self._hurst_threshold = self.Param("HurstThreshold", 0.5) \
            .SetDisplay("Hurst Threshold", "Hurst exponent threshold for trend strength", "Hurst Exponent")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait between reversals", "Trading")

        self._prices = []
        self._hurst_exponent = 0.5
        self._prev_tenkan = None
        self._prev_kijun = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_hurst_exponent_strategy, self).OnReseted()
        self._prices = []
        self._hurst_exponent = 0.5
        self._prev_tenkan = None
        self._prev_kijun = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ichimoku_hurst_exponent_strategy, self).OnStarted2(time)

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = int(self._tenkan_period.Value)
        ichimoku.Kijun.Length = int(self._kijun_period.Value)
        ichimoku.SenkouB.Length = int(self._senkou_spanb_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        tenkan_val = ichimoku_value.Tenkan
        kijun_val = ichimoku_value.Kijun
        senkou_a_val = ichimoku_value.SenkouA
        senkou_b_val = ichimoku_value.SenkouB

        if tenkan_val is None or kijun_val is None or senkou_a_val is None or senkou_b_val is None:
            return

        tenkan = float(tenkan_val)
        kijun = float(kijun_val)
        senkou_a = float(senkou_a_val)
        senkou_b = float(senkou_b_val)

        hurst_period = int(self._hurst_period.Value)
        self._prices.append(float(candle.ClosePrice))
        while len(self._prices) > hurst_period:
            self._prices.pop(0)

        if len(self._prices) >= hurst_period:
            self._calculate_hurst_exponent()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        close_price = float(candle.ClosePrice)
        is_price_above_kumo = close_price > max(senkou_a, senkou_b)
        is_price_below_kumo = close_price < min(senkou_a, senkou_b)

        cd = int(self._signal_cooldown_bars.Value)
        ht = float(self._hurst_threshold.Value)

        if self._prev_tenkan is not None and self._prev_kijun is not None:
            cross_up = self._prev_tenkan <= self._prev_kijun and tenkan > kijun
            cross_down = self._prev_tenkan >= self._prev_kijun and tenkan < kijun

            long_exit = self.Position > 0 and (is_price_below_kumo or cross_down)
            short_exit = self.Position < 0 and (is_price_above_kumo or cross_up)

            if long_exit:
                self.SellMarket(self.Position)
                self._cooldown_remaining = cd
            elif short_exit:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown_remaining = cd
            elif self._cooldown_remaining == 0 and is_price_above_kumo and cross_up and self._hurst_exponent > ht and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self._cooldown_remaining = cd
            elif self._cooldown_remaining == 0 and is_price_below_kumo and cross_down and self._hurst_exponent > ht and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self._cooldown_remaining = cd

        self._prev_tenkan = tenkan
        self._prev_kijun = kijun

    def _calculate_hurst_exponent(self):
        log_returns = []
        for i in range(1, len(self._prices)):
            if self._prices[i - 1] != 0:
                log_returns.append(math.log(self._prices[i] / self._prices[i - 1]))

        if len(log_returns) < 10:
            return

        mean = sum(log_returns) / len(log_returns)

        cumulative_deviation = []
        sum_val = 0.0
        for lr in log_returns:
            sum_val += (lr - mean)
            cumulative_deviation.append(sum_val)

        range_val = max(cumulative_deviation) - min(cumulative_deviation)

        sum_squares = sum((x - mean) ** 2 for x in log_returns)
        std_dev = math.sqrt(sum_squares / len(log_returns))

        if std_dev == 0:
            return

        rs = range_val / std_dev

        log_n = math.log(len(log_returns))
        if log_n != 0:
            self._hurst_exponent = math.log(rs) / log_n

    def CreateClone(self):
        return ichimoku_hurst_exponent_strategy()
