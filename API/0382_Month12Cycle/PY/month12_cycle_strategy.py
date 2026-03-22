import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class month12_cycle_strategy(Strategy):
    """12-month cycle strategy that trades the primary instrument when its 12-month minus 1-month seasonal return outperforms a benchmark."""

    def __init__(self):
        super(month12_cycle_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General")

        self._annual_period = self.Param("AnnualPeriod", 90) \
            .SetRange(30, 400) \
            .SetDisplay("Annual Period", "Long lookback period used to approximate the prior 12-month cycle", "Indicators")

        self._recent_period = self.Param("RecentPeriod", 10) \
            .SetRange(2, 60) \
            .SetDisplay("Recent Period", "Short lookback period used to remove the most recent month", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 12) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the seasonal spread", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 0.65) \
            .SetRange(0.1, 5.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.15) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetRange(0, 120) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 4.0) \
            .SetRange(0.5, 15.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type used for calculations", "General")

        self._benchmark = None
        self._primary_annual_momentum = None
        self._benchmark_annual_momentum = None
        self._primary_recent_momentum = None
        self._benchmark_recent_momentum = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_signal = 0.0
        self._latest_benchmark_signal = 0.0
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
        super(month12_cycle_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_annual_momentum = None
        self._benchmark_annual_momentum = None
        self._primary_recent_momentum = None
        self._benchmark_recent_momentum = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_signal = 0.0
        self._latest_benchmark_signal = 0.0
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(month12_cycle_strategy, self).OnStarted(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        annual = int(self._annual_period.Value)
        recent = int(self._recent_period.Value)
        norm_period = int(self._normalization_period.Value)

        self._primary_annual_momentum = RateOfChange()
        self._primary_annual_momentum.Length = annual
        self._benchmark_annual_momentum = RateOfChange()
        self._benchmark_annual_momentum.Length = annual
        self._primary_recent_momentum = RateOfChange()
        self._primary_recent_momentum.Length = recent
        self._benchmark_recent_momentum = RateOfChange()
        self._benchmark_recent_momentum.Length = recent
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

        annual_iv = CandleIndicatorValue(self._primary_annual_momentum, candle)
        annual_iv.IsFinal = True
        annual_result = self._primary_annual_momentum.Process(annual_iv)

        recent_iv = CandleIndicatorValue(self._primary_recent_momentum, candle)
        recent_iv.IsFinal = True
        recent_result = self._primary_recent_momentum.Process(recent_iv)

        if annual_result.IsEmpty or recent_result.IsEmpty or not self._primary_annual_momentum.IsFormed or not self._primary_recent_momentum.IsFormed:
            return

        self._latest_primary_signal = float(annual_result) - float(recent_result)
        self._primary_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        annual_iv = CandleIndicatorValue(self._benchmark_annual_momentum, candle)
        annual_iv.IsFinal = True
        annual_result = self._benchmark_annual_momentum.Process(annual_iv)

        recent_iv = CandleIndicatorValue(self._benchmark_recent_momentum, candle)
        recent_iv.IsFinal = True
        recent_result = self._benchmark_recent_momentum.Process(recent_iv)

        if annual_result.IsEmpty or recent_result.IsEmpty or not self._benchmark_annual_momentum.IsFormed or not self._benchmark_recent_momentum.IsFormed:
            return

        self._latest_benchmark_signal = float(annual_result) - float(recent_result)
        self._benchmark_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def TryProcessSpread(self, time):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        spread = self._latest_primary_signal - self._latest_benchmark_signal

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

        bullish_entry = z_score >= entry_thresh
        bearish_entry = z_score <= -entry_thresh

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

    def CreateClone(self):
        return month12_cycle_strategy()
