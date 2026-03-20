import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class ichimoku_volume_strategy(Strategy):
    """
    Strategy combining Ichimoku (manual Tenkan/Kijun) with volume filter.
    """

    def __init__(self):
        super(ichimoku_volume_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetRange(5, 20) \
            .SetDisplay("Tenkan Period", "Tenkan-sen period (fast)", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetRange(15, 40) \
            .SetDisplay("Kijun Period", "Kijun-sen period (slow)", "Ichimoku")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Volume Average Period", "Period for volume moving average", "Volume")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._highs = []
        self._lows = []
        self._vols = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(ichimoku_volume_strategy, self).OnStarted(time)
        self._highs = []
        self._lows = []
        self._vols = []
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._kijun_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        vol = float(candle.TotalVolume)

        self._highs.append(high)
        self._lows.append(low)
        self._vols.append(vol)

        tenkan_prd = self._tenkan_period.Value
        kijun_prd = self._kijun_period.Value
        vol_prd = self._volume_avg_period.Value
        min_bars = max(kijun_prd, vol_prd)

        if len(self._highs) < min_bars:
            if self._cooldown > 0:
                self._cooldown -= 1
            return

        count = len(self._highs)

        # Manual Tenkan-sen
        tenkan_hh = max(self._highs[count - tenkan_prd:count])
        tenkan_ll = min(self._lows[count - tenkan_prd:count])
        tenkan = (tenkan_hh + tenkan_ll) / 2.0

        # Manual Kijun-sen
        kijun_hh = max(self._highs[count - kijun_prd:count])
        kijun_ll = min(self._lows[count - kijun_prd:count])
        kijun = (kijun_hh + kijun_ll) / 2.0

        # Senkou Span A and B
        senkou_a = (tenkan + kijun) / 2.0
        senkou_b = kijun  # simplified
        upper_kumo = max(senkou_a, senkou_b)
        lower_kumo = min(senkou_a, senkou_b)

        # Volume average
        sum_vol = sum(self._vols[count - vol_prd:count])
        avg_vol = sum_vol / vol_prd
        high_volume = vol > avg_vol

        # Trim lists
        max_keep = min_bars * 3
        if len(self._highs) > max_keep:
            trim = len(self._highs) - min_bars * 2
            self._highs = self._highs[trim:]
            self._lows = self._lows[trim:]
            self._vols = self._vols[trim:]

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value

        # Buy: price above cloud + Tenkan above Kijun + high volume
        if close > upper_kumo and tenkan > kijun and high_volume and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif close < lower_kumo and tenkan < kijun and high_volume and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price drops below kijun
        if self.Position > 0 and close < kijun:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > kijun:
            self.BuyMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(ichimoku_volume_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._vols = []
        self._cooldown = 0

    def CreateClone(self):
        return ichimoku_volume_strategy()
