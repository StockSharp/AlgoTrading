import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class volume_supported_linear_regression_trend_modified_strategy(Strategy):
    def __init__(self):
        super(volume_supported_linear_regression_trend_modified_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "General")
        self._entry_level = self.Param("EntryLevel", 60.0) \
            .SetDisplay("Entry Level", "RSI value to enter long", "General")
        self._exit_level = self.Param("ExitLevel", 45.0) \
            .SetDisplay("Exit Level", "RSI value to close long", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before re-entering", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = None
        self._cooldown_remaining = 0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def entry_level(self):
        return self._entry_level.Value

    @property
    def exit_level(self):
        return self._exit_level.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_supported_linear_regression_trend_modified_strategy, self).OnReseted()
        self._prev_rsi = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(volume_supported_linear_regression_trend_modified_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        self._prev_rsi = None
        self._cooldown_remaining = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self._prev_rsi is None:
            self._prev_rsi = float(rsi)
            return
        previous_rsi = self._prev_rsi
        rsi_val = float(rsi)
        long_entry = previous_rsi <= self.entry_level and rsi_val > self.entry_level
        long_exit = previous_rsi >= self.exit_level and rsi_val < self.exit_level
        if long_exit and self.Position > 0:
            self.SellMarket()
            self._cooldown_remaining = self.signal_cooldown_bars
        elif self._cooldown_remaining == 0 and long_entry and self.Position <= 0:
            self.BuyMarket()
            self._cooldown_remaining = self.signal_cooldown_bars
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return volume_supported_linear_regression_trend_modified_strategy()
