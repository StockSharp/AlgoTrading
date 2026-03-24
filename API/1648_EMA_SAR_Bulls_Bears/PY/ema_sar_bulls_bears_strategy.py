import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ParabolicSar, BearPower, BullPower
from StockSharp.Algo.Strategies import Strategy


class ema_sar_bulls_bears_strategy(Strategy):
    def __init__(self):
        super(ema_sar_bulls_bears_strategy, self).__init__()
        self._short_ema_period = self.Param("ShortEmaPeriod", 3) \
            .SetDisplay("Short EMA", "Short EMA period", "Indicators")
        self._long_ema_period = self.Param("LongEmaPeriod", 34) \
            .SetDisplay("Long EMA", "Long EMA period", "Indicators")
        self._bears_bulls_period = self.Param("BearsBullsPeriod", 13) \
            .SetDisplay("Bulls/Bears Period", "Period for Bulls and Bears Power", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series type", "General")
        self._prev_bears = 0.0
        self._prev_bulls = 0.0
        self._has_prev = False

    @property
    def short_ema_period(self):
        return self._short_ema_period.Value

    @property
    def long_ema_period(self):
        return self._long_ema_period.Value

    @property
    def bears_bulls_period(self):
        return self._bears_bulls_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_sar_bulls_bears_strategy, self).OnReseted()
        self._prev_bears = 0.0
        self._prev_bulls = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ema_sar_bulls_bears_strategy, self).OnStarted(time)
        short_ema = ExponentialMovingAverage()
        short_ema.Length = self.short_ema_period
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self.long_ema_period
        sar = ParabolicSar()
        bears_power = BearPower()
        bears_power.Length = self.bears_bulls_period
        bulls_power = BullPower()
        bulls_power.Length = self.bears_bulls_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ema, long_ema, sar, bears_power, bulls_power, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, short_ema, long_ema, sar_value, bears_power, bulls_power):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_bears = bears_power
            self._prev_bulls = bulls_power
            self._has_prev = True
            return
        short_signal = (short_ema < long_ema and sar_value > candle.HighPrice and bears_power < 0 and
            bears_power > self._prev_bears)
        long_signal = (short_ema > long_ema and sar_value < candle.LowPrice and bulls_power > 0 and
            bulls_power < self._prev_bulls)
        if short_signal and self.Position >= 0:
            self.SellMarket()
        elif long_signal and self.Position <= 0:
            self.BuyMarket()
        self._prev_bears = bears_power
        self._prev_bulls = bulls_power

    def CreateClone(self):
        return ema_sar_bulls_bears_strategy()
