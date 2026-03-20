import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class adx_volume_strategy(Strategy):
    """
    ADX + Volume strategy.
    Enter trades when ADX is above threshold with above average volume.
    """

    def __init__(self):
        super(adx_volume_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX indicator", "ADX Parameters")
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetRange(10, 50) \
            .SetDisplay("ADX Threshold", "Threshold above which trend is considered strong", "ADX Parameters")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetDisplay("Volume Average Period", "Period for volume moving average", "Volume Parameters")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.4) \
            .SetRange(1.0, 3.0) \
            .SetDisplay("Volume Multiplier", "Multiplier over average volume", "Volume Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 160) \
            .SetRange(5, 500) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._average_volume = 0.0
        self._volume_counter = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(adx_volume_strategy, self).OnStarted(time)
        self._average_volume = 0.0
        self._volume_counter = 0
        self._cooldown = 0

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if not adx_value.IsFormed:
            return

        current_volume = float(candle.TotalVolume)
        vol_avg_prd = self._volume_avg_period.Value

        if self._volume_counter < vol_avg_prd:
            self._volume_counter += 1
            self._average_volume = ((self._average_volume * (self._volume_counter - 1)) + current_volume) / self._volume_counter
        else:
            self._average_volume = (self._average_volume * (vol_avg_prd - 1) + current_volume) / vol_avg_prd

        if self._volume_counter < vol_avg_prd:
            if self._cooldown > 0:
                self._cooldown -= 1
            return

        # Get ADX values
        di_plus = float(adx_value.Dx.Plus) if adx_value.Dx.Plus is not None else 0
        di_minus = float(adx_value.Dx.Minus) if adx_value.Dx.Minus is not None else 0
        adx_ma = float(adx_value.MovingAverage) if adx_value.MovingAverage is not None else 0

        is_volume_above_avg = current_volume > self._average_volume * self._volume_multiplier.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value
        threshold = self._adx_threshold.Value

        if adx_ma > threshold and is_volume_above_avg:
            if di_plus > di_minus and self.Position == 0:
                self.BuyMarket()
                self._cooldown = cd
            elif di_minus > di_plus and self.Position == 0:
                self.SellMarket()
                self._cooldown = cd
        elif adx_ma < threshold * 0.8:
            if self.Position > 0:
                self.SellMarket()
                self._cooldown = cd
            elif self.Position < 0:
                self.BuyMarket()
                self._cooldown = cd
        elif di_plus < di_minus and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        elif di_plus > di_minus and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(adx_volume_strategy, self).OnReseted()
        self._average_volume = 0.0
        self._volume_counter = 0
        self._cooldown = 0

    def CreateClone(self):
        return adx_volume_strategy()
