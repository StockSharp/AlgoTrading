import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class moc_delta_moo_entry_v2_reverse_strategy(Strategy):
    def __init__(self):
        super(moc_delta_moo_entry_v2_reverse_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._delta_window = self.Param("DeltaWindow", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("Delta Window", "Bars per delta calculation window", "General")
        self._delta_threshold_percent = self.Param("DeltaThresholdPercent", 12.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Delta Threshold %", "Minimum delta percent for reversal", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 16) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._window_buy_volume = 0.0
        self._window_sell_volume = 0.0
        self._window_bar_count = 0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(moc_delta_moo_entry_v2_reverse_strategy, self).OnReseted()
        self._window_buy_volume = 0.0
        self._window_sell_volume = 0.0
        self._window_bar_count = 0
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(moc_delta_moo_entry_v2_reverse_strategy, self).OnStarted2(time)
        self._window_buy_volume = 0.0
        self._window_sell_volume = 0.0
        self._window_bar_count = 0
        self._bars_from_signal = self._signal_cooldown_bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        vol = float(candle.TotalVolume)
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        if close > opn:
            self._window_buy_volume += vol
        elif close < opn:
            self._window_sell_volume += vol
        else:
            self._window_buy_volume += vol * 0.5
            self._window_sell_volume += vol * 0.5
        self._window_bar_count += 1
        self._bars_from_signal += 1
        if self._window_bar_count < self._delta_window.Value:
            return
        total_volume = self._window_buy_volume + self._window_sell_volume
        delta_percent = (self._window_buy_volume - self._window_sell_volume) / total_volume * 100.0 if total_volume > 0.0 else 0.0
        reverse_signal = 0
        thr = float(self._delta_threshold_percent.Value)
        if delta_percent > thr:
            reverse_signal = -1
        elif delta_percent < -thr:
            reverse_signal = 1
        if self._bars_from_signal >= self._signal_cooldown_bars.Value and reverse_signal != 0:
            if reverse_signal > 0 and self.Position <= 0:
                self.BuyMarket()
                self._bars_from_signal = 0
            elif reverse_signal < 0 and self.Position >= 0:
                self.SellMarket()
                self._bars_from_signal = 0
        self._window_buy_volume = 0.0
        self._window_sell_volume = 0.0
        self._window_bar_count = 0

    def CreateClone(self):
        return moc_delta_moo_entry_v2_reverse_strategy()
