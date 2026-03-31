import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange, StandardDeviation, SimpleMovingAverage, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class momentum_rev_vol_strategy(Strategy):
    """Momentum, reversal, and volatility composite strategy that trades the primary instrument when its composite score diverges from a benchmark instrument."""

    def __init__(self):
        super(momentum_rev_vol_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General")

        self._momentum_period = self.Param("MomentumPeriod", 36) \
            .SetRange(8, 200) \
            .SetDisplay("Momentum Period", "Lookback period for medium-term momentum", "Indicators")

        self._reversal_period = self.Param("ReversalPeriod", 8) \
            .SetRange(2, 60) \
            .SetDisplay("Reversal Period", "Lookback period for short-term reversal", "Indicators")

        self._volatility_period = self.Param("VolatilityPeriod", 18) \
            .SetRange(5, 120) \
            .SetDisplay("Volatility Period", "Lookback period used to estimate realized volatility", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 24) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the relative composite spread", "Indicators")

        self._momentum_weight = self.Param("MomentumWeight", 1.0) \
            .SetRange(0.1, 10.0) \
            .SetDisplay("Momentum Weight", "Weight applied to the momentum component", "Signals")

        self._reversal_weight = self.Param("ReversalWeight", 0.8) \
            .SetRange(0.1, 10.0) \
            .SetDisplay("Reversal Weight", "Weight applied to the reversal component", "Signals")

        self._volatility_weight = self.Param("VolatilityWeight", 1.5) \
            .SetRange(0.1, 10.0) \
            .SetDisplay("Volatility Weight", "Weight applied to the volatility component", "Signals")

        self._entry_threshold = self.Param("EntryThreshold", 1.1) \
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
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._benchmark = None
        self._primary_momentum = None
        self._benchmark_momentum = None
        self._primary_reversal = None
        self._benchmark_reversal = None
        self._primary_volatility = None
        self._benchmark_volatility = None
        self._spread_average = None
        self._spread_deviation = None
        self._previous_primary_close = None
        self._previous_benchmark_close = None
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
        super(momentum_rev_vol_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_momentum = None
        self._benchmark_momentum = None
        self._primary_reversal = None
        self._benchmark_reversal = None
        self._primary_volatility = None
        self._benchmark_volatility = None
        self._spread_average = None
        self._spread_deviation = None
        self._previous_primary_close = None
        self._previous_benchmark_close = None
        self._latest_primary_signal = 0.0
        self._latest_benchmark_signal = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(momentum_rev_vol_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        mom_period = int(self._momentum_period.Value)
        rev_period = int(self._reversal_period.Value)
        vol_period = int(self._volatility_period.Value)
        norm_period = int(self._normalization_period.Value)

        self._primary_momentum = RateOfChange()
        self._primary_momentum.Length = mom_period
        self._benchmark_momentum = RateOfChange()
        self._benchmark_momentum.Length = mom_period
        self._primary_reversal = RateOfChange()
        self._primary_reversal.Length = rev_period
        self._benchmark_reversal = RateOfChange()
        self._benchmark_reversal.Length = rev_period
        self._primary_volatility = StandardDeviation()
        self._primary_volatility.Length = vol_period
        self._benchmark_volatility = StandardDeviation()
        self._benchmark_volatility.Length = vol_period
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

        rev_iv = CandleIndicatorValue(self._primary_reversal, candle)
        rev_iv.IsFinal = True
        rev_result = self._primary_reversal.Process(rev_iv)

        close_price = float(candle.ClosePrice)
        ret = self.CalculateReturn(close_price, True)

        if mom_result.IsEmpty or rev_result.IsEmpty or ret is None or not self._primary_momentum.IsFormed or not self._primary_reversal.IsFormed:
            return

        abs_ret = abs(ret)
        vol_iv = DecimalIndicatorValue(self._primary_volatility, abs_ret, candle.OpenTime)
        vol_iv.IsFinal = True
        vol_result = self._primary_volatility.Process(vol_iv)
        if vol_result.IsEmpty or not self._primary_volatility.IsFormed:
            return

        mom_w = float(self._momentum_weight.Value)
        rev_w = float(self._reversal_weight.Value)
        vol_w = float(self._volatility_weight.Value)

        self._latest_primary_signal = (mom_w * float(mom_result)) - (rev_w * float(rev_result)) - (vol_w * float(vol_result))
        self._primary_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        mom_iv = CandleIndicatorValue(self._benchmark_momentum, candle)
        mom_iv.IsFinal = True
        mom_result = self._benchmark_momentum.Process(mom_iv)

        rev_iv = CandleIndicatorValue(self._benchmark_reversal, candle)
        rev_iv.IsFinal = True
        rev_result = self._benchmark_reversal.Process(rev_iv)

        close_price = float(candle.ClosePrice)
        ret = self.CalculateReturn(close_price, False)

        if mom_result.IsEmpty or rev_result.IsEmpty or ret is None or not self._benchmark_momentum.IsFormed or not self._benchmark_reversal.IsFormed:
            return

        abs_ret = abs(ret)
        vol_iv = DecimalIndicatorValue(self._benchmark_volatility, abs_ret, candle.OpenTime)
        vol_iv.IsFinal = True
        vol_result = self._benchmark_volatility.Process(vol_iv)
        if vol_result.IsEmpty or not self._benchmark_volatility.IsFormed:
            return

        mom_w = float(self._momentum_weight.Value)
        rev_w = float(self._reversal_weight.Value)
        vol_w = float(self._volatility_weight.Value)

        self._latest_benchmark_signal = (mom_w * float(mom_result)) - (rev_w * float(rev_result)) - (vol_w * float(vol_result))
        self._benchmark_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def CalculateReturn(self, close_price, is_primary):
        if is_primary:
            prev = self._previous_primary_close
        else:
            prev = self._previous_benchmark_close

        if prev is None or prev <= 0.0:
            if is_primary:
                self._previous_primary_close = close_price
            else:
                self._previous_benchmark_close = close_price
            return None

        ret = (close_price - prev) / prev
        if is_primary:
            self._previous_primary_close = close_price
        else:
            self._previous_benchmark_close = close_price
        return ret

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
        return momentum_rev_vol_strategy()
