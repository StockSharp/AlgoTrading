import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class multi_confluence_swing_hunter_v1_strategy(Strategy):
    def __init__(self):
        super(multi_confluence_swing_hunter_v1_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI calculation length", "Indicators")
        self._sma_length = self.Param("SmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "SMA period", "Indicators")
        self._min_entry_score = self.Param("MinEntryScore", 4) \
            .SetDisplay("Min Entry Score", "Minimum entry score", "Entry")
        self._min_exit_score = self.Param("MinExitScore", 3) \
            .SetDisplay("Min Exit Score", "Minimum exit score", "Exit")
        self._rsi_oversold = self.Param("RsiOversold", 35.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 48) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait after an entry", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_confluence_swing_hunter_v1_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(multi_confluence_swing_hunter_v1_strategy, self).OnStarted2(time)
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._sma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._sma, self.OnProcess).Start()

    def OnProcess(self, candle, rsi_value, sma_value):
        if candle.State != CandleStates.Finished:
            return
        rv = float(rsi_value)
        sv = float(sma_value)
        if not self._has_prev:
            self._prev_rsi = rv
            self._has_prev = True
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        os_level = float(self._rsi_oversold.Value)
        ob_level = float(self._rsi_overbought.Value)
        entry_score = 0
        if rv < os_level:
            entry_score += 2
        if rv > self._prev_rsi:
            entry_score += 1
        if close > sv:
            entry_score += 1
        if close > opn:
            entry_score += 1
        exit_score = 0
        if rv > ob_level:
            exit_score += 2
        if rv < self._prev_rsi:
            exit_score += 1
        if close < sv:
            exit_score += 1
        if close < opn:
            exit_score += 1
        if self.Position > 0 and exit_score >= self._min_exit_score.Value:
            self.SellMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        elif self.Position == 0 and self._cooldown_remaining == 0 and entry_score >= self._min_entry_score.Value:
            self.BuyMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        self._prev_rsi = rv

    def CreateClone(self):
        return multi_confluence_swing_hunter_v1_strategy()
