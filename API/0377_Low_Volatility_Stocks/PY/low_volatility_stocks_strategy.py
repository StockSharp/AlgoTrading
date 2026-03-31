import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StandardDeviation, SimpleMovingAverage, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class low_volatility_stocks_strategy(Strategy):
    """Low volatility anomaly strategy that trades the primary instrument when its realized volatility diverges from a benchmark instrument."""

    def __init__(self):
        super(low_volatility_stocks_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General")

        self._volatility_period = self.Param("VolatilityPeriod", 18) \
            .SetRange(5, 120) \
            .SetDisplay("Volatility Period", "Lookback period used to estimate realized volatility", "Indicators")

        self._normalization_period = self.Param("NormalizationPeriod", 24) \
            .SetRange(5, 120) \
            .SetDisplay("Normalization Period", "Lookback period used to normalize the volatility spread", "Indicators")

        self._trend_period = self.Param("TrendPeriod", 30) \
            .SetRange(5, 200) \
            .SetDisplay("Trend Period", "Trend period used to align entries with the primary instrument direction", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.1) \
            .SetRange(0.2, 5.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.25) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetRange(0, 120) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._benchmark = None
        self._primary_volatility = None
        self._benchmark_volatility = None
        self._spread_average = None
        self._spread_deviation = None
        self._primary_trend = None
        self._previous_primary_close = None
        self._previous_benchmark_close = None
        self._latest_primary_volatility = 0.0
        self._latest_benchmark_volatility = 0.0
        self._latest_primary_trend = 0.0
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
        super(low_volatility_stocks_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_volatility = None
        self._benchmark_volatility = None
        self._spread_average = None
        self._spread_deviation = None
        self._primary_trend = None
        self._previous_primary_close = None
        self._previous_benchmark_close = None
        self._latest_primary_volatility = 0.0
        self._latest_benchmark_volatility = 0.0
        self._latest_primary_trend = 0.0
        self._primary_updated = False
        self._benchmark_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(low_volatility_stocks_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        vol_period = int(self._volatility_period.Value)
        norm_period = int(self._normalization_period.Value)
        trend_period = int(self._trend_period.Value)

        self._primary_volatility = StandardDeviation()
        self._primary_volatility.Length = vol_period
        self._benchmark_volatility = StandardDeviation()
        self._benchmark_volatility.Length = vol_period
        self._spread_average = SimpleMovingAverage()
        self._spread_average.Length = norm_period
        self._spread_deviation = StandardDeviation()
        self._spread_deviation.Length = norm_period
        self._primary_trend = SimpleMovingAverage()
        self._primary_trend.Length = trend_period

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

        trend_iv = CandleIndicatorValue(self._primary_trend, candle)
        trend_iv.IsFinal = True
        trend_result = self._primary_trend.Process(trend_iv)
        if not trend_result.IsEmpty and self._primary_trend.IsFormed:
            self._latest_primary_trend = float(trend_result)

        close_price = float(candle.ClosePrice)
        if self._previous_primary_close is None or self._previous_primary_close <= 0.0:
            self._previous_primary_close = close_price
            return

        ret = (close_price - self._previous_primary_close) / self._previous_primary_close
        self._previous_primary_close = close_price

        abs_ret = abs(ret)
        vol_iv = DecimalIndicatorValue(self._primary_volatility, abs_ret, candle.OpenTime)
        vol_iv.IsFinal = True
        vol_result = self._primary_volatility.Process(vol_iv)
        if not vol_result.IsEmpty and self._primary_volatility.IsFormed:
            self._latest_primary_volatility = float(vol_result)
            self._primary_updated = True
            self.TryProcessSpread(candle)

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close_price = float(candle.ClosePrice)
        if self._previous_benchmark_close is None or self._previous_benchmark_close <= 0.0:
            self._previous_benchmark_close = close_price
            return

        ret = (close_price - self._previous_benchmark_close) / self._previous_benchmark_close
        self._previous_benchmark_close = close_price

        abs_ret = abs(ret)
        vol_iv = DecimalIndicatorValue(self._benchmark_volatility, abs_ret, candle.OpenTime)
        vol_iv.IsFinal = True
        vol_result = self._benchmark_volatility.Process(vol_iv)
        if not vol_result.IsEmpty and self._benchmark_volatility.IsFormed:
            self._latest_benchmark_volatility = float(vol_result)
            self._benchmark_updated = True
            self.TryProcessSpread(candle)

    def TryProcessSpread(self, candle):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        spread = self._latest_benchmark_volatility - self._latest_primary_volatility

        mean_iv = DecimalIndicatorValue(self._spread_average, spread, candle.OpenTime)
        mean_iv.IsFinal = True
        mean_result = self._spread_average.Process(mean_iv)
        mean = float(mean_result)

        dev_iv = DecimalIndicatorValue(self._spread_deviation, spread, candle.OpenTime)
        dev_iv.IsFinal = True
        dev_result = self._spread_deviation.Process(dev_iv)
        deviation = float(dev_result)

        if not self._spread_average.IsFormed or not self._spread_deviation.IsFormed or deviation <= 0:
            return

        if not self._primary_trend.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        z_score = (spread - mean) / deviation
        entry_thresh = float(self._entry_threshold.Value)
        exit_thresh = float(self._exit_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        close_price = float(candle.ClosePrice)
        bullish_trend = close_price >= self._latest_primary_trend
        bearish_trend = close_price <= self._latest_primary_trend
        bullish_entry = z_score >= entry_thresh and bullish_trend
        bearish_entry = z_score <= -entry_thresh and bearish_trend

        if self._cooldown_remaining == 0 and self.Position == 0:
            if bullish_entry:
                self.BuyMarket()
                self._cooldown_remaining = cooldown
            elif bearish_entry:
                self.SellMarket()
                self._cooldown_remaining = cooldown
        elif self.Position > 0 and (z_score <= exit_thresh or bearish_entry):
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and (z_score >= -exit_thresh or bullish_entry):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return low_volatility_stocks_strategy()
