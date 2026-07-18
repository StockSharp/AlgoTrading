import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from indicator_extensions import *

class small_cap_premium_strategy(Strategy):
    """Small-cap premium strategy that trades the primary stock when its synthetic size profile diverges from a benchmark stock."""

    def __init__(self):
        super(small_cap_premium_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark stock", "General")

        self._size_length = self.Param("SizeLength", 10) \
            .SetRange(2, 80) \
            .SetDisplay("Size Length", "Smoothing length for the synthetic size proxy", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 24) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the size spread", "Indicators")

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
        self._primary_size = None
        self._benchmark_size = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_size = 0.0
        self._latest_benchmark_size = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(small_cap_premium_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_size = None
        self._benchmark_size = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_size = 0.0
        self._latest_benchmark_size = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(small_cap_premium_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        size_len = int(self._size_length.Value)
        norm_period = int(self._normalization_period.Value)

        self._primary_size = ExponentialMovingAverage()
        self._primary_size.Length = size_len
        self._benchmark_size = ExponentialMovingAverage()
        self._benchmark_size.Length = size_len
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

        self._latest_primary_size = self._update_size(self._primary_size, candle)
        self._primary_updated = True
        self._try_process_spread(candle.OpenTime)

    def _process_benchmark_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_benchmark_size = self._update_size(self._benchmark_size, candle)
        self._benchmark_updated = True
        self._try_process_spread(candle.OpenTime)

    def _update_size(self, average, candle):
        price_base = max(float(candle.ClosePrice), 1.0)
        range_ratio = (float(candle.HighPrice) - float(candle.LowPrice)) / price_base
        size_proxy = 1.0 / price_base + (range_ratio * 0.5)

        return float(process_float(average, size_proxy, candle.OpenTime, True))

    def _try_process_spread(self, time):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        spread = self._latest_primary_size - self._latest_benchmark_size

        mean = float(process_float(self._spread_average, spread, time, True))

        deviation = float(process_float(self._spread_deviation, spread, time, True))

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
        return small_cap_premium_strategy()
