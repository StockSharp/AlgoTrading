import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adx_volume_multiplier_strategy(Strategy):
    def __init__(self):
        super(adx_volume_multiplier_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX", "ADX")
        self._adx_threshold = self.Param("AdxThreshold", 20.0) \
            .SetDisplay("ADX Threshold", "Trend strength threshold", "ADX")
        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Period", "Period for volume SMA", "Volume")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(adx_volume_multiplier_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adx_volume_multiplier_strategy, self).OnStarted(time)
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._volume_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, adx_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        adx_typed = adx_value
        adx_ma = adx_typed.MovingAverage
        if adx_ma is None:
            return
        dx = adx_typed.Dx
        di_plus_val = dx.Plus
        di_minus_val = dx.Minus
        if di_plus_val is None or di_minus_val is None:
            return
        adx_v = float(adx_ma)
        di_plus = float(di_plus_val)
        di_minus = float(di_minus_val)
        ema_v = float(ema_value.GetValue[float]())
        close = float(candle.ClosePrice)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        threshold = float(self._adx_threshold.Value)
        above_ema = close > ema_v
        below_ema = close < ema_v
        long_cond = adx_v > threshold and di_plus > di_minus and above_ema
        short_cond = adx_v > threshold and di_minus > di_plus and below_ema
        if long_cond and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif short_cond and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return adx_volume_multiplier_strategy()
