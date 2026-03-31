import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange, ExponentialMovingAverage, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class mutual_fund_momentum_strategy(Strategy):
    """Mutual fund momentum strategy that trades the primary instrument when its medium-term momentum leadership diverges from a benchmark fund."""

    def __init__(self):
        super(mutual_fund_momentum_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark fund", "General")

        self._momentum_period = self.Param("MomentumPeriod", 32) \
            .SetRange(5, 200) \
            .SetDisplay("Momentum Period", "Momentum lookback period", "Indicators")

        self._trend_period = self.Param("TrendPeriod", 20) \
            .SetRange(5, 200) \
            .SetDisplay("Trend Period", "Trend period used to align entries with the primary fund direction", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 24) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the momentum leadership spread", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.0) \
            .SetRange(0.2, 5.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.2) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetRange(0, 120) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 3.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._benchmark = None
        self._primary_momentum = None
        self._benchmark_momentum = None
        self._primary_trend = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_momentum = 0.0
        self._latest_benchmark_momentum = 0.0
        self._latest_primary_trend = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0
        self._last_primary_candle = None

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
        super(mutual_fund_momentum_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_momentum = None
        self._benchmark_momentum = None
        self._primary_trend = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_momentum = 0.0
        self._latest_benchmark_momentum = 0.0
        self._latest_primary_trend = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0
        self._last_primary_candle = None

    def OnStarted2(self, time):
        super(mutual_fund_momentum_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        mom_period = int(self._momentum_period.Value)
        trend_period = int(self._trend_period.Value)
        norm_period = int(self._normalization_period.Value)

        self._primary_momentum = RateOfChange()
        self._primary_momentum.Length = mom_period
        self._benchmark_momentum = RateOfChange()
        self._benchmark_momentum.Length = mom_period
        self._primary_trend = ExponentialMovingAverage()
        self._primary_trend.Length = trend_period
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

        mom_iv = CandleIndicatorValue(self._primary_momentum, candle)
        mom_iv.IsFinal = True
        mom_result = self._primary_momentum.Process(mom_iv)

        trend_iv = CandleIndicatorValue(self._primary_trend, candle)
        trend_iv.IsFinal = True
        trend_result = self._primary_trend.Process(trend_iv)

        if mom_result.IsEmpty or trend_result.IsEmpty or not self._primary_momentum.IsFormed or not self._primary_trend.IsFormed:
            return

        self._latest_primary_momentum = float(mom_result)
        self._latest_primary_trend = float(trend_result)
        self._last_primary_candle = candle
        self._primary_updated = True
        self.TryProcessSpread(candle)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        mom_iv = CandleIndicatorValue(self._benchmark_momentum, candle)
        mom_iv.IsFinal = True
        mom_result = self._benchmark_momentum.Process(mom_iv)
        if mom_result.IsEmpty or not self._benchmark_momentum.IsFormed:
            return

        self._latest_benchmark_momentum = float(mom_result)
        self._benchmark_updated = True
        self.TryProcessSpread(candle)

    def TryProcessSpread(self, candle):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        spread = self._latest_primary_momentum - self._latest_benchmark_momentum

        mean_iv = DecimalIndicatorValue(self._spread_average, spread, candle.OpenTime)
        mean_iv.IsFinal = True
        mean = float(self._spread_average.Process(mean_iv))

        dev_iv = DecimalIndicatorValue(self._spread_deviation, spread, candle.OpenTime)
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

        # Use last primary candle close for trend alignment
        close_price = float(candle.ClosePrice)

        bullish_entry = (self._previous_z_score is not None and
                         self._previous_z_score < entry_thresh and
                         close_price >= self._latest_primary_trend and
                         z_score >= entry_thresh)

        bearish_entry = (self._previous_z_score is not None and
                         self._previous_z_score > -entry_thresh and
                         close_price <= self._latest_primary_trend and
                         z_score <= -entry_thresh)

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
        return mutual_fund_momentum_strategy()
