import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class vwap_volume_strategy(Strategy):
    """
    Strategy combining VWAP with volume confirmation.
    """

    def __init__(self):
        super(vwap_volume_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Volume MA Period", "Period for volume moving average", "Indicators")
        self._volume_threshold = self.Param("VolumeThreshold", 1.5) \
            .SetDisplay("Volume Threshold", "Multiplier for average volume", "Trading Levels")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._volumes = []
        self._cum_vol = 0.0
        self._cum_tpv = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(vwap_volume_strategy, self).OnStarted(time)
        self._volumes = []
        self._cum_vol = 0.0
        self._cum_tpv = 0.0
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._volume_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        vol = float(candle.TotalVolume)
        typical_price = (high + low + close) / 3.0

        self._volumes.append(vol)
        self._cum_vol += vol
        self._cum_tpv += typical_price * vol

        vol_prd = self._volume_period.Value

        if len(self._volumes) < vol_prd:
            if self._cooldown > 0:
                self._cooldown -= 1
            return

        # Manual VWAP (cumulative)
        vwap_value = self._cum_tpv / self._cum_vol if self._cum_vol > 0 else close

        # Manual volume average
        count = len(self._volumes)
        sum_vol = sum(self._volumes[count - vol_prd:count])
        avg_vol = sum_vol / vol_prd

        high_volume = vol > avg_vol * self._volume_threshold.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value

        # Buy: price above VWAP + high volume
        if close > vwap_value and high_volume and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif close < vwap_value and high_volume and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price below VWAP
        if self.Position > 0 and close < vwap_value:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > vwap_value:
            self.BuyMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(vwap_volume_strategy, self).OnReseted()
        self._volumes = []
        self._cum_vol = 0.0
        self._cum_tpv = 0.0
        self._cooldown = 0

    def CreateClone(self):
        return vwap_volume_strategy()
