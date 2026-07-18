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

class book_to_market_value_strategy(Strategy):
    """Relative book-to-market factor strategy using dual securities."""

    def __init__(self):
        super(book_to_market_value_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General")

        self._book_length = self.Param("BookLength", 10) \
            .SetRange(2, 50) \
            .SetDisplay("Book Length", "Smoothing length for the synthetic book value", "Indicators")

        self._lookback_period = self.Param("LookbackPeriod", 28) \
            .SetRange(10, 150) \
            .SetDisplay("Lookback Period", "Lookback period used to normalize valuation spread", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.35) \
            .SetRange(0.5, 4.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.35) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetRange(0, 120) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series for both instruments", "General")

        self._benchmark = None
        self._primary_book = None
        self._benchmark_book = None
        self._ratio_spread_average = None
        self._ratio_spread_deviation = None
        self._latest_primary_ratio = 0.0
        self._latest_benchmark_ratio = 0.0
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
        super(book_to_market_value_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_book = None
        self._benchmark_book = None
        self._ratio_spread_average = None
        self._ratio_spread_deviation = None
        self._latest_primary_ratio = 0.0
        self._latest_benchmark_ratio = 0.0
        self._previous_z_score = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(book_to_market_value_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        book_len = int(self._book_length.Value)
        lookback = int(self._lookback_period.Value)

        self._primary_book = ExponentialMovingAverage()
        self._primary_book.Length = book_len
        self._benchmark_book = ExponentialMovingAverage()
        self._benchmark_book.Length = book_len
        self._ratio_spread_average = SimpleMovingAverage()
        self._ratio_spread_average.Length = lookback
        self._ratio_spread_deviation = StandardDeviation()
        self._ratio_spread_deviation.Length = lookback
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

        self._latest_primary_ratio = self.UpdateRatio(self._primary_book, candle)
        self._primary_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_benchmark_ratio = self.UpdateRatio(self._benchmark_book, candle)
        self._benchmark_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def UpdateRatio(self, average, candle):
        synthetic_book = self.CalculateSyntheticBookValue(candle)
        result = process_float(average, synthetic_book, candle.OpenTime, True)
        smoothed_book = float(result)

        return smoothed_book / max(float(candle.ClosePrice), 1.0)

    def CalculateSyntheticBookValue(self, candle):
        price_base = max(float(candle.OpenPrice), 1.0)
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        range_val = max(float(candle.HighPrice) - float(candle.LowPrice), price_step)
        balance_component = (float(candle.OpenPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        stability_component = price_base * (1.0 - min(0.2, range_val / price_base))

        return balance_component + stability_component

    def TryProcessSpread(self, time):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        if not self._primary_book.IsFormed or not self._benchmark_book.IsFormed:
            return

        ratio_spread = self._latest_primary_ratio - self._latest_benchmark_ratio

        mean_result = process_float(self._ratio_spread_average, ratio_spread, time, True)
        mean = float(mean_result)

        dev_result = process_float(self._ratio_spread_deviation, ratio_spread, time, True)
        deviation = float(dev_result)

        if not self._ratio_spread_average.IsFormed or not self._ratio_spread_deviation.IsFormed or deviation <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        z_score = (ratio_spread - mean) / deviation
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
        return book_to_market_value_strategy()
