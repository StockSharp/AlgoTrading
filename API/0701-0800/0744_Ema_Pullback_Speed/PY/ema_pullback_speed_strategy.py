import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

MIN_LENGTH = 5
LOOKBACK = 200


class ema_pullback_speed_strategy(Strategy):
    """
    EMA pullback speed strategy.
    A dynamic EMA changes its length between 5 and MaxLength with the candle body relative to the largest recent body and
    speeds up with price acceleration scaled by AccelMultiplier. Speed accumulates candle bodies while the dynamic EMA keeps its
    direction and restarts when it turns. A long needs the close above the dynamic EMA after the low came back within
    ReturnThreshold percent of it, a bullish reversal candle, the short EMA above the long EMA and speed of at least LongSpeedMin;
    a short mirrors it with speed of at most ShortSpeedMax. The stop is AtrMultiplier ATR from entry, the take profit FixedTpPct percent.
    """

    def __init__(self):
        super(ema_pullback_speed_strategy, self).__init__()
        self._max_length = self.Param("MaxLength", 50).SetGreaterThanZero().SetDisplay("Max Length", "Maximum length of the dynamic EMA", "Dynamic EMA")
        self._accel_multiplier = self.Param("AccelMultiplier", 3.0).SetNotNegative().SetDisplay("Accel Multiplier", "Weight of price acceleration", "Dynamic EMA")
        self._return_threshold = self.Param("ReturnThreshold", 5.0).SetNotNegative().SetDisplay("Return Threshold %", "Maximum pullback distance from the dynamic EMA", "Dynamic EMA")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 4.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop loss distance in ATR", "Risk")
        self._fixed_tp_pct = self.Param("FixedTpPct", 1.5).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._short_ema_length = self.Param("ShortEmaLength", 21).SetGreaterThanZero().SetDisplay("Short EMA", "Short EMA length", "Trend")
        self._long_ema_length = self.Param("LongEmaLength", 50).SetGreaterThanZero().SetDisplay("Long EMA", "Long EMA length", "Trend")
        self._long_speed_min = self.Param("LongSpeedMin", 1000.0).SetDisplay("Long Speed Min", "Minimum speed for a long", "Speed")
        self._short_speed_max = self.Param("ShortSpeedMax", -1000.0).SetDisplay("Short Speed Max", "Maximum speed for a short", "Speed")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._bodies = []
        self._deltas = []
        self._dyn_ema = None
        self._prev_close = None
        self._prev_open = None
        self._direction = 0
        self._speed = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(ema_pullback_speed_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_pullback_speed_strategy, self).OnStarted2(time)

        self._reset_state()

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self._short_ema_length.Value
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self._long_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ema, long_ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    @staticmethod
    def _push(items, value):
        items.append(value)
        if len(items) > LOOKBACK:
            items.pop(0)

    def _process_candle(self, candle, short_value, long_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        short_ema = float(short_value)
        long_ema = float(long_value)
        atr = float(atr_value)

        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        body = close - open_price
        prev_close = self._prev_close
        prev_open = self._prev_open
        self._prev_close = close
        self._prev_open = open_price

        self._push(self._bodies, abs(body))
        self._push(self._deltas, abs(close - prev_close) if prev_close is not None else 0.0)

        max_body = max(self._bodies)
        max_delta = max(self._deltas)

        # Larger bullish bodies lengthen the EMA, bearish ones shorten it, and acceleration speeds it up.
        max_length = self._max_length.Value
        norm = (body + max_body) / (2.0 * max_body) if max_body > 0 else 0.5
        dyn_length = MIN_LENGTH + norm * (max_length - MIN_LENGTH)
        accel = self._deltas[-1] / max_delta if max_delta > 0 else 0.0
        alpha = min(1.0, 2.0 / (dyn_length + 1.0) * (1.0 + accel * float(self._accel_multiplier.Value)))

        prev_dyn = self._dyn_ema
        dyn = alpha * close + (1.0 - alpha) * prev_dyn if prev_dyn is not None else close
        self._dyn_ema = dyn

        if prev_dyn is not None:
            diff = dyn - prev_dyn
            direction = 1 if diff > 0 else (-1 if diff < 0 else 0)
            if direction != 0 and direction != self._direction:
                self._direction = direction
                self._speed = 0.0
            self._speed += body

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr_multiplier = float(self._atr_multiplier.Value)
        tp_pct = float(self._fixed_tp_pct.Value)

        if self.Position > 0:
            if (atr_multiplier > 0 and low <= self._stop_price) or (tp_pct > 0 and high >= self._take_price):
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if (atr_multiplier > 0 and high >= self._stop_price) or (tp_pct > 0 and low <= self._take_price):
                self.BuyMarket(-self.Position)
            return

        if prev_open is None or prev_close is None or prev_dyn is None or dyn <= 0:
            return

        threshold = float(self._return_threshold.Value) / 100.0
        bullish_reversal = close > open_price and prev_close < prev_open
        bearish_reversal = close < open_price and prev_close > prev_open
        returned_long = low <= dyn * (1.0 + threshold)
        returned_short = high >= dyn * (1.0 - threshold)

        if close > dyn and bullish_reversal and returned_long and self._speed > 0 and self._speed >= float(self._long_speed_min.Value) and short_ema > long_ema:
            self._stop_price = close - atr * atr_multiplier
            self._take_price = close * (1.0 + tp_pct / 100.0)
            self.BuyMarket(self.Volume)
        elif close < dyn and bearish_reversal and returned_short and self._speed < 0 and self._speed <= float(self._short_speed_max.Value) and short_ema < long_ema:
            self._stop_price = close + atr * atr_multiplier
            self._take_price = close * (1.0 - tp_pct / 100.0)
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return ema_pullback_speed_strategy()
