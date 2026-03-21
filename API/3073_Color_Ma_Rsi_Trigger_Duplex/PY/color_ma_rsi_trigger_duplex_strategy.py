import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class color_ma_rsi_trigger_duplex_strategy(Strategy):
    def __init__(self):
        super(color_ma_rsi_trigger_duplex_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 5) \
            .SetDisplay("Fast MA Period", "Fast moving average length", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 10) \
            .SetDisplay("Slow MA Period", "Slow moving average length", "Indicators")
        self._fast_rsi_period = self.Param("FastRsiPeriod", 3) \
            .SetDisplay("Fast RSI Period", "Fast RSI length", "Indicators")
        self._slow_rsi_period = self.Param("SlowRsiPeriod", 13) \
            .SetDisplay("Slow RSI Period", "Slow RSI length", "Indicators")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "History shift for signal evaluation", "Strategy")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetDisplay("Signal Cooldown", "Bars to wait between reversals", "Strategy")

        self._color_history = []
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value
    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value
    @property
    def FastRsiPeriod(self):
        return self._fast_rsi_period.Value
    @property
    def SlowRsiPeriod(self):
        return self._slow_rsi_period.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(color_ma_rsi_trigger_duplex_strategy, self).OnReseted()
        self._color_history = []
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(color_ma_rsi_trigger_duplex_strategy, self).OnStarted(time)
        self._color_history = []
        self._cooldown_remaining = 0

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowMaPeriod
        fast_rsi = RelativeStrengthIndex()
        fast_rsi.Length = self.FastRsiPeriod
        slow_rsi = RelativeStrengthIndex()
        slow_rsi.Length = self.SlowRsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, fast_rsi, slow_rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_ma_val, slow_ma_val, fast_rsi_val, slow_rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        fmv = float(fast_ma_val)
        smv = float(slow_ma_val)
        frv = float(fast_rsi_val)
        srv = float(slow_rsi_val)

        score = 0.0
        if fmv > smv:
            score = 1.0
        elif fmv < smv:
            score = -1.0

        if frv > srv:
            score += 1.0
        elif frv < srv:
            score -= 1.0

        if score > 1.0:
            score = 1.0
        elif score < -1.0:
            score = -1.0

        self._color_history.insert(0, score)
        max_history = max(2, self.SignalBar + 2)
        while len(self._color_history) > max_history:
            self._color_history.pop()

        if len(self._color_history) <= self.SignalBar + 1:
            return

        recent = self._color_history[self.SignalBar]
        older = self._color_history[self.SignalBar + 1]

        long_open = older == -1.0 and recent == 1.0
        short_open = older == 1.0 and recent == -1.0
        long_exit = self.Position > 0 and recent < 0.0
        short_exit = self.Position < 0 and recent > 0.0

        if long_exit:
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif short_exit:
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and long_open and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and short_open and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars

    def CreateClone(self):
        return color_ma_rsi_trigger_duplex_strategy()
