import clr
from collections import deque

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class mnist_pattern_classifier_strategy(Strategy):
    def __init__(self):
        super(mnist_pattern_classifier_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._lookback_period = self.Param("LookbackPeriod", 14)
        self._target_class = self.Param("TargetClass", 1)
        self._confidence_threshold = self.Param("ConfidenceThreshold", 0.2)

        self._close_window = deque()
        self._first_close = 0.0
        self._previous_close = 0.0
        self._last_class = -1
        self._last_confidence = 0.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def TargetClass(self):
        return self._target_class.Value

    @TargetClass.setter
    def TargetClass(self, value):
        self._target_class.Value = value

    @property
    def ConfidenceThreshold(self):
        return self._confidence_threshold.Value

    @ConfidenceThreshold.setter
    def ConfidenceThreshold(self, value):
        self._confidence_threshold.Value = value

    def OnReseted(self):
        super(mnist_pattern_classifier_strategy, self).OnReseted()
        self._close_window = deque()
        self._first_close = 0.0
        self._previous_close = 0.0
        self._last_class = -1
        self._last_confidence = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(mnist_pattern_classifier_strategy, self).OnStarted2(time)
        self._close_window = deque()
        self._first_close = 0.0
        self._previous_close = 0.0
        self._last_class = -1
        self._last_confidence = 0.0
        self._cooldown = 0

        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent))

        rsi = RelativeStrengthIndex()
        rsi.Length = self.LookbackPeriod
        atr = AverageTrueRange()
        atr.Length = self.LookbackPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, atr, self._process_candle).Start()

    def _update_window(self, close):
        lookback = self.LookbackPeriod
        self._close_window.append(close)
        while len(self._close_window) > lookback:
            self._close_window.popleft()
        self._first_close = self._close_window[0] if len(self._close_window) > 0 else 0.0

    def _classify_pattern(self, current_close, rsi_val, atr_val):
        lookback = self.LookbackPeriod
        window = list(self._close_window)
        min_val = min(window) if window else 0.0
        max_val = max(window) if window else 0.0

        first = self._first_close
        last = current_close
        r = max_val - min_val
        range_strength = r / first if first != 0 else 0.0
        trend = (last - first) / first if first != 0 else 0.0
        momentum = (last - self._previous_close) / self._previous_close if self._previous_close != 0 else 0.0
        rsi_deviation = min(1.0, abs(rsi_val - 50.0) / 50.0)
        atr_normalized = min(1.0, atr_val / first) if first != 0 else 0.0
        range_position = (last - min_val) / r if r > 0 else 0.5

        base_threshold = 0.001
        trend_threshold = base_threshold
        breakout_threshold = base_threshold * 1.4
        flat_threshold = base_threshold * 0.3
        momentum_threshold = base_threshold

        # Confidence
        confidence = min(1.0, (abs(trend) + range_strength + min(1.0, abs(momentum) / momentum_threshold) + rsi_deviation + atr_normalized) / 5.0)

        # Neutral = 0, Bullish = 1, Bearish = 2
        if range_strength < flat_threshold:
            return (0, max(confidence, 0.4), 0)

        if trend >= trend_threshold:
            if range_position >= 0.75 and range_strength >= breakout_threshold:
                return (3, confidence, 1)
            if momentum < 0:
                return (6, confidence * 0.8, 1)
            return (1, confidence, 1)

        if trend <= -trend_threshold:
            if range_position <= 0.25 and range_strength >= breakout_threshold:
                return (4, confidence, 2)
            if momentum > 0:
                return (7, confidence * 0.8, 2)
            return (2, confidence, 2)

        if range_strength >= breakout_threshold:
            return (5, confidence * 0.9, 0)

        if range_position <= 0.4 and rsi_val >= 55.0:
            return (8, confidence * 0.85, 1)

        if range_position >= 0.6 and rsi_val <= 45.0:
            return (9, confidence * 0.85, 2)

        return (0, confidence * 0.7, 0)

    def _process_candle(self, candle, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)
        atr_val = float(atr_value)

        self._update_window(close)

        lookback = self.LookbackPeriod
        if len(self._close_window) < lookback:
            self._previous_close = close
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_close = close
            return

        pattern_class, confidence, bias = self._classify_pattern(close, rsi_val, atr_val)
        self._last_class = pattern_class
        self._last_confidence = confidence

        conf_threshold = float(self.ConfidenceThreshold)

        if confidence >= conf_threshold and bias != 0 and self.Position == 0:
            if bias == 1:
                self.BuyMarket()
                self._cooldown = 50
            elif bias == 2:
                self.SellMarket()
                self._cooldown = 50

        self._previous_close = close

    def CreateClone(self):
        return mnist_pattern_classifier_strategy()
