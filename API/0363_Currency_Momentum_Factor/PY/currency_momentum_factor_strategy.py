import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage, StandardDeviation, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from indicator_extensions import *

class currency_momentum_factor_strategy(Strategy):
    """Currency momentum factor strategy that trades the primary instrument when its relative momentum versus a benchmark currency is strong or weak."""

    def __init__(self):
        super(currency_momentum_factor_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark currency security", "General")

        self._lookback = self.Param("Lookback", 40) \
            .SetRange(5, 200) \
            .SetDisplay("Lookback", "Momentum lookback period", "Indicators")

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetRange(5, 120) \
            .SetDisplay("Lookback Period", "Lookback period used to normalize the relative momentum spread", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.2) \
            .SetRange(0.2, 5.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.25) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetRange(0, 120) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

        self._benchmark = None
        self._primary_momentum = None
        self._benchmark_momentum = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_momentum = 0.0
        self._latest_benchmark_momentum = 0.0
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
        super(currency_momentum_factor_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_momentum = None
        self._benchmark_momentum = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_momentum = 0.0
        self._latest_benchmark_momentum = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(currency_momentum_factor_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark currency identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        lookback = int(self._lookback.Value)
        lookback_period = int(self._lookback_period.Value)

        self._primary_momentum = RateOfChange()
        self._primary_momentum.Length = lookback
        self._benchmark_momentum = RateOfChange()
        self._benchmark_momentum.Length = lookback
        self._spread_average = SimpleMovingAverage()
        self._spread_average.Length = lookback_period
        self._spread_deviation = StandardDeviation()
        self._spread_deviation.Length = lookback_period

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

        civ = CandleIndicatorValue(self._primary_momentum, candle)
        civ.IsFinal = True
        momentum_value = self._primary_momentum.Process(civ)

        if not momentum_value.IsEmpty and self._primary_momentum.IsFormed:
            self._latest_primary_momentum = float(momentum_value)
            self._primary_updated = True
            self.TryProcessSpread(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ = CandleIndicatorValue(self._benchmark_momentum, candle)
        civ.IsFinal = True
        momentum_value = self._benchmark_momentum.Process(civ)

        if not momentum_value.IsEmpty and self._benchmark_momentum.IsFormed:
            self._latest_benchmark_momentum = float(momentum_value)
            self._benchmark_updated = True
            self.TryProcessSpread(candle.OpenTime)

    def TryProcessSpread(self, time):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        spread = self._latest_primary_momentum - self._latest_benchmark_momentum

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
        return currency_momentum_factor_strategy()
