import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class long_ema_advanced_exit_strategy(Strategy):
    """Triple EMA crossover with long EMA trend filter."""

    def __init__(self):
        super(long_ema_advanced_exit_strategy, self).__init__()
        self._short_period = self.Param("ShortPeriod", 10).SetDisplay("Short EMA", "Short EMA period", "Indicators")
        self._mid_period = self.Param("MidPeriod", 20).SetDisplay("Mid EMA", "Mid EMA period", "Indicators")
        self._long_period = self.Param("LongPeriod", 40).SetDisplay("Long EMA", "Long EMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 38).SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")
        self._prev_short = 0.0
        self._prev_mid = 0.0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(long_ema_advanced_exit_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_mid = 0.0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(long_ema_advanced_exit_strategy, self).OnStarted(time)
        ema_short = ExponentialMovingAverage()
        ema_short.Length = self._short_period.Value
        ema_mid = ExponentialMovingAverage()
        ema_mid.Length = self._mid_period.Value
        ema_long = ExponentialMovingAverage()
        ema_long.Length = self._long_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_short, ema_mid, ema_long, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_mid)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_val, mid_val, long_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        sv = float(short_val)
        mv = float(mid_val)
        lv = float(long_val)
        if self._prev_short == 0 or self._prev_mid == 0:
            self._prev_short = sv
            self._prev_mid = mv
            return
        if self._bars_since_signal < self._cooldown_bars.Value:
            self._prev_short = sv
            self._prev_mid = mv
            return
        close = float(candle.ClosePrice)
        cross_up = self._prev_short <= self._prev_mid and sv > mv
        cross_down = self._prev_short >= self._prev_mid and sv < mv
        if cross_up and close > lv and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_signal = 0
        elif cross_down and close < lv and self.Position >= 0:
            self.SellMarket()
            self._bars_since_signal = 0
        self._prev_short = sv
        self._prev_mid = mv

    def CreateClone(self):
        return long_ema_advanced_exit_strategy()
