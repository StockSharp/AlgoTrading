import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Lowest, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class larry_conners_smtp_strategy(Strategy):
    def __init__(self):
        super(larry_conners_smtp_strategy, self).__init__()
        self._tick_size = self.Param("TickSize", 0.01) \
            .SetGreaterThanZero() \
            .SetDisplay("Tick Size", "Minimum price increment", "General")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(larry_conners_smtp_strategy, self).OnReseted()
        self._stop_loss = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(larry_conners_smtp_strategy, self).OnStarted2(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        lowest = Lowest()
        lowest.Length = 10
        dummy_ema = ExponentialMovingAverage()
        dummy_ema.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(lowest, dummy_ema, self.OnProcess).Start()

    def OnProcess(self, candle, low10_val, dummy_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        low = float(candle.LowPrice)
        low10 = float(low10_val)
        tick = float(self._tick_size.Value)
        is_10_period_low = low <= low10 + tick
        buy_cond = is_10_period_low and close > open_p
        if buy_cond and self.Position == 0 and self._entries_executed < self._max_entries.Value and self._bars_since_signal >= self._cooldown_bars.Value:
            self._stop_loss = low
            self.BuyMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0
        if self.Position > 0:
            if low > self._stop_loss:
                self._stop_loss = low
            if close <= self._stop_loss:
                self.SellMarket()
                self._bars_since_signal = 0

    def CreateClone(self):
        return larry_conners_smtp_strategy()
