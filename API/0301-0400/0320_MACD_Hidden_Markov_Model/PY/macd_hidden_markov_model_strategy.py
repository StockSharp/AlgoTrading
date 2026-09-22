import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class macd_hidden_markov_model_strategy(Strategy):
    """
    MACD strategy with Hidden Markov Model for state detection.
    """

    # Hidden Markov Model states, listed in the order used by the model tables below.
    BULLISH = 0
    NEUTRAL = 1
    BEARISH = 2

    # Typical move of every state measured in average ranges: the bullish state rises by one
    # average range, the bearish state falls by one and the neutral state goes nowhere.
    STATE_MEANS = [1.0, 0.0, -1.0]

    # Transition matrix of the hidden chain. States are sticky, and a jump from bullish
    # straight to bearish is far less likely than a stop in the neutral state.
    TRANSITIONS = [
        [0.80, 0.15, 0.05],
        [0.15, 0.70, 0.15],
        [0.05, 0.15, 0.80],
    ]

    def __init__(self):
        super(macd_hidden_markov_model_strategy, self).__init__()

        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")

        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")

        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal Period", "Signal EMA period for MACD", "Indicators")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._hmm_history_length = self.Param("HmmHistoryLength", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("HMM History Length", "Number of observations the model is estimated on", "HMM Parameters")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period used to measure the stop distance", "Protection")

        self._atr_stop_multiplier = self.Param("AtrStopMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Stop Multiplier", "Stop distance in ATR multiples", "Protection")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait between position changes", "Trading")

        self._current_state = macd_hidden_markov_model_strategy.NEUTRAL
        self._price_changes = []
        self._prev_price = 0.0
        self._prev_macd = None
        self._prev_signal = None
        self._stop_price = None
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
        self._stop_price = None
        self._cooldown_remaining = 0
        self._price_changes = []

    def OnStarted2(self, time):
        super(macd_hidden_markov_model_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = int(self._macd_fast.Value)
        macd.Macd.LongMa.Length = int(self._macd_slow.Value)
        macd.SignalMa.Length = int(self._macd_signal.Value)

        # ATR measures the current range and sets how far the protective stop sits from the entry
        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value, atr_value):
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

        # Stop distance follows volatility: the wider the average range, the wider the stop.
        stop_distance = float(atr_value) * float(self._atr_stop_multiplier.Value)

        close_price = float(candle.ClosePrice)
        low_price = float(candle.LowPrice)
        high_price = float(candle.HighPrice)

        cross_up = self._prev_macd <= self._prev_signal and macd_f > signal_f
        cross_down = self._prev_macd >= self._prev_signal and macd_f < signal_f

        long_stop = self.Position > 0 and self._stop_price is not None and low_price <= self._stop_price
        short_stop = self.Position < 0 and self._stop_price is not None and high_price >= self._stop_price

        long_exit = self.Position > 0 and (self._current_state == macd_hidden_markov_model_strategy.BEARISH or cross_down)
        short_exit = self.Position < 0 and (self._current_state == macd_hidden_markov_model_strategy.BULLISH or cross_up)

        cd = int(self._signal_cooldown_bars.Value)

        if long_stop or long_exit:
            self.SellMarket(self.Position)
            self._stop_price = None
            self._cooldown_remaining = cd
        elif short_stop or short_exit:
            self.BuyMarket(Math.Abs(self.Position))
            self._stop_price = None
            self._cooldown_remaining = cd
        elif self._cooldown_remaining == 0 and cross_up and self._current_state == macd_hidden_markov_model_strategy.BULLISH and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._stop_price = close_price - stop_distance
            self._cooldown_remaining = cd
        elif self._cooldown_remaining == 0 and cross_down and self._current_state == macd_hidden_markov_model_strategy.BEARISH and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._stop_price = close_price + stop_distance
            self._cooldown_remaining = cd

        self._prev_macd = macd_f
        self._prev_signal = signal_f

    def _update_hmm_data(self, candle):
        close_price = float(candle.ClosePrice)
        if self._prev_price > 0:
            self._price_changes.append(close_price - self._prev_price)

            hmm_len = int(self._hmm_history_length.Value)
            while len(self._price_changes) > hmm_len:
                self._price_changes.pop(0)

        self._prev_price = close_price

    def _calculate_market_state(self):
        # The model observes exactly HmmHistoryLength price changes, so it stays neutral
        # until that much history is collected.
        if len(self._price_changes) < int(self._hmm_history_length.Value):
            return

        # The average absolute move of the window scales the observations, so the same
        # emission shapes fit both a quiet and a volatile market.
        scale = 0.0

        for change in self._price_changes:
            scale += abs(change)

        scale /= len(self._price_changes)

        if scale <= 0:
            return

        # Forward pass of the Hidden Markov Model: the belief starts uniform and every
        # observation of the window moves it, so the window length shapes the result.
        state_means = macd_hidden_markov_model_strategy.STATE_MEANS
        transitions = macd_hidden_markov_model_strategy.TRANSITIONS
        states = len(state_means)
        belief = [1.0 / states] * states
        updated = [0.0] * states

        for change in self._price_changes:
            observation = change / scale
            total = 0.0

            for next_state in range(states):
                # Chance of standing in "next_state" before the observation is taken into account.
                predicted = 0.0

                for current in range(states):
                    predicted += belief[current] * transitions[current][next_state]

                # Cauchy-shaped likelihood: the closer the move is to the typical move of the
                # state, the stronger the evidence, and an extreme move never kills a state.
                distance = observation - state_means[next_state]

                updated[next_state] = predicted / (1.0 + distance * distance)
                total += updated[next_state]

            for i in range(states):
                belief[i] = updated[i] / total

        # The state the filter considers most likely after the last observation.
        best = 0

        for i in range(1, states):
            if belief[i] > belief[best]:
                best = i

        self._current_state = best

    def CreateClone(self):
        return macd_hidden_markov_model_strategy()
