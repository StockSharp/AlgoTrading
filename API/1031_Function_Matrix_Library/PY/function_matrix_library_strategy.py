import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class function_matrix_library_strategy(Strategy):
    """
    Function Matrix Library: two-factor SMA model strategy.
    Weighted combination of fast/slow SMA vs close price.
    Enters when model edge exceeds threshold.
    """

    def __init__(self):
        super(function_matrix_library_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast Length", "Fast SMA length", "General")
        self._slow_length = self.Param("SlowLength", 48) \
            .SetDisplay("Slow Length", "Slow SMA length", "General")
        self._entry_threshold_percent = self.Param("EntryThresholdPercent", 0.25) \
            .SetDisplay("Entry Threshold %", "Required model edge in percent", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")

        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(function_matrix_library_strategy, self).OnReseted()
        self._bars_from_signal = 0

    def OnStarted(self, time):
        super(function_matrix_library_strategy, self).OnStarted(time)

        self._bars_from_signal = self._signal_cooldown_bars.Value

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self._fast_length.Value
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self._slow_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        if close <= 0:
            return

        fast = float(fast_value)
        slow = float(slow_value)

        self._bars_from_signal += 1
        if self._bars_from_signal < self._signal_cooldown_bars.Value:
            return

        model_price = (2.0 * fast + slow) / 3.0
        edge_percent = (model_price - close) / close * 100.0
        threshold = self._entry_threshold_percent.Value

        if edge_percent >= threshold and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif edge_percent <= -threshold and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return function_matrix_library_strategy()
