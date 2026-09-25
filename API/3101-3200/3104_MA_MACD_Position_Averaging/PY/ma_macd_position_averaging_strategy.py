import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class ma_macd_position_averaging_strategy(Strategy):
    MA_SIMPLE = 0
    MA_EXPONENTIAL = 1
    MA_SMOOTHED = 2
    MA_WEIGHTED = 3

    PRICE_CLOSE = 0
    PRICE_OPEN = 1
    PRICE_HIGH = 2
    PRICE_LOW = 3
    PRICE_MEDIAN = 4
    PRICE_TYPICAL = 5
    PRICE_WEIGHTED = 6

    def __init__(self):
        super(ma_macd_position_averaging_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._order_volume = self.Param("OrderVolume", 0.1).SetGreaterThanZero()
        self._stop_loss = self.Param("StopLossPips", 50).SetNotNegative()
        self._take_profit = self.Param("TakeProfitPips", 50).SetNotNegative()
        self._trailing_stop = self.Param("TrailingStopPips", 5).SetNotNegative()
        self._trailing_step = self.Param("TrailingStepPips", 5).SetNotNegative()
        self._step_lossing = self.Param("StepLossingPips", 30).SetNotNegative()
        self._lot_coefficient = self.Param("LotCoefficient", 2.0).SetGreaterThanZero()
        self._signal_bar = self.Param("SignalBar", 0).SetNotNegative()
        self._ma_period = self.Param("MaPeriod", 15).SetGreaterThanZero()
        self._ma_shift = self.Param("MaShift", 0).SetNotNegative()
        self._ma_method = self.Param("MaMethod", self.MA_WEIGHTED)
        self._ma_applied = self.Param("MaAppliedPrice", self.PRICE_WEIGHTED)
        self._indent = self.Param("IndentPips", 4).SetNotNegative()
        self._macd_fast_period = self.Param("MacdFastPeriod", 12).SetGreaterThanZero()
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26).SetGreaterThanZero()
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9).SetGreaterThanZero()
        self._macd_applied = self.Param("MacdAppliedPrice", self.PRICE_WEIGHTED)
        self._macd_ratio = self.Param("MacdRatio", 0.9).SetNotNegative()

        self._ma_inputs = []
        self._ma_values = []
        self._macd_values = []
        self._legs = []
        self._macd_fast = None
        self._macd_slow = None
        self._macd_signal = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(ma_macd_position_averaging_strategy, self).OnReseted()
        self._ma_inputs = []
        self._ma_values = []
        self._macd_values = []
        self._legs = []
        self._macd_fast = None
        self._macd_slow = None
        self._macd_signal = None

    def OnStarted2(self, time):
        super(ma_macd_position_averaging_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_indicators(candle)

        if len(set(leg["side"] for leg in self._legs)) > 1:
            self._flatten_all()
            return

        self._apply_protection(candle)

        if self._legs:
            self._try_average(float(candle.ClosePrice), candle.OpenTime)
            return

        if self.Position != 0:
            return

        ma_index = len(self._ma_values) - 1 - int(self._signal_bar.Value) - int(self._ma_shift.Value)
        macd_index = len(self._macd_values) - 1 - int(self._signal_bar.Value)
        if ma_index < 0 or macd_index < 0:
            return

        ma = self._ma_values[ma_index]
        main, signal = self._macd_values[macd_index]
        if signal == 0:
            return

        ratio = main / signal
        pip = self._pip_size()
        indent = int(self._indent.Value) * pip
        close = float(candle.ClosePrice)

        if main < 0 and signal < 0 and ratio >= float(self._macd_ratio.Value) and close > ma and close - ma >= indent:
            self._add_leg(Sides.Buy, self._normalize_volume(float(self._order_volume.Value)), close, candle.OpenTime)
        elif main > 0 and signal > 0 and ratio >= float(self._macd_ratio.Value) and close < ma and ma - close >= indent:
            self._add_leg(Sides.Sell, self._normalize_volume(float(self._order_volume.Value)), close, candle.OpenTime)

    def _update_indicators(self, candle):
        ma_price = self._applied_price(candle, int(self._ma_applied.Value))
        self._ma_inputs.append(ma_price)
        self._ma_values.append(self._calculate_ma(self._ma_inputs, int(self._ma_period.Value), int(self._ma_method.Value)))

        macd_price = self._applied_price(candle, int(self._macd_applied.Value))
        self._macd_fast = self._ema(self._macd_fast, macd_price, int(self._macd_fast_period.Value), 2.0)
        self._macd_slow = self._ema(self._macd_slow, macd_price, int(self._macd_slow_period.Value), 2.0)
        main = self._macd_fast - self._macd_slow
        self._macd_signal = self._ema(self._macd_signal, main, int(self._macd_signal_period.Value), 2.0)
        self._macd_values.append((main, self._macd_signal))

        keep = max(
            int(self._ma_period.Value) + int(self._ma_shift.Value) + int(self._signal_bar.Value) + 10,
            int(self._macd_slow_period.Value) + int(self._macd_signal_period.Value) + int(self._signal_bar.Value) + 10)
        if len(self._ma_inputs) > keep:
            remove = len(self._ma_inputs) - keep
            del self._ma_inputs[:remove]
            del self._ma_values[:remove]
            del self._macd_values[:remove]

    def _try_average(self, close, candle_time):
        step_pips = int(self._step_lossing.Value)
        if step_pips <= 0 or not self._legs:
            return

        side = self._legs[0]["side"]
        if any(leg["side"] != side for leg in self._legs):
            return

        distance = step_pips * self._pip_size()
        volume = self._normalize_volume(self._legs[-1]["volume"] * float(self._lot_coefficient.Value))

        if side == Sides.Buy:
            best = min(leg["entry"] for leg in self._legs)
            if close <= best - distance:
                self._add_leg(Sides.Buy, volume, close, candle_time)
        else:
            best = max(leg["entry"] for leg in self._legs)
            if close >= best + distance:
                self._add_leg(Sides.Sell, volume, close, candle_time)

    def _apply_protection(self, candle):
        if not self._legs:
            return

        pip = self._pip_size()
        exit_buy = 0.0
        exit_sell = 0.0

        for i in range(len(self._legs) - 1, -1, -1):
            leg = self._legs[i]
            if candle.OpenTime <= leg["entry_time"]:
                continue

            self._update_trailing(leg, float(candle.ClosePrice), pip)

            if leg["side"] == Sides.Buy:
                hit = ((leg["stop"] is not None and float(candle.LowPrice) <= leg["stop"]) or
                       (leg["take"] is not None and float(candle.HighPrice) >= leg["take"]))
            else:
                hit = ((leg["stop"] is not None and float(candle.HighPrice) >= leg["stop"]) or
                       (leg["take"] is not None and float(candle.LowPrice) <= leg["take"]))

            if not hit:
                continue

            if leg["side"] == Sides.Buy:
                exit_sell += leg["volume"]
            else:
                exit_buy += leg["volume"]
            del self._legs[i]

        if exit_sell > 0:
            self.SellMarket(exit_sell)
        if exit_buy > 0:
            self.BuyMarket(exit_buy)

    def _update_trailing(self, leg, close, pip):
        trail_pips = int(self._trailing_stop.Value)
        step_pips = int(self._trailing_step.Value)
        if trail_pips <= 0 or step_pips < 0:
            return

        trail = trail_pips * pip
        step = step_pips * pip

        if leg["side"] == Sides.Buy:
            if close - leg["entry"] < trail + step:
                return
            candidate = close - trail
            if leg["stop"] is None or candidate >= leg["stop"] + step:
                leg["stop"] = candidate
        else:
            if leg["entry"] - close < trail + step:
                return
            candidate = close + trail
            if leg["stop"] is None or candidate <= leg["stop"] - step:
                leg["stop"] = candidate

    def _add_leg(self, side, volume, price, entry_time):
        if volume <= 0:
            return

        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        pip = self._pip_size()
        sl = int(self._stop_loss.Value) * pip
        tp = int(self._take_profit.Value) * pip

        self._legs.append({
            "side": side,
            "volume": volume,
            "entry": price,
            "entry_time": entry_time,
            "stop": (price - sl if side == Sides.Buy else price + sl) if int(self._stop_loss.Value) > 0 else None,
            "take": (price + tp if side == Sides.Buy else price - tp) if int(self._take_profit.Value) > 0 else None,
        })

    def _flatten_all(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
        self._legs = []

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

    @classmethod
    def _applied_price(cls, candle, kind):
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        if kind == cls.PRICE_OPEN:
            return o
        if kind == cls.PRICE_HIGH:
            return h
        if kind == cls.PRICE_LOW:
            return l
        if kind == cls.PRICE_MEDIAN:
            return (h + l) / 2.0
        if kind == cls.PRICE_TYPICAL:
            return (h + l + c) / 3.0
        if kind == cls.PRICE_WEIGHTED:
            return (h + l + 2.0 * c) / 4.0
        return c

    @classmethod
    def _calculate_ma(cls, values, period, method):
        count = min(period, len(values))
        sample = values[-count:]

        if method == cls.MA_WEIGHTED:
            weights = list(range(1, count + 1))
            return sum(v * w for v, w in zip(sample, weights)) / float(sum(weights))
        if method == cls.MA_SIMPLE:
            return sum(sample) / float(count)

        alpha = 1.0 / period if method == cls.MA_SMOOTHED else 2.0 / (period + 1.0)
        result = sample[0]
        for value in sample[1:]:
            result += alpha * (value - result)
        return result

    @staticmethod
    def _ema(previous, value, period, numerator):
        if previous is None:
            return value
        alpha = numerator / (period + 1.0)
        return previous + alpha * (value - previous)

    def CreateClone(self):
        return ma_macd_position_averaging_strategy()
