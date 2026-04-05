import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class multi_factor_strategy(Strategy):
    def __init__(self):
        super(multi_factor_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "MACD fast EMA length", "MACD")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "MACD slow EMA length", "MACD")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "MACD signal EMA length", "MACD")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait after entries and exits", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_diff = 0.0
        self._has_prev_diff = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_factor_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._has_prev_diff = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(multi_factor_strategy, self).OnStarted2(time)
        self._prev_diff = 0.0
        self._has_prev_diff = False
        self._cooldown_remaining = 0
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self._fast_length.Value
        self._macd.Macd.LongMa.Length = self._slow_length.Value
        self._macd.SignalMa.Length = self._signal_length.Value
        self._sma50 = SimpleMovingAverage()
        self._sma50.Length = 50
        self._dummy = SimpleMovingAverage()
        self._dummy.Length = 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._dummy, self.OnProcess).Start()

    def OnProcess(self, candle, dummy_value):
        if candle.State != CandleStates.Finished:
            return
        macd_result = process_float(self._macd, candle.ClosePrice, candle.ServerTime, True)
        sma50_result = process_float(self._sma50, candle.ClosePrice, candle.ServerTime, True)
        if not self._macd.IsFormed or not self._sma50.IsFormed:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        macd_line = macd_result.Macd
        signal_line = macd_result.Signal
        if macd_line is None or signal_line is None:
            return
        diff = float(macd_line) - float(signal_line)
        sma50 = float(sma50_result)
        close = float(candle.ClosePrice)
        if not self._has_prev_diff:
            self._prev_diff = diff
            self._has_prev_diff = True
            return
        if self._cooldown_remaining == 0 and self.Position == 0:
            bullish_cross = self._prev_diff <= 0.0 and diff > 0.0
            bearish_cross = self._prev_diff >= 0.0 and diff < 0.0
            if bullish_cross and close > sma50:
                self.BuyMarket()
                self._cooldown_remaining = self._signal_cooldown_bars.Value
            elif bearish_cross and close < sma50:
                self.SellMarket()
                self._cooldown_remaining = self._signal_cooldown_bars.Value
        self._prev_diff = diff

    def CreateClone(self):
        return multi_factor_strategy()
