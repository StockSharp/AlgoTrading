import clr
import collections

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from indicator_extensions import *

class return_asymmetry_commodity_strategy(Strategy):
    """Return asymmetry strategy that trades the primary commodity when its positive-versus-negative return balance diverges from a benchmark commodity."""

    def __init__(self):
        super(return_asymmetry_commodity_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark commodity", "General")

        self._window_length = self.Param("WindowLength", 20) \
            .SetRange(5, 120) \
            .SetDisplay("Window Length", "Lookback period used to compute return asymmetry", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 16) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the asymmetry spread", "Indicators")

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
        self._primary_returns = collections.deque()
        self._benchmark_returns = collections.deque()
        self._previous_primary_close = None
        self._previous_benchmark_close = None
        self._previous_z_score = None
        self._latest_primary_asymmetry = 0.0
        self._latest_benchmark_asymmetry = 0.0
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        sec2_id = str(self._security2_id.Value)
        if sec2_id:
            s = Security()
            s.Id = sec2_id
            result.append((s, self.candle_type))
        return result

    def OnReseted(self):
        super(return_asymmetry_commodity_strategy, self).OnReseted()
        self._benchmark = None
        self._spread_average = None
        self._spread_deviation = None
        self._primary_returns = collections.deque()
        self._benchmark_returns = collections.deque()
        self._previous_primary_close = None
        self._previous_benchmark_close = None
        self._previous_z_score = None
        self._latest_primary_asymmetry = 0.0
        self._latest_benchmark_asymmetry = 0.0
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(return_asymmetry_commodity_strategy, self).OnStarted2(time)

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

        primary_subscription.Bind(self.ProcessPrimaryCandle).Start()
        benchmark_subscription.Bind(self.ProcessBenchmarkCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawCandles(area, benchmark_subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessPrimaryCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ret = self._update_returns(self._primary_returns, float(candle.ClosePrice), "primary")
        if ret is None:
            return

        self._latest_primary_asymmetry = self._calculate_asymmetry(self._primary_returns)
        self._primary_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ret = self._update_returns(self._benchmark_returns, float(candle.ClosePrice), "benchmark")
        if ret is None:
            return

        self._latest_benchmark_asymmetry = self._calculate_asymmetry(self._benchmark_returns)
        self._benchmark_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def _update_returns(self, queue, close_price, which):
        if which == "primary":
            prev = self._previous_primary_close
        else:
            prev = self._previous_benchmark_close

        if prev is None or prev <= 0:
            if which == "primary":
                self._previous_primary_close = close_price
            else:
                self._previous_benchmark_close = close_price
            return None

        ret = (close_price - prev) / prev

        if which == "primary":
            self._previous_primary_close = close_price
        else:
            self._previous_benchmark_close = close_price

        window_len = int(self._window_length.Value)
        if len(queue) == window_len:
            queue.popleft()

        queue.append(ret)
        return ret

    def _calculate_asymmetry(self, returns):
        positive = 0.0
        negative = 0.0
        for ret in returns:
            if ret > 0:
                positive += ret
            else:
                negative += abs(ret)
        return positive / max(negative, 0.0001)

    def TryProcessSpread(self, time):
        window_len = int(self._window_length.Value)
        if not self._primary_updated or not self._benchmark_updated or len(self._primary_returns) < window_len or len(self._benchmark_returns) < window_len:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        spread = self._latest_primary_asymmetry - self._latest_benchmark_asymmetry

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
        return return_asymmetry_commodity_strategy()
