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
        self._entry_price = 0.0
        self._spike_size = 0.0
        self._is_long = False

    @property
    def spike_percent(self):
        return self._spike_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vrs_vegas_reversal_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._spike_size = 0.0
        self._is_long = False

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
        upper_spike = float(candle.HighPrice) - max(float(candle.ClosePrice), float(candle.OpenPrice))
        lower_spike = min(float(candle.ClosePrice), float(candle.OpenPrice)) - float(candle.LowPrice)
        close = float(candle.ClosePrice)
        sp = float(self.spike_percent)
        valid_upper = upper_spike >= close * sp
        valid_lower = lower_spike >= close * sp
        valid = (valid_upper and not valid_lower) or (valid_lower and not valid_upper)
        enter_long = valid and valid_lower
        enter_short = valid and valid_upper
        if enter_long and self.Position <= 0:
            self._entry_price = close
            self._spike_size = lower_spike
            self._is_long = True
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif enter_short and self.Position >= 0:
            self._entry_price = close
            self._spike_size = upper_spike
            self._is_long = False
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        if self.Position > 0 and self._is_long:
            target = self._entry_price + self._spike_size * 2.0
            if close >= target:
                self.SellMarket(self.Position)
        elif self.Position < 0 and not self._is_long:
            target = self._entry_price - self._spike_size * 2.0
            if close <= target:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return vrs_vegas_reversal_strategy()
