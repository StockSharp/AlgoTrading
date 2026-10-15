import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange, ExponentialMovingAverage, SimpleMovingAverage, StandardDeviation, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from indicator_extensions import *

class fscore_reversal_strategy(Strategy):
    """F-Score reversal strategy that trades the primary instrument when a synthetic fundamental score aligns with relative short-term reversal versus a benchmark."""

    def __init__(self):
        super(fscore_reversal_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General")

        self._lookback = self.Param("Lookback", 12) \
            .SetRange(2, 80) \
            .SetDisplay("Lookback", "Lookback period in bars", "General")

        self._score_length = self.Param("ScoreLength", 8) \
            .SetRange(2, 50) \
            .SetDisplay("Score Length", "Smoothing length for the synthetic F-Score proxy", "General")

        self._entry_threshold = self.Param("EntryThreshold", 1.2) \
            .SetRange(0.2, 5.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.3) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetRange(0, 120) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._benchmark = None
        self._primary_reversal = None
        self._benchmark_reversal = None
        self._primary_score = None
        self._benchmark_score = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_signal = 0.0
        self._latest_benchmark_signal = 0.0
        self._previous_z_score = None
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
        super(fscore_reversal_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_reversal = None
        self._benchmark_reversal = None
        self._primary_score = None
        self._benchmark_score = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_signal = 0.0
        self._latest_benchmark_signal = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(fscore_reversal_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        lookback = int(self._lookback.Value)
        score_len = int(self._score_length.Value)

        self._primary_reversal = RateOfChange()
        self._primary_reversal.Length = lookback
        self._benchmark_reversal = RateOfChange()
        self._benchmark_reversal.Length = lookback
        self._primary_score = ExponentialMovingAverage()
        self._primary_score.Length = score_len
        self._benchmark_score = ExponentialMovingAverage()
        self._benchmark_score.Length = score_len
        self._spread_average = SimpleMovingAverage()
        self._spread_average.Length = 24
        self._spread_deviation = StandardDeviation()
        self._spread_deviation.Length = 24

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

        civ = CandleIndicatorValue(self._primary_reversal, candle)
        civ.IsFinal = True
        reversal_value = self._primary_reversal.Process(civ)

        fscore_proxy = self.CalculateFScoreProxy(candle)
        score_value = process_float(self._primary_score, fscore_proxy, candle.OpenTime, True)

        if not reversal_value.IsEmpty and not score_value.IsEmpty and self._primary_reversal.IsFormed and self._primary_score.IsFormed:
            self._latest_primary_signal = float(score_value) - float(reversal_value)
            self._primary_updated = True
            self.TryProcessSpread(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ = CandleIndicatorValue(self._benchmark_reversal, candle)
        civ.IsFinal = True
        reversal_value = self._benchmark_reversal.Process(civ)

        fscore_proxy = self.CalculateFScoreProxy(candle)
        score_value = process_float(self._benchmark_score, fscore_proxy, candle.OpenTime, True)

        if not reversal_value.IsEmpty and not score_value.IsEmpty and self._benchmark_reversal.IsFormed and self._benchmark_score.IsFormed:
            self._latest_benchmark_signal = float(score_value) - float(reversal_value)
            self._benchmark_updated = True
            self.TryProcessSpread(candle.OpenTime)

    def CalculateFScoreProxy(self, candle):
        price_base = max(float(candle.OpenPrice), 1.0)
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        range_val = max(float(candle.HighPrice) - float(candle.LowPrice), price_step)
        close_location = ((float(candle.ClosePrice) - float(candle.LowPrice)) - (float(candle.HighPrice) - float(candle.ClosePrice))) / range_val
        efficiency = (float(candle.ClosePrice) - float(candle.OpenPrice)) / price_base

        return (close_location * 2.0) + (efficiency * 100.0)

    def TryProcessSpread(self, time):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        spread = self._latest_primary_signal - self._latest_benchmark_signal

        mean_result = process_float(self._spread_average, spread, time, True)
        mean = float(mean_result)

        dev_result = process_float(self._spread_deviation, spread, time, True)
        deviation = float(dev_result)

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
        return fscore_reversal_strategy()
