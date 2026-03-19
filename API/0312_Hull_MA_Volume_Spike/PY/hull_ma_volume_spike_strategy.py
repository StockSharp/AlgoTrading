import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hull_ma_volume_spike_strategy(Strategy):
    """
    Hull MA trend-following confirmed by volume spike detection.
    """

    def __init__(self):
        super(hull_ma_volume_spike_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetDisplay("HMA Period", "Hull MA period", "Indicators")
        self._vol_period = self.Param("VolumeAvgPeriod", 20).SetDisplay("Vol Period", "Volume stats period", "Indicators")
        self._vol_factor = self.Param("VolumeThresholdFactor", 1.8).SetDisplay("Vol Factor", "Volume spike multiplier", "Signals")
        self._cooldown_bars = self.Param("CooldownBars", 72).SetDisplay("Cooldown", "Bars between trades", "Risk")
        self._sl_pct = self.Param("StopLossPercent", 2.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_hma = 0.0
        self._is_init = False
        self._cooldown = 0
        self._volumes = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_volume_spike_strategy, self).OnReseted()
        self._prev_hma = 0.0
        self._is_init = False
        self._cooldown = 0
        self._volumes = []

    def OnStarted(self, time):
        super(hull_ma_volume_spike_strategy, self).OnStarted(time)
        hma = HullMovingAverage()
        hma.Length = self._hma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, self._process_candle).Start()
        self.StartProtection(None, Unit(self._sl_pct.Value, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hma_val):
        if candle.State != CandleStates.Finished:
            return
        hma = float(hma_val)
        vol = float(candle.TotalVolume)
        vp = self._vol_period.Value
        self._volumes.append(vol)
        if len(self._volumes) > vp * 2:
            self._volumes = self._volumes[-(vp * 2):]
        if not self._is_init:
            self._prev_hma = hma
            self._is_init = True
            return
        if len(self._volumes) < vp:
            self._prev_hma = hma
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_hma = hma
            return
        recent = self._volumes[-vp:]
        avg = sum(recent) / len(recent)
        import math
        var = sum((v - avg) ** 2 for v in recent) / len(recent)
        std = math.sqrt(var)
        threshold = avg + self._vol_factor.Value * std
        spiking = vol >= threshold
        rising = hma > self._prev_hma
        falling = hma < self._prev_hma
        if self.Position == 0:
            if rising and spiking:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
            elif falling and spiking:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self.Position > 0:
            if falling:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0:
            if rising:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
        self._prev_hma = hma

    def CreateClone(self):
        return hull_ma_volume_spike_strategy()
