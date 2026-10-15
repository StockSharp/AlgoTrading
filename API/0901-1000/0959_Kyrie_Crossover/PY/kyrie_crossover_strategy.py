import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class kyrie_crossover_strategy(Strategy):
    def __init__(self):
        super(kyrie_crossover_strategy, self).__init__()
        self._short_ema_period = self.Param("ShortEmaPeriod", 11) \
            .SetGreaterThanZero() \
            .SetDisplay("Short EMA Period", "Period of the short EMA", "EMA Settings")
        self._long_ema_period = self.Param("LongEmaPeriod", 323) \
            .SetGreaterThanZero() \
            .SetDisplay("Long EMA Period", "Period of the long EMA", "EMA Settings")
        self._risk_percent = self.Param("RiskPercent", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk Pct", "Stop loss percentage from entry price", "Risk Management")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 240) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between orders", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_price = 0.0
        self._is_long = False
        self._entries_executed = 0
        self._bars_since_signal = 0
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._was_short_below_long = False
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(kyrie_crossover_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._is_long = False
        self._entries_executed = 0
        self._bars_since_signal = 0
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._was_short_below_long = False
        self._initialized = False

    def OnStarted2(self, time):
        super(kyrie_crossover_strategy, self).OnStarted2(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        short_ema = ExponentialMovingAverage()
        short_ema.Length = self._short_ema_period.Value
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self._long_ema_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ema, long_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, short_val, long_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        sv = float(short_val)
        lv = float(long_val)
        close = float(candle.ClosePrice)
        if not self._initialized:
            self._prev_short = sv
            self._prev_long = lv
            self._was_short_below_long = sv < lv
            self._initialized = True
            return
        is_short_below_long = sv < lv
        if self._was_short_below_long != is_short_below_long and self._entries_executed < self._max_entries.Value and self._bars_since_signal >= self._cooldown_bars.Value:
            if not is_short_below_long and self.Position <= 0:
                self._entry_price = close
                self._is_long = True
                self.BuyMarket()
                self._entries_executed += 1
                self._bars_since_signal = 0
            elif is_short_below_long and self.Position >= 0:
                self._entry_price = close
                self._is_long = False
                self.SellMarket()
                self._entries_executed += 1
                self._bars_since_signal = 0
            self._was_short_below_long = is_short_below_long
        if self.Position != 0 and self._entry_price != 0.0:
            self._check_stop_loss(close)
        self._prev_short = sv
        self._prev_long = lv

    def _check_stop_loss(self, current_price):
        threshold = float(self._risk_percent.Value) / 100.0
        if self._is_long and self.Position > 0:
            stop_price = self._entry_price * (1.0 - threshold)
            if current_price <= stop_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._bars_since_signal = 0
        elif not self._is_long and self.Position < 0:
            stop_price = self._entry_price * (1.0 + threshold)
            if current_price >= stop_price:
                self.BuyMarket()
                self._entry_price = 0.0
                self._bars_since_signal = 0

    def CreateClone(self):
        return kyrie_crossover_strategy()
