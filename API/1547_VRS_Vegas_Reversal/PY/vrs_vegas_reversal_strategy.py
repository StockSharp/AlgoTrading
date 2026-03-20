import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class vrs_vegas_reversal_strategy(Strategy):
    def __init__(self):
        super(vrs_vegas_reversal_strategy, self).__init__()
        self._spike_percent = self.Param("SpikePercent", 0.025) \
            .SetDisplay("Spike %", "Spike percentage threshold", "Reversal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def spike_percent(self):
        return self._spike_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vrs_vegas_reversal_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(vrs_vegas_reversal_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        upper_spike = candle.HighPrice - max(candle.ClosePrice, candle.OpenPrice)
        lower_spike = min(candle.ClosePrice, candle.OpenPrice) - candle.LowPrice
        valid_upper = upper_spike >= candle.ClosePrice * self.spike_percent
        valid_lower = lower_spike >= candle.ClosePrice * self.spike_percent
        valid = (valid_upper and not valid_lower) or (valid_lower and not valid_upper)
        enter_long = valid and valid_lower
        enter_short = valid and valid_upper
        if enter_long and self.Position <= 0:
            self._entry_price = candle.ClosePrice
            self._spike_size = lower_spike
            self._is_long = True
            self.BuyMarket()
        elif enter_short and self.Position >= 0:
            self._entry_price = candle.ClosePrice
            self._spike_size = upper_spike
            self._is_long = False
            self.SellMarket()
        if self.Position > 0 and self._is_long:
            target = self._entry_price + self._spike_size * 2
            if candle.ClosePrice >= target:
                self.SellMarket()
        elif self.Position < 0 and not self._is_long:
            target = self._entry_price - self._spike_size * 2
            if candle.ClosePrice <= target:
                self.BuyMarket()

    def normalize_volume(self, volume):
        step = ((Security.VolumeStep if Security is not None else None) if (Security.VolumeStep if Security is not None else None) is not None else 1)
        if step <= 0:
            step = 1
        minimum = ((Security.MinVolume if Security is not None else None) if (Security.MinVolume if Security is not None else None) is not None else step)
        if volume < minimum:
            # volume = minimum
        rounded = Math.Ceiling(volume / step) * step
        (minimum if return rounded < minimum else rounded)

    def CreateClone(self):
        return vrs_vegas_reversal_strategy()
