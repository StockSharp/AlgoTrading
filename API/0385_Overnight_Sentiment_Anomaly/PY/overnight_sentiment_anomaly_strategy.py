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


class overnight_sentiment_anomaly_strategy(Strategy):
    """Overnight sentiment anomaly strategy that trades the primary instrument when its opening gap diverges from benchmark sentiment."""

    def __init__(self):
        super(overnight_sentiment_anomaly_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security used as a sentiment proxy", "General")

        self._sentiment_period = self.Param("SentimentPeriod", 4) \
            .SetRange(2, 80) \
            .SetDisplay("Sentiment Period", "Lookback period used to estimate benchmark sentiment", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 8) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the anomaly signal", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 0.4) \
            .SetRange(0.2, 5.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.1) \
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
        self._benchmark_sentiment = None
        self._gap_average = None
        self._signal_average = None
        self._signal_deviation = None
        self._latest_benchmark_sentiment = 0.0
        self._latest_gap = 0.0
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
        super(overnight_sentiment_anomaly_strategy, self).OnReseted()
        self._benchmark = None
        self._benchmark_sentiment = None
        self._gap_average = None
        self._signal_average = None
        self._signal_deviation = None
        self._latest_benchmark_sentiment = 0.0
        self._latest_gap = 0.0
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(overnight_sentiment_anomaly_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        sentiment_period = int(self._sentiment_period.Value)
        norm_period = int(self._normalization_period.Value)

        self._benchmark_sentiment = RateOfChange()
        self._benchmark_sentiment.Length = sentiment_period
        self._gap_average = ExponentialMovingAverage()
        self._gap_average.Length = max(2, sentiment_period)
        self._signal_average = SimpleMovingAverage()
        self._signal_average.Length = norm_period
        self._signal_deviation = StandardDeviation()
        self._signal_deviation.Length = norm_period

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

        gap = (float(candle.OpenPrice) - float(candle.LowPrice)) / max(float(candle.LowPrice), 1.0)
        gap_iv = DecimalIndicatorValue(self._gap_average, gap, candle.OpenTime)
        gap_iv.IsFinal = True
        smoothed_gap = float(self._gap_average.Process(gap_iv))

        if not self._gap_average.IsFormed:
            return

        self._latest_gap = smoothed_gap
        self._primary_updated = True
        self.TryProcessSignal(candle)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        sent_iv = CandleIndicatorValue(self._benchmark_sentiment, candle)
        sent_iv.IsFinal = True
        sent_result = self._benchmark_sentiment.Process(sent_iv)
        if sent_result.IsEmpty or not self._benchmark_sentiment.IsFormed:
            return

        self._latest_benchmark_sentiment = float(sent_result)
        self._benchmark_updated = True
        self.TryProcessSignal(candle)

    def TryProcessSignal(self, candle):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        signal = self._latest_benchmark_sentiment - (self._latest_gap * 10.0)

        mean_iv = DecimalIndicatorValue(self._signal_average, signal, candle.OpenTime)
        mean_iv.IsFinal = True
        mean = float(self._signal_average.Process(mean_iv))

        dev_iv = DecimalIndicatorValue(self._signal_deviation, signal, candle.OpenTime)
        dev_iv.IsFinal = True
        deviation = float(self._signal_deviation.Process(dev_iv))

        if not self._signal_average.IsFormed or not self._signal_deviation.IsFormed or deviation <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        z_score = (signal - mean) / deviation
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
        return overnight_sentiment_anomaly_strategy()
