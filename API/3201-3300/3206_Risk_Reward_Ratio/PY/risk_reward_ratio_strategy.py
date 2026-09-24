import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class risk_reward_ratio_strategy(Strategy):
    def __init__(self):
        super(risk_reward_ratio_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._fast_ma = self.Param("FastMaPeriod", 6).SetGreaterThanZero()
        self._slow_ma = self.Param("SlowMaPeriod", 85).SetGreaterThanZero()
        self._momentum_threshold = self.Param("MomentumThreshold", 0.3).SetNotNegative()
        self._reward_ratio = self.Param("RewardRatio", 2.0).SetGreaterThanZero()
        self._stop_loss = self.Param("StopLossPips", 20).SetGreaterThanZero()
        self._max_positions = self.Param("MaxPositions", 10).SetGreaterThanZero()
        self._enable_trailing = self.Param("EnableTrailing", True)
        self._trailing_stop = self.Param("TrailingStopPips", 40).SetNotNegative()
        self._enable_break_even = self.Param("EnableBreakEven", True)
        self._break_even_trigger = self.Param("BreakEvenTriggerPips", 30).SetNotNegative()
        self._break_even_offset = self.Param("BreakEvenOffsetPips", 30).SetNotNegative()
        self._exit_switch = self.Param("ExitSwitch", False)

        self._bars = []
        self._macd_fast = None
        self._macd_slow = None
        self._macd_signal = None
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(risk_reward_ratio_strategy, self).OnReseted()
        self._bars = []
        self._macd_fast = None
        self._macd_slow = None
        self._macd_signal = None
        self._reset_trade()

    def OnStarted2(self, time):
        super(risk_reward_ratio_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process).Start()

    def _process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bars.append((float(candle.HighPrice), float(candle.LowPrice), float(candle.ClosePrice)))
        keep = max(int(self._slow_ma.Value) + 5, 100)
        if len(self._bars) > keep:
            del self._bars[:-keep]

        close = float(candle.ClosePrice)
        self._macd_fast = self._ema(self._macd_fast, close, 12)
        self._macd_slow = self._ema(self._macd_slow, close, 26)
        main = self._macd_fast - self._macd_slow
        self._macd_signal = self._ema(self._macd_signal, main, 9)

        if bool(self._exit_switch.Value):
            if self.Position != 0:
                self._flatten()
            return

        if self.Position != 0 and self._apply_risk(candle):
            return

        slow_period = int(self._slow_ma.Value)
        if len(self._bars) < max(slow_period, 25):
            return

        fast_k = self._stoch_k(5)
        slow_d = self._stoch_d(21, 10)
        rsi = self._rsi(14)
        fast_lwma = self._lwma(int(self._fast_ma.Value))
        slow_lwma = self._lwma(slow_period)
        burst = self._momentum_burst(14, 3)
        signal = self._macd_signal

        long_signal = (fast_k > slow_d and rsi > 50.0 and fast_lwma > slow_lwma and
                       main > signal and main > 0 and burst >= float(self._momentum_threshold.Value))
        short_signal = (fast_k < slow_d and rsi < 50.0 and fast_lwma < slow_lwma and
                        main < signal and main < 0 and burst >= float(self._momentum_threshold.Value))

        volume = float(self._trade_volume.Value)
        max_exposure = int(self._max_positions.Value) * volume

        if long_signal and self.Position >= 0 and float(self.Position) + volume <= max_exposure:
            self._enter(Sides.Buy, close)
        elif short_signal and self.Position <= 0 and abs(float(self.Position) - volume) <= max_exposure:
            self._enter(Sides.Sell, close)

    def _enter(self, side, close):
        volume = float(self._trade_volume.Value)
        previous = abs(float(self.Position))
        new_abs = previous + volume

        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._entry_price = ((self._entry_price * previous + close * volume) / new_abs) if previous > 0 else close
        pip = self._pip_size()
        stop_distance = int(self._stop_loss.Value) * pip
        target_distance = stop_distance * float(self._reward_ratio.Value)
        self._stop_price = self._entry_price - stop_distance if side == Sides.Buy else self._entry_price + stop_distance
        self._take_price = self._entry_price + target_distance if side == Sides.Buy else self._entry_price - target_distance
        self._best_price = close

    def _apply_risk(self, candle):
        pip = self._pip_size()

        if self.Position > 0:
            high = float(candle.HighPrice)
            self._best_price = high if self._best_price is None else max(self._best_price, high)

            if bool(self._enable_break_even.Value) and high - self._entry_price >= int(self._break_even_trigger.Value) * pip:
                candidate = self._entry_price + int(self._break_even_offset.Value) * pip
                if self._stop_price is None or candidate > self._stop_price:
                    self._stop_price = candidate

            if bool(self._enable_trailing.Value) and int(self._trailing_stop.Value) > 0:
                candidate = self._best_price - int(self._trailing_stop.Value) * pip
                if self._stop_price is None or candidate > self._stop_price:
                    self._stop_price = candidate

            if ((self._stop_price is not None and float(candle.LowPrice) <= self._stop_price) or
                    (self._take_price is not None and high >= self._take_price)):
                self._flatten()
                return True
        else:
            low = float(candle.LowPrice)
            self._best_price = low if self._best_price is None else min(self._best_price, low)

            if bool(self._enable_break_even.Value) and self._entry_price - low >= int(self._break_even_trigger.Value) * pip:
                candidate = self._entry_price - int(self._break_even_offset.Value) * pip
                if self._stop_price is None or candidate < self._stop_price:
                    self._stop_price = candidate

            if bool(self._enable_trailing.Value) and int(self._trailing_stop.Value) > 0:
                candidate = self._best_price + int(self._trailing_stop.Value) * pip
                if self._stop_price is None or candidate < self._stop_price:
                    self._stop_price = candidate

            if ((self._stop_price is not None and float(candle.HighPrice) >= self._stop_price) or
                    (self._take_price is not None and low <= self._take_price)):
                self._flatten()
                return True

        return False

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
        self._reset_trade()

    def _stoch_k(self, period):
        window = self._bars[-period:]
        high = max(x[0] for x in window)
        low = min(x[1] for x in window)
        return 50.0 if high == low else (self._bars[-1][2] - low) / (high - low) * 100.0

    def _stoch_d(self, period, smoothing):
        values = []
        for shift in range(smoothing - 1, -1, -1):
            end = len(self._bars) - shift
            if end < period:
                continue
            window = self._bars[end-period:end]
            high = max(x[0] for x in window)
            low = min(x[1] for x in window)
            close = self._bars[end-1][2]
            values.append(50.0 if high == low else (close - low) / (high - low) * 100.0)
        return sum(values) / len(values) if values else 50.0

    def _rsi(self, period):
        gains = 0.0
        losses = 0.0
        for i in range(len(self._bars) - period, len(self._bars)):
            diff = self._bars[i][2] - self._bars[i-1][2]
            if diff > 0:
                gains += diff
            else:
                losses -= diff
        if losses == 0:
            return 100.0
        rs = gains / losses
        return 100.0 - 100.0 / (1.0 + rs)

    def _lwma(self, period):
        values = self._bars[-period:]
        weights = list(range(1, period + 1))
        return sum(v[2] * w for v, w in zip(values, weights)) / float(sum(weights))

    def _momentum_burst(self, period, samples):
        result = 0.0
        for shift in range(samples):
            index = len(self._bars) - 1 - shift
            base = index - period
            if base < 0 or self._bars[base][2] == 0:
                continue
            momentum = self._bars[index][2] / self._bars[base][2] * 100.0
            result = max(result, abs(momentum - 100.0))
        return result

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

    def _reset_trade(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None

    def CreateClone(self):
        return risk_reward_ratio_strategy()
