import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_pattern_trader_all_v001_strategy(Strategy):
    """
    MACD Pattern Trader All v0.01: simplified MACD signal crossover.
    The full C# version has 6 independent patterns, martingale, and partial exits.
    This Python version implements the core MACD signal crossover pattern.
    """

    def __init__(self):
        super(macd_pattern_trader_all_v001_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetDisplay("Fast", "Fast EMA", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26).SetDisplay("Slow", "Slow EMA", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetDisplay("Signal", "Signal period", "MACD")
        self._max_threshold = self.Param("MaxThreshold", 50.0).SetDisplay("Max Threshold", "Upper MACD threshold", "Signals")
        self._min_threshold = self.Param("MinThreshold", -50.0).SetDisplay("Min Threshold", "Lower MACD threshold", "Signals")
        self._cooldown_bars = self.Param("CooldownBars", 10).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_above = False
        self._initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_pattern_trader_all_v001_strategy, self).OnReseted()
        self._prev_above = False
        self._initialized = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(macd_pattern_trader_all_v001_strategy, self).OnStarted2(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_period.Value
        macd.Macd.LongMa.Length = self._slow_period.Value
        macd.SignalMa.Length = self._signal_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        typed_val = macd_value
        macd_line = typed_val.Macd
        signal_line = typed_val.Signal
        if macd_line is None or signal_line is None:
            return
        macd_f = float(macd_line)
        signal_f = float(signal_line)
        is_above = macd_f > signal_f
        if not self._initialized:
            self._prev_above = is_above
            self._initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_above = is_above
            return
        cross_up = is_above and not self._prev_above
        cross_down = not is_above and self._prev_above
        mt = float(self._max_threshold.Value)
        mnt = float(self._min_threshold.Value)
        if cross_up and macd_f >= mnt and macd_f <= mt and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif cross_down and macd_f >= mnt and macd_f <= mt and self.Position >= 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        self._prev_above = is_above

    def CreateClone(self):
        return macd_pattern_trader_all_v001_strategy()
