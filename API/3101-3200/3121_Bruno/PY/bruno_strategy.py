import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class _sar_state:
    def __init__(self, step=0.055, maximum=0.21):
        self.step = step
        self.maximum = maximum
        self.reset()

    def reset(self):
        self.initialized = False
        self.up = False
        self.sar = self.ep = self.af = 0.0
        self.previous_high = self.previous_low = self.previous_close = 0.0
        self.older_high = self.older_low = 0.0
        self.count = 0

    def process(self, high, low, close):
        self.count += 1
        if self.count == 1:
            self.previous_high = self.older_high = high
            self.previous_low = self.older_low = low
            self.previous_close = close
            return None

        if not self.initialized:
            self.up = close >= self.previous_close
            self.sar = min(self.previous_low, low) if self.up else max(self.previous_high, high)
            self.ep = max(self.previous_high, high) if self.up else min(self.previous_low, low)
            self.af = self.step
            self.initialized = True
            self._shift(high, low, close)
            return self.sar

        value = self.sar + self.af * (self.ep - self.sar)
        if self.up:
            value = min(value, self.previous_low, self.older_low)
            if low < value:
                self.up = False
                value = self.ep
                self.ep = low
                self.af = self.step
            elif high > self.ep:
                self.ep = high
                self.af = min(self.maximum, self.af + self.step)
        else:
            value = max(value, self.previous_high, self.older_high)
            if high > value:
                self.up = True
                value = self.ep
                self.ep = high
                self.af = self.step
            elif low < self.ep:
                self.ep = low
                self.af = min(self.maximum, self.af + self.step)

        self.sar = value
        self._shift(high, low, close)
        return self.sar

    def _shift(self, high, low, close):
        self.older_high = self.previous_high
        self.older_low = self.previous_low
        self.previous_high = high
        self.previous_low = low
        self.previous_close = close


