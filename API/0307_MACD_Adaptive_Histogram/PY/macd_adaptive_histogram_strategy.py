import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class macd_adaptive_histogram_strategy(Strategy):
    """
    MACD strategy that adapts entry thresholds to the rolling distribution of the histogram.
    """

    def __init__(self):
        super(macd_adaptive_histogram_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26).SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetDisplay("Signal Period", "Signal line period for MACD", "MACD")
        self._histogram_avg_period = self.Param("HistogramAvgPeriod", 20).SetDisplay("Histogram Avg Period", "Lookback period for histogram statistics", "Signals")
        self._std_dev_multiplier = self.Param("StdDevMultiplier", 1.2).SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Signals")
        self._stop_loss_pct = self.Param("StopLossPercent", 2.0).SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 16).SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe", "General")

        self._hist_avg = None
        self._hist_std_dev = None
        self._cooldown = 0
        self._hist_values = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_adaptive_histogram_strategy, self).OnReseted()
        self._hist_avg = None
        self._hist_std_dev = None
        self._cooldown = 0
        self._hist_values = []

    def OnStarted(self, time):
        super(macd_adaptive_histogram_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_period.Value
        macd.Macd.LongMa.Length = self._slow_period.Value
        macd.SignalMa.Length = self._signal_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()
        self.StartProtection(None, Unit(self._stop_loss_pct.Value, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        typed_val = macd_value
        if typed_val.Macd is None or typed_val.Signal is None:
            return
        macd_line = float(typed_val.Macd)
        signal_line = float(typed_val.Signal)
        histogram = macd_line - signal_line
        lb = self._histogram_avg_period.Value
        self._hist_values.append(histogram)
        if len(self._hist_values) > lb:
            self._hist_values.pop(0)
        if len(self._hist_values) < lb:
            return
        avg = sum(self._hist_values) / lb
        var = sum((h - avg) ** 2 for h in self._hist_values) / lb
        std = math.sqrt(var) if var > 0 else 0.0
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        if std <= 0:
            return
        mult = self._std_dev_multiplier.Value
        upper = avg + mult * std
        lower = avg - mult * std
        if self.Position == 0:
            if histogram >= upper and histogram > 0:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
            elif histogram <= lower and histogram < 0:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
            return
        if self.Position > 0 and histogram <= avg:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and histogram >= avg:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return macd_adaptive_histogram_strategy()
