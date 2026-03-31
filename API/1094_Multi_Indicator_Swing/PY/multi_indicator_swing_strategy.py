import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class multi_indicator_swing_strategy(Strategy):
    def __init__(self):
        super(multi_indicator_swing_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._sma_length = self.Param("SmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "SMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 45.0) \
            .SetDisplay("RSI Oversold", "RSI oversold", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 55.0) \
            .SetDisplay("RSI Overbought", "RSI overbought", "Indicators")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait between new entries", "Trading")
        self._prev_close = 0.0
        self._prev_sma = 0.0
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
        super(multi_indicator_swing_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(multi_indicator_swing_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._prev_rsi = 0.0
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
        if self._has_prev:
            os_level = float(self._rsi_oversold.Value)
            ob_level = float(self._rsi_overbought.Value)
            trend_up = close > sv and sv > self._prev_sma
            trend_down = close < sv and sv < self._prev_sma
            cross_above_sma = self._prev_close <= self._prev_sma and close > sv
            cross_below_sma = self._prev_close >= self._prev_sma and close < sv
            long_entry = trend_up and cross_above_sma and self._prev_rsi <= os_level and rv > os_level
            short_entry = trend_down and cross_below_sma and self._prev_rsi >= ob_level and rv < ob_level
            if self._cooldown_remaining == 0:
                if long_entry and self.Position <= 0:
                    self.BuyMarket()
                    self._cooldown_remaining = self._signal_cooldown_bars.Value
                elif short_entry and self.Position >= 0:
                    self.SellMarket()
                    self._cooldown_remaining = self._signal_cooldown_bars.Value
        self._prev_close = close
        self._prev_sma = sv
        self._prev_rsi = rv
        self._has_prev = True

    def CreateClone(self):
        return multi_indicator_swing_strategy()
