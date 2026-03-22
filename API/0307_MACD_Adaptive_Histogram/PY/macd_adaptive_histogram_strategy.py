import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class macd_adaptive_histogram_strategy(Strategy):
    """
    MACD strategy that adapts entry thresholds to the rolling distribution of the histogram.
    """

    def __init__(self):
        super(macd_adaptive_histogram_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD")

        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD")

        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line period for MACD", "MACD")

        self._histogram_avg_period = self.Param("HistogramAvgPeriod", 20) \
            .SetDisplay("Histogram Avg Period", "Lookback period for histogram statistics", "Signals")

        self._std_dev_multiplier = self.Param("StdDevMultiplier", 1.2) \
            .SetDisplay("StdDev Multiplier", "Standard deviation multiplier for adaptive thresholds", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 16) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")

        self._macd = None
        self._hist_avg = None
        self._hist_std_dev = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_adaptive_histogram_strategy, self).OnReseted()
        self._macd = None
        self._hist_avg = None
        self._hist_std_dev = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_adaptive_histogram_strategy, self).OnStarted(time)

        hist_period = int(self._histogram_avg_period.Value)

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = int(self._fast_period.Value)
        self._macd.Macd.LongMa.Length = int(self._slow_period.Value)
        self._macd.SignalMa.Length = int(self._signal_period.Value)

        self._hist_avg = SimpleMovingAverage()
        self._hist_avg.Length = hist_period
        self._hist_std_dev = StandardDeviation()
        self._hist_std_dev.Length = hist_period
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), False)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_val = macd_value.Macd
        signal_val = macd_value.Signal

        if macd_val is None or signal_val is None:
            return

        macd_f = float(macd_val)
        signal_f = float(signal_val)
        histogram = macd_f - signal_f

        avg_input = DecimalIndicatorValue(self._hist_avg, Decimal(histogram), candle.OpenTime)
        avg_input.IsFinal = True
        histogram_average = float(self._hist_avg.Process(avg_input))

        std_input = DecimalIndicatorValue(self._hist_std_dev, Decimal(histogram), candle.OpenTime)
        std_input.IsFinal = True
        histogram_std_dev = float(self._hist_std_dev.Process(std_input))

        if not self._macd.IsFormed or not self._hist_avg.IsFormed or not self._hist_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if histogram_std_dev <= 0:
            return

        sdm = float(self._std_dev_multiplier.Value)
        upper_threshold = histogram_average + sdm * histogram_std_dev
        lower_threshold = histogram_average - sdm * histogram_std_dev
        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if histogram >= upper_threshold and histogram > 0:
                self.BuyMarket()
                self._cooldown = cd
            elif histogram <= lower_threshold and histogram < 0:
                self.SellMarket()
                self._cooldown = cd
            return

        if self.Position > 0 and histogram <= histogram_average:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = cd
        elif self.Position < 0 and histogram >= histogram_average:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = cd

    def CreateClone(self):
        return macd_adaptive_histogram_strategy()
