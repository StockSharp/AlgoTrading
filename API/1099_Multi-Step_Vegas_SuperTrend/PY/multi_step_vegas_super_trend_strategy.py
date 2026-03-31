import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class multi_step_vegas_super_trend_strategy(Strategy):
    def __init__(self):
        super(multi_step_vegas_super_trend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Timeframe", "Parameters")
        self._sma_length = self.Param("SmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "SMA period", "Parameters")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Parameters")
        self._entry_rsi_level = self.Param("EntryRsiLevel", 55.0) \
            .SetDisplay("Entry RSI", "RSI threshold for long entries", "Parameters")
        self._exit_rsi_level = self.Param("ExitRsiLevel", 45.0) \
            .SetDisplay("Exit RSI", "RSI threshold for exits", "Parameters")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 3) \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new entry", "Parameters")
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_step_vegas_super_trend_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(multi_step_vegas_super_trend_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._sma_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, sma_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        sv = float(sma_val)
        rv = float(rsi_val)
        close = float(candle.ClosePrice)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if not self._sma.IsFormed or not self._rsi.IsFormed or not self._has_prev:
            self._prev_close = close
            self._prev_sma = sv
            self._has_prev = True
            return
        entry_rsi = float(self._entry_rsi_level.Value)
        exit_rsi = float(self._exit_rsi_level.Value)
        long_entry = self._cooldown_remaining == 0 and self._prev_close <= self._prev_sma and close > sv and rv >= entry_rsi and self.Position <= 0
        long_exit = self.Position > 0 and (close < sv or rv <= exit_rsi)
        if long_exit:
            self.SellMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        elif long_entry:
            self.BuyMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        self._prev_close = close
        self._prev_sma = sv

    def CreateClone(self):
        return multi_step_vegas_super_trend_strategy()
