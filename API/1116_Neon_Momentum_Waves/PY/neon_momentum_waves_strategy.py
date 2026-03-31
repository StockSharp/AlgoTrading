import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class neon_momentum_waves_strategy(Strategy):
    def __init__(self):
        super(neon_momentum_waves_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast Length", "MACD fast EMA length", "MACD")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow Length", "MACD slow EMA length", "MACD")
        self._signal_length = self.Param("SignalLength", 20) \
            .SetDisplay("Signal Length", "MACD signal smoothing", "MACD")
        self._entry_level = self.Param("EntryLevel", 0.0) \
            .SetDisplay("Entry Level", "Histogram entry threshold", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_hist = None
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(neon_momentum_waves_strategy, self).OnReseted()
        self._prev_hist = None
        self._last_signal_ticks = 0

    def OnStarted2(self, time):
        super(neon_momentum_waves_strategy, self).OnStarted2(time)
        self._prev_hist = None
        self._last_signal_ticks = 0
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self._fast_length.Value
        self._macd.Macd.LongMa.Length = self._slow_length.Value
        self._macd.SignalMa.Length = self._signal_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd, self.OnProcess).Start()

    def OnProcess(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._macd.IsFormed:
            return
        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return
        hist = float(macd_line) - float(signal_line)
        if self._prev_hist is None:
            self._prev_hist = hist
            return
        entry = float(self._entry_level.Value)
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks >= cooldown_ticks:
            if self._prev_hist <= entry and hist > entry and self.Position <= 0:
                self.BuyMarket()
                self._last_signal_ticks = current_ticks
            elif self._prev_hist >= entry and hist < entry and self.Position > 0:
                self.SellMarket()
                self._last_signal_ticks = current_ticks
        self._prev_hist = hist

    def CreateClone(self):
        return neon_momentum_waves_strategy()
