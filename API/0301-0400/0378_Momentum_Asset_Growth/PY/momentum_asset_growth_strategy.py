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

class momentum_asset_growth_strategy(Strategy):
    """Momentum plus asset-growth strategy that trades the primary instrument when its risk-adjusted momentum outperforms a benchmark while synthetic asset growth remains contained."""

    def __init__(self):
        super(momentum_asset_growth_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General")

        self._momentum_length = self.Param("MomentumLength", 28) \
            .SetRange(5, 150) \
            .SetDisplay("Momentum Length", "Momentum lookback period", "Indicators")

        self._asset_length = self.Param("AssetLength", 8) \
            .SetRange(2, 40) \
            .SetDisplay("Asset Length", "Smoothing length for the synthetic asset base", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 24) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the composite signal", "Indicators")

        self._growth_penalty = self.Param("GrowthPenalty", 1.8) \
            .SetRange(0.1, 10.0) \
            .SetDisplay("Growth Penalty", "Penalty applied to relative asset growth inside the composite score", "Signals")

        self._entry_threshold = self.Param("EntryThreshold", 1.15) \
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
        self._primary_asset_base = None
        self._benchmark_asset_base = None
        self._signal_average = None
        self._signal_deviation = None
        self._previous_primary_asset_base = 0.0
        self._previous_benchmark_asset_base = 0.0
        self._latest_primary_momentum = 0.0
        self._latest_benchmark_momentum = 0.0
        self._latest_primary_growth = 0.0
        self._latest_benchmark_growth = 0.0
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
        super(momentum_asset_growth_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_momentum = None
        self._benchmark_momentum = None
        self._primary_asset_base = None
        self._benchmark_asset_base = None
        self._signal_average = None
        self._signal_deviation = None
        self._previous_primary_asset_base = 0.0
        self._previous_benchmark_asset_base = 0.0
        self._latest_primary_momentum = 0.0
        self._latest_benchmark_momentum = 0.0
        self._latest_primary_growth = 0.0
        self._latest_benchmark_growth = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(momentum_asset_growth_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        mom_len = int(self._momentum_length.Value)
        asset_len = int(self._asset_length.Value)
        norm_period = int(self._normalization_period.Value)

        self._primary_momentum = RateOfChange()
        self._primary_momentum.Length = mom_len
        self._benchmark_momentum = RateOfChange()
        self._benchmark_momentum.Length = mom_len
        self._primary_asset_base = ExponentialMovingAverage()
        self._primary_asset_base.Length = asset_len
        self._benchmark_asset_base = ExponentialMovingAverage()
        self._benchmark_asset_base.Length = asset_len
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

        mom_iv = CandleIndicatorValue(self._primary_momentum, candle)
        mom_iv.IsFinal = True
        mom_result = self._primary_momentum.Process(mom_iv)
        if mom_result.IsEmpty or not self._primary_momentum.IsFormed:
            return

        self._latest_primary_momentum = float(mom_result)
        self._latest_primary_growth = self.UpdateGrowth(self._primary_asset_base, candle, True)
        self._primary_updated = True
        self.TryProcessSignal(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        mom_iv = CandleIndicatorValue(self._benchmark_momentum, candle)
        mom_iv.IsFinal = True
        mom_result = self._benchmark_momentum.Process(mom_iv)
        if mom_result.IsEmpty or not self._benchmark_momentum.IsFormed:
            return

        self._latest_benchmark_momentum = float(mom_result)
        self._latest_benchmark_growth = self.UpdateGrowth(self._benchmark_asset_base, candle, False)
        self._benchmark_updated = True
        self.TryProcessSignal(candle.OpenTime)

    def UpdateGrowth(self, average, candle, is_primary):
        asset_base = self.CalculateSyntheticAssetBase(candle)
        smoothed_base = float(process_float(average, asset_base, candle.OpenTime, True))

        if is_primary:
            prev = self._previous_primary_asset_base
        else:
            prev = self._previous_benchmark_asset_base

        if prev == 0.0:
            if is_primary:
                self._previous_primary_asset_base = smoothed_base
            else:
                self._previous_benchmark_asset_base = smoothed_base
            return 0.0

        growth = (smoothed_base - prev) / max(abs(prev), 1.0)
        if is_primary:
            self._previous_primary_asset_base = smoothed_base
        else:
            self._previous_benchmark_asset_base = smoothed_base
        return growth

    def CalculateSyntheticAssetBase(self, candle):
        price_base = max(float(candle.OpenPrice), 1.0)
        range_ratio = (float(candle.HighPrice) - float(candle.LowPrice)) / price_base
        turnover_proxy = float(candle.ClosePrice) * (1.0 + (range_ratio * 6.0))
        balance_proxy = (float(candle.OpenPrice) + float(candle.ClosePrice) + float(candle.HighPrice) + float(candle.LowPrice)) / 4.0

        return turnover_proxy + balance_proxy

    def TryProcessSignal(self, time):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        if not self._primary_asset_base.IsFormed or not self._benchmark_asset_base.IsFormed:
            return

        penalty = float(self._growth_penalty.Value)
        signal = (self._latest_primary_momentum - self._latest_benchmark_momentum) - (penalty * (self._latest_primary_growth - self._latest_benchmark_growth))

        mean = float(process_float(self._signal_average, signal, time, True))

        deviation = float(process_float(self._signal_deviation, signal, time, True))

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
        return momentum_asset_growth_strategy()
