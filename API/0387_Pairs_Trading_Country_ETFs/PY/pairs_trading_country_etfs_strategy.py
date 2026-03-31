import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Sides, OrderTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order


class pairs_trading_country_etfs_strategy(Strategy):
    """Mean-reversion pairs strategy for country ETFs that trades the primary instrument against a benchmark ETF using the ratio z-score."""

    def __init__(self):
        super(pairs_trading_country_etfs_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark ETF", "General")

        self._window_length = self.Param("WindowLength", 24) \
            .SetRange(5, 120) \
            .SetDisplay("Window Length", "Lookback period used to estimate the ratio mean and deviation", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.4) \
            .SetRange(0.2, 5.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a paired position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.35) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close the paired position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetRange(0, 120) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 3.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._benchmark = None
        self._ratio_average = None
        self._ratio_deviation = None
        self._latest_primary_close = 0.0
        self._latest_benchmark_close = 0.0
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
        super(pairs_trading_country_etfs_strategy, self).OnReseted()
        self._benchmark = None
        self._ratio_average = None
        self._ratio_deviation = None
        self._latest_primary_close = 0.0
        self._latest_benchmark_close = 0.0
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(pairs_trading_country_etfs_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        window_len = int(self._window_length.Value)

        self._ratio_average = SimpleMovingAverage()
        self._ratio_average.Length = window_len
        self._ratio_deviation = StandardDeviation()
        self._ratio_deviation.Length = window_len

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

        self._latest_primary_close = float(candle.ClosePrice)
        self._primary_updated = True
        self.TryProcessPair(candle.OpenTime)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_benchmark_close = float(candle.ClosePrice)
        self._benchmark_updated = True
        self.TryProcessPair(candle.OpenTime)

    def TryProcessPair(self, time):
        if not self._primary_updated or not self._benchmark_updated or self._latest_primary_close <= 0 or self._latest_benchmark_close <= 0:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        ratio = self._latest_primary_close / self._latest_benchmark_close

        mean_iv = DecimalIndicatorValue(self._ratio_average, ratio, time)
        mean_iv.IsFinal = True
        mean = float(self._ratio_average.Process(mean_iv))

        dev_iv = DecimalIndicatorValue(self._ratio_deviation, ratio, time)
        dev_iv.IsFinal = True
        deviation = float(self._ratio_deviation.Process(dev_iv))

        if not self._ratio_average.IsFormed or not self._ratio_deviation.IsFormed or deviation <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        z_score = (ratio - mean) / deviation
        entry_thresh = float(self._entry_threshold.Value)
        exit_thresh = float(self._exit_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        if abs(z_score) <= exit_thresh:
            self.FlattenPair()
            return

        if self._cooldown_remaining > 0:
            return

        if z_score >= entry_thresh:
            self.SetPairPosition(-1.0)
            self._cooldown_remaining = cooldown
        elif z_score <= -entry_thresh:
            self.SetPairPosition(1.0)
            self._cooldown_remaining = cooldown

    def FlattenPair(self):
        primary_pos_val = self.GetPositionValue(self.Security, self.Portfolio)
        primary_position = float(primary_pos_val) if primary_pos_val is not None else 0.0
        benchmark_pos_val = self.GetPositionValue(self._benchmark, self.Portfolio)
        benchmark_position = float(benchmark_pos_val) if benchmark_pos_val is not None else 0.0

        if primary_position > 0:
            self.SellMarket(primary_position)
        elif primary_position < 0:
            self.BuyMarket(Math.Abs(primary_position))

        if benchmark_position > 0:
            order = Order()
            order.Security = self._benchmark
            order.Portfolio = self.Portfolio
            order.Side = Sides.Sell
            order.Volume = benchmark_position
            order.Type = OrderTypes.Market
            order.Comment = "PairsExit"
            self.RegisterOrder(order)
        elif benchmark_position < 0:
            order = Order()
            order.Security = self._benchmark
            order.Portfolio = self.Portfolio
            order.Side = Sides.Buy
            order.Volume = Math.Abs(benchmark_position)
            order.Type = OrderTypes.Market
            order.Comment = "PairsExit"
            self.RegisterOrder(order)

    def SetPairPosition(self, primary_direction):
        primary_pos_val = self.GetPositionValue(self.Security, self.Portfolio)
        primary_position = float(primary_pos_val) if primary_pos_val is not None else 0.0
        benchmark_pos_val = self.GetPositionValue(self._benchmark, self.Portfolio)
        benchmark_position = float(benchmark_pos_val) if benchmark_pos_val is not None else 0.0

        target_primary = primary_direction
        target_benchmark = -primary_direction

        self.MoveSecurity(self.Security, primary_position, target_primary)
        self.MoveSecurity(self._benchmark, benchmark_position, target_benchmark)

    def MoveSecurity(self, security, current_position, target_position):
        diff = target_position - current_position
        if diff == 0:
            return

        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "PairsETF"
        self.RegisterOrder(order)

    def CreateClone(self):
        return pairs_trading_country_etfs_strategy()
