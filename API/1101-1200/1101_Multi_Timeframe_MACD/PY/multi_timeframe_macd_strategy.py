import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class _macd_frame:
    def __init__(self, fast_length, slow_length, signal_length):
        self.fast_length = fast_length
        self.slow_length = slow_length
        self.signal_length = signal_length
        self.fast = None
        self.slow = None
        self.signal = None
        self.macd = 0.0
        self.count = 0

    @staticmethod
    def ema(previous, value, length):
        if previous is None:
            return value
        alpha = 2.0 / (length + 1.0)
        return previous + alpha * (value - previous)

    def process(self, price):
        self.count += 1
        self.fast = self.ema(self.fast, price, self.fast_length)
        self.slow = self.ema(self.slow, price, self.slow_length)
        self.macd = self.fast - self.slow
        self.signal = self.ema(self.signal, self.macd, self.signal_length)

    def ready(self):
        return self.count >= self.slow_length + self.signal_length

    def direction(self, mode):
        if not self.ready():
            return 0
        value = self.macd - self.signal if mode.lower() == "crossover" else self.macd
        return 1 if value > 0 else (-1 if value < 0 else 0)


class multi_timeframe_macd_strategy(Strategy):
    def __init__(self):
        super(multi_timeframe_macd_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 12).SetGreaterThanZero()
        self._slow_length = self.Param("SlowLength", 26).SetGreaterThanZero()
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._higher_candle_type = self.Param("HigherCandleType", DataType.TimeFrame(TimeSpan.FromDays(1)))
        self._show_current = self.Param("ShowCurrentTimeframe", True)
        self._show_higher = self.Param("ShowHigherTimeframe", True)
        self._entry = self.Param("Entry", "Crossover")
        self._use_trailing = self.Param("UseTrailingStop", False)
        self._trailing_percent = self.Param("TrailingStopPercent", 2.0).SetGreaterThanZero()

        self._current = None
        self._higher = None
        self._last_combined = 0
        self._best_price = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, self._higher_candle_type.Value)]

    def OnReseted(self):
        super(multi_timeframe_macd_strategy, self).OnReseted()
        self._current = None
        self._higher = None
        self._last_combined = 0
        self._best_price = None

    def OnStarted2(self, time):
        super(multi_timeframe_macd_strategy, self).OnStarted2(time)

        fast = int(self._fast_length.Value)
        slow = int(self._slow_length.Value)
        signal = int(self._signal_length.Value)
        self._current = _macd_frame(fast, slow, signal)
        self._higher = _macd_frame(fast, slow, signal)

        def on_current(candle):
            if candle.State != CandleStates.Finished:
                return
            self._current.process(float(candle.ClosePrice))
            if self._apply_trailing(candle):
                return
            self._evaluate()

        def on_higher(candle):
            if candle.State != CandleStates.Finished:
                return
            self._higher.process(float(candle.ClosePrice))
            self._evaluate()

        self.SubscribeCandles(self._candle_type.Value).Bind(on_current).Start()
        self.SubscribeCandles(self._higher_candle_type.Value).Bind(on_higher).Start()

    def _evaluate(self):
        if not self._current.ready() or not self._higher.ready():
            return

        mode = str(self._entry.Value)
        current = self._current.direction(mode)
        higher = self._higher.direction(mode)
        combined = current if current != 0 and current == higher else 0

        if combined == 0:
            self._last_combined = 0
            return

        if combined == self._last_combined:
            return

        if combined > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._best_price = None
        elif combined < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._best_price = None

        self._last_combined = combined

    def _apply_trailing(self, candle):
        if not bool(self._use_trailing.Value) or self.Position == 0:
            return False

        pct = float(self._trailing_percent.Value) / 100.0
        if self.Position > 0:
            high = float(candle.HighPrice)
            self._best_price = high if self._best_price is None else max(self._best_price, high)
            stop = self._best_price * (1.0 - pct)
            if float(candle.LowPrice) <= stop:
                self.SellMarket(Math.Abs(self.Position))
                self._best_price = None
                self._last_combined = 0
                return True
        else:
            low = float(candle.LowPrice)
            self._best_price = low if self._best_price is None else min(self._best_price, low)
            stop = self._best_price * (1.0 + pct)
            if float(candle.HighPrice) >= stop:
                self.BuyMarket(Math.Abs(self.Position))
                self._best_price = None
                self._last_combined = 0
                return True

        return False

    def CreateClone(self):
        return multi_timeframe_macd_strategy()
