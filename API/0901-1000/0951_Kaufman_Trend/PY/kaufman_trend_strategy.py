import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class kaufman_trend_strategy(Strategy):
    """
    Kaufman Trend strategy.
    A two-state Kalman filter estimates price and its velocity. Trend strength is the velocity divided by the largest absolute
    velocity of the last OscBufferLength candles, in percent. A long opens when strength reaches TrendStrengthEntry with the close above
    the filtered price and a short in the mirror case, reversing an opposite position. The stop sits at the SwingLookback low (high)
    minus (plus) ATR. While in profit, TakeProfit1Percent of the entry volume closes when strength falls below TrendStrengthEntry and
    TakeProfit2Percent when it falls below the midpoint of the two thresholds; the rest closes below TrendStrengthExit.
    """

    def __init__(self):
        super(kaufman_trend_strategy, self).__init__()
        self._take_profit1_percent = self.Param("TakeProfit1Percent", 50.0).SetNotNegative().SetDisplay("Take Profit 1 %", "Percent of the entry volume closed at the first stage", "Exits")
        self._take_profit2_percent = self.Param("TakeProfit2Percent", 25.0).SetNotNegative().SetDisplay("Take Profit 2 %", "Percent of the entry volume closed at the second stage", "Exits")
        self._take_profit3_percent = self.Param("TakeProfit3Percent", 25.0).SetNotNegative().SetDisplay("Take Profit 3 %", "Percent of the entry volume closed at the final stage", "Exits")
        self._swing_lookback = self.Param("SwingLookback", 10).SetGreaterThanZero().SetDisplay("Swing Lookback", "Candles the swing high/low spans", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._trend_strength_entry = self.Param("TrendStrengthEntry", 60.0).SetDisplay("Trend Strength Entry", "Trend strength required for entries", "Trend")
        self._trend_strength_exit = self.Param("TrendStrengthExit", 40.0).SetDisplay("Trend Strength Exit", "Trend strength below which the position closes", "Trend")
        self._process_noise = self.Param("ProcessNoise", 0.01).SetGreaterThanZero().SetDisplay("Process Noise", "Kalman process noise", "Kalman")
        self._measurement_noise = self.Param("MeasurementNoise", 500.0).SetGreaterThanZero().SetDisplay("Measurement Noise", "Kalman measurement noise", "Kalman")
        self._osc_buffer_length = self.Param("OscBufferLength", 10).SetGreaterThanZero().SetDisplay("Oscillator Buffer", "Candles the trend strength normalization spans", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._swing_high = None
        self._swing_low = None
        self._osc_max = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._filtered = None
        self._velocity = 0.0
        self._p00 = 1.0
        self._p01 = 0.0
        self._p10 = 0.0
        self._p11 = 1.0
        self._stop_price = None
        self._entry_price = 0.0
        self._entry_volume = 0.0
        self._stage = 0

    def OnReseted(self):
        super(kaufman_trend_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(kaufman_trend_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._swing_high = Highest()
        self._swing_high.Length = self._swing_lookback.Value
        self._swing_low = Lowest()
        self._swing_low.Length = self._swing_lookback.Value
        self._osc_max = Highest()
        self._osc_max.Length = self._osc_buffer_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        time = candle.OpenTime
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        atr = float(atr_value)

        self._update_kalman(close)

        max_abs = float(process_float(self._osc_max, abs(self._velocity), time, True))
        swing_high = float(process_float(self._swing_high, high, time, True))
        swing_low = float(process_float(self._swing_low, low, time, True))

        if not self._osc_max.IsFormed or not self._swing_high.IsFormed or not self._swing_low.IsFormed or self._filtered is None:
            return

        strength = self._velocity / max_abs * 100.0 if max_abs > 0.0 else 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        entry_level = float(self._trend_strength_entry.Value)
        exit_level = float(self._trend_strength_exit.Value)
        mid_level = (entry_level + exit_level) / 2.0

        if self.Position > 0:
            if (self._stop_price is not None and low <= self._stop_price) or strength < exit_level:
                self.SellMarket(self.Position)
                self._stop_price = None
                return
            if close > self._entry_price and self._try_take_partial(strength < entry_level, strength < mid_level, False):
                return
        elif self.Position < 0:
            if (self._stop_price is not None and high >= self._stop_price) or strength > -exit_level:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                return
            if close < self._entry_price and self._try_take_partial(strength > -entry_level, strength > -mid_level, True):
                return

        if strength >= entry_level and close > self._filtered and self.Position <= 0:
            stop = swing_low - atr
            if stop >= close:
                return
            self.BuyMarket(self.Volume + abs(self.Position))
            self._on_entered(close, stop)
        elif strength <= -entry_level and close < self._filtered and self.Position >= 0:
            stop = swing_high + atr
            if stop <= close:
                return
            self.SellMarket(self.Volume + abs(self.Position))
            self._on_entered(close, stop)

    def _on_entered(self, price, stop):
        self._entry_price = price
        self._entry_volume = float(self.Volume)
        self._stop_price = stop
        self._stage = 0

    def _try_take_partial(self, first_stage, second_stage, is_short):
        if self._stage == 0 and first_stage:
            percent = float(self._take_profit1_percent.Value)
        elif self._stage == 1 and second_stage:
            percent = float(self._take_profit2_percent.Value)
        else:
            return False

        self._stage += 1

        volume = min(abs(float(self.Position)), self._entry_volume * percent / 100.0)
        if volume <= 0.0:
            return False

        if is_short:
            self.BuyMarket(Decimal(volume))
        else:
            self.SellMarket(Decimal(volume))
        return True

    def _update_kalman(self, price):
        if self._filtered is None:
            self._filtered = price
            return

        pn = float(self._process_noise.Value)
        mn = float(self._measurement_noise.Value)

        # Constant-velocity model: predict, then correct with the new close.
        predicted = self._filtered + self._velocity

        p00 = self._p00 + self._p01 + self._p10 + self._p11 + pn
        p01 = self._p01 + self._p11
        p10 = self._p10 + self._p11
        p11 = self._p11 + pn

        s = p00 + mn
        k0 = p00 / s
        k1 = p10 / s
        innovation = price - predicted

        self._filtered = predicted + k0 * innovation
        self._velocity += k1 * innovation

        self._p00 = (1.0 - k0) * p00
        self._p01 = (1.0 - k0) * p01
        self._p10 = p10 - k1 * p00
        self._p11 = p11 - k1 * p01

    def CreateClone(self):
        return kaufman_trend_strategy()