class bruno_strategy(Strategy):
    def __init__(self):
        super(bruno_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 0.1).SetGreaterThanZero()
        self._signal_multiplier = self.Param("SignalMultiplier", 1.6).SetGreaterThanZero()
        self._stop_loss = self.Param("StopLossPips", 50).SetNotNegative()
        self._take_profit = self.Param("TakeProfitPips", 100).SetNotNegative()
        self._trailing_stop = self.Param("TrailingStopPips", 30).SetNotNegative()
        self._trailing_step = self.Param("TrailingStepPips", 5).SetNotNegative()
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero()
        self._adx_positive = self.Param("AdxPositiveThreshold", 20.0)
        self._adx_negative = self.Param("AdxNegativeThreshold", 40.0)
        self._fast_ema_period = self.Param("FastEmaPeriod", 8).SetGreaterThanZero()
        self._slow_ema_period = self.Param("SlowEmaPeriod", 21).SetGreaterThanZero()
        self._macd_fast_period = self.Param("MacdFastPeriod", 13).SetGreaterThanZero()
        self._macd_slow_period = self.Param("MacdSlowPeriod", 34).SetGreaterThanZero()
        self._macd_signal_period = self.Param("MacdSignalPeriod", 8).SetGreaterThanZero()
        self._stoch_period = self.Param("StochasticPeriod", 21).SetGreaterThanZero()
        self._stoch_k_smoothing = self.Param("StochasticKsmoothing", 3).SetGreaterThanZero()
        self._stoch_d_smoothing = self.Param("StochasticDsmoothing", 3).SetGreaterThanZero()
        self._stoch_overbought = self.Param("StochasticOverbought", 80.0)
        self._stoch_oversold = self.Param("StochasticOversold", 20.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._bars = []
        self._raw_k = []
        self._smooth_k = []
        self._sar_values = []
        self._fast_ema = self._slow_ema = None
        self._macd_fast = self._macd_slow = self._macd_signal = None
        self._sar = _sar_state()

        self._entry_price = 0.0
        self._stop_price = self._take_price = self._best_price = None
        self._entry_candle_time = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(bruno_strategy, self).OnReseted()
        self._bars = []
        self._raw_k = []
        self._smooth_k = []
        self._sar_values = []
        self._fast_ema = self._slow_ema = None
        self._macd_fast = self._macd_slow = self._macd_signal = None
        self._sar.reset()
        self._reset_protection()

    def OnStarted2(self, time):
        super(bruno_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        self._bars.append((high, low, close))

        keep = max(int(self._adx_period.Value) + 2,
                   int(self._macd_slow_period.Value) + int(self._macd_signal_period.Value) + 2,
                   int(self._stoch_period.Value) + 10)
        if len(self._bars) > keep:
            del self._bars[:-keep]

        self._fast_ema = self._ema(self._fast_ema, close, int(self._fast_ema_period.Value))
        self._slow_ema = self._ema(self._slow_ema, close, int(self._slow_ema_period.Value))
        self._macd_fast = self._ema(self._macd_fast, close, int(self._macd_fast_period.Value))
        self._macd_slow = self._ema(self._macd_slow, close, int(self._macd_slow_period.Value))
        macd_main = self._macd_fast - self._macd_slow
        self._macd_signal = self._ema(self._macd_signal, macd_main, int(self._macd_signal_period.Value))

        sar = self._sar.process(high, low, close)
        if sar is not None:
            self._sar_values.append(sar)
            if len(self._sar_values) > 4:
                del self._sar_values[0]

        self._update_stochastic()

        if self.Position != 0 and self._apply_protection(candle):
            return

        if (len(self._bars) < int(self._adx_period.Value) + 1 or
                len(self._smooth_k) < int(self._stoch_d_smoothing.Value) or
                len(self._sar_values) < 3):
            return

        plus_di, minus_di = self._directional_index()
        k = self._smooth_k[-1]
        d_count = int(self._stoch_d_smoothing.Value)
        d = sum(self._smooth_k[-d_count:]) / d_count
        histogram = macd_main - self._macd_signal

        long_directional = plus_di > minus_di and plus_di > float(self._adx_positive.Value)
        short_directional = plus_di < minus_di and plus_di < float(self._adx_negative.Value)

        long_momentum = self._fast_ema > self._slow_ema and k > d and k < float(self._stoch_overbought.Value)
        short_momentum = self._fast_ema < self._slow_ema and k < d and k > float(self._stoch_oversold.Value)

        long_macd = histogram > 0 and macd_main > self._macd_signal
        short_macd = histogram < 0 and macd_main < self._macd_signal

        long_sar = self._sar_values[-3] < self._sar_values[-2] < self._sar_values[-1] and self._fast_ema > self._slow_ema
        short_sar = self._sar_values[-3] > self._sar_values[-2] > self._sar_values[-1] and self._fast_ema < self._slow_ema

        long_volume, short_volume = self.calculate_signal_volumes(
            float(self._base_volume.Value), float(self._signal_multiplier.Value),
            long_directional, short_directional,
            long_momentum, short_momentum,
            long_macd, short_macd,
            long_sar, short_sar)

        base = float(self._base_volume.Value)
        has_long = long_volume > base
        has_short = short_volume > base

        if has_long == has_short:
            return

        self._enter(Sides.Buy if has_long else Sides.Sell,
                    long_volume if has_long else short_volume, candle)

    def _enter(self, side, target_volume, candle):
        position = float(self.Position)
        volume = self._normalize_volume(target_volume + abs(position) if
                                        (position < 0 and side == Sides.Buy) or (position > 0 and side == Sides.Sell)
                                        else target_volume)
        if volume <= 0:
            return

        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._entry_price = float(candle.ClosePrice)
        self._entry_candle_time = candle.OpenTime
        self._best_price = self._entry_price
        pip = self._pip_size()

        sl = int(self._stop_loss.Value)
        tp = int(self._take_profit.Value)
        self._stop_price = (self._entry_price - sl * pip if side == Sides.Buy else self._entry_price + sl * pip) if sl > 0 else None
        self._take_price = (self._entry_price + tp * pip if side == Sides.Buy else self._entry_price - tp * pip) if tp > 0 else None

    def _apply_protection(self, candle):
        if self._entry_candle_time is not None and candle.OpenTime <= self._entry_candle_time:
            return False

        pip = self._pip_size()
        trail = int(self._trailing_stop.Value)
        step = int(self._trailing_step.Value)

        if self.Position > 0:
            high = float(candle.HighPrice)
            self._best_price = high if self._best_price is None else max(self._best_price, high)
            if trail > 0 and self._best_price - self._entry_price >= (trail + step) * pip:
                candidate = self._best_price - trail * pip
                if self._stop_price is None or candidate >= self._stop_price + step * pip:
                    self._stop_price = candidate

            if ((self._stop_price is not None and float(candle.LowPrice) <= self._stop_price) or
                    (self._take_price is not None and high >= self._take_price)):
                self.SellMarket(Math.Abs(self.Position))
                self._reset_protection()
                return True

        elif self.Position < 0:
            low = float(candle.LowPrice)
            self._best_price = low if self._best_price is None else min(self._best_price, low)
            if trail > 0 and self._entry_price - self._best_price >= (trail + step) * pip:
                candidate = self._best_price + trail * pip
                if self._stop_price is None or candidate <= self._stop_price - step * pip:
                    self._stop_price = candidate

            if ((self._stop_price is not None and float(candle.HighPrice) >= self._stop_price) or
                    (self._take_price is not None and low <= self._take_price)):
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_protection()
                return True

        return False

    def _update_stochastic(self):
        period = int(self._stoch_period.Value)
        if len(self._bars) < period:
            return

        window = self._bars[-period:]
        high = max(b[0] for b in window)
        low = min(b[1] for b in window)
        raw = 50.0 if high == low else (self._bars[-1][2] - low) / (high - low) * 100.0
        self._raw_k.append(raw)

        k_smooth = int(self._stoch_k_smoothing.Value)
        d_smooth = int(self._stoch_d_smoothing.Value)
        if len(self._raw_k) > k_smooth + d_smooth + 2:
            del self._raw_k[0]

        if len(self._raw_k) >= k_smooth:
            self._smooth_k.append(sum(self._raw_k[-k_smooth:]) / k_smooth)
            if len(self._smooth_k) > d_smooth + 2:
                del self._smooth_k[0]

    def _directional_index(self):
        period = int(self._adx_period.Value)
        start = len(self._bars) - period
        tr = plus = minus = 0.0

        for i in range(start, len(self._bars)):
            current = self._bars[i]
            previous = self._bars[i - 1]
            up = current[0] - previous[0]
            down = previous[1] - current[1]
            plus += up if up > down and up > 0 else 0.0
            minus += down if down > up and down > 0 else 0.0
            tr += max(current[0] - current[1],
                      abs(current[0] - previous[2]),
                      abs(current[1] - previous[2]))

        return (0.0, 0.0) if tr <= 0 else (plus / tr * 100.0, minus / tr * 100.0)

    @staticmethod
    def calculate_signal_volumes(base_volume, multiplier,
                                 long_directional, short_directional,
                                 long_momentum, short_momentum,
                                 long_macd, short_macd,
                                 long_sar, short_sar):
        long_volume = base_volume
        short_volume = base_volume
        for flag in [long_directional, long_momentum, long_macd, long_sar]:
            if flag:
                long_volume *= multiplier
        for flag in [short_directional, short_momentum, short_macd, short_sar]:
            if flag:
                short_volume *= multiplier
        return long_volume, short_volume

    def _normalize_volume(self, volume):
        if self.Security is not None:
            if self.Security.MaxVolume is not None and float(self.Security.MaxVolume) > 0:
                volume = min(volume, float(self.Security.MaxVolume))
            if self.Security.MinVolume is not None and float(self.Security.MinVolume) > 0:
                volume = max(volume, float(self.Security.MinVolume))
            if self.Security.VolumeStep is not None and float(self.Security.VolumeStep) > 0:
                step = float(self.Security.VolumeStep)
                volume = math.floor(volume / step) * step
        return volume

    def _pip_size(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if step <= 0:
            return 0.0001
        return step * 10.0 if abs(step - 0.00001) < 1e-12 or abs(step - 0.001) < 1e-12 else step

    @staticmethod
    def _ema(previous, value, period):
        if previous is None:
            return value
        alpha = 2.0 / (period + 1.0)
        return previous + alpha * (value - previous)

    def _reset_protection(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None
        self._entry_candle_time = None

    def CreateClone(self):
        return bruno_strategy()
