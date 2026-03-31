import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA
from StockSharp.Algo.Strategies import Strategy


class medico_action_zone_self_adjust_tf_version_2_strategy(Strategy):
    def __init__(self):
        super(medico_action_zone_self_adjust_tf_version_2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._higher_candle_type = self.Param("HigherCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Higher Candle Type", "EMA calculation timeframe", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 12) \
            .SetDisplay("Fast EMA Length", "Short EMA period", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 26) \
            .SetDisplay("Slow EMA Length", "Long EMA period", "Indicators")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "Indicators")

        self._fast_ema_htf_value = 0.0
        self._slow_ema_htf_value = 0.0
        self._close_htf = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def higher_candle_type(self):
        return self._higher_candle_type.Value

    @property
    def fast_ema_length(self):
        return self._fast_ema_length.Value

    @property
    def slow_ema_length(self):
        return self._slow_ema_length.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(medico_action_zone_self_adjust_tf_version_2_strategy, self).OnReseted()
        self._fast_ema_htf_value = 0.0
        self._slow_ema_htf_value = 0.0
        self._close_htf = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(medico_action_zone_self_adjust_tf_version_2_strategy, self).OnStarted2(time)
        self.StartProtection(None, None)

        self._fast_ema_cur = EMA()
        self._fast_ema_cur.Length = self.fast_ema_length
        self._slow_ema_cur = EMA()
        self._slow_ema_cur.Length = self.slow_ema_length
        self._fast_ema_htf = EMA()
        self._fast_ema_htf.Length = self.fast_ema_length
        self._slow_ema_htf = EMA()
        self._slow_ema_htf.Length = self.slow_ema_length
        self._bars_from_signal = int(self.signal_cooldown_bars)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema_cur, self._slow_ema_cur, self._process_candle).Start()

        if self.higher_candle_type != self.candle_type:
            higher_sub = self.SubscribeCandles(self.higher_candle_type)
            higher_sub.Bind(self._fast_ema_htf, self._slow_ema_htf, self._process_higher).Start()

    def _process_higher(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        self._fast_ema_htf_value = float(fast)
        self._slow_ema_htf_value = float(slow)
        self._close_htf = float(candle.ClosePrice)

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        same_tf = (self.higher_candle_type == self.candle_type)

        if same_tf:
            if not self._fast_ema_cur.IsFormed or not self._slow_ema_cur.IsFormed:
                return
        else:
            if not self._fast_ema_htf.IsFormed or not self._slow_ema_htf.IsFormed:
                return

        ema_fast = float(fast) if same_tf else self._fast_ema_htf_value
        ema_slow = float(slow) if same_tf else self._slow_ema_htf_value
        close_htf = float(candle.ClosePrice) if same_tf else self._close_htf

        buy_signal = self._prev_fast <= self._prev_slow and ema_fast > ema_slow and close_htf > ema_fast
        sell_signal = self._prev_fast >= self._prev_slow and ema_fast < ema_slow and close_htf < ema_slow
        self._bars_from_signal += 1

        if self._bars_from_signal >= int(self.signal_cooldown_bars) and buy_signal and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= int(self.signal_cooldown_bars) and sell_signal and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

        self._prev_fast = ema_fast
        self._prev_slow = ema_slow

    def CreateClone(self):
        return medico_action_zone_self_adjust_tf_version_2_strategy()
