import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class expert_ichimoku_strategy(Strategy):
    def __init__(self):
        super(expert_ichimoku_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan Period", "Short channel period", "Indicators")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun Period", "Long channel period", "Indicators")

        self._prev_tenkan = None
        self._prev_kijun = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    def OnReseted(self):
        super(expert_ichimoku_strategy, self).OnReseted()
        self._prev_tenkan = None
        self._prev_kijun = None

    def OnStarted2(self, time):
        super(expert_ichimoku_strategy, self).OnStarted2(time)
        self._prev_tenkan = None
        self._prev_kijun = None

        tenkan_high = Highest()
        tenkan_high.Length = self.TenkanPeriod
        tenkan_low = Lowest()
        tenkan_low.Length = self.TenkanPeriod
        kijun_high = Highest()
        kijun_high.Length = self.KijunPeriod
        kijun_low = Lowest()
        kijun_low.Length = self.KijunPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(tenkan_high, tenkan_low, kijun_high, kijun_low, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, t_high_value, t_low_value, k_high_value, k_low_value):
        if candle.State != CandleStates.Finished:
            return

        th = float(t_high_value)
        tl = float(t_low_value)
        kh = float(k_high_value)
        kl = float(k_low_value)

        tenkan = (th + tl) / 2.0
        kijun = (kh + kl) / 2.0

        if self._prev_tenkan is None or self._prev_kijun is None:
            self._prev_tenkan = tenkan
            self._prev_kijun = kijun
            return

        # Tenkan crosses above Kijun
        if self._prev_tenkan <= self._prev_kijun and tenkan > kijun:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Tenkan crosses below Kijun
        elif self._prev_tenkan >= self._prev_kijun and tenkan < kijun:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_tenkan = tenkan
        self._prev_kijun = kijun

    def CreateClone(self):
        return expert_ichimoku_strategy()
