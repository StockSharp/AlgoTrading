import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class keltner_volume_strategy(Strategy):
    """
    Strategy combining Keltner Channels with volume confirmation.
    """

    def __init__(self):
        super(keltner_volume_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetRange(10, 40) \
            .SetDisplay("EMA Period", "EMA period for center line", "Keltner")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(7, 21) \
            .SetDisplay("ATR Period", "ATR period for channel width", "Keltner")
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR", "Keltner")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Volume Avg Period", "Period for volume average", "Volume")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._average_volume = 0.0
        self._volume_counter = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(keltner_volume_strategy, self).OnStarted(time)
        self._average_volume = 0.0
        self._volume_counter = 0
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)
        ev = float(ema_value)
        av = float(atr_value)

        vol_avg_prd = self._volume_avg_period.Value

        if self._volume_counter < vol_avg_prd:
            self._volume_counter += 1
            self._average_volume = ((self._average_volume * (self._volume_counter - 1)) + volume) / self._volume_counter
        else:
            self._average_volume = (self._average_volume * (vol_avg_prd - 1) + volume) / vol_avg_prd

        if self._volume_counter < vol_avg_prd:
            if self._cooldown > 0:
                self._cooldown -= 1
            return

        mult = float(self._multiplier.Value)
        upper_band = ev + mult * av
        lower_band = ev - mult * av
        high_volume = volume > self._average_volume

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value

        # Buy: price above upper band + high volume
        if close > upper_band and high_volume and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif close < lower_band and high_volume and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price below EMA
        if self.Position > 0 and close < ev:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > ev:
            self.BuyMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(keltner_volume_strategy, self).OnReseted()
        self._average_volume = 0.0
        self._volume_counter = 0
        self._cooldown = 0

    def CreateClone(self):
        return keltner_volume_strategy()
