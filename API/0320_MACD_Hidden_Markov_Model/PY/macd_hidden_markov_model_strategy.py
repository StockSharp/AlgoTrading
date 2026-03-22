import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_hidden_markov_model_strategy(Strategy):
    """
    MACD strategy with Hidden Markov Model for state detection.
    """

    BULLISH = 0
    NEUTRAL = 1
    BEARISH = 2

    def __init__(self):
        super(macd_hidden_markov_model_strategy, self).__init__()

        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")

        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")

        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal Period", "Signal EMA period for MACD", "Indicators")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._hmm_history_length = self.Param("HmmHistoryLength", 100) \
            .SetDisplay("HMM History Length", "Length of history for Hidden Markov Model", "HMM Parameters")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait between position changes", "Trading")

        self._current_state = macd_hidden_markov_model_strategy.NEUTRAL
        self._price_changes = []
        self._volumes = []
        self._prev_price = 0.0
        self._prev_macd = None
        self._prev_signal = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_hidden_markov_model_strategy, self).OnReseted()
        self._current_state = macd_hidden_markov_model_strategy.NEUTRAL
        self._prev_price = 0.0
        self._prev_macd = None
        self._prev_signal = None
        self._cooldown_remaining = 0
        self._price_changes = []
        self._volumes = []

    def OnStarted(self, time):
        super(macd_hidden_markov_model_strategy, self).OnStarted(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = int(self._macd_fast.Value)
        macd.Macd.LongMa.Length = int(self._macd_slow.Value)
        macd.SignalMa.Length = int(self._macd_signal.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._update_hmm_data(candle)
        self._calculate_market_state()

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        macd_val = macd_value.Macd
        signal_val = macd_value.Signal

        if macd_val is None or signal_val is None:
            return

        macd_f = float(macd_val)
        signal_f = float(signal_val)

        if self._prev_macd is None or self._prev_signal is None:
            self._prev_macd = macd_f
            self._prev_signal = signal_f
            return

        cross_up = self._prev_macd <= self._prev_signal and macd_f > signal_f
        cross_down = self._prev_macd >= self._prev_signal and macd_f < signal_f

        long_exit = self.Position > 0 and (self._current_state == macd_hidden_markov_model_strategy.BEARISH or cross_down)
        short_exit = self.Position < 0 and (self._current_state == macd_hidden_markov_model_strategy.BULLISH or cross_up)

        cd = int(self._signal_cooldown_bars.Value)

        if long_exit:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cd
        elif short_exit:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cd
        elif self._cooldown_remaining == 0 and cross_up and self._current_state == macd_hidden_markov_model_strategy.BULLISH and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._cooldown_remaining = cd
        elif self._cooldown_remaining == 0 and cross_down and self._current_state == macd_hidden_markov_model_strategy.BEARISH and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._cooldown_remaining = cd

        self._prev_macd = macd_f
        self._prev_signal = signal_f

    def _update_hmm_data(self, candle):
        close_price = float(candle.ClosePrice)
        if self._prev_price > 0:
            price_change = close_price - self._prev_price
            self._price_changes.append(price_change)
            self._volumes.append(float(candle.TotalVolume))

            hmm_len = int(self._hmm_history_length.Value)
            while len(self._price_changes) > hmm_len:
                self._price_changes.pop(0)
                self._volumes.pop(0)

        self._prev_price = close_price

    def _calculate_market_state(self):
        if len(self._price_changes) < 10:
            return

        start_index = max(0, len(self._price_changes) - 10)
        positive_changes = 0
        negative_changes = 0

        for i in range(start_index, len(self._price_changes)):
            if self._price_changes[i] > 0:
                positive_changes += 1
            elif self._price_changes[i] < 0:
                negative_changes += 1

        up_volume = 0.0
        down_volume = 0.0
        up_count = 0
        down_count = 0

        for i in range(start_index, len(self._price_changes)):
            if self._price_changes[i] > 0:
                up_volume += self._volumes[i]
                up_count += 1
            elif self._price_changes[i] < 0:
                down_volume += self._volumes[i]
                down_count += 1

        up_volume = up_volume / up_count if up_count > 0 else 0.0
        down_volume = down_volume / down_count if down_count > 0 else 0.0

        if positive_changes >= 7 or (positive_changes >= 6 and up_volume > down_volume * 1.5):
            self._current_state = macd_hidden_markov_model_strategy.BULLISH
        elif negative_changes >= 7 or (negative_changes >= 6 and down_volume > up_volume * 1.5):
            self._current_state = macd_hidden_markov_model_strategy.BEARISH
        else:
            self._current_state = macd_hidden_markov_model_strategy.NEUTRAL

    def CreateClone(self):
        return macd_hidden_markov_model_strategy()
