import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class omega_galsky_strategy(Strategy):
    def __init__(self):
        super(omega_galsky_strategy, self).__init__()
        self._ema8_period = self.Param("Ema8Period", 14) \
            .SetGreaterThanZero()
        self._ema21_period = self.Param("Ema21Period", 40) \
            .SetGreaterThanZero()
        self._ema89_period = self.Param("Ema89Period", 89) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._prev_e8 = 0.0
        self._prev_e21 = 0.0
        self._initialized = False
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(omega_galsky_strategy, self).OnReseted()
        self._prev_e8 = 0.0
        self._prev_e21 = 0.0
        self._initialized = False
        self._last_signal_ticks = 0

    def OnStarted(self, time):
        super(omega_galsky_strategy, self).OnStarted(time)
        self._prev_e8 = 0.0
        self._prev_e21 = 0.0
        self._initialized = False
        self._last_signal_ticks = 0
        self._ema8 = ExponentialMovingAverage()
        self._ema8.Length = self._ema8_period.Value
        self._ema21 = ExponentialMovingAverage()
        self._ema21.Length = self._ema21_period.Value
        self._ema89 = ExponentialMovingAverage()
        self._ema89.Length = self._ema89_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema8, self._ema21, self._ema89, self.OnProcess).Start()

    def OnProcess(self, candle, e8, e21, e89):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema8.IsFormed or not self._ema21.IsFormed or not self._ema89.IsFormed:
            return
        e8v = float(e8)
        e21v = float(e21)
        e89v = float(e89)
        close = float(candle.ClosePrice)
        if not self._initialized:
            self._prev_e8 = e8v
            self._prev_e21 = e21v
            self._initialized = True
            return
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks >= cooldown_ticks:
            if self._prev_e8 <= self._prev_e21 and e8v > e21v and close > e89v and self.Position <= 0:
                self.BuyMarket()
                self._last_signal_ticks = current_ticks
            elif self._prev_e8 >= self._prev_e21 and e8v < e21v and close < e89v and self.Position >= 0:
                self.SellMarket()
                self._last_signal_ticks = current_ticks
        self._prev_e8 = e8v
        self._prev_e21 = e21v

    def CreateClone(self):
        return omega_galsky_strategy()
