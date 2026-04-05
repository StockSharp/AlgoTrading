import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal, ValueTuple
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Correlation, StandardDeviation, PairIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from indicator_extensions import *

class betting_against_beta_stocks_strategy(Strategy):
    """Betting-against-beta strategy using dual securities and rolling beta."""

    def __init__(self):
        super(betting_against_beta_stocks_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General")

        self._beta_length = self.Param("BetaLength", 16) \
            .SetRange(10, 150) \
            .SetDisplay("Beta Length", "Rolling beta lookback length", "Indicators")

        self._low_beta_threshold = self.Param("LowBetaThreshold", 0.95) \
            .SetRange(0.2, 1.2) \
            .SetDisplay("Low Beta Threshold", "Maximum beta required to open a long position", "Signals")

        self._high_beta_threshold = self.Param("HighBetaThreshold", 1.05) \
            .SetRange(0.8, 2.5) \
            .SetDisplay("High Beta Threshold", "Minimum beta required to open a short position", "Signals")

        self._exit_beta_threshold = self.Param("ExitBetaThreshold", 1.0) \
            .SetRange(0.5, 1.5) \
            .SetDisplay("Exit Beta Threshold", "Neutral beta threshold used to close positions", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetRange(0, 100) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series for both instruments", "General")

        self._benchmark = None
        self._correlation = None
        self._primary_deviation = None
        self._benchmark_deviation = None
        self._latest_primary_price = 0.0
        self._latest_benchmark_price = 0.0
        self._previous_primary_price = 0.0
        self._previous_benchmark_price = 0.0
        self._previous_beta = None
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
        super(betting_against_beta_stocks_strategy, self).OnReseted()
        self._benchmark = None
        self._correlation = None
        self._primary_deviation = None
        self._benchmark_deviation = None
        self._latest_primary_price = 0.0
        self._latest_benchmark_price = 0.0
        self._previous_primary_price = 0.0
        self._previous_benchmark_price = 0.0
        self._previous_beta = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(betting_against_beta_stocks_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        beta_len = int(self._beta_length.Value)
        self._correlation = Correlation()
        self._correlation.Length = beta_len
        self._primary_deviation = StandardDeviation()
        self._primary_deviation.Length = beta_len
        self._benchmark_deviation = StandardDeviation()
        self._benchmark_deviation.Length = beta_len
        self._cooldown_remaining = 0

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

        self._latest_primary_price = float(candle.ClosePrice)
        self._primary_updated = True
        self.TryProcessBeta(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_benchmark_price = float(candle.ClosePrice)
        self._benchmark_updated = True
        self.TryProcessBeta(candle.OpenTime)

    def TryProcessBeta(self, time):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        if self._previous_primary_price <= 0.0 or self._previous_benchmark_price <= 0.0:
            self._previous_primary_price = self._latest_primary_price
            self._previous_benchmark_price = self._latest_benchmark_price
            return

        primary_return = (self._latest_primary_price - self._previous_primary_price) / max(self._previous_primary_price, 1.0)
        benchmark_return = (self._latest_benchmark_price - self._previous_benchmark_price) / max(self._previous_benchmark_price, 1.0)

        self._previous_primary_price = self._latest_primary_price
        self._previous_benchmark_price = self._latest_benchmark_price

        pair_val = ValueTuple[Decimal, Decimal](Decimal(primary_return), Decimal(benchmark_return))
        pair_input = PairIndicatorValue[Decimal](self._correlation, pair_val, time)
        pair_input.IsFinal = True
        corr_result = self._correlation.Process(pair_input)
        correlation = float(corr_result)

        prim_dev_result = process_float(self._primary_deviation, Decimal(primary_return), time, True)
        primary_dev = float(prim_dev_result)

        bench_dev_result = process_float(self._benchmark_deviation, Decimal(benchmark_return), time, True)
        benchmark_dev = float(bench_dev_result)

        if not self._correlation.IsFormed or not self._primary_deviation.IsFormed or not self._benchmark_deviation.IsFormed or benchmark_dev <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        beta = correlation * (primary_dev / benchmark_dev)
        low_thresh = float(self._low_beta_threshold.Value)
        high_thresh = float(self._high_beta_threshold.Value)
        exit_thresh = float(self._exit_beta_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        bullish_entry = beta <= low_thresh
        bearish_entry = beta >= high_thresh

        if self._cooldown_remaining == 0 and self.Position == 0:
            if bullish_entry:
                self.BuyMarket()
                self._cooldown_remaining = cooldown
            elif bearish_entry:
                self.SellMarket()
                self._cooldown_remaining = cooldown
        elif self.Position > 0 and beta >= exit_thresh:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and beta <= exit_thresh:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._previous_beta = beta

    def CreateClone(self):
        return betting_against_beta_stocks_strategy()
