import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class color_ma_rsi_trigger_strategy(Strategy):
    """
    ColorMaRsi Trigger strategy.
    Combines fast and slow EMA crossover with RSI crossover to generate trading signals.
    """

    def __init__(self):
        super(color_ma_rsi_trigger_strategy, self).__init__()
        self._ema_fast_length = self.Param("EmaFastLength", 5) \
            .SetDisplay("Fast EMA", "Fast EMA period", "General")
        self._ema_slow_length = self.Param("EmaSlowLength", 10) \
            .SetDisplay("Slow EMA", "Slow EMA period", "General")
        self._rsi_fast_length = self.Param("RsiFastLength", 3) \
            .SetDisplay("Fast RSI", "Fast RSI period", "General")
        self._rsi_slow_length = self.Param("RsiSlowLength", 13) \
            .SetDisplay("Slow RSI", "Slow RSI period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_signal = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_ma_rsi_trigger_strategy, self).OnReseted()
        self._prev_signal = 0.0

    def OnStarted(self, time):
        super(color_ma_rsi_trigger_strategy, self).OnStarted(time)

        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self._ema_fast_length.Value
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self._ema_slow_length.Value
        rsi_fast = RelativeStrengthIndex()
        rsi_fast.Length = self._rsi_fast_length.Value
        rsi_slow = RelativeStrengthIndex()
        rsi_slow.Length = self._rsi_slow_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_fast, ema_slow, rsi_fast, rsi_slow, self.on_process).Start()

    def on_process(self, candle, ema_fast_val, ema_slow_val, rsi_fast_val, rsi_slow_val):
        if candle.State != CandleStates.Finished:
            return

        signal = 0.0
        if ema_fast_val > ema_slow_val:
            signal += 1
        if ema_fast_val < ema_slow_val:
            signal -= 1
        if rsi_fast_val > rsi_slow_val:
            signal += 1
        if rsi_fast_val < rsi_slow_val:
            signal -= 1

        signal = max(-1.0, min(1.0, signal))

        if self._prev_signal <= 0 and signal > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_signal >= 0 and signal < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_signal = signal

    def CreateClone(self):
        return color_ma_rsi_trigger_strategy()
