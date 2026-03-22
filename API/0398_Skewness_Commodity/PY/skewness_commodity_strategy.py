import clr
import math
from collections import deque

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class skewness_commodity_strategy(Strategy):
    """Skewness-based commodity strategy that trades the primary commodity when its return skewness diverges from a benchmark commodity."""

    def __init__(self):
        super(skewness_commodity_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark commodity", "General")

        self._window_length = self.Param("WindowLength", 20) \
            .SetRange(5, 120) \
            .SetDisplay("Window Length", "Lookback period used to estimate return skewness", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 16) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the skewness spread", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.1) \
            .SetRange(0.2, 5.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.25) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetRange(0, 120) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 3.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._benchmark = None
        self._spread_average = None
        self._spread_deviation = None
        self._primary_returns = deque()
        self._benchmark_returns = deque()
        self._previous_primary_close = None
        self._previous_benchmark_close = None
        self._previous_z_score = None
        self._latest_primary_skewness = 0.0
        self._latest_benchmark_skewness = 0.0
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(skewness_commodity_strategy, self).OnReseted()
        self._benchmark = None
        self._spread_average = None
        self._spread_deviation = None
        self._primary_returns = deque()
        self._benchmark_returns = deque()
        self._previous_primary_close = None
        self._previous_benchmark_close = None
        self._previous_z_score = None
        self._latest_primary_skewness = 0.0
        self._latest_benchmark_skewness = 0.0
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(skewness_commodity_strategy, self).OnStarted(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        norm_period = int(self._normalization_period.Value)

        self._spread_average = SimpleMovingAverage()
        self._spread_average.Length = norm_period
        self._spread_deviation = StandardDeviation()
        self._spread_deviation.Length = norm_period

        primary_subscription = self.SubscribeCandles(self.candle_type, True, self.Security)
        benchmark_subscription = self.SubscribeCandles(self.candle_type, True, self._benchmark)

        primary_subscription.Bind(self._process_primary_candle).Start()
        benchmark_subscription.Bind(self._process_benchmark_candle).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def _process_primary_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ret = self._update_returns(self._primary_returns, float(candle.ClosePrice), True)
        if ret is None:
            return

        self._latest_primary_skewness = self._calculate_skewness(self._primary_returns)
        self._primary_updated = True
        self._try_process_spread(candle.OpenTime)

    def _process_benchmark_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ret = self._update_returns(self._benchmark_returns, float(candle.ClosePrice), False)
        if ret is None:
            return

        self._latest_benchmark_skewness = self._calculate_skewness(self._benchmark_returns)
        self._benchmark_updated = True
        self._try_process_spread(candle.OpenTime)

    def _update_returns(self, queue, close_price, is_primary):
        if is_primary:
            previous = self._previous_primary_close
        else:
            previous = self._previous_benchmark_close

        if previous is None or previous <= 0:
            if is_primary:
                self._previous_primary_close = close_price
            else:
                self._previous_benchmark_close = close_price
            return None

        ret = (close_price - previous) / previous
        if is_primary:
            self._previous_primary_close = close_price
        else:
            self._previous_benchmark_close = close_price

        window_len = int(self._window_length.Value)
        if len(queue) == window_len:
            queue.popleft()

        queue.append(ret)
        return ret

    def _calculate_skewness(self, returns):
        values = list(returns)
        if len(values) < 3:
            return 0.0

        mean = sum(values) / len(values)
        variance = sum((v - mean) ** 2 for v in values) / len(values)
        if variance <= 0:
            return 0.0

        deviation = math.sqrt(variance)
        third_moment = sum((v - mean) ** 3 for v in values) / len(values)
        return third_moment / (deviation ** 3)

    def _try_process_spread(self, time):
        window_len = int(self._window_length.Value)
        if not self._primary_updated or not self._benchmark_updated or len(self._primary_returns) < window_len or len(self._benchmark_returns) < window_len:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        spread = self._latest_benchmark_skewness - self._latest_primary_skewness

        mean_iv = DecimalIndicatorValue(self._spread_average, spread, time)
        mean_iv.IsFinal = True
        mean = float(self._spread_average.Process(mean_iv))

        dev_iv = DecimalIndicatorValue(self._spread_deviation, spread, time)
        dev_iv.IsFinal = True
        deviation = float(self._spread_deviation.Process(dev_iv))

        if not self._spread_average.IsFormed or not self._spread_deviation.IsFormed or deviation <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        z_score = (spread - mean) / deviation
        entry_thresh = float(self._entry_threshold.Value)
        exit_thresh = float(self._exit_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        bullish_entry = self._previous_z_score is not None and self._previous_z_score < entry_thresh and z_score >= entry_thresh
        bearish_entry = self._previous_z_score is not None and self._previous_z_score > -entry_thresh and z_score <= -entry_thresh

        if self._cooldown_remaining == 0 and self.Position == 0:
            if bullish_entry:
                self.BuyMarket()
                self._cooldown_remaining = cooldown
            elif bearish_entry:
                self.SellMarket()
                self._cooldown_remaining = cooldown
        elif self.Position > 0 and z_score <= exit_thresh:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and z_score >= -exit_thresh:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._previous_z_score = z_score

    def CreateClone(self):
        return skewness_commodity_strategy()
